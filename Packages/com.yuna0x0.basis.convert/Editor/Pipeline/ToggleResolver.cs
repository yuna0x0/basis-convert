using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Sources;

namespace yuna0x0.Basis.Convert.Pipeline
{
    /// <summary>One choice a control offers, and what its clip does.</summary>
    public sealed class ResolvedChoice
    {
        /// <summary>Label from the menu control that selects this choice.</summary>
        public string Name = string.Empty;

        /// <summary>Value of the parameter that selects it.</summary>
        public int Value;

        public ClipEffects Effects = new ClipEffects();
    }

    /// <summary>A menu control whose animator layer and clips were found.</summary>
    public sealed class ResolvedToggle
    {
        public string MenuName = string.Empty;
        public string Parameter = string.Empty;
        public string LayerName = string.Empty;

        /// <summary>
        /// What the control offers, in the order it offers them. A toggle has two, off first. A
        /// selector has one per value of its parameter.
        /// </summary>
        public List<ResolvedChoice> Choices = new List<ResolvedChoice>();

        public bool IsSelector => Choices.Count > 2;

        public ClipEffects WhenOff
        {
            get => EffectsFor(0);
            set => SetEffects(0, value);
        }

        public ClipEffects WhenOn
        {
            get => EffectsFor(1);
            set => SetEffects(1, value);
        }

        private ClipEffects EffectsFor(int index)
        {
            while (Choices.Count <= index)
            {
                Choices.Add(new ResolvedChoice {Value = Choices.Count});
            }

            return Choices[index].Effects;
        }

        private void SetEffects(int index, ClipEffects effects)
        {
            EffectsFor(index);
            Choices[index].Effects = effects ?? new ClipEffects();
        }

        /// <summary>
        /// True when both sides do nothing but switch objects on and off, set blendshapes or set
        /// material properties, which is exactly what a Vixxy control holds.
        /// </summary>
        public bool IsSimple
        {
            get
            {
                bool anything = false;

                foreach (ResolvedChoice choice in Choices)
                {
                    if (choice.Effects.OtherCurves > 0 || choice.Effects.AnimatedCurves > 0)
                    {
                        return false;
                    }

                    anything |= !choice.Effects.IsEmpty;
                }

                return anything;
            }
        }
    }

    /// <summary>
    /// Ties an avatar's menu toggles to the animator layers that implement them, and reduces
    /// each to what it actually does.
    /// <para>
    /// The FX layer is found by its slot in the descriptor. That slot is easy to get wrong: the
    /// layer type ordering has a deprecated entry at 1 that shifts everything after it.
    /// </para>
    /// </summary>
    public static class ToggleResolver
    {
        public static List<ResolvedToggle> Resolve(
            VrcExpressionInventory inventory, string fxControllerGuid)
        {
            List<ResolvedToggle> resolved = new List<ResolvedToggle>();

            AnimatorController controller = LoadController(fxControllerGuid);
            if (controller == null || inventory == null)
            {
                return resolved;
            }

            // Controls are grouped by parameter, because several of them commonly share one and
            // pick different values from it: a hairstyle, an outfit, a facial expression.
            Dictionary<string, List<VrcExpressionControl>> byParameter =
                new Dictionary<string, List<VrcExpressionControl>>();

            foreach (VrcExpressionMenu menu in inventory.Menus)
            {
                foreach (VrcExpressionControl control in menu.Controls)
                {
                    if (control.Type != VrcExpressionControlType.Toggle
                        || string.IsNullOrEmpty(control.Parameter))
                    {
                        continue;
                    }

                    if (!byParameter.TryGetValue(control.Parameter,
                            out List<VrcExpressionControl> controls))
                    {
                        controls = new List<VrcExpressionControl>();
                        byParameter[control.Parameter] = controls;
                    }

                    controls.Add(control);
                }
            }

            foreach (FxToggleLayer layer in
                     FxControllerReader.FindToggleLayers(controller, byParameter.Keys))
            {
                List<VrcExpressionControl> controls = byParameter[layer.Parameter];

                ResolvedToggle toggle = new ResolvedToggle
                {
                    MenuName = NameFor(controls, layer),
                    Parameter = layer.Parameter,
                    LayerName = layer.LayerName,
                };

                foreach (FxParameterState state in layer.States)
                {
                    toggle.Choices.Add(new ResolvedChoice
                    {
                        Name = ChoiceName(controls, state.Value, layer),
                        Value = state.Value,
                        Effects = AnimationClipReader.Read(state.Clip),
                    });
                }

                resolved.Add(toggle);
            }

            return resolved;
        }

        /// <summary>
        /// What to call the control. A toggle is named after the single menu entry that drives
        /// it; a selector has an entry per choice, so the parameter names the control and the
        /// entries name its choices.
        /// </summary>
        private static string NameFor(List<VrcExpressionControl> controls, FxToggleLayer layer)
        {
            if (controls.Count == 1)
            {
                return controls[0].Name;
            }

            return string.IsNullOrEmpty(layer.Parameter) ? layer.LayerName : layer.Parameter;
        }

        private static string ChoiceName(
            List<VrcExpressionControl> controls, int value, FxToggleLayer layer)
        {
            foreach (VrcExpressionControl control in controls)
            {
                if (Mathf.RoundToInt(control.Value) == value)
                {
                    return control.Name;
                }
            }

            // A value the menu never offers still needs a label, and for a plain toggle the
            // menu only ever names the on side.
            if (!layer.IsSelector)
            {
                return value == 0 ? "OFF" : "ON";
            }

            return $"{layer.Parameter} {value}";
        }

        public static AnimatorController LoadController(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        }
    }
}
