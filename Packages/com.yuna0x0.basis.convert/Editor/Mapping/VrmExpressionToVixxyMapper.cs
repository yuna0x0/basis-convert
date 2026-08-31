using System.Collections.Generic;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Mapping
{
    /// <summary>
    /// Turns a VRM expression into a Vixxy control.
    /// <para>
    /// An expression is a set of blendshape weights with a name, and a Vixxy control holds a
    /// value per choice, so one becomes a two-choice control: off, where every shape keeps the
    /// weight the avatar was authored with, and on, where it takes the expression's.
    /// </para>
    /// <para>
    /// Only the expressions an author added and the emotions are rebuilt. Visemes, blinking and
    /// looking around are driven by Basis itself, and putting them in a menu would offer the
    /// wearer a control over something already being driven.
    /// </para>
    /// </summary>
    public static class VrmExpressionToVixxyMapper
    {
        /// <summary>Whether this is an expression a menu control should be made for.</summary>
        public static bool IsMenuWorthy(VrmExpressionData expression) =>
            expression != null
            && expression.Bindings.Count > 0
            && (expression.Role == VrmExpressionRole.Custom
                || expression.Role == VrmExpressionRole.Emotion);

        public static VixxyControlPlan Map(VrmExpressionData expression)
        {
            VixxyControlPlan plan = new VixxyControlPlan
            {
                MenuName = expression.Name,
                Parameter = expression.Name,
            };

            plan.ChoiceNames.Add("OFF");
            plan.ChoiceNames.Add("ON");
            plan.ChoiceValues.Add(0);
            plan.ChoiceValues.Add(1);

            // One subject per renderer the expression touches: an expression commonly sets
            // shapes on the face and the eyebrows at once, and Vixxy addresses each renderer.
            Dictionary<string, VixxySubjectPlan> subjects =
                new Dictionary<string, VixxySubjectPlan>();

            int unnamed = 0;

            foreach (VrmMorphBinding binding in expression.Bindings)
            {
                if (string.IsNullOrEmpty(binding.ShapeName))
                {
                    unnamed++;
                    continue;
                }

                if (!subjects.TryGetValue(binding.Path, out VixxySubjectPlan subject))
                {
                    subject = new VixxySubjectPlan {Path = binding.Path};
                    subjects[binding.Path] = subject;
                    plan.Subjects.Add(subject);
                }

                subject.BlendShapes.Add(new VixxyBlendShapePlan
                {
                    ShapeName = binding.ShapeName,

                    // Off is filled in from the avatar as authored, the same rule a menu toggle
                    // that animates one side follows.
                    Choices = new[] {0f, binding.Weight},
                    Set = new[] {false, true},
                });
            }

            if (unnamed > 0)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "vrm.expression.shapeMissing",
                    $"'{expression.Name}' sets {unnamed} blendshapes that are not on the mesh "
                    + "they name. VRM refers to a shape by its position in the mesh, so this "
                    + "usually means the mesh has changed since the expression was authored.");
            }

            if (expression.MaterialBindingCount > 0)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "vrm.expression.materials",
                    $"'{expression.Name}' also changes {expression.MaterialBindingCount} "
                    + "material values. VRM names the material to change, while Vixxy acts on a "
                    + "renderer's properties, so those were not carried over.");
            }

            return plan;
        }
    }
}
