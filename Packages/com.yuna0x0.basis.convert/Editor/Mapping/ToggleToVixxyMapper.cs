using System.Collections.Generic;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Pipeline;

namespace yuna0x0.Basis.Convert.Mapping
{
    /// <summary>
    /// Turns a resolved menu toggle into a Vixxy control, where it can be turned into one at all.
    /// <para>
    /// Only object switching is rebuilt. A toggle that also sets blendshapes or drives material
    /// properties is left alone rather than half converted: emitting the object switching and
    /// silently dropping the rest would produce a control that looks right and does part of the
    /// job, which is worse than not making it.
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
                    $"'{toggle.MenuName}' drives material properties or animates over time, "
                    + "which a Vixxy control cannot hold. Rebuild it by hand.");
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
                    BothSidesAnimated = hasOn && hasOff,
                });
            }

            MapBlendShapes(toggle, plan);

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
            Dictionary<string, VixxySubjectPlan> byPath =
                new Dictionary<string, VixxySubjectPlan>();

            Dictionary<(string, string), float> off = ShapesIn(toggle.WhenOff);
            Dictionary<(string, string), float> on = ShapesIn(toggle.WhenOn);

            HashSet<(string, string)> keys = new HashSet<(string, string)>(on.Keys);
            keys.UnionWith(off.Keys);

            foreach ((string path, string shape) in keys)
            {
                bool hasOn = on.TryGetValue((path, shape), out float onValue);
                bool hasOff = off.TryGetValue((path, shape), out float offValue);

                if (!byPath.TryGetValue(path, out VixxySubjectPlan subject))
                {
                    subject = new VixxySubjectPlan { Path = path };
                    byPath[path] = subject;
                    plan.Subjects.Add(subject);
                }

                subject.BlendShapes.Add(new VixxyBlendShapePlan
                {
                    ShapeName = shape,
                    Choices = new[] { offValue, onValue },
                    BothSidesAnimated = hasOn && hasOff,
                });
            }
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
