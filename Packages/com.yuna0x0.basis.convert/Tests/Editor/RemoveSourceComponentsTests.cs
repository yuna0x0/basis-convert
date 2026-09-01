using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Pipeline;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// The components a conversion reads arrive as missing scripts, and Unity refuses to save a
    /// prefab holding one, so a converted avatar cannot be saved as a prefab until they go. The
    /// option removes them after writing. It is off by default and cannot be undone, so what
    /// matters here is that it does nothing unless asked.
    /// </summary>
    public class RemoveSourceComponentsTests
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

        private AvatarConversionPlan PlanAndSpawn(bool removeSource)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath);
            Assert.That(prefab, Is.Not.Null, $"Fixture missing at {FixturePath}.");

            _instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(_instance);
            plan.Options.RemoveSourceComponents = removeSource;
            return plan;
        }

        /// <summary>Counts components whose script is missing, which is how they arrive here.</summary>
        private static int MissingScripts(GameObject root)
        {
            int count = 0;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (Component c in t.GetComponents<Component>())
                {
                    if (c == null)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        [Test]
        public void TheOptionIsOffByDefault()
        {
            Assert.That(new ConversionOptions().RemoveSourceComponents, Is.False);
        }

        [Test]
        public void TheFixtureCarriesMissingScripts()
        {
            PlanAndSpawn(false);

            Assert.That(MissingScripts(_instance), Is.GreaterThan(0),
                "Without these the rest of the file tests nothing.");
        }

        [Test]
        public void ConvertingLeavesThemAloneByDefault()
        {
            AvatarConversionPlan plan = PlanAndSpawn(false);
            int before = MissingScripts(_instance);

            ConversionResult result = AvatarConverter.Apply(plan, _instance);

            Assert.That(MissingScripts(_instance), Is.EqualTo(before));
            Assert.That(result.SourceComponentsRemoved, Is.Zero);
            Assert.That(result.Diagnostics.HasCode("apply.sourceRemoved"), Is.False);
        }

        [Test]
        public void TheOptionRemovesThemAndSaysSo()
        {
            AvatarConversionPlan plan = PlanAndSpawn(true);
            int before = MissingScripts(_instance);
            Assert.That(before, Is.GreaterThan(0));

            ConversionResult result = AvatarConverter.Apply(plan, _instance);

            Assert.That(MissingScripts(_instance), Is.Zero,
                "The point of the option is that a prefab can be saved afterwards.");
            Assert.That(result.SourceComponentsRemoved, Is.EqualTo(before));
            Assert.That(result.Diagnostics.HasCode("apply.sourceRemoved"), Is.True,
                "Removing source data is not something to do quietly.");
        }

        /// <summary>
        /// The conversion still has to happen. Removing the source afterwards must not take the
        /// written components with it.
        /// </summary>
        [Test]
        public void TheConversionStillWrites()
        {
            AvatarConversionPlan plan = PlanAndSpawn(true);

            ConversionResult result = AvatarConverter.Apply(plan, _instance);

            Assert.That(result.TotalWritten, Is.GreaterThan(0));
            Assert.That(result.RigsWritten, Is.EqualTo(plan.SelectedRigCount));
        }
    }
}
