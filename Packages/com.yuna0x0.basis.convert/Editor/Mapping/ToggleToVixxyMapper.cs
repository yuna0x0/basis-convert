using System.Collections.Generic;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Pipeline;

namespace yuna0x0.Basis.Convert.Mapping
{
    /// <summary>
    /// Turns a resolved menu toggle into a Vixxy control, where it can be turned into one at all.
    /// <para>
    /// Object switching, blendshapes and material properties are rebuilt. A toggle that does
    /// anything else is left alone rather than half converted: emitting part of it and silently
    /// dropping the rest would produce a control that looks right and does some of the job,
    /// which is worse than not making it.
    /// </para>
    /// </summary>
    public static class ToggleToVixxyMapper
    {
        public static VixxyControlPlan Map(ResolvedToggle toggle)
        {
            VixxyControlPlan plan = new VixxyControlPlan
            {
                MenuName = toggle.MenuName,
                Parameter = toggle.Parameter,
            };

            if (toggle.WhenOn.OtherCurves + toggle.WhenOff.OtherCurves > 0
                || toggle.WhenOn.AnimatedCurves + toggle.WhenOff.AnimatedCurves > 0)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "vixxy.notSimple",
                    $"'{toggle.MenuName}' animates over time or drives something a Vixxy control "
                    + "cannot hold, such as a transform. Rebuild it by hand.");
                return plan;
            }

            Dictionary<string, bool> off = StatesIn(toggle.WhenOff);
            Dictionary<string, bool> on = StatesIn(toggle.WhenOn);

            HashSet<string> paths = new HashSet<string>(on.Keys);
            paths.UnionWith(off.Keys);

            foreach (string path in paths)
            {
                bool hasOn = on.TryGetValue(path, out bool onState);
                bool hasOff = off.TryGetValue(path, out bool offState);

                // Only one side animating is the common shape: the other state leaves the object
                // as the avatar was authored, so its value is read from the hierarchy later
                // rather than assumed to be the opposite.
                plan.Activations.Add(new VixxyActivationPlan
                {
                    Path = path,
                    Choices = new[] { offState, onState },
                    SetWhenOff = hasOff,
                    SetWhenOn = hasOn,
                });
            }

            MapBlendShapes(toggle, plan);
            MapMaterialProperties(toggle, plan);

            if (plan.Activations.Count == 0 && plan.Subjects.Count == 0
                && plan.Diagnostics.Count == 0)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "vixxy.nothingToSwitch",
                    $"'{toggle.MenuName}' did not switch any objects, so there was nothing to "
                    + "rebuild.");
            }

            return plan;
        }

        /// <summary>
        /// Blendshapes become subjects holding a float property per shape. As with objects, a
        /// shape set on only one side leaves the other at the avatar's authored weight.
        /// </summary>
        private static void MapBlendShapes(ResolvedToggle toggle, VixxyControlPlan plan)
        {
            Dictionary<(string, string), float> off = ShapesIn(toggle.WhenOff);
            Dictionary<(string, string), float> on = ShapesIn(toggle.WhenOn);

            HashSet<(string, string)> keys = new HashSet<(string, string)>(on.Keys);
            keys.UnionWith(off.Keys);

            foreach ((string path, string shape) in keys)
            {
                bool hasOn = on.TryGetValue((path, shape), out float onValue);
                bool hasOff = off.TryGetValue((path, shape), out float offValue);

                VixxySubjectPlan subject = SubjectFor(plan, path);

                subject.BlendShapes.Add(new VixxyBlendShapePlan
                {
                    ShapeName = shape,
                    Choices = new[] { offValue, onValue },
                    SetWhenOff = hasOff,
                    SetWhenOn = hasOn,
                });
            }
        }

        /// <summary>
        /// Material properties become subjects too, holding one property each. Vixxy applies
        /// them through a MaterialPropertyBlock, so the property name is the shader's own.
        /// <para>
        /// A clip sets one channel of a colour at a time, `material._Color.r` and its siblings,
        /// so the channels are gathered back into one property here. Which channels each side of
        /// the toggle set is recorded rather than assumed: what a clip does not set keeps the
        /// value the material was authored with, which is filled in once the renderer is known.
        /// </para>
        /// </summary>
        private static void MapMaterialProperties(ResolvedToggle toggle, VixxyControlPlan plan)
        {
            Dictionary<(string, string), VixxyMaterialPropertyPlan> byProperty =
                new Dictionary<(string, string), VixxyMaterialPropertyPlan>();

            Collect(plan, toggle.WhenOff, byProperty, 0);
            Collect(plan, toggle.WhenOn, byProperty, 1);
        }

        private static void Collect(
            VixxyControlPlan plan, Sources.ClipEffects effects,
            Dictionary<(string, string), VixxyMaterialPropertyPlan> byProperty, int choice)
        {
            foreach (Sources.MaterialPropertyEffect effect in effects.MaterialProperties)
            {
                if (!byProperty.TryGetValue((effect.Path, effect.PropertyName),
                        out VixxyMaterialPropertyPlan property))
                {
                    VixxySubjectPlan subject = SubjectFor(plan, effect.Path);
                    subject.RendererTypeName = effect.RendererTypeName;

                    property = new VixxyMaterialPropertyPlan
                    {
                        PropertyName = effect.PropertyName,
                        Kind = KindOf(effect),
                    };

                    subject.MaterialProperties.Add(property);
                    byProperty[(effect.Path, effect.PropertyName)] = property;
                }

                int channel = effect.Channel < 0 ? 0 : effect.Channel;
                Vector4 value = property.Choices[choice];
                value[channel] = effect.Value;
                property.Choices[choice] = value;

                bool[] set = choice == 0 ? property.SetWhenOff : property.SetWhenOn;
                set[channel] = true;
            }
        }

        private static VixxyMaterialPropertyKind KindOf(Sources.MaterialPropertyEffect effect)
        {
            if (effect.Channel < 0)
            {
                return VixxyMaterialPropertyKind.Float;
            }

            return effect.ColourChannel
                ? VixxyMaterialPropertyKind.Colour
                : VixxyMaterialPropertyKind.Vector;
        }

        /// <summary>
        /// The subject for one path, shared between blendshapes and material properties so a
        /// toggle setting both on the same renderer produces one subject rather than two.
        /// </summary>
        private static VixxySubjectPlan SubjectFor(VixxyControlPlan plan, string path)
        {
            foreach (VixxySubjectPlan existing in plan.Subjects)
            {
                if (existing.Path == path)
                {
                    return existing;
                }
            }

            VixxySubjectPlan subject = new VixxySubjectPlan { Path = path };
            plan.Subjects.Add(subject);
            return subject;
        }

        private static Dictionary<(string, string), float> ShapesIn(
            yuna0x0.Basis.Convert.Sources.ClipEffects effects)
        {
            Dictionary<(string, string), float> shapes =
                new Dictionary<(string, string), float>();

            foreach (yuna0x0.Basis.Convert.Sources.BlendShapeEffect shape in effects.BlendShapes)
            {
                shapes[(shape.Path, shape.ShapeName)] = shape.Value;
            }

            return shapes;
        }

        private static Dictionary<string, bool> StatesIn(yuna0x0.Basis.Convert.Sources.ClipEffects effects)
        {
            Dictionary<string, bool> states = new Dictionary<string, bool>();

            foreach (string path in effects.Deactivated)
            {
                states[path] = false;
            }

            foreach (string path in effects.Activated)
            {
                states[path] = true;
            }

            return states;
        }
    }
}
