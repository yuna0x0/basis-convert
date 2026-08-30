using System.IO;
using HVR.Vixxy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Pipeline;

namespace yuna0x0.Basis.Convert.Tests
{
    public class VixxyEmissionTests
    {
        private const string FixturePath =
            "Assets/yuna0x0/Avatars/Shinano/Prefab/Shinano.prefab";

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

        [Test]
        public void ToggleThatOnlySwitchesObjectsBecomesAControl()
        {
            if (!File.Exists(FixturePath))
            {
                Assert.Ignore($"Fixture not present at {FixturePath}.");
            }

            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);

            TestContext.WriteLine($"vixxy controls planned: {plan.VixxyControls.Count}");
            foreach (PlannedVixxyControl control in plan.VixxyControls)
            {
                TestContext.WriteLine($"  {control.Plan.MenuName} [{control.Plan.Parameter}]: "
                    + $"{control.Plan.Activations.Count} objects");
                foreach (VixxyActivationPlan activation in control.Plan.Activations)
                {
                    TestContext.WriteLine(
                        $"    {activation.Path}: off={activation.Choices[0]} "
                        + $"on={activation.Choices[1]}");
                }
            }

            Assert.That(plan.VixxyControls, Is.Not.Empty,
                "Toggles that only switch objects should be rebuildable.");

            foreach (PlannedVixxyControl control in plan.VixxyControls)
            {
                Assert.That(control.SourceTargets.Count,
                    Is.EqualTo(control.Plan.Activations.Count),
                    "Every switched object should have resolved to a transform.");

                foreach (VixxyActivationPlan activation in control.Plan.Activations)
                {
                    Assert.That(activation.Choices[0], Is.Not.EqualTo(activation.Choices[1]),
                        $"{control.Plan.MenuName} would not change {activation.Path} at all.");
                }
            }
        }

        [Test]
        public void ConvertingWritesTheControlsAndTheirMenuItems()
        {
            if (!File.Exists(FixturePath))
            {
                Assert.Ignore($"Fixture not present at {FixturePath}.");
            }

            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));

            ConversionResult result = AvatarConverter.Apply(plan, _instance);

            Assert.That(result.VixxyControlsWritten, Is.EqualTo(plan.VixxyControls.Count));

            HVRVixxyControl[] controls = _instance.GetComponentsInChildren<HVRVixxyControl>(true);
            HVRVixxyMenuItem[] items = _instance.GetComponentsInChildren<HVRVixxyMenuItem>(true);

            Assert.That(controls.Length, Is.EqualTo(plan.VixxyControls.Count));
            Assert.That(items.Length, Is.EqualTo(controls.Length),
                "Every control should get a menu item, or it cannot be reached in game.");

            foreach (HVRVixxyControl control in controls)
            {
                Assert.That(control.choices.Length, Is.EqualTo(2));
                Assert.That(control.choices[0].value, Is.EqualTo(0f).Within(1e-6f));
                Assert.That(control.choices[1].value, Is.EqualTo(1f).Within(1e-6f));
            }
        }

        [Test]
        public void AToggleDoingMoreThanSwitchingObjectsIsLeftAlone()
        {
            if (!File.Exists(FixturePath))
            {
                Assert.Ignore($"Fixture not present at {FixturePath}.");
            }

            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);

            bool reported = false;
            foreach (ConversionDiagnostic diagnostic in plan.AllDiagnostics())
            {
                if (diagnostic.Code == "vixxy.blendShapes" || diagnostic.Code == "vixxy.notSimple")
                {
                    reported = true;
                    TestContext.WriteLine(diagnostic.Message);
                }
            }

            Assert.That(reported, Is.True,
                "Toggles doing more than switching objects should be reported, not half built.");
        }
    }
}
