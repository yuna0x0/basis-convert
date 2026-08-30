using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace yuna0x0.Basis.Convert.Sources
{
    public sealed class BlendShapeEffect
    {
        /// <summary>Transform path of the renderer, relative to the avatar root.</summary>
        public string Path = string.Empty;

        public string ShapeName = string.Empty;
        public float Value;
    }

    /// <summary>What a clip does, reduced to the states Vixxy can hold.</summary>
    public sealed class ClipEffects
    {
        /// <summary>Transform paths the clip switches on, by driving m_IsActive to 1.</summary>
        public List<string> Activated = new List<string>();

        /// <summary>Transform paths the clip switches off.</summary>
        public List<string> Deactivated = new List<string>();

        public List<BlendShapeEffect> BlendShapes = new List<BlendShapeEffect>();

        /// <summary>
        /// Curves driving something other than object activity or a blendshape: material
        /// properties, transforms, anything else. Counted so a conversion can say the clip did
        /// more than came across.
        /// </summary>
        public int OtherCurves;

        /// <summary>Curves whose value changes over time rather than holding one value.</summary>
        public int AnimatedCurves;

        public bool IsEmpty =>
            Activated.Count == 0 && Deactivated.Count == 0 && BlendShapes.Count == 0;
    }

    /// <summary>
    /// Reduces an AnimationClip to the handful of things a Vixxy control can express: which
    /// objects are on, which are off, and what blendshapes are set to.
    /// <para>
    /// Vixxy holds a value per choice rather than a curve, so only clips that hold a constant
    /// carry across. A clip whose value moves over time is counted separately: that is animation,
    /// and it belongs in Authored Motion if anywhere.
    /// </para>
    /// </summary>
    public static class AnimationClipReader
    {
        private const string BlendShapePrefix = "blendShape.";

        public static ClipEffects Read(AnimationClip clip)
        {
            ClipEffects effects = new ClipEffects();
            if (clip == null)
            {
                return effects;
            }

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.length == 0)
                {
                    continue;
                }

                if (!TryGetConstant(curve, out float value))
                {
                    effects.AnimatedCurves++;
                    continue;
                }

                if (binding.type == typeof(GameObject) && binding.propertyName == "m_IsActive")
                {
                    if (value >= 0.5f)
                    {
                        effects.Activated.Add(binding.path);
                    }
                    else
                    {
                        effects.Deactivated.Add(binding.path);
                    }

                    continue;
                }

                if (binding.propertyName.StartsWith(BlendShapePrefix))
                {
                    effects.BlendShapes.Add(new BlendShapeEffect
                    {
                        Path = binding.path,
                        ShapeName = binding.propertyName.Substring(BlendShapePrefix.Length),
                        Value = value,
                    });
                    continue;
                }

                effects.OtherCurves++;
            }

            return effects;
        }

        /// <summary>
        /// True when every key holds the same value, which is what a toggle clip looks like.
        /// </summary>
        private static bool TryGetConstant(AnimationCurve curve, out float value)
        {
            value = curve.keys[0].value;

            for (int i = 1; i < curve.length; i++)
            {
                if (!Mathf.Approximately(curve.keys[i].value, value))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
