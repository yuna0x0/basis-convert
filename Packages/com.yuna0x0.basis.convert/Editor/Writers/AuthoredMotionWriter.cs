using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Writers
{
    public sealed class ResolvedAuthoredMotion
    {
        public AuthoredMotionPlan Plan;

        /// <summary>Where the component goes, normally the avatar root.</summary>
        public GameObject Host;

        /// <summary>
        /// What the baked paths are relative to. This is the hierarchy the clip was authored
        /// against, which for an avatar's own animation is the avatar root.
        /// </summary>
        public Transform Root;

        /// <summary>The clip to bake.</summary>
        public AnimationClip Clip;

        /// <summary>Folder the baked clip asset is written into.</summary>
        public string OutputFolder = string.Empty;
    }

    public sealed class WrittenAuthoredMotion
    {
        public BasisAuthoredMotion Component;
        public BasisMotionClip Clip;
        public string AssetPath = string.Empty;
        public List<ConversionDiagnostic> Diagnostics = new List<ConversionDiagnostic>();
    }

    /// <summary>
    /// Bakes a clip into a `BasisMotionClip` and adds the `BasisAuthoredMotion` that replays it.
    /// <para>
    /// A Basis motion clip holds rotations sampled at a fixed rate, laid out one row per driven
    /// transform, so the runtime job can interpolate without touching an AnimationCurve. The
    /// sampling is done the way Basis's own baker does it, through <c>AnimationMode</c>, which
    /// poses the hierarchy and restores it afterwards.
    /// </para>
    /// <para>
    /// The baked clip is an asset on disk, so unlike everything else a conversion writes it does
    /// not disappear on undo. It is written to a fixed path per source clip, which means
    /// converting twice replaces it rather than leaving a second copy.
    /// </para>
    /// </summary>
    public static class AuthoredMotionWriter
    {
        /// <summary>
        /// Samples per second the clip is baked at. Basis's own baker defaults to 60, which is
        /// above the rate ambient motion is usually authored at.
        /// </summary>
        public const float FrameRate = 60f;

        public static WrittenAuthoredMotion Write(
            ResolvedAuthoredMotion motion, string undoName = "Convert authored motion")
        {
            if (motion?.Host == null || motion.Root == null || motion.Clip == null)
            {
                throw new System.ArgumentException(
                    "A host, a root and a clip are all required", nameof(motion));
            }

            WrittenAuthoredMotion written = new WrittenAuthoredMotion();

            BasisMotionClip baked = Bake(motion, written);
            if (baked == null)
            {
                return written;
            }

            written.Clip = baked;

            BasisAuthoredMotion component = Undo.AddComponent<BasisAuthoredMotion>(motion.Host);
            Undo.SetCurrentGroupName(undoName);

            BasisAuthoredMotion.Movement movement = new BasisAuthoredMotion.Movement
            {
                kind = BasisAuthoredMotion.Movement.Kind.Sequence,
                label = motion.Plan.Label,
                enabled = true,
                sequenceRoot = motion.Root,
                bakedClip = baked,
                loop = motion.Plan.Loop,
                sequenceSpeed = motion.Plan.Speed,
            };

            component.movements = new[] {movement};
            EditorUtility.SetDirty(component);

            written.Component = component;
            return written;
        }

        /// <summary>
        /// Samples the clip onto the hierarchy and records each turning transform's local
        /// rotation, frame by frame.
        /// <para>
        /// Rotations are recorded as they end up on the transform rather than as the curve
        /// states them, which is what makes a clip authored against another rest pose replay
        /// correctly: the runtime writes these straight to the bone.
        /// </para>
        /// </summary>
        private static BasisMotionClip Bake(
            ResolvedAuthoredMotion motion, WrittenAuthoredMotion written)
        {
            List<string> paths = new List<string>();
            List<Transform> transforms = new List<Transform>();

            foreach (string path in motion.Plan.Paths)
            {
                Transform found = string.IsNullOrEmpty(path)
                    ? motion.Root
                    : motion.Root.Find(path);

                if (found == null)
                {
                    written.Diagnostics.Add(DiagnosticSeverity.Warning, "motion.pathMissing",
                        $"'{motion.Plan.Label}' turns '{path}', which is not under the avatar "
                        + "root. That part of the motion was not baked.");
                    continue;
                }

                paths.Add(path);
                transforms.Add(found);
            }

            if (transforms.Count == 0)
            {
                written.Diagnostics.Add(DiagnosticSeverity.Warning, "motion.nothingToBake",
                    $"'{motion.Plan.Label}' turns nothing that exists on this avatar, so no "
                    + "motion was written.");
                return null;
            }

            int frameCount = Mathf.Max(1, Mathf.CeilToInt(motion.Clip.length * FrameRate));
            Vector4[] rotations = new Vector4[transforms.Count * frameCount];

            AnimationMode.StartAnimationMode();
            try
            {
                for (int frame = 0; frame < frameCount; frame++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(
                        motion.Root.gameObject, motion.Clip, frame / FrameRate);
                    AnimationMode.EndSampling();

                    for (int index = 0; index < transforms.Count; index++)
                    {
                        Quaternion rotation = transforms[index].localRotation;
                        rotations[index * frameCount + frame] =
                            new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);
                    }
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            BasisMotionClip baked = ScriptableObject.CreateInstance<BasisMotionClip>();
            baked.frameRate = FrameRate;
            baked.frameCount = frameCount;
            baked.transformCount = transforms.Count;
            baked.paths = paths.ToArray();
            baked.rotationSamples = rotations;

            string assetPath = Save(motion, baked, written);
            if (string.IsNullOrEmpty(assetPath))
            {
                Object.DestroyImmediate(baked);
                return null;
            }

            written.AssetPath = assetPath;
            return AssetDatabase.LoadAssetAtPath<BasisMotionClip>(assetPath);
        }

        /// <summary>
        /// Writes the baked clip beside the animation it came from, under a folder of our own.
        /// The path is derived from the source clip, so a second conversion writes over the same
        /// asset instead of leaving the first behind.
        /// </summary>
        private static string Save(
            ResolvedAuthoredMotion motion, BasisMotionClip baked, WrittenAuthoredMotion written)
        {
            string folder = motion.OutputFolder;
            if (string.IsNullOrEmpty(folder))
            {
                written.Diagnostics.Add(DiagnosticSeverity.Warning, "motion.noFolder",
                    $"'{motion.Plan.Label}' had nowhere to write its baked clip, so no motion "
                    + "was written.");
                return null;
            }

            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
                string name = Path.GetFileName(folder);
                if (string.IsNullOrEmpty(parent) || !AssetDatabase.IsValidFolder(parent))
                {
                    written.Diagnostics.Add(DiagnosticSeverity.Warning, "motion.noFolder",
                        $"'{motion.Plan.Label}' could not be written to {folder}, which is not "
                        + "inside the project.");
                    return null;
                }

                AssetDatabase.CreateFolder(parent, name);
            }

            string assetPath = $"{folder}/{SafeName(motion.Clip.name)}.asset";
            BasisMotionClip existing = AssetDatabase.LoadAssetAtPath<BasisMotionClip>(assetPath);

            if (existing != null)
            {
                // Overwriting the asset in place keeps whatever already references it, which is
                // what makes converting twice replace the motion rather than orphan it.
                EditorUtility.CopySerialized(baked, existing);
                Object.DestroyImmediate(baked);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssetIfDirty(existing);
                return assetPath;
            }

            AssetDatabase.CreateAsset(baked, assetPath);
            AssetDatabase.SaveAssetIfDirty(baked);
            return assetPath;
        }

        private static string SafeName(string name)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }

            return string.IsNullOrEmpty(name) ? "Motion" : name;
        }
    }
}
