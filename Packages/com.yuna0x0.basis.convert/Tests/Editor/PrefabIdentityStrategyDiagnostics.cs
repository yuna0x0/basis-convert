using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Sources;
using Object = UnityEngine.Object;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// Diagnostic, not a gate. Prints what each candidate strategy for tying a YAML document to
    /// a live object actually achieves on a real nested-prefab avatar, so the reader design is
    /// chosen from measurements rather than from assumptions about the AssetDatabase API.
    /// </summary>
    public class PrefabIdentityStrategyDiagnostics
    {
        private const string FixturePath =
            "Assets/yuna0x0/Avatars/Shinano/Prefab/Shinano.prefab";

        [Test]
        public void ReportResolutionStrategies()
        {
            if (!File.Exists(FixturePath))
            {
                Assert.Ignore($"Fixture not present at {FixturePath}.");
            }

            List<UnityYamlDocument> documents = UnityYamlScanner.ScanFile(FixturePath);

            // Strategy A: what LoadAllAssetsAtPath even returns.
            Object[] loaded = AssetDatabase.LoadAllAssetsAtPath(FixturePath);
            Dictionary<string, int> loadedTypes = new Dictionary<string, int>();
            int loadedNulls = 0;
            foreach (Object asset in loaded)
            {
                if (asset == null)
                {
                    loadedNulls++;
                    continue;
                }

                string typeName = asset.GetType().Name;
                loadedTypes.TryGetValue(typeName, out int n);
                loadedTypes[typeName] = n + 1;
            }

            TestContext.WriteLine($"[A] LoadAllAssetsAtPath returned {loaded.Length} entries, "
                + $"{loadedNulls} of them null.");
            foreach (KeyValuePair<string, int> pair in loadedTypes)
            {
                TestContext.WriteLine($"      {pair.Key}: {pair.Value}");
            }

            // Strategy B: traverse the live hierarchy off the loaded root instead.
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath);
            Assert.That(root, Is.Not.Null, "Fixture prefab failed to load.");

            Transform[] liveTransforms = root.GetComponentsInChildren<Transform>(true);
            TestContext.WriteLine($"[B] Live hierarchy has {liveTransforms.Length} transforms.");

            int withSource = 0;
            int sourceIdentified = 0;
            Dictionary<(string Guid, long FileId), List<GameObject>> bySourceIdentity =
                new Dictionary<(string, long), List<GameObject>>();

            foreach (Transform live in liveTransforms)
            {
                Object source = PrefabUtility.GetCorrespondingObjectFromSource(live.gameObject);
                if (source == null)
                {
                    continue;
                }

                withSource++;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        source, out string sourceGuid, out long sourceFileId))
                {
                    continue;
                }

                sourceIdentified++;
                (string, long) key = (sourceGuid, sourceFileId);
                if (!bySourceIdentity.TryGetValue(key, out List<GameObject> bucket))
                {
                    bucket = new List<GameObject>();
                    bySourceIdentity[key] = bucket;
                }

                bucket.Add(live.gameObject);
            }

            int ambiguousBuckets = 0;
            foreach (KeyValuePair<(string, long), List<GameObject>> pair in bySourceIdentity)
            {
                if (pair.Value.Count > 1)
                {
                    ambiguousBuckets++;
                }
            }

            TestContext.WriteLine($"[B] transforms with a corresponding source: {withSource}");
            TestContext.WriteLine($"[B] of those, source identity readable:     {sourceIdentified}");
            TestContext.WriteLine($"[B] distinct source identities:             {bySourceIdentity.Count}");
            TestContext.WriteLine($"[B] identities claimed by more than one live object: {ambiguousBuckets}");

            // How many stripped GameObject documents can be matched through that map.
            int strippedDocs = 0;
            int strippedMatched = 0;
            int strippedAmbiguous = 0;
            foreach (UnityYamlDocument document in documents)
            {
                if (document.ClassId != UnityYamlScanner.ClassIdGameObject || !document.Stripped)
                {
                    continue;
                }

                strippedDocs++;
                if (!document.TryGetTopLevelObjectReference(
                        "m_CorrespondingSourceObject", out string guid, out long fileId)
                    || guid == null)
                {
                    continue;
                }

                if (bySourceIdentity.TryGetValue((guid, fileId), out List<GameObject> matches))
                {
                    strippedMatched++;
                    if (matches.Count > 1)
                    {
                        strippedAmbiguous++;
                    }
                }
            }

            TestContext.WriteLine($"[B] stripped GameObject docs matched via corresponding source: "
                + $"{strippedMatched}/{strippedDocs} ({strippedAmbiguous} ambiguous)");

            // Strategy C: are missing-script components reachable, and do they carry file ids?
            int componentSlots = 0;
            int nullSlots = 0;
            int missingScriptObjects = 0;
            int missingScriptWithFileId = 0;

            foreach (Transform live in liveTransforms)
            {
                SerializedObject serialized = new SerializedObject(live.gameObject);
                SerializedProperty components = serialized.FindProperty("m_Component");
                if (components == null)
                {
                    continue;
                }

                for (int i = 0; i < components.arraySize; i++)
                {
                    componentSlots++;
                    SerializedProperty entry = components
                        .GetArrayElementAtIndex(i)
                        .FindPropertyRelative("component");
                    if (entry == null)
                    {
                        continue;
                    }

                    Object value = entry.objectReferenceValue;
                    if (value == null)
                    {
                        nullSlots++;
                        continue;
                    }

                    if (value is MonoBehaviour behaviour && MonoScriptIsMissing(behaviour))
                    {
                        missingScriptObjects++;
                        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                                value, out string _, out long _))
                        {
                            missingScriptWithFileId++;
                        }
                    }
                }

                serialized.Dispose();
            }

            TestContext.WriteLine($"[C] component slots walked: {componentSlots}, null slots: {nullSlots}");
            TestContext.WriteLine($"[C] reachable missing-script objects: {missingScriptObjects}, "
                + $"of which carry a local file id: {missingScriptWithFileId}");

            Assert.Pass("Diagnostic only. Read the output above.");
        }

        private static bool MonoScriptIsMissing(MonoBehaviour behaviour)
        {
            SerializedObject serialized = new SerializedObject(behaviour);
            SerializedProperty script = serialized.FindProperty("m_Script");
            bool missing = script != null && script.objectReferenceValue == null;
            serialized.Dispose();
            return missing;
        }
    }
}
