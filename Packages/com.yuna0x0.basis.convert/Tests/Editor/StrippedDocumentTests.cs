using System.Collections.Generic;
using NUnit.Framework;
using yuna0x0.Basis.Convert.Sources;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// A prefab built on another writes a "stripped" entry per component of the base it refers
    /// to, carrying the identity and no data. Reading one as a component gave a collider with
    /// nothing in it, and double counted it once the base was read: 16 colliders on an avatar
    /// that has 11, five of them called unresolved.
    /// </summary>
    public class StrippedDocumentTests
    {
        private const string ColliderScript =
            "  m_Script: {fileID: -1631200402, guid: 2a2c05204084d904aa4945ccff20d8e5, type: 3}";

        private static List<UnityYamlDocument> Scan(params string[] lines)
        {
            return UnityYamlScanner.Scan(lines);
        }

        [Test]
        public void AStrippedEntryIsRecognisedAsStripped()
        {
            List<UnityYamlDocument> documents = Scan(
                "--- !u!114 &602108953830830595 stripped",
                "MonoBehaviour:",
                "  m_CorrespondingSourceObject: {fileID: 7122368781892401609, guid: abc, type: 3}",
                "  m_PrefabInstance: {fileID: 7677689914496650186}",
                "  m_GameObject: {fileID: 0}",
                ColliderScript);

            Assert.That(documents.Count, Is.EqualTo(1));
            Assert.That(documents[0].Stripped, Is.True);
        }

        [Test]
        public void AnOrdinaryEntryIsNotStripped()
        {
            List<UnityYamlDocument> documents = Scan(
                "--- !u!114 &602108953830830595",
                "MonoBehaviour:",
                "  m_GameObject: {fileID: 777}",
                ColliderScript,
                "  radius: 0.05");

            Assert.That(documents.Count, Is.EqualTo(1));
            Assert.That(documents[0].Stripped, Is.False);
        }

        /// <summary>
        /// The identity is still readable on a stripped entry, which is why skipping has to be
        /// on <c>Stripped</c> rather than on failing to recognise the script.
        /// </summary>
        [Test]
        public void AStrippedEntryStillNamesItsScript()
        {
            UnityYamlDocument document = Scan(
                "--- !u!114 &1 stripped",
                "MonoBehaviour:",
                "  m_CorrespondingSourceObject: {fileID: 2, guid: abc, type: 3}",
                ColliderScript)[0];

            Assert.That(document.TryGetScriptIdentity(out string guid, out long fileId), Is.True);
            Assert.That(KnownScriptIdentities.Resolve(guid, fileId),
                Is.EqualTo(SourceComponentKind.VrcPhysBoneCollider));
        }
    }
}
