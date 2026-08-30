using System.Collections.Generic;
using GatorDragonGames.JigglePhysics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Pipeline;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// End to end over an avatar that ships with this package.
    /// <para>
    /// Most of this suite needs a purchased avatar and skips itself without one, which means it
    /// proves nothing on anyone else's machine. This fixture is hand-written: a prefab carrying
    /// the components a VRChat avatar has, as the missing scripts they arrive as, plus an
    /// expression menu, parameters, an animator and its clips. No third-party asset is
    /// redistributed; script identities and field names are facts about a file format.
    /// </para>
    /// </summary>
    public class SampleAvatarFixtureTests
    {
        private const string FixturePath =
            "Packages/com.yuna0x0.basis.convert/Tests/Editor/Fixtures/SampleAvatar/SampleAvatar.prefab";

        private GameObject _instance;

        [TearDown]
        public void TearDown()
        {
            if (_instance != null)
            {
                Object.DestroyImmediate(_instance);
                _instance = null;
            }
        }

        private static AvatarConversionPlan Plan()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);

            foreach (ConversionDiagnostic diagnostic in plan.AllDiagnostics())
            {
                TestContext.WriteLine($"[{diagnostic.Severity}] {diagnostic.Code}: {diagnostic.Message}");
            }

            return plan;
        }

        [Test]
        public void TheFixtureLoads()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath);
            Assert.That(prefab, Is.Not.Null, $"No prefab at {FixturePath}.");
            Assert.That(prefab.transform.Find("Tail"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Hair/HairBone1/HairBone2"), Is.Not.Null);
        }

        [Test]
        public void ThePhysBoneAndConstraintAreRead()
        {
            AvatarConversionPlan plan = Plan();

            Assert.That(plan.PhysBonesFound, Is.EqualTo(1));
            Assert.That(plan.ConstraintsFound, Is.EqualTo(1));
            Assert.That(plan.Rigs.Count, Is.EqualTo(1));
            Assert.That(plan.Rigs[0].SourceRootBone.name, Is.EqualTo("HairBone1"),
                "The rig roots at the PhysBone's root transform, not the object it sits on.");
            Assert.That(plan.Constraints.Count, Is.EqualTo(1));
            Assert.That(plan.Constraints[0].Plan.Kind,
                Is.EqualTo(BasisConstraintKind.Rotation));
        }

        [Test]
        public void TheDescriptorIsRead()
        {
            AvatarConversionPlan plan = Plan();

            Assert.That(plan.Descriptor, Is.Not.Null);
            Assert.That(plan.Descriptor.Plan.EyePosition.x, Is.EqualTo(1.6f).Within(1e-4f),
                "Basis takes the eye position from the descriptor's view position y and z.");
            Assert.That(plan.Profile.Kind, Is.EqualTo("VRChat avatar"));
        }

        [Test]
        public void TheExpressionMenuIsRead()
        {
            AvatarConversionPlan plan = Plan();

            Assert.That(plan.Expressions.Menus.Count, Is.EqualTo(1));
            Assert.That(plan.Expressions.CountOf(VrcExpressionControlType.Toggle),
                Is.EqualTo(4), "One toggle and three controls sharing a selector parameter.");
        }

        [Test]
        public void TheToggleIsTracedAndSwitchesTheRightWayRound()
        {
            AvatarConversionPlan plan = Plan();

            ResolvedToggle tail = plan.Toggles.Find(toggle => toggle.Parameter == "Tail");
            Assert.That(tail, Is.Not.Null, "The Tail layer is a plain two-state toggle.");
            Assert.That(tail.WhenOn.Deactivated, Does.Contain("Tail"));
            Assert.That(tail.WhenOff.Deactivated, Is.Empty,
                "The off side animates nothing, which is the shape that used to invert.");

            PlannedVixxyControl control =
                plan.VixxyControls.Find(c => c.Plan.Parameter == "Tail");
            Assert.That(control, Is.Not.Null);

            VixxyActivationPlan activation = control.Plan.Activations[0];
            Assert.That(activation.Path, Is.EqualTo("Tail"));
            Assert.That(activation.Choices[0], Is.True, "Off leaves it as the avatar authored it.");
            Assert.That(activation.Choices[1], Is.False, "On hides it, as the clip says.");
        }

        [Test]
        public void ASelectorBecomesOneControlWithAChoicePerValue()
        {
            // Three menu controls share HairStyle and pick different values from it, and its
            // layer holds one state per value. Vixxy holds that as one control with three
            // choices rather than three separate toggles.
            AvatarConversionPlan plan = Plan();

            ResolvedToggle selector = plan.Toggles.Find(toggle => toggle.Parameter == "HairStyle");
            Assert.That(selector, Is.Not.Null);
            Assert.That(selector.IsSelector, Is.True);
            Assert.That(selector.Choices.Count, Is.EqualTo(3));

            PlannedVixxyControl control =
                plan.VixxyControls.Find(c => c.Plan.Parameter == "HairStyle");
            Assert.That(control, Is.Not.Null, "The selector rebuilds as a Vixxy control.");
            Assert.That(control.Plan.ChoiceNames,
                Is.EqualTo(new[] {"Hair_Long", "Hair_Braid", "Hair_Short"}),
                "Choices are named by the menu entries that select them, in value order.");
            Assert.That(control.Plan.ChoiceValues, Is.EqualTo(new[] {0, 1, 2}));

            foreach (VixxyActivationPlan activation in control.Plan.Activations)
            {
                Assert.That(activation.Choices.Length, Is.EqualTo(3),
                    $"{activation.Path} needs a state for every choice.");
            }
        }

        [Test]
        public void ASelectorIsWrittenWithItsChoices()
        {
            AvatarConversionPlan plan = Plan();
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));

            AvatarConverter.Apply(plan, _instance);

            HVR.Vixxy.HVRVixxyControl written = null;
            foreach (HVR.Vixxy.HVRVixxyControl control in
                     _instance.GetComponentsInChildren<HVR.Vixxy.HVRVixxyControl>(true))
            {
                if (control.choices != null && control.choices.Length == 3)
                {
                    written = control;
                    break;
                }
            }

            Assert.That(written, Is.Not.Null, "The selector's control has three choices.");
            Assert.That(written.choices[1].title, Is.EqualTo("Hair_Braid"));
            Assert.That(written.choices[2].value, Is.EqualTo(2f),
                "A choice carries the parameter value that selects it.");
        }

        [Test]
        public void ConvertingWritesTheRigAndTheConstraint()
        {
            AvatarConversionPlan plan = Plan();
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));

            ConversionResult result = AvatarConverter.Apply(plan, _instance);

            Assert.That(result.RigsWritten, Is.EqualTo(1));
            Assert.That(result.ConstraintsWritten, Is.EqualTo(1));
            Assert.That(result.DescriptorWritten, Is.True);

            JiggleRig[] rigs = _instance.GetComponentsInChildren<JiggleRig>(true);
            Assert.That(rigs.Length, Is.EqualTo(1));
            Assert.That(rigs[0].GetJiggleRigData().rootBone.name, Is.EqualTo("HairBone1"));
        }
    }
}
