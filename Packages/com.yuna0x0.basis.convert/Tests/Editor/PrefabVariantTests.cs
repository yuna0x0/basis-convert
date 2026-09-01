using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Pipeline;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// A prefab variant stores only what it overrides. Everything it inherits, physics included,
    /// stays in the base prefab's file, which a conversion reads nothing from. That cannot be
    /// silent: an avatar sold as a variant would otherwise convert with none of its physics and
    /// nothing in the report to say so.
    /// <para>
    /// The base is built here rather than taken from `SampleAvatar`, because that fixture carries
    /// the missing scripts a VRChat avatar arrives as and Unity refuses to save a prefab holding
    /// one. What is under test is whether a variant is recognised, which needs no components.
    /// </para>
    /// </summary>
    public class PrefabVariantTests
    {
        private const string PlainFixturePath =
            "Packages/com.yuna0x0.basis.convert/Tests/Editor/Fixtures/SampleAvatar/SampleAvatar.prefab";

        private const string Folder = "Assets/WatariVariantTests";

        private string _basePath;
        private string _variantPath;

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets", "WatariVariantTests");
            }

            GameObject root = new GameObject("VariantTestBase");
            new GameObject("Child").transform.SetParent(root.transform, false);

            _basePath = Path.Combine(Folder, "VariantTestBase.prefab");
            GameObject baseAsset = PrefabUtility.SaveAsPrefabAsset(root, _basePath);
            Object.DestroyImmediate(root);

            // Saving an instance of a prefab under a new path is what makes a variant of it.
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(baseAsset);
            _variantPath = Path.Combine(Folder, "VariantTestVariant.prefab");
            PrefabUtility.SaveAsPrefabAsset(instance, _variantPath);
            Object.DestroyImmediate(instance);
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.DeleteAsset(Folder);
            }
        }

        [Test]
        public void AVariantNamesTheBaseItInheritsFrom()
        {
            ConversionSource source = ConversionSource.ForAsset(_variantPath);

            Assert.That(source.BaseAssetPath(), Is.EqualTo(_basePath));
        }

        [Test]
        public void APlainPrefabNamesNoBase()
        {
            ConversionSource source = ConversionSource.ForAsset(_basePath);

            Assert.That(source.BaseAssetPath(), Is.Null);
        }

        [Test]
        public void AVariantIsReportedBecauseItsInheritedComponentsAreNotRead()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(_variantPath);

            Assert.That(plan.Diagnostics.HasCode("source.prefabVariant"), Is.True,
                "A variant's inherited components are not read, so it has to be reported.");
        }

        /// <summary>
        /// An avatar prefab is normally made from an FBX, which Unity calls a variant of that
        /// model. There is nothing in a model but the imported hierarchy, so there is nothing to
        /// miss and nothing to report. Found by converting a real avatar, which reported itself.
        /// </summary>
        [Test]
        public void APrefabMadeFromAModelIsNotReportedAsAVariant()
        {
            string modelPath = ModelFixturePath();
            if (string.IsNullOrEmpty(modelPath))
            {
                Assert.Ignore("No model fixture in this project to build a prefab from.");
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            string path = Path.Combine(Folder, "FromModel.prefab");
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);

            ConversionSource source = ConversionSource.ForAsset(path);

            Assert.That(PrefabUtility.GetPrefabAssetType(
                    AssetDatabase.LoadAssetAtPath<GameObject>(path)),
                Is.EqualTo(PrefabAssetType.Variant),
                "Unity should call a prefab made from a model a variant; the point of the test "
                + "is that it is not reported as one.");

            Assert.That(source.BaseAssetPath(), Is.Null,
                "A model carries no components to miss, so this is not worth reporting.");
        }

        /// <summary>Any imported model in the project, since none ships with the package.</summary>
        private static string ModelFixturePath()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
            }

            return null;
        }

        [Test]
        public void APlainPrefabIsNotReportedAsAVariant()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(PlainFixturePath);

            Assert.That(plan.Diagnostics.HasCode("source.prefabVariant"), Is.False,
                "The fixture is a plain prefab, not a variant.");
        }
    }
}
