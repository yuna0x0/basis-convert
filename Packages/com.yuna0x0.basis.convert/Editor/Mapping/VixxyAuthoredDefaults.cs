using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Mapping
{
    /// <summary>
    /// Fills in the side of a toggle that animated nothing, from what the avatar was authored
    /// with.
    /// <para>
    /// Which side to fill is read from the flags recorded when the clips were mapped, never
    /// inferred from the values: a clip that switches an object off is indistinguishable from a
    /// side that said nothing if you only look at the result, and inferring it inverted every
    /// one-sided toggle.
    /// </para>
    /// </summary>
    public static class VixxyAuthoredDefaults
    {
        public static void Apply(VixxyActivationPlan activation, bool authored)
        {
            if (activation == null)
            {
                return;
            }

            for (int choice = 0; choice < activation.Choices.Length; choice++)
            {
                if (choice >= activation.Set.Length || !activation.Set[choice])
                {
                    activation.Choices[choice] = authored;
                }
            }
        }

        public static void Apply(VixxyBlendShapePlan shape, float authored)
        {
            if (shape == null)
            {
                return;
            }

            for (int choice = 0; choice < shape.Choices.Length; choice++)
            {
                if (choice >= shape.Set.Length || !shape.Set[choice])
                {
                    shape.Choices[choice] = authored;
                }
            }
        }
    }
}
