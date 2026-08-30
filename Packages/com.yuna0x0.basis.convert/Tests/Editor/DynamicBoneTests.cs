using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using yuna0x0.Basis.Convert.Mapping;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Sources;

namespace yuna0x0.Basis.Convert.Tests
{
    public class DynamicBoneTests
    {
        private static DynamicBoneData ReadBone(params string[] extra)
        {
            List<string> lines = new List<string>
            {
                "--- !u!114 &800",
                "MonoBehaviour:",
                "  m_GameObject: {fileID: 10}",
                "  m_Script: {fileID: 11500000, guid: f9ac8d30c6a0d9642a11e5be4c440740, type: 3}",
                "  m_Root: {fileID: 20}",
                "  m_Roots: []",
                "  m_UpdateRate: 60",
                "  m_UpdateMode: 3",
                "  m_Damping: 0.35",
                "  m_Elasticity: 0.4",
                "  m_Stiffness: 0.2",
                "  m_Inert: 0.15",
                "  m_Friction: 0",
                "  m_Radius: 0.03",
                "  m_EndLength: 0",
                "  m_EndOffset: {x: 0, y: 0, z: 0}",
                "  m_Gravity: {x: 0, y: -0.5, z: 0}",
                "  m_Force: {x: 0, y: 0, z: 0}",
                "  m_BlendWeight: 1",
                "  m_Colliders: []",
                "  m_Exclusions: []",
                "  m_FreezeAxis: 0",
            };
            lines.AddRange(extra);

            List<UnityYamlDocument> documents = UnityYamlScanner.Scan(lines);
            Assert.That(documents.Count, Is.EqualTo(1));
            return DynamicBoneDocumentReader.ReadBone(documents[0]);
        }

        [Test]
        public void TheScriptIdentityIsRecognised()
        {
            Assert.That(
                KnownScriptIdentities.Resolve("f9ac8d30c6a0d9642a11e5be4c440740", 11500000L),
                Is.EqualTo(SourceComponentKind.DynamicBone));
        }

        [Test]
        public void ReadsTheSettings()
        {
            DynamicBoneData data = ReadBone();

            Assert.That(data.OwnerGameObjectFileId, Is.EqualTo(10L));
            Assert.That(data.RootFileId, Is.EqualTo(20L));
            Assert.That(data.Damping.Value, Is.EqualTo(0.35f).Within(1e-6f));
            Assert.That(data.Elasticity.Value, Is.EqualTo(0.4f).Within(1e-6f));
            Assert.That(data.Stiffness.Value, Is.EqualTo(0.2f).Within(1e-6f));
            Assert.That(data.Inert.Value, Is.EqualTo(0.15f).Within(1e-6f));
            Assert.That(data.Radius.Value, Is.EqualTo(0.03f).Within(1e-6f));
            Assert.That(data.Gravity.y, Is.EqualTo(-0.5f).Within(1e-6f));
        }

        [Test]
        public void DampingAndInertMapDirectly()
        {
            // Both systems use the same 0 to 1 scale for these, so they are not approximations.
            List<JiggleRigPlan> plans = DynamicBoneToJiggleMapper.Map(ReadBone());
            Assert.That(plans.Count, Is.EqualTo(1));

            JiggleRigPlan plan = plans[0];
            Assert.That(plan.Parameters.Drag.Value.Value, Is.EqualTo(0.35f).Within(1e-6f));
            Assert.That(plan.Parameters.IgnoreRootMotion, Is.EqualTo(0.15f).Within(1e-6f));
            Assert.That(plan.Diagnostics.HasCode("dynamicbone.damping.drag"), Is.True);
            Assert.That(plan.Diagnostics.HasCode("dynamicbone.inert.ignoreRootMotion"), Is.True);
        }

        [Test]
        public void EachRootBecomesItsOwnRig()
        {
            DynamicBoneData data = ReadBone();
            data.RootFileIds = new List<long> { 21L, 22L };

            List<JiggleRigPlan> plans = DynamicBoneToJiggleMapper.Map(data);

            Assert.That(plans.Count, Is.EqualTo(3));
            Assert.That(plans[0].RootBoneFileId, Is.EqualTo(20L));
            Assert.That(plans[1].RootBoneFileId, Is.EqualTo(21L));
            Assert.That(plans[2].RootBoneFileId, Is.EqualTo(22L));
            Assert.That(plans[0].Diagnostics.HasCode("dynamicbone.multipleRoots"), Is.True);

            foreach (JiggleRigPlan plan in plans)
            {
                Assert.That(plan.Parameters.Drag.Value.Value, Is.EqualTo(0.35f).Within(1e-6f),
                    "Chains from one component share its settings.");
            }
        }

        [Test]
        public void ABoneWithNoRootFallsBackToItsOwnObject()
        {
            DynamicBoneData data = ReadBone();
            data.RootFileId = 0L;

            List<JiggleRigPlan> plans = DynamicBoneToJiggleMapper.Map(data);

            Assert.That(plans.Count, Is.EqualTo(1));
            Assert.That(plans[0].RootBoneFileId, Is.Zero,
                "Zero means the object the component sits on, same as a PhysBone.");
        }

        [Test]
        public void GravityKeepsOnlyItsDownwardPart()
        {
            DynamicBoneData straight = ReadBone();
            straight.Gravity = new Vector3(0f, -0.5f, 0f);
            JiggleRigPlan plan = DynamicBoneToJiggleMapper.Map(straight)[0];
            Assert.That(plan.Parameters.Gravity.Value.Value, Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(plan.Diagnostics.HasCode("dynamicbone.gravity"), Is.True);

            DynamicBoneData sideways = ReadBone();
            sideways.Gravity = new Vector3(0.4f, -0.5f, 0f);
            JiggleRigPlan skewed = DynamicBoneToJiggleMapper.Map(sideways)[0];
            Assert.That(skewed.Diagnostics.HasCode("dynamicbone.gravity.direction"), Is.True);
        }

        [Test]
        public void SettingsWithNoJiggleEquivalentAreReported()
        {
            DynamicBoneData data = ReadBone();
            data.Force = new Vector3(0f, 0f, 1f);
            data.FreezeAxis = DynamicBoneFreezeAxis.Y;
            data.Friction = new PhysBoneCurvedFloat(0.5f);
            data.BlendWeight = 0.5f;
            data.EndOffset = new Vector3(0f, 0.1f, 0f);

            List<ConversionDiagnostic> log = DynamicBoneToJiggleMapper.Map(data)[0].Diagnostics;

            foreach (string code in new[]
                     {
                         "dynamicbone.force.dropped",
                         "dynamicbone.freezeAxis.dropped",
                         "dynamicbone.friction.dropped",
                         "dynamicbone.blendWeight.dropped",
                         "dynamicbone.endpoint.dropped",
                     })
            {
                Assert.That(log.HasCode(code), Is.True, $"missing diagnostic {code}");
            }
        }

        [Test]
        public void ColliderShapeComesFromItsHeight()
        {
            DynamicBoneColliderData sphere = new DynamicBoneColliderData
            {
                Radius = 0.1f,
                Height = 0f,
            };
            Assert.That(DynamicBoneColliderToJiggleMapper.Map(sphere).Shape,
                Is.EqualTo(JiggleColliderShape.Sphere));

            DynamicBoneColliderData capsule = new DynamicBoneColliderData
            {
                Radius = 0.1f,
                Height = 0.5f,
                Direction = DynamicBoneColliderDirection.Z,
            };
            JiggleColliderPlan mapped = DynamicBoneColliderToJiggleMapper.Map(capsule);
            Assert.That(mapped.Shape, Is.EqualTo(JiggleColliderShape.Capsule));
            Assert.That(mapped.CapsuleAxis, Is.EqualTo(JiggleCapsuleAxis.Z));

            DynamicBoneColliderData plane = new DynamicBoneColliderData { IsPlane = true };
            Assert.That(DynamicBoneColliderToJiggleMapper.Map(plane).Shape,
                Is.EqualTo(JiggleColliderShape.Plane));
        }

        [Test]
        public void ATaperedCapsuleIsReported()
        {
            DynamicBoneColliderData tapered = new DynamicBoneColliderData
            {
                Radius = 0.1f,
                Height = 0.5f,
                Radius2 = 0.02f,
            };

            JiggleColliderPlan plan = DynamicBoneColliderToJiggleMapper.Map(tapered);

            Assert.That(plan.Diagnostics.HasCode("collider.taper.dropped"), Is.True);
            Assert.That(plan.Radius, Is.EqualTo(0.1f).Within(1e-6f));
        }
    }
}
