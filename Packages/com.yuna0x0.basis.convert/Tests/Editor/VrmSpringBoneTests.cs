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
        public void AVrm10ExpressionBecomesAVixxyControl()
        {
            // An expression is a named set of blendshape weights, which is what a control holds
            // once it has two choices. VRM has no menu, so on Basis the wearer picks it.
            AvatarConversionPlan plan = Plan(Vrm10Path);

            PlannedVixxyControl happy =
                plan.VixxyControls.Find(control => control.Plan.MenuName == "Happy");

            Assert.That(happy, Is.Not.Null);
            Assert.That(happy.Plan.Subjects.Count, Is.EqualTo(1));
            Assert.That(happy.SourceRenderers[0].name, Is.EqualTo("Face"));

            VixxyBlendShapePlan shape = happy.Plan.Subjects[0].BlendShapes[0];
            Assert.That(shape.ShapeName, Is.EqualTo("Smile"),
                "VRM names a shape by its index in the mesh, so the mesh is what names it.");
            Assert.That(shape.Choices[1], Is.EqualTo(100f).Within(0.01f),
                "A VRM 1.0 weight of 1 is Unity's 100.");
            Assert.That(shape.Set[0], Is.False, "Off keeps whatever the avatar was authored with.");
        }

        [Test]
        public void ACustomExpressionIsRebuiltAndItsMaterialChangesReported()
        {
            AvatarConversionPlan plan = Plan(Vrm10Path);

            PlannedVixxyControl wink =
                plan.VixxyControls.Find(control => control.Plan.MenuName == "Wink");

            Assert.That(wink, Is.Not.Null, "Expressions the author added are what a menu is for.");
            Assert.That(wink.Plan.Subjects[0].BlendShapes[0].Choices[1],
                Is.EqualTo(75f).Within(0.01f));
            Assert.That(plan.AllDiagnostics().HasCode("vrm.expression.materials"), Is.True,
                "It also changes a material colour, which Vixxy cannot address the same way.");
        }

        [Test]
        public void ExpressionsBasisDrivesItselfAreLeftToIt()
        {
            // The lip sync shapes, blinking and looking around are driven by Basis. A menu item
            // the wearer has to hold down would fight it.
            AvatarConversionPlan plan = Plan(Vrm10Path);

            Assert.That(plan.VixxyControls.Find(c => c.Plan.MenuName == "Aa"), Is.Null);
            Assert.That(plan.AllDiagnostics().HasCode("vrm.expressionsDriven"), Is.True);
            Assert.That(plan.AllDiagnostics().HasCode("vrm.expressionsRebuilt"), Is.True);
        }

        [Test]
        public void AVrm0ClipBecomesAControlWithItsOwnWeightScale()
        {
            // VRM 0.x weights are already on Unity's 0 to 100 scale: UniVRM passes them straight
            // to SetBlendShapeWeight. Only 1.0 needs scaling.
            AvatarConversionPlan plan = Plan(Vrm0Path);

            PlannedVixxyControl joy =
                plan.VixxyControls.Find(control => control.Plan.MenuName == "Joy");

            Assert.That(joy, Is.Not.Null);
            Assert.That(joy.Plan.Subjects[0].BlendShapes[0].ShapeName, Is.EqualTo("Smile"));
            Assert.That(joy.Plan.Subjects[0].BlendShapes[0].Choices[1],
                Is.EqualTo(100f).Within(0.01f));

            Assert.That(plan.VixxyControls.Find(c => c.Plan.MenuName == "A"), Is.Null,
                "A is a viseme, whatever the author called the clip.");
        }

        [Test]
        public void AVrm0EyeOffsetBecomesTheAvatarsEyePosition()
        {
            // VRM measures the camera point from the head bone. Basis stores the height and
            // depth of the same point relative to the avatar root, which is what VRChat's view
            // position holds.
            AvatarConversionPlan plan = Plan(Vrm0Path);

            Assert.That(plan.VrmSettings, Is.Not.Null);
            Assert.That(plan.VrmSettings.HasEyeOffset, Is.True);
            Assert.That(plan.VrmSettings.EyeOffsetFromHead,
                Is.EqualTo(new Vector3(0f, 0.06f, 0.08f)));

            // The fixture is a hand-written hierarchy with no humanoid rig, so there is no head
            // bone to measure from and no Basis Avatar component to put the result on.
            Assert.That(plan.AllDiagnostics().HasCode("vrm.eyePosition.noRig"), Is.True);
        }

        [Test]
        public void AnEyeOffsetIsMeasuredFromTheHeadInTheRootsSpace()
        {
            // The arithmetic on its own: a head 1.4 up, eyes 0.06 above it and 0.08 forward.
            GameObject root = new GameObject("Root");
            GameObject head = new GameObject("Head");

            try
            {
                head.transform.SetParent(root.transform);
                head.transform.localPosition = new Vector3(0f, 1.4f, 0f);

                Vector2 eyes = AvatarConversionPlanner.EyePositionFrom(
                    root.transform, head.transform, new Vector3(0f, 0.06f, 0.08f));

                Assert.That(eyes.x, Is.EqualTo(1.46f).Within(0.001f), "Height above the root.");
                Assert.That(eyes.y, Is.EqualTo(0.08f).Within(0.001f), "Depth in front of it.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FirstPersonRendererFlagsAreReported()
        {
            // Basis hides the head bone and everything under it, which covers the usual case,
            // so the flags are reported rather than turned into head chop targets.
            AvatarConversionPlan plan = Plan(Vrm0Path);

            Assert.That(plan.VrmSettings.ThirdPersonOnlyRenderers, Is.EqualTo(1));
            Assert.That(plan.AllDiagnostics().HasCode("vrm.firstPerson"), Is.True);
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
