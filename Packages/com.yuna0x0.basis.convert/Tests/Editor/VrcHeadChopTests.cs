using System.Linq;
using Basis.Scripts.BasisSdk;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Mapping;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Pipeline;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// A VRCHeadChop names bones to scale away in first person; Basis has the same component.
    /// The sample avatar carries one with two bones, one of them VR only, alongside a raycast
    /// and a per-platform override that have nothing to become.
    /// </summary>
    public class VrcHeadChopTests
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
            }
        }

        [Test]
        public void TheFixtureHeadChopIsPlannedWithBothBones()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);

            Assert.That(plan.HeadChopsFound, Is.EqualTo(1));
            Assert.That(plan.HeadChops.Count, Is.EqualTo(1));

            PlannedHeadChop chop = plan.HeadChops[0];
            Assert.That(chop.SourceHost.name, Is.EqualTo("SampleAvatar"));
            Assert.That(chop.SourceTargets.Select(t => t.Transform.name),
                Is.EqualTo(new[] { "HairBone1", "Hair" }));
            Assert.That(chop.SourceTargets[0].Scale, Is.EqualTo(0f));
            Assert.That(chop.SourceTargets[1].Scale, Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(chop.Plan.Diagnostics.HasCode("headChop.condition.dropped"), Is.True,
                "The second bone was VR only.");
        }

        [Test]
        public void TheGlobalFactorMultipliesIntoEachBone()
        {
            BasisHeadChopPlan plan = VrcHeadChopToBasisMapper.Map(new VrcHeadChopData
            {
                GlobalScaleFactor = 0.5f,
                Bones =
                {
                    new VrcHeadChopBoneData { TransformFileId = 1, ScaleFactor = 0.5f },
                    new VrcHeadChopBoneData { TransformFileId = 2, ScaleFactor = 1f,
                        Condition = VrcHeadChopCondition.NonVrOnly },
                    new VrcHeadChopBoneData { TransformFileId = 0, ScaleFactor = 0f },
                },
            });

            Assert.That(plan.Targets.Count, Is.EqualTo(2), "A bone with no transform is skipped.");
            Assert.That(plan.Targets[0].Scale, Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(plan.Targets[1].Scale, Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(plan.Diagnostics.Count(d => d.Code == "headChop.condition.dropped"),
                Is.EqualTo(1));
        }

        [Test]
        public void ConvertingWritesABasisHeadChop()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath);
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            ConversionResult result = AvatarConverter.Apply(plan, _instance);

            Assert.That(result.HeadChopsWritten, Is.EqualTo(1));

            BasisHeadChop written = _instance.GetComponent<BasisHeadChop>();
            Assert.That(written, Is.Not.Null);
            Assert.That(written.Targets.Length, Is.EqualTo(2));
            Assert.That(written.Targets[0].Target.name, Is.EqualTo("HairBone1"));
            Assert.That(written.Targets[0].Scale, Is.EqualTo(0f));
            Assert.That(written.Targets[1].Target.name, Is.EqualTo("Hair"));
            Assert.That(written.Targets[1].Scale, Is.EqualTo(0.5f).Within(1e-6f));

            AvatarConverter.Apply(plan, _instance);
            Assert.That(_instance.GetComponents<BasisHeadChop>().Length, Is.EqualTo(1),
                "Converting twice rewrites the head chop rather than adding another.");
        }

        [Test]
        public void TurningOffTheDescriptorLeavesTheHeadChopOut()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);
            plan.Options.Descriptor = false;

            Assert.That(plan.SelectedHeadChopCount, Is.Zero);
            Assert.That(plan.TotalPlanned, Is.GreaterThan(plan.TotalSelected));
        }

        [Test]
        public void RaycastAndBuildSettingsAreNamedRatherThanUnknown()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);

            Assert.That(plan.RaycastsFound, Is.EqualTo(1));
            Assert.That(plan.VrcBuildSettingsFound, Is.EqualTo(1));
            Assert.That(plan.Diagnostics.HasCode("raycast.dropped"), Is.True);
            Assert.That(plan.Diagnostics.HasCode("vrchat.buildSettings"), Is.True);
            Assert.That(plan.Diagnostics.HasCode("source.unknownScript"), Is.False);
        }

        [Test]
        public void AGlobalColliderIsReported()
        {
            JiggleColliderPlan local = PhysBoneColliderToJiggleMapper.Map(new PhysBoneColliderData
            {
                Rotation = Quaternion.identity,
            });
            Assert.That(local.Diagnostics.HasCode("collider.global.dropped"), Is.False);

            JiggleColliderPlan global = PhysBoneColliderToJiggleMapper.Map(new PhysBoneColliderData
            {
                Rotation = Quaternion.identity,
                GlobalCollision = true,
            });
            Assert.That(global.Diagnostics.HasCode("collider.global.dropped"), Is.True);
        }
    }
}
