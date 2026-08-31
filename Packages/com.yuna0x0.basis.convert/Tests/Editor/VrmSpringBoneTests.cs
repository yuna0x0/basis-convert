using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Pipeline;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// VRM spring bones, in both formats, against hand-written fixtures.
    /// <para>
    /// UniVRM is not installed here, which is the point: a VRM avatar imported into a Basis
    /// project arrives with its spring bones as missing scripts, exactly as VRChat components
    /// do, and the data is read from the file either way.
    /// </para>
    /// </summary>
    public class VrmSpringBoneTests
    {
        private const string Folder =
            "Packages/com.yuna0x0.basis.convert/Tests/Editor/Fixtures/SampleVrmAvatar";

        private const string Vrm10Path = Folder + "/SampleVrm10Avatar.prefab";
        private const string Vrm0Path = Folder + "/SampleVrm0Avatar.prefab";

        private static AvatarConversionPlan Plan(string path)
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(path);

            foreach (ConversionDiagnostic diagnostic in plan.AllDiagnostics())
            {
                TestContext.WriteLine($"[{diagnostic.Severity}] {diagnostic.Code}: {diagnostic.Message}");
            }

            return plan;
        }

        [Test]
        public void TheFixturesLoad()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(Vrm10Path), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(Vrm0Path), Is.Not.Null);
        }

        [Test]
        public void AVrm10SpringBecomesOneRigOnItsFirstJoint()
        {
            AvatarConversionPlan plan = Plan(Vrm10Path);

            Assert.That(plan.VrmChainsFound, Is.EqualTo(1));
            Assert.That(plan.Rigs.Count, Is.EqualTo(1));

            PlannedJiggleRig rig = plan.Rigs[0];
            Assert.That(rig.SourceRootBone.name, Is.EqualTo("HairRoot"),
                "The chain hangs from the bone its first joint sits on.");
        }

        [Test]
        public void JointsThatDifferAlongTheChainBecomeACurve()
        {
            // Stiffness is 1 at the root and 0.5 in the middle. Jiggle evaluates its parameters
            // over normalized distance from the root, which is the same axis VRM's joints sit
            // on, so the chain keeps its shape rather than being averaged.
            AvatarConversionPlan plan = Plan(Vrm10Path);
            JiggleCurvedFloatPlan? stiffness = plan.Rigs[0].Plan.Parameters.Stiffness;

            Assert.That(stiffness.HasValue, Is.True);
            Assert.That(stiffness.Value.Value, Is.EqualTo(1f).Within(0.001f));
            Assert.That(stiffness.Value.CurveEnabled, Is.True);
            Assert.That(stiffness.Value.Curve.Evaluate(1f), Is.EqualTo(0.5f).Within(0.001f),
                "The tail joint carries no parameters, so the last one that does ends the curve.");
        }

        [Test]
        public void ABoneTheSpringNeverNamedIsExcluded()
        {
            // HairAccessory hangs off the chain root but is not one of the spring's joints. VRM
            // leaves it still; a jiggle rig would swing it unless it is excluded.
            AvatarConversionPlan plan = Plan(Vrm10Path);

            List<string> excluded = new List<string>();
            foreach (Transform transform in plan.Rigs[0].SourceExcludedTransforms)
            {
                excluded.Add(transform.name);
            }

            Assert.That(excluded, Does.Contain("HairAccessory"));
            Assert.That(excluded, Does.Not.Contain("HairMiddle"));
            Assert.That(plan.AllDiagnostics().HasCode("vrm.branchesExcluded"), Is.True);
        }

        [Test]
        public void AVrm10ColliderGroupIsAttachedToTheChain()
        {
            AvatarConversionPlan plan = Plan(Vrm10Path);

            Assert.That(plan.Rigs[0].Colliders.Count, Is.EqualTo(1));

            JiggleColliderPlan collider = plan.Rigs[0].Colliders[0].Plan;
            Assert.That(collider.Shape, Is.EqualTo(JiggleColliderShape.Sphere));
            Assert.That(collider.Radius, Is.EqualTo(0.12f).Within(0.0001f));
            Assert.That(plan.Rigs[0].Colliders[0].SourceTransform.name, Is.EqualTo("Head"));
        }

        [Test]
        public void AVrm0SpringBoneBecomesOneRigPerRootBone()
        {
            // One VRM 0.x component carries a group of chains and one set of parameters for all
            // of them, the same shape one Dynamic Bone with several roots has.
            AvatarConversionPlan plan = Plan(Vrm0Path);

            Assert.That(plan.VrmChainsFound, Is.EqualTo(2));
            Assert.That(plan.Rigs.Count, Is.EqualTo(2));

            List<string> roots = new List<string>();
            foreach (PlannedJiggleRig rig in plan.Rigs)
            {
                roots.Add(rig.SourceRootBone.name);
            }

            Assert.That(roots, Is.EquivalentTo(new[] {"TwintailLeft", "TwintailRight"}));
        }

        [Test]
        public void AVrm0ChainKeepsItsParametersAndColliders()
        {
            AvatarConversionPlan plan = Plan(Vrm0Path);
            PlannedJiggleRig rig = plan.Rigs[0];

            Assert.That(rig.Plan.Parameters.Drag.Value.Value, Is.EqualTo(0.6f).Within(0.001f),
                "Drag force and jiggle drag are the same 0 to 1 scale.");
            Assert.That(rig.Plan.Parameters.CollisionRadius.Value.Value,
                Is.EqualTo(0.03f).Within(0.001f));
            Assert.That(rig.Plan.Parameters.Gravity.Value.Value, Is.EqualTo(0.3f).Within(0.001f));

            // The group holds its spheres inline rather than referencing components.
            Assert.That(rig.Colliders.Count, Is.EqualTo(2));
            Assert.That(rig.Colliders[0].Plan.Shape, Is.EqualTo(JiggleColliderShape.Sphere));
            Assert.That(rig.Colliders[0].Plan.Radius, Is.EqualTo(0.11f).Within(0.0001f));
        }

        [Test]
        public void AVrmChainsParametersAreNotSilentlyExact()
        {
            // VRM measures stiffness as a force with no upper bound and jiggle runs 0 to 1, so
            // the report has to say that one is a fit rather than a conversion.
            AvatarConversionPlan plan = Plan(Vrm0Path);

            Assert.That(plan.AllDiagnostics().HasCode("vrm.stiffness"), Is.True);
            Assert.That(plan.AllDiagnostics().HasCode("vrm.drag"), Is.True);
        }

        [Test]
        public void NoVrmComponentIsReportedAsAnUnknownScript()
        {
            foreach (string path in new[] {Vrm10Path, Vrm0Path})
            {
                AvatarConversionPlan plan = AvatarConversionPlanner.Plan(path);
                Assert.That(plan.AllDiagnostics().HasCode("source.unknownScript"), Is.False,
                    $"{path} carries only components this recognises.");
            }
        }
    }
}
