using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// Builds the humanoid <see cref="Avatar"/> assets the VRM fixtures use.
    /// <para>
    /// The fixtures themselves are hand-written YAML, because the components they carry belong to
    /// an SDK that cannot be installed here. An Avatar is a native asset that cannot be written
    /// by hand at all: Unity builds it from a live hierarchy and validates the proportions. So
    /// the bones are in the fixture text and the Avatar is generated from them.
    /// </para>
    /// <para>
    /// A VRM avatar is always humanoid, and the Basis Avatar component is only planned for one
    /// with a rig, so without this the fixtures could not cover the avatar descriptor, the
    /// visemes or the rig check.
    /// </para>
    /// <para>
    /// Run from <c>Tools/Watari/Development/Regenerate Test Fixtures</c>. The generated assets
    /// are committed; this is not run by tests.
    /// </para>
    /// </summary>
    public static class FixtureHumanoidGenerator
    {
        private const string Folder =
            "Packages/com.yuna0x0.basis.convert/Tests/Editor/Fixtures/SampleVrmAvatar";

        private static readonly (string Prefab, string Avatar)[] Fixtures =
        {
            ("SampleVrm10Avatar", "SampleVrm10Humanoid"),
            ("SampleVrm0Avatar", "SampleVrm0Humanoid"),
        };

        /// <summary>
        /// The shapes the fixture's face carries. The first two are what it started with, so the
        /// bindings that name them by index keep their meaning; the rest are the mouth and eye
        /// shapes a VRM's own expressions drive.
        /// </summary>
        private static readonly string[] Shapes =
        {
            "Smile",
            "BrowUp",
            "MouthA",
            "MouthI",
            "MouthU",
            "MouthE",
            "MouthO",
            "EyeClose",
        };

        public static void Generate()
        {
            AddShapesToTheFace();

            foreach ((string prefab, string avatarName) in Fixtures)
            {
                string prefabPath = $"{Folder}/{prefab}.prefab";
                GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);

                if (contents == null)
                {
                    Debug.LogError($"{prefabPath} did not load.");
                    continue;
                }

                try
                {
                    Avatar avatar = Build(contents);
                    if (avatar == null || !avatar.isValid || !avatar.isHuman)
                    {
                        Debug.LogError(
                            $"{prefab}: the rig did not build as a humanoid "
                            + $"(valid={avatar != null && avatar.isValid}, "
                            + $"human={avatar != null && avatar.isHuman}).");
                        continue;
                    }

                    string avatarPath = $"{Folder}/{avatarName}.asset";
                    AssetDatabase.DeleteAsset(avatarPath);
                    AssetDatabase.CreateAsset(avatar, avatarPath);

                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        avatar, out string guid, out long fileId);

                    Debug.Log($"[fixture] {avatarName}: guid {guid} fileID {fileId}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Puts the blendshapes the expression fixtures bind to on the face mesh.
        /// <para>
        /// The mesh is edited in place rather than recreated, so its guid survives and the
        /// renderers that reference it keep working. Blendshape frames hold vertex deltas, which
        /// is the other thing in these fixtures that cannot be written by hand.
        /// </para>
        /// </summary>
        private static void AddShapesToTheFace()
        {
            const string path = Folder + "/SampleFace.mesh";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (mesh == null)
            {
                Debug.LogError($"{path} did not load.");
                return;
            }

            mesh.ClearBlendShapes();

            for (int i = 0; i < Shapes.Length; i++)
            {
                Vector3[] deltas = new Vector3[mesh.vertexCount];
                for (int vertex = 0; vertex < deltas.Length; vertex++)
                {
                    // Enough of a difference to be a real shape, and different per shape so a
                    // mistaken index is visible rather than silently identical.
                    deltas[vertex] = new Vector3(0f, 0.01f * (i + 1), 0f);
                }

                mesh.AddBlendShapeFrame(Shapes[i], 100f, deltas, null, null);
            }

            EditorUtility.SetDirty(mesh);
            Debug.Log($"[fixture] SampleFace: {mesh.blendShapeCount} shapes");
        }

        /// <summary>
        /// A humanoid avatar from whatever of Unity's own bone names the hierarchy uses. Matching
        /// by name keeps the mapping in the fixture rather than in a table here.
        /// </summary>
        private static Avatar Build(GameObject root)
        {
            List<SkeletonBone> skeleton = new List<SkeletonBone>();
            List<HumanBone> human = new List<HumanBone>();
            HashSet<string> humanNames = new HashSet<string>(HumanTrait.BoneName);

            foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
            {
                skeleton.Add(new SkeletonBone
                {
                    name = bone.name,
                    position = bone.localPosition,
                    rotation = bone.localRotation,
                    scale = bone.localScale,
                });

                if (!humanNames.Contains(bone.name))
                {
                    continue;
                }

                HumanBone mapped = new HumanBone
                {
                    boneName = bone.name,
                    humanName = bone.name,
                };

                mapped.limit.useDefaultValues = true;
                human.Add(mapped);
            }

            HumanDescription description = new HumanDescription
            {
                human = human.ToArray(),
                skeleton = skeleton.ToArray(),
                upperArmTwist = 0.5f,
                lowerArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                lowerLegTwist = 0.5f,
                armStretch = 0.05f,
                legStretch = 0.05f,
                feetSpacing = 0f,
                hasTranslationDoF = false,
            };

            return AvatarBuilder.BuildHumanAvatar(root, description);
        }
    }
}
