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
    /// End to end over a hand-authored Dynamic Bone prefab.
    /// <para>
    /// Unlike the VRChat avatar tests, this fixture ships with the package: it is a few
    /// GameObjects of YAML written by hand, containing no third party asset, so this path is
    /// covered on any machine rather than only where a purchased avatar happens to exist.
    /// </para>
    /// </summary>
    public class DynamicBoneFixtureTests
    {
        private const string FixturePath =
            "Packages/com.yuna0x0.basis.convert/Tests/Editor/Fixtures/DynamicBoneChain.prefab";

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
        public void TheFixtureLoadsAsAPrefabWithMissingScripts()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath);
            Assert.That(prefab, Is.Not.Null,
                $"The fixture at {FixturePath} did not import as a prefab.");
            Assert.That(prefab.transform.Find("Bone1"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Bone1/Bone2"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Collider"), Is.Not.Null);
        }

        [Test]
        public void PlanningFindsTheDynamicBoneAndItsCollider()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);

            TestContext.WriteLine($"dynamic bones: {plan.DynamicBonesFound}");
            TestContext.WriteLine($"colliders:     {plan.CollidersFound}");
            TestContext.WriteLine($"rigs planned:  {plan.Rigs.Count}");
            foreach (ConversionDiagnostic diagnostic in plan.AllDiagnostics())
            {
                TestContext.WriteLine($"  [{diagnostic.Severity}] {diagnostic.Code}");
            }

            Assert.That(plan.DynamicBonesFound, Is.EqualTo(1));
            Assert.That(plan.CollidersFound, Is.EqualTo(1));
            Assert.That(plan.PhysBonesFound, Is.Zero, "This fixture has no PhysBones.");
            Assert.That(plan.Rigs.Count, Is.EqualTo(1));
            Assert.That(plan.Unresolved, Is.Zero);

            PlannedJiggleRig rig = plan.Rigs[0];
            Assert.That(rig.SourceRootBone.name, Is.EqualTo("Bone1"),
                "The rig should be rooted at the chain root, not the component's own object.");
            Assert.That(rig.Colliders.Count, Is.EqualTo(1));
            Assert.That(rig.SourceExcludedTransforms.Count, Is.EqualTo(1));
            Assert.That(rig.SourceExcludedTransforms[0].name, Is.EqualTo("Bone2"));
        }

        [Test]
        public void ConvertingProducesAJiggleRigCarryingTheSettings()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));

            ConversionResult result = AvatarConverter.Apply(plan, _instance);

            Assert.That(result.RigsWritten, Is.EqualTo(1));

            JiggleRig[] rigs = _instance.GetComponentsInChildren<JiggleRig>(true);
            Assert.That(rigs.Length, Is.EqualTo(1));

            JiggleRigData data = rigs[0].GetJiggleRigData();
            Assert.That(data.rootBone.name, Is.EqualTo("Bone1"));

            // Damping and inert map straight across, so these are exact rather than fitted.
            Assert.That(data.jiggleTreeInputParameters.drag.value,
                Is.EqualTo(0.35f).Within(1e-5f));
            Assert.That(data.jiggleTreeInputParameters.ignoreRootMotion,
                Is.EqualTo(0.15f).Within(1e-5f));
            Assert.That(data.jiggleTreeInputParameters.collisionRadius.value,
                Is.EqualTo(0.04f).Within(1e-5f));

            // The elasticity distribution curve should have carried across with the value.
            Assert.That(data.jiggleTreeInputParameters.stiffness.curveEnabled, Is.True,
                "The elasticity distribution curve should become the stiffness curve.");

            Assert.That(data.excludedTransforms.Length, Is.EqualTo(1));
            Assert.That(data.excludedTransforms[0].name, Is.EqualTo("Bone2"));

            Assert.That(data.jiggleColliders.Length, Is.EqualTo(1));
            JiggleColliderSerializable collider = data.jiggleColliders[0];
            Assert.That(collider.transform.name, Is.EqualTo("Collider"));
            Assert.That(collider.collider.type,
                Is.EqualTo(JiggleCollider.JiggleColliderType.Capsule),
                "A Dynamic Bone collider with a height is a capsule.");
            Assert.That(collider.collider.capsuleAxis,
                Is.EqualTo(JiggleCollider.CapsuleAxis.Z));
            Assert.That(collider.collider.radius, Is.EqualTo(0.08f).Within(1e-5f));
        }
    }
}
