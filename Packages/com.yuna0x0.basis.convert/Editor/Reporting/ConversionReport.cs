using System;
using System.Collections.Generic;
using System.Text;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Pipeline;

namespace yuna0x0.Basis.Convert.Reporting
{
    public sealed class DiagnosticGroup
    {
        public string Code;
        public DiagnosticSeverity Severity;
        public int Count;

        /// <summary>One representative message. They only differ by the values quoted in them.</summary>
        public string Example;
    }

    /// <summary>
    /// Summarises a plan, or a plan and what applying it did.
    /// <para>
    /// Diagnostics are grouped by code because they repeat per component: a single avatar
    /// produced 61 identical "Is Animated was on" warnings, which is unreadable listed one by
    /// one but useful as a count.
    /// </para>
    /// </summary>
    public static class ConversionReport
    {
        public static List<DiagnosticGroup> Group(AvatarConversionPlan plan)
        {
            Dictionary<string, DiagnosticGroup> groups = new Dictionary<string, DiagnosticGroup>();

            foreach (ConversionDiagnostic diagnostic in plan.AllDiagnostics())
            {
                if (!groups.TryGetValue(diagnostic.Code, out DiagnosticGroup group))
                {
                    group = new DiagnosticGroup
                    {
                        Code = diagnostic.Code,
                        Severity = diagnostic.Severity,
                        Example = diagnostic.Message,
                    };
                    groups[diagnostic.Code] = group;
                }

                group.Count++;
            }

            List<DiagnosticGroup> ordered = new List<DiagnosticGroup>(groups.Values);
            ordered.Sort((left, right) =>
            {
                int bySeverity = right.Severity.CompareTo(left.Severity);
                return bySeverity != 0
                    ? bySeverity
                    : string.Compare(left.Code, right.Code, StringComparison.Ordinal);
            });

            return ordered;
        }

        public static string Write(AvatarConversionPlan plan, ConversionResult result = null)
        {
            StringBuilder text = new StringBuilder();

            text.AppendLine($"# {ProductInfo.Name}: VRChat avatar to Basis");
            text.AppendLine();
            text.AppendLine($"Source: {plan.SourceAssetPath}");
            text.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
            text.AppendLine();

            text.AppendLine("## Summary");
            text.AppendLine();
            text.AppendLine($"- Detected: {plan.Profile.Describe()}");
            text.AppendLine($"- PhysBones found: {plan.PhysBonesFound}");
            text.AppendLine($"- Dynamic Bones found: {plan.DynamicBonesFound}");
            text.AppendLine($"- Colliders found: {plan.CollidersFound}");
            text.AppendLine($"- Constraints found: {plan.ConstraintsFound}");
            text.AppendLine($"- Jiggle rigs planned: {plan.Rigs.Count}");
            text.AppendLine($"- Basis constraints planned: {plan.Constraints.Count}");
            text.AppendLine($"- Avatar descriptor: "
                + (plan.Descriptor != null ? "converted" : "none found"));

            if (plan.Unresolved > 0)
            {
                text.AppendLine($"- Could not be placed: {plan.Unresolved}");
            }

            if (result != null)
            {
                text.AppendLine($"- Written: {result.TotalWritten}");
                if (result.TotalSkipped > 0)
                {
                    text.AppendLine($"- Skipped while writing: {result.TotalSkipped}");
                }
            }

            text.AppendLine();
            text.AppendLine("Conversion is not lossless. Anything approximated or dropped is "
                + "listed below.");
            text.AppendLine();

            List<DiagnosticGroup> groups = Group(plan);
            foreach (DiagnosticSeverity severity in new[]
                     {
                         DiagnosticSeverity.Warning,
                         DiagnosticSeverity.Dropped,
                         DiagnosticSeverity.Approximated,
                         DiagnosticSeverity.Mapped,
                     })
            {
                List<DiagnosticGroup> section = groups.FindAll(group => group.Severity == severity);
                if (section.Count == 0)
                {
                    continue;
                }

                text.AppendLine($"## {HeadingFor(severity)}");
                text.AppendLine();
                foreach (DiagnosticGroup group in section)
                {
                    text.AppendLine($"- **{group.Code}** ({group.Count}): {group.Example}");
                }

                text.AppendLine();
            }

            if (plan.Constraints.Count > 0)
            {
                text.AppendLine("## Constraints");
                text.AppendLine();
                foreach (PlannedConstraint constraint in plan.Constraints)
                {
                    text.AppendLine($"- {constraint.Describe()}, "
                        + $"{constraint.Plan.Sources.Count} sources");
                }

                text.AppendLine();
            }

            if (plan.RigDiagnostics.Count > 0)
            {
                text.AppendLine("## Rig");
                text.AppendLine();
                text.AppendLine("What the humanoid rig looks like to Basis's full-body IK. These "
                    + "are not conversions; they are things to fix on the model itself.");
                text.AppendLine();
                foreach (ConversionDiagnostic diagnostic in plan.RigDiagnostics)
                {
                    text.AppendLine($"- **{diagnostic.Code}**: {diagnostic.Message}");
                }

                text.AppendLine();
            }

            text.AppendLine("## Rigs");
            text.AppendLine();
            foreach (PlannedJiggleRig rig in plan.Rigs)
            {
                text.AppendLine($"- {rig.Describe()} "
                    + $"[{rig.Plan.Preset}]"
                    + $"{(rig.Plan.ExcludeRoot ? ", motionless root" : string.Empty)}"
                    + $"{(rig.Colliders.Count > 0 ? $", {rig.Colliders.Count} colliders" : string.Empty)}");
            }

            return text.ToString();
        }

        private static string HeadingFor(DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Warning => "Needs attention",
                DiagnosticSeverity.Dropped => "Not carried over",
                DiagnosticSeverity.Approximated => "Approximated, check by eye",
                _ => "Mapped directly",
            };
        }
    }
}
