using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Sources;

namespace yuna0x0.Basis.Convert.Mapping
{
    /// <summary>
    /// Plans a `BasisAuthoredMotion` movement from a layer that plays unprompted.
    /// <para>
    /// Basis bakes rotation only, so a clip that also moves or scales something keeps the turning
    /// and loses the rest. What a clip does besides turning transforms is reported here rather
    /// than at the bake, where the counts are no longer separable.
    /// </para>
    /// </summary>
    public static class AmbientMotionToAuthoredMapper
    {
        public static AuthoredMotionPlan Map(string label, bool loop, ClipEffects effects)
        {
            AuthoredMotionPlan plan = new AuthoredMotionPlan
            {
                Label = label ?? string.Empty,
                Loop = loop,
            };

            if (effects == null)
            {
                return plan;
            }

            plan.Paths.AddRange(effects.AnimatedRotationPaths);

            plan.Diagnostics.Add(DiagnosticSeverity.Mapped, "motion.baked",
                $"'{plan.Label}' plays without anything switching it on, so it was rebuilt as an "
                + $"authored motion turning {plan.Paths.Count} transforms. Basis replays it from "
                + "a clip baked at conversion time rather than from an animator.");

            if (!loop)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Approximated, "motion.notLooping",
                    $"'{plan.Label}' was not authored to loop, but an animator layer with nothing "
                    + "to leave it plays its state indefinitely. The motion loops.");
                plan.Loop = true;
            }

            int other = effects.AnimatedCurves - effects.AnimatedRotationCurves;
            if (other > 0)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "motion.rotationOnly",
                    $"'{plan.Label}' animates {other} curves that are not transform rotation. A "
                    + "baked Basis motion clip holds rotation only, so movement, scaling and "
                    + "anything else the clip animates over time were not carried over.");
            }

            return plan;
        }
    }
}
