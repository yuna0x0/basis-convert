using NUnit.Framework;
using UnityEngine;
using yuna0x0.Basis.Convert.Mapping;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// The three collider sources and jiggle agree on the shapes but not on how a capsule is
    /// measured or which way a plane faces. VRChat and Dynamic Bone give a capsule's height end
    /// to end and turn a capsule no taller than its diameter into a sphere; jiggle measures
    /// between the cap centres. A jiggle plane faces its transform's Y and nothing else.
    /// </summary>
    public class ColliderShapeTests
    {
        [Test]
        public void APhysBoneCapsuleLosesADiameterOfHeight()
        {
            JiggleColliderPlan plan = PhysBoneColliderToJiggleMapper.Map(new PhysBoneColliderData
            {
                ShapeType = PhysBoneColliderShape.Capsule,
                Radius = 0.05f,
                Height = 0.4f,
                Rotation = Quaternion.identity,
            });

            Assert.That(plan.Shape, Is.EqualTo(JiggleColliderShape.Capsule));
            Assert.That(plan.Height, Is.EqualTo(0.3f).Within(1e-6f),
                "0.4 end to end with 0.05 caps is 0.3 between the cap centres.");
            Assert.That(plan.Radius, Is.EqualTo(0.05f).Within(1e-6f));
        }

        [Test]
        public void APhysBoneCapsuleNoTallerThanItsDiameterIsASphere()
        {
            JiggleColliderPlan plan = PhysBoneColliderToJiggleMapper.Map(new PhysBoneColliderData
            {
                ShapeType = PhysBoneColliderShape.Capsule,
                Radius = 0.1f,
                Height = 0.2f,
                Rotation = Quaternion.identity,
            });

            Assert.That(plan.Shape, Is.EqualTo(JiggleColliderShape.Sphere));
            Assert.That(plan.Height, Is.EqualTo(0f));
        }

        [Test]
        public void APhysBonePlaneRotatedOffItsYAxisIsReported()
        {
            JiggleColliderPlan upright = PhysBoneColliderToJiggleMapper.Map(new PhysBoneColliderData
            {
                ShapeType = PhysBoneColliderShape.Plane,
                Rotation = Quaternion.identity,
            });
            Assert.That(upright.Shape, Is.EqualTo(JiggleColliderShape.Plane));
            Assert.That(upright.Diagnostics.HasCode("collider.planeRotation.dropped"), Is.False);

            JiggleColliderPlan turned = PhysBoneColliderToJiggleMapper.Map(new PhysBoneColliderData
            {
                ShapeType = PhysBoneColliderShape.Plane,
                Rotation = Quaternion.Euler(-90f, 0f, 0f),
            });
            Assert.That(turned.Shape, Is.EqualTo(JiggleColliderShape.Plane));
            Assert.That(turned.Diagnostics.HasCode("collider.planeRotation.dropped"), Is.True,
                "A plane facing forward cannot be written; jiggle planes face Y.");
        }

        [Test]
        public void ADynamicBoneCapsuleLosesADiameterOfHeight()
        {
            JiggleColliderPlan plan = DynamicBoneColliderToJiggleMapper.Map(new DynamicBoneColliderData
            {
                Radius = 0.08f,
                Height = 0.3f,
                Direction = DynamicBoneColliderDirection.Z,
            });

            Assert.That(plan.Shape, Is.EqualTo(JiggleColliderShape.Capsule));
            Assert.That(plan.Height, Is.EqualTo(0.14f).Within(1e-6f));
            Assert.That(plan.CapsuleAxis, Is.EqualTo(JiggleCapsuleAxis.Z));
        }

        [Test]
        public void ADynamicBoneCapsuleNoTallerThanItsDiameterIsASphere()
        {
            JiggleColliderPlan plan = DynamicBoneColliderToJiggleMapper.Map(new DynamicBoneColliderData
            {
                Radius = 0.1f,
                Height = 0.15f,
            });

            Assert.That(plan.Shape, Is.EqualTo(JiggleColliderShape.Sphere),
                "Dynamic Bone itself collides this as a sphere.");
            Assert.That(plan.Height, Is.EqualTo(0f));
        }

        [Test]
        public void ADynamicBonePlaneOffItsYAxisIsReported()
        {
            JiggleColliderPlan upright = DynamicBoneColliderToJiggleMapper.Map(new DynamicBoneColliderData
            {
                IsPlane = true,
                Direction = DynamicBoneColliderDirection.Y,
            });
            Assert.That(upright.Diagnostics.HasCode("collider.planeAxis.dropped"), Is.False);

            JiggleColliderPlan sideways = DynamicBoneColliderToJiggleMapper.Map(new DynamicBoneColliderData
            {
                IsPlane = true,
                Direction = DynamicBoneColliderDirection.X,
            });
            Assert.That(sideways.Shape, Is.EqualTo(JiggleColliderShape.Plane));
            Assert.That(sideways.Diagnostics.HasCode("collider.planeAxis.dropped"), Is.True);
        }

        [Test]
        public void AVrmCapsuleKeepsItsLengthBetweenTheCapCentres()
        {
            JiggleColliderPlan plan = VrmColliderToJiggleMapper.Map(new VrmColliderData
            {
                Type = VrmColliderType.Capsule,
                Radius = 0.05f,
                Offset = new Vector3(0f, 0.1f, 0f),
                Tail = new Vector3(0f, 0.4f, 0f),
            });

            Assert.That(plan.Shape, Is.EqualTo(JiggleColliderShape.Capsule));
            Assert.That(plan.Height, Is.EqualTo(0.3f).Within(1e-6f),
                "VRM states the cap centres, which is what jiggle measures too.");
            Assert.That(plan.CapsuleAxis, Is.EqualTo(JiggleCapsuleAxis.Y));
            Assert.That(plan.LocalOffset.y, Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(plan.Diagnostics.HasCode("vrm.collider.capsuleSnapped"), Is.False);
        }

        [Test]
        public void AVrmPlaneWhoseNormalIsNotUpIsReported()
        {
            JiggleColliderPlan upright = VrmColliderToJiggleMapper.Map(new VrmColliderData
            {
                Type = VrmColliderType.Plane,
                Normal = Vector3.up,
            });
            Assert.That(upright.Shape, Is.EqualTo(JiggleColliderShape.Plane));
            Assert.That(upright.Diagnostics.HasCode("vrm.collider.planeNormal"), Is.False);

            JiggleColliderPlan forward = VrmColliderToJiggleMapper.Map(new VrmColliderData
            {
                Type = VrmColliderType.Plane,
                Normal = Vector3.forward,
            });
            Assert.That(forward.Diagnostics.HasCode("vrm.collider.planeNormal"), Is.True);

            JiggleColliderPlan flipped = VrmColliderToJiggleMapper.Map(new VrmColliderData
            {
                Type = VrmColliderType.Plane,
                Normal = Vector3.down,
            });
            Assert.That(flipped.Diagnostics.HasCode("vrm.collider.planeNormal"), Is.True,
                "A plane facing down pushes the other way from one facing up.");
        }
    }
}
