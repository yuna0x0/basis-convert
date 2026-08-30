using System.Collections.Generic;
using System.IO;
using Basis.Scripts.BasisSdk.Constraints;
using GatorDragonGames.JigglePhysics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Pipeline;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// Converting an avatar twice should replace the previous output rather than stack a second
    /// set on top of it, without needing anything stored on the avatar between runs.
    /// </summary>
    public class RepeatedConversionTests
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

        private GameObject Instantiate()
        {
            if (!File.Exists(FixturePath))
            {
                Assert.Ignore($"Fixture not present at {FixturePath}.");
            }

            _instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));
            return _instance;
        }

        [Test]
        public void NothingIsReplaceableOnAnAvatarThatWasNeverConverted()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);
            GameObject instance = Instantiate();

            Assert.That(AvatarConverter.FindReplaceable(plan, instance), Is.Empty);
        }

        [Test]
        public void AfterConvertingEverythingWrittenIsReplaceable()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);
            GameObject instance = Instantiate();

            ConversionResult result = AvatarConverter.Apply(plan, instance);

            List<Component> replaceable = AvatarConverter.FindReplaceable(plan, instance);
            Assert.That(replaceable.Count, Is.EqualTo(result.TotalWritten),
                "Everything the conversion wrote should be found again by the scoped lookup.");
        }

        [Test]
        public void ConvertingTwiceLeavesOneSetOfComponents()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);
            GameObject instance = Instantiate();

            ConversionResult first = AvatarConverter.Apply(plan, instance);
            AvatarConverter.RemoveReplaceable(plan, instance, "replace");
            ConversionResult second = AvatarConverter.Apply(plan, instance);

            Assert.That(second.TotalWritten, Is.EqualTo(first.TotalWritten));
            Assert.That(instance.GetComponentsInChildren<JiggleRig>(true).Length,
                Is.EqualTo(second.RigsWritten),
                "A second conversion should not stack another set of rigs.");
            Assert.That(instance.GetComponentsInChildren<BasisConstraintBase>(true).Length,
                Is.EqualTo(second.ConstraintsWritten));
        }

        [Test]
        public void ComponentsElsewhereOnTheAvatarAreLeftAlone()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);
            GameObject instance = Instantiate();

            AvatarConverter.Apply(plan, instance);

            // A rig added by hand on an object the conversion never writes to.
            GameObject mine = new GameObject("HandTuned");
            mine.transform.SetParent(instance.transform, false);
            mine.AddComponent<JiggleRig>();

            AvatarConverter.RemoveReplaceable(plan, instance, "replace");

            Assert.That(mine.GetComponent<JiggleRig>(), Is.Not.Null,
                "A rig outside the conversion's footprint must survive a replace.");
            Assert.That(instance.GetComponentsInChildren<JiggleRig>(true).Length, Is.EqualTo(1),
                "Only the hand-added rig should remain.");
        }

        [Test]
        public void RemovingIsUndoable()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);
            GameObject instance = Instantiate();

            AvatarConverter.Apply(plan, instance);
            int before = instance.GetComponentsInChildren<JiggleRig>(true).Length;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            AvatarConverter.RemoveReplaceable(plan, instance, "replace");
            Assert.That(instance.GetComponentsInChildren<JiggleRig>(true), Is.Empty);

            Undo.RevertAllDownToGroup(group);

            Assert.That(instance.GetComponentsInChildren<JiggleRig>(true).Length,
                Is.EqualTo(before), "Undo should bring the replaced components back.");
        }
    }
}
