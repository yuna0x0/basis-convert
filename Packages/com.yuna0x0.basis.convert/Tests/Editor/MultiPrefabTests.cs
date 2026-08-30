using System.Collections.Generic;
using GatorDragonGames.JigglePhysics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Pipeline;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// An avatar is rarely one prefab. Clothing, hair and accessories are prefabs of their own
    /// carrying their own physics, so a conversion has to read each of them and place what it
    /// finds where that prefab sits.
    /// <para>
    /// The fixture stands in for that shape: one prefab instance parented under another, which
    /// is exactly what dropping clothing onto an avatar produces.
    /// </para>
    /// </summary>
    public class MultiPrefabTests
    {
        private const string FixturePath =
            "Packages/com.yuna0x0.basis.convert/Tests/Editor/Fixtures/DynamicBoneChain.prefab";

        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject spawned in _spawned)
            {
                if (spawned != null)
                {
                    Object.DestroyImmediate(spawned);
                }
            }

            _spawned.Clear();
        }

        /// <summary>A root with a second copy of the fixture parented under it.</summary>
        private GameObject Assembled()
        {
            GameObject outer = Instantiate();
            GameObject inner = Instantiate();
            inner.transform.SetParent(outer.transform, false);
            return outer;
        }

        private GameObject Instantiate()
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));
            _spawned.Add(instance);
            return instance;
        }

        [Test]
        public void EveryPrefabInTheHierarchyIsFound()
        {
            GameObject assembled = Assembled();

            List<ConversionSource> sources = ConversionSourceDiscovery.Discover(assembled);

            Assert.That(sources.Count, Is.EqualTo(2),
                "The hierarchy itself, and the prefab parented under it.");
            Assert.That(sources[0].IsPrimary, Is.True);
            Assert.That(sources[0].PathInHierarchy, Is.Empty);
            Assert.That(sources[1].PathInHierarchy.Length, Is.EqualTo(1),
                "The second prefab sits one level down, and its path is how its contents are "
                + "found again at conversion time.");
        }

        [Test]
        public void ASingleUnassembledPrefabIsStillJustOneSource()
        {
            List<ConversionSource> sources = ConversionSourceDiscovery.Discover(Instantiate());

            Assert.That(sources.Count, Is.EqualTo(1));
            Assert.That(sources[0].IsPrimary, Is.True);
        }

        [Test]
        public void PhysicsIsReadFromEveryPrefab()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(Assembled());

            Assert.That(plan.DynamicBonesFound, Is.EqualTo(2),
                "One in each prefab. Reading only the outermost would find one.");
            Assert.That(plan.Rigs.Count, Is.EqualTo(2));
            Assert.That(plan.Rigs[0].Source, Is.Not.SameAs(plan.Rigs[1].Source),
                "Each rig records which prefab it came from, since its transforms are in that "
                + "prefab's space.");
        }

        [Test]
        public void TheConversionSaysWhatItReadFrom()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(Assembled());

            List<string> codes = new List<string>();
            foreach (Model.ConversionDiagnostic diagnostic in plan.Diagnostics)
            {
                codes.Add(diagnostic.Code);
            }

            Assert.That(codes, Contains.Item("source.severalPrefabs"));
        }

        [Test]
        public void RigsLandOnTheirOwnPrefabsBones()
        {
            GameObject assembled = Assembled();
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(assembled);

            ConversionResult result = AvatarConverter.Apply(plan, assembled);

            Assert.That(result.RigsWritten, Is.EqualTo(2));

            JiggleRig[] rigs = assembled.GetComponentsInChildren<JiggleRig>(true);
            Assert.That(rigs.Length, Is.EqualTo(2));

            Transform nested = assembled.transform.GetChild(assembled.transform.childCount - 1);
            Assert.That(nested.GetComponentsInChildren<JiggleRig>(true).Length, Is.EqualTo(1),
                "The rig read from the parented prefab belongs under that prefab, not on the "
                + "outer one's bones.");

            foreach (JiggleRig rig in rigs)
            {
                Assert.That(rig.GetJiggleRigData().rootBone, Is.Not.Null);
                Assert.That(rig.GetJiggleRigData().rootBone.IsChildOf(rig.transform),
                    "A rig's root bone belongs to the prefab the rig was read from.");
            }
        }

        [Test]
        public void ReadingOneAssetDirectlyStillWorks()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);

            Assert.That(plan.Sources.Count, Is.EqualTo(1));
            Assert.That(plan.Rigs.Count, Is.EqualTo(1));
            Assert.That(plan.Rigs[0].Source, Is.Not.Null);
        }
    }
}
