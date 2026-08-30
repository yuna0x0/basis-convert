using NUnit.Framework;
using yuna0x0.Basis.Convert.Mapping;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Pipeline;
using yuna0x0.Basis.Convert.Sources;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// A toggle has to switch the same way round as the toggle it was read from.
    /// <para>
    /// It did not. A clip that switches an object off leaves the same value behind as a side
    /// that animated nothing, so working out which side to fill from the value put the avatar's
    /// authored state on the side that had animated, and every one-sided toggle came out
    /// inverted. These need no avatar: the mapper's half is pure, and the fill is a function of
    /// the flags it records.
    /// </para>
    /// </summary>
    public class VixxyAuthoredDefaultsTests
    {
        private static ResolvedToggle ToggleThatHidesOnTheOnSide(string path)
        {
            ResolvedToggle toggle = new ResolvedToggle { MenuName = "Tail_OFF", Parameter = "Tail" };
            toggle.WhenOn.Deactivated.Add(path);
            return toggle;
        }

        [Test]
        public void TheMapperRecordsWhichSideAnimated()
        {
            VixxyControlPlan plan = ToggleToVixxyMapper.Map(ToggleThatHidesOnTheOnSide("Tail"));

            VixxyActivationPlan activation = plan.Activations[0];
            Assert.That(activation.Set[1], Is.True, "The on side switched the object off.");
            Assert.That(activation.Set[0], Is.False, "The off side animated nothing.");
            Assert.That(activation.Choices[1], Is.False, "On hides it, as the clip says.");
        }

        [Test]
        public void OnlyTheSideThatAnimatedNothingTakesTheAuthoredState()
        {
            VixxyActivationPlan activation =
                ToggleToVixxyMapper.Map(ToggleThatHidesOnTheOnSide("Tail")).Activations[0];

            VixxyAuthoredDefaults.Apply(activation, authored: true);

            Assert.That(activation.Choices[0], Is.True,
                "Off animated nothing, so the object keeps the state it was authored with.");
            Assert.That(activation.Choices[1], Is.False,
                "On switched it off, and that is not something the authored state overwrites.");
        }

        [Test]
        public void AToggleThatAnimatesBothSidesIsLeftAlone()
        {
            VixxyActivationPlan activation = new VixxyActivationPlan
            {
                Path = "Tail",
                Choices = new[] {true, false},
                Set = new[] {true, true},
            };

            VixxyAuthoredDefaults.Apply(activation, authored: false);

            Assert.That(activation.Choices[0], Is.True);
            Assert.That(activation.Choices[1], Is.False);
        }

        [Test]
        public void BlendShapesFollowTheSameRule()
        {
            VixxyBlendShapePlan shape = new VixxyBlendShapePlan
            {
                ShapeName = "Corset",
                Choices = new[] {0f, 0f},
                Set = new[] {false, true},
            };

            VixxyAuthoredDefaults.Apply(shape, authored: 100f);

            Assert.That(shape.Choices[0], Is.EqualTo(100f), "The side that set nothing.");
            Assert.That(shape.Choices[1], Is.EqualTo(0f), "The side the clip set.");
        }
    }
}
