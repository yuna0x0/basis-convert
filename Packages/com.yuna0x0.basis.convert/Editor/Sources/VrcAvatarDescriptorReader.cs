using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Sources
{
    /// <summary>
    /// Turns a VRCAvatarDescriptor document into plain data.
    /// <para>
    /// Two shapes need care. The viseme blendshape names are a plain sequence of fifteen
    /// strings, and the eyelid blendshape indices are a hex byte blob rather than a sequence,
    /// which is how Unity serializes some small fixed arrays.
    /// </para>
    /// </summary>
    public static class VrcAvatarDescriptorReader
    {
        private static readonly Regex SequenceItemPattern = new Regex(
            @"^\s*-\s*(?<value>.*?)\s*$", RegexOptions.Compiled);

        private static readonly Regex NestedFieldPattern = new Regex(
            @"^\s*(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*(?<value>.*?)\s*$", RegexOptions.Compiled);

        public static VrcAvatarDescriptorData Read(UnityYamlDocument document)
        {
            VrcAvatarDescriptorData data = new VrcAvatarDescriptorData
            {
                DocumentFileId = document.FileId,
            };

            document.TryGetTopLevelFileIdReference("m_GameObject", out data.OwnerGameObjectFileId);
            document.TryGetTopLevelFileIdReference(
                "VisemeSkinnedMesh", out data.VisemeSkinnedMeshFileId);

            if (document.TryGetVector3("ViewPosition", out Vector3 view))
            {
                data.ViewPosition = view;
            }

            if (document.TryGetInt("lipSync", out int lipSync))
            {
                data.LipSync = (VrcLipSyncStyle)lipSync;
            }

            if (document.TryGetBool("enableEyeLook", out bool eyeLook))
            {
                data.EnableEyeLook = eyeLook;
            }

            data.VisemeBlendShapes = ReadStringSequence(document, "VisemeBlendShapes");
            ReadEyeLookSettings(document, data);

            return data;
        }

        private static List<string> ReadStringSequence(UnityYamlDocument document, string key)
        {
            List<string> values = new List<string>();
            if (!document.TryGetTopLevelBlock(key, out List<string> block))
            {
                return values;
            }

            foreach (string line in block)
            {
                Match match = SequenceItemPattern.Match(line);
                if (match.Success)
                {
                    values.Add(match.Groups["value"].Value);
                }
            }

            return values;
        }

        /// <summary>
        /// The eye look block is nested, and the fields wanted from it sit at different depths,
        /// so it is scanned for the handful of keys that matter rather than parsed in full.
        /// </summary>
        private static void ReadEyeLookSettings(
            UnityYamlDocument document, VrcAvatarDescriptorData data)
        {
            if (!document.TryGetTopLevelBlock("customEyeLookSettings", out List<string> block))
            {
                return;
            }

            foreach (string line in block)
            {
                Match match = NestedFieldPattern.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                string value = match.Groups["value"].Value;
                switch (match.Groups["key"].Value)
                {
                    case "eyelidType":
                        if (UnityYamlValues.TryParseInt(value, out int eyelidType))
                        {
                            data.EyelidType = (VrcEyelidType)eyelidType;
                        }

                        break;

                    case "eyelidsSkinnedMesh":
                        data.EyelidsSkinnedMeshFileId = FileIdIn(value);
                        break;

                    case "eyelidsBlendshapes":
                        data.EyelidsBlendshapes = UnityYamlValues.ParseHexInt32Blob(value);
                        break;

                    case "leftEye":
                        data.LeftEyeFileId = FileIdIn(value);
                        break;

                    case "rightEye":
                        data.RightEyeFileId = FileIdIn(value);
                        break;
                }
            }
        }

        private static long FileIdIn(string raw)
        {
            Match match = Regex.Match(raw, @"fileID:\s*(?<id>-?\d+)");
            return match.Success
                && long.TryParse(match.Groups["id"].Value, out long id)
                ? id
                : 0L;
        }
    }
}
