using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Sources;

namespace yuna0x0.Basis.Convert.Pipeline
{
    /// <summary>A menu toggle whose animator layer and clips were found.</summary>
    public sealed class ResolvedToggle
    {
        public string MenuName = string.Empty;
        public string Parameter = string.Empty;
        public string LayerName = string.Empty;

        public ClipEffects WhenOff = new ClipEffects();
        public ClipEffects WhenOn = new ClipEffects();

        /// <summary>
        /// True when both sides do nothing but switch objects on and off or set blendshapes,
        /// which is exactly what a Vixxy control holds.
        /// </summary>
        public bool IsSimple =>
            WhenOff.OtherCurves == 0 && WhenOn.OtherCurves == 0
            && WhenOff.AnimatedCurves == 0 && WhenOn.AnimatedCurves == 0
            && !(WhenOff.IsEmpty && WhenOn.IsEmpty);
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

            Dictionary<string, VrcExpressionControl> toggles =
                new Dictionary<string, VrcExpressionControl>();

            foreach (VrcExpressionMenu menu in inventory.Menus)
            {
                foreach (VrcExpressionControl control in menu.Controls)
                {
                    if (control.Type == VrcExpressionControlType.Toggle
                        && !string.IsNullOrEmpty(control.Parameter)
                        && !toggles.ContainsKey(control.Parameter))
                    {
                        toggles[control.Parameter] = control;
                    }
                }
            }

            foreach (FxToggleLayer layer in
                     FxControllerReader.FindToggleLayers(controller, toggles.Keys))
            {
                resolved.Add(new ResolvedToggle
                {
                    MenuName = toggles[layer.Parameter].Name,
                    Parameter = layer.Parameter,
                    LayerName = layer.LayerName,
                    WhenOff = AnimationClipReader.Read(layer.WhenOff),
                    WhenOn = AnimationClipReader.Read(layer.WhenOn),
                });
            }

            return resolved;
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
