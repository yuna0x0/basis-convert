using System.Collections.Generic;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Mapping
{
    /// <summary>
    /// Turns a VRM avatar's expressions into one Vixxy selector.
    /// <para>
    /// An expression is a named set of blendshape weights. VRM lets an application wear several
    /// at once, each at its own strength, and sums the result; a menu has no strength and no
    /// application driving it, so the wearer picks one. One control with a choice per expression
    /// says that; a toggle per expression let two be on together and filled the menu with one
    /// entry each. Every shape any expression touches is written at every choice, at the
    /// expression's weight or at zero, which is the spec's own rule: all morph targets start at
    /// zero and the worn expressions are added.
    /// </para>
    /// <para>
    /// Only the expressions an author added and the emotions are choices. Visemes, blinking and
    /// looking around are driven by Basis itself. Neutral, when it carries weights of its own, is
    /// the first choice.
    /// </para>
    /// </summary>
    public static class VrmExpressionToVixxyMapper
    {
        public const string MenuName = "Expression";
        public const string NeutralChoice = "Neutral";

        /// <summary>Whether this expression is a choice on the selector.</summary>
        public static bool IsMenuWorthy(VrmExpressionData expression) =>
            expression != null
            && expression.Bindings.Count > 0
            && (expression.Role == VrmExpressionRole.Custom
                || expression.Role == VrmExpressionRole.Emotion);

        /// <summary>
        /// One selector over these expressions. <paramref name="neutral"/> is the avatar's
        /// Neutral expression when it has bindings, and may be null.
        /// </summary>
        public static VixxyControlPlan MapSelector(
            IReadOnlyList<VrmExpressionData> expressions, VrmExpressionData neutral = null)
        {
            VixxyControlPlan plan = new VixxyControlPlan
            {
                MenuName = MenuName,
                Parameter = MenuName,
                DefaultValue = 0f,
            };

            int count = expressions.Count + 1;
            plan.ChoiceNames.Add(NeutralChoice);
            plan.ChoiceValues.Add(0);
            for (int i = 0; i < expressions.Count; i++)
            {
                plan.ChoiceNames.Add(expressions[i].Name);
                plan.ChoiceValues.Add(i + 1);
            }

            // One subject per renderer any expression touches: an expression commonly sets
            // shapes on the face and the eyebrows at once, and Vixxy addresses each renderer.
            Dictionary<string, VixxySubjectPlan> subjects =
                new Dictionary<string, VixxySubjectPlan>();
            Dictionary<(string, string), VixxyBlendShapePlan> shapes =
                new Dictionary<(string, string), VixxyBlendShapePlan>();

            VixxyBlendShapePlan ShapeFor(VrmMorphBinding binding)
            {
                if (!shapes.TryGetValue((binding.Path, binding.ShapeName),
                        out VixxyBlendShapePlan shape))
                {
                    if (!subjects.TryGetValue(binding.Path, out VixxySubjectPlan subject))
                    {
                        subject = new VixxySubjectPlan { Path = binding.Path };
                        subjects[binding.Path] = subject;
                        plan.Subjects.Add(subject);
                    }

                    bool[] set = new bool[count];
                    for (int c = 0; c < count; c++)
                    {
                        set[c] = true;
                    }

                    shape = new VixxyBlendShapePlan
                    {
                        ShapeName = binding.ShapeName,
                        Choices = new float[count],
                        Set = set,
                    };
                    shapes[(binding.Path, binding.ShapeName)] = shape;
                    subject.BlendShapes.Add(shape);
                }

                return shape;
            }

            void Apply(VrmExpressionData expression, int choice)
            {
                int unnamed = 0;
                foreach (VrmMorphBinding binding in expression.Bindings)
                {
                    if (string.IsNullOrEmpty(binding.ShapeName))
                    {
                        unnamed++;
                        continue;
                    }

                    ShapeFor(binding).Choices[choice] = binding.Weight;
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
                        + "material values. VRM names the material to change, while Vixxy acts "
                        + "on a renderer's properties, so those were not carried over.");
                }
            }

            if (neutral != null)
            {
                Apply(neutral, 0);
            }

            int continuous = 0;
            for (int i = 0; i < expressions.Count; i++)
            {
                VrmExpressionData expression = expressions[i];
                Apply(expression, i + 1);

                if (!expression.IsBinary)
                {
                    continuous++;
                }

                if (expression.HasOverride)
                {
                    plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "vrm.expression.override",
                        $"'{expression.Name}' {Describe(expression)} while it is worn, which is "
                        + "how VRM keeps a face from fighting its own blink and lip sync. Basis "
                        + "keeps blinking, gaze and lip sync running whatever choice is picked.");
                }
            }

            if (continuous > 0)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Approximated, "vrm.expression.continuous",
                    $"{continuous} expressions can be worn at any strength in VRM. A choice is "
                    + "worn fully or not at all.");
            }

            return plan;
        }

        /// <summary>"blocks blink and lip sync", from the three override fields.</summary>
        private static string Describe(VrmExpressionData expression)
        {
            List<string> blocked = new List<string>();
            List<string> attenuated = new List<string>();

            void Sort(VrmExpressionOverride value, string what)
            {
                if (value == VrmExpressionOverride.Block) blocked.Add(what);
                else if (value == VrmExpressionOverride.Blend) attenuated.Add(what);
            }

            Sort(expression.OverrideBlink, "blink");
            Sort(expression.OverrideLookAt, "gaze");
            Sort(expression.OverrideMouth, "lip sync");

            List<string> parts = new List<string>();
            if (blocked.Count > 0) parts.Add("blocks " + string.Join(" and ", blocked));
            if (attenuated.Count > 0) parts.Add("attenuates " + string.Join(" and ", attenuated));
            return string.Join(" and ", parts);
        }
    }
}
