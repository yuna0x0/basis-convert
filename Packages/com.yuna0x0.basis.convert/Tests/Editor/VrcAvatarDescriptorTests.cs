using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using yuna0x0.Basis.Convert.Mapping;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Sources;

namespace yuna0x0.Basis.Convert.Tests
{
    public class VrcAvatarDescriptorTests
    {
        private const string FixturePath =
            "Assets/yuna0x0/Avatars/Shinano/Prefab/Shinano.prefab";

        private static readonly string[] VisemeNames =
        {
            "vrc.v_sil", "vrc.v_pp", "vrc.v_ff", "vrc.v_th", "vrc.v_dd",
            "vrc.v_kk", "vrc.v_ch", "vrc.v_ss", "vrc.v_nn", "vrc.v_rr",
            "vrc.v_aa", "vrc.v_e", "vrc.v_ih", "vrc.v_oh", "vrc.v_ou",
        };

        private static VrcAvatarDescriptorData ReadDescriptor(
            string viewPosition = "{x: 0, y: 1.208, z: 0.08}",
            int lipSync = 3,
            int eyelidType = 2,
            string eyelidsBlob = "1d000000ffffffffffffffff")
        {
            List<string> lines = new List<string>
            {
                "--- !u!114 &700",
                "MonoBehaviour:",
                "  m_GameObject: {fileID: 1}",
                $"  ViewPosition: {viewPosition}",
                $"  lipSync: {lipSync}",
                "  VisemeSkinnedMesh: {fileID: 55}",
                "  VisemeBlendShapes:",
            };

            foreach (string name in VisemeNames)
            {
                lines.Add($"  - {name}");
            }

            lines.Add("  enableEyeLook: 1");
            lines.Add("  customEyeLookSettings:");
            lines.Add("    eyeMovement:");
            lines.Add("      confidence: 0.5");
            lines.Add("    leftEye: {fileID: 11}");
            lines.Add("    rightEye: {fileID: 12}");
            lines.Add($"    eyelidType: {eyelidType}");
            lines.Add("    eyelidsSkinnedMesh: {fileID: 66}");
            lines.Add($"    eyelidsBlendshapes: {eyelidsBlob}");

            List<UnityYamlDocument> documents = UnityYamlScanner.Scan(lines);
            Assert.That(documents.Count, Is.EqualTo(1));
            return VrcAvatarDescriptorReader.Read(documents[0]);
        }

        [Test]
        public void ReadsTheDescriptorIncludingItsNestedEyeSettings()
        {
            VrcAvatarDescriptorData data = ReadDescriptor();

            Assert.That(data.OwnerGameObjectFileId, Is.EqualTo(1L));
            Assert.That(data.ViewPosition.y, Is.EqualTo(1.208f).Within(1e-5f));
            Assert.That(data.LipSync, Is.EqualTo(VrcLipSyncStyle.VisemeBlendShape));
            Assert.That(data.VisemeSkinnedMeshFileId, Is.EqualTo(55L));
            Assert.That(data.VisemeBlendShapes.Count, Is.EqualTo(15));
            Assert.That(data.VisemeBlendShapes[10], Is.EqualTo("vrc.v_aa"));
            Assert.That(data.EnableEyeLook, Is.True);
            Assert.That(data.EyelidType, Is.EqualTo(VrcEyelidType.Blendshapes));
            Assert.That(data.EyelidsSkinnedMeshFileId, Is.EqualTo(66L));
            Assert.That(data.LeftEyeFileId, Is.EqualTo(11L));
            Assert.That(data.RightEyeFileId, Is.EqualTo(12L));
        }

        [Test]
        public void EyelidIndicesAreDecodedFromTheHexBlob()
        {
            // Unity writes this small fixed array as bytes, not as a sequence. Reading it as a
            // string would silently lose the blink blendshape.
            Assert.That(UnityYamlValues.ParseHexInt32Blob("1d000000ffffffffffffffff"),
                Is.EqualTo(new[] { 29, -1, -1 }));
            Assert.That(UnityYamlValues.ParseHexInt32Blob("00000000"), Is.EqualTo(new[] { 0 }));
            Assert.That(UnityYamlValues.ParseHexInt32Blob(string.Empty), Is.Empty);

            VrcAvatarDescriptorData data = ReadDescriptor();
            Assert.That(data.EyelidsBlendshapes, Is.EqualTo(new[] { 29, -1, -1 }));
        }

        [Test]
        public void ViewPositionBecomesHeightAndForwardOffset()
        {
            BasisAvatarPlan plan = VrcAvatarDescriptorToBasisMapper.Map(ReadDescriptor());

            Assert.That(plan.EyePosition.x, Is.EqualTo(1.208f).Within(1e-5f));
            Assert.That(plan.EyePosition.y, Is.EqualTo(0.08f).Within(1e-5f));
        }

        [Test]
        public void ASidewaysViewOffsetIsReportedRatherThanSilentlyLost()
        {
            BasisAvatarPlan plan = VrcAvatarDescriptorToBasisMapper.Map(
                ReadDescriptor(viewPosition: "{x: 0.05, y: 1.2, z: 0.08}"));

            Assert.That(plan.Diagnostics.HasCode("descriptor.viewPosition.sideways"), Is.True);
        }

        [Test]
        public void VisemesMapPositionForPosition()
        {
            BasisAvatarPlan plan = VrcAvatarDescriptorToBasisMapper.Map(ReadDescriptor());

            Assert.That(plan.VisemeMeshFileId, Is.EqualTo(55L));
            Assert.That(plan.VisemeBlendShapeNames.Count, Is.EqualTo(15));
            Assert.That(plan.VisemeBlendShapeNames[0], Is.EqualTo("vrc.v_sil"));
            Assert.That(plan.VisemeBlendShapeNames[14], Is.EqualTo("vrc.v_ou"));
            Assert.That(plan.Diagnostics.HasCode("descriptor.visemes"), Is.True);
        }

        [Test]
        public void BlinkTakesTheFirstEyelidBlendshapeAndReportsTheRest()
        {
            BasisAvatarPlan plan = VrcAvatarDescriptorToBasisMapper.Map(ReadDescriptor());

            Assert.That(plan.BlinkMeshFileId, Is.EqualTo(66L));
            Assert.That(plan.BlinkBlendShapeIndex, Is.EqualTo(29));
            Assert.That(plan.Diagnostics.HasCode("descriptor.eyelids.lookUpDown"), Is.True);
        }

        [Test]
        public void LipSyncModesBasisCannotDriveAreReported()
        {
            foreach (int mode in new[] { 0, 1, 2, 4 })
            {
                BasisAvatarPlan plan = VrcAvatarDescriptorToBasisMapper.Map(
                    ReadDescriptor(lipSync: mode));

                Assert.That(plan.Diagnostics.HasCode("descriptor.lipSync.unsupported"), Is.True,
                    $"lipSync mode {mode} should be reported as unsupported.");
                Assert.That(plan.VisemeMeshFileId, Is.Zero);
            }
        }

        [Test]
        public void BoneDrivenEyelidsAreReported()
        {
            BasisAvatarPlan plan = VrcAvatarDescriptorToBasisMapper.Map(
                ReadDescriptor(eyelidType: 1));

            Assert.That(plan.Diagnostics.HasCode("descriptor.eyelids.bones"), Is.True);
            Assert.That(plan.BlinkBlendShapeIndex, Is.EqualTo(-1));
        }

        [Test]
        public void ReadsTheDescriptorInARealAvatar()
        {
            if (!File.Exists(FixturePath))
            {
                Assert.Ignore($"Fixture not present at {FixturePath}.");
            }

            int found = 0;
            foreach (UnityYamlDocument document in UnityYamlScanner.ScanFile(FixturePath))
            {
                if (document.ClassId != UnityYamlScanner.ClassIdMonoBehaviour
                    || !document.TryGetScriptIdentity(out string guid, out long scriptFileId)
                    || KnownScriptIdentities.Resolve(guid, scriptFileId)
                        != SourceComponentKind.VrcAvatarDescriptor)
                {
                    continue;
                }

                found++;
                VrcAvatarDescriptorData data = VrcAvatarDescriptorReader.Read(document);
                BasisAvatarPlan plan = VrcAvatarDescriptorToBasisMapper.Map(data);

                TestContext.WriteLine($"view position: {data.ViewPosition}");
                TestContext.WriteLine($"lip sync: {data.LipSync}");
                TestContext.WriteLine($"visemes listed: {data.VisemeBlendShapes.Count}");
                TestContext.WriteLine($"eyelid type: {data.EyelidType}");
                TestContext.WriteLine($"eyelid indices: "
                    + string.Join(", ", data.EyelidsBlendshapes));
                TestContext.WriteLine($"eye position: {plan.EyePosition}");
                TestContext.WriteLine($"blink index: {plan.BlinkBlendShapeIndex}");

                Assert.That(data.OwnerGameObjectFileId, Is.Not.Zero);
                Assert.That(plan.EyePosition.x, Is.GreaterThan(0f),
                    "An avatar's eyes should sit above its root.");
            }

            Assert.That(found, Is.EqualTo(1), "Expected exactly one avatar descriptor.");
        }
    }
}
