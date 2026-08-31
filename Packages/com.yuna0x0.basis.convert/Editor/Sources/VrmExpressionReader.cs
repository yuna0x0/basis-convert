using System.Collections.Generic;
using UnityEditor;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Sources
{
    /// <summary>
    /// Reads a VRM avatar's expressions.
    /// <para>
    /// Both formats keep them in assets rather than on the avatar: VRM 0.x has a blend shape
    /// avatar asset listing clip assets, and VRM 1.0 has a VRM10Object asset holding one
    /// expression per preset plus a list of custom ones. So the components on the prefab are
    /// followed off disk, the same way an expression menu's submenus are.
    /// </para>
    /// </summary>
    public static class VrmExpressionReader
    {
        /// <summary>
        /// VRM 1.0 weights run from 0 to 1, and Unity's blendshape weights from 0 to 100.
        /// UniVRM's own constant for this is `MorphTargetBinding.VRM_TO_UNITY`.
        /// </summary>
        private const float Vrm10WeightToUnity = 100f;

        /// <summary>
        /// The presets of VRM 0.x, in the order its `BlendShapePreset` enum declares them, which
        /// is what the asset stores.
        /// </summary>
        private static readonly VrmExpressionRole[] Vrm0Roles =
        {
            VrmExpressionRole.Custom,   // Unknown, named by the author instead
            VrmExpressionRole.Neutral,
            VrmExpressionRole.Viseme,   // A
            VrmExpressionRole.Viseme,   // I
            VrmExpressionRole.Viseme,   // U
            VrmExpressionRole.Viseme,   // E
            VrmExpressionRole.Viseme,   // O
            VrmExpressionRole.Blink,
            VrmExpressionRole.Emotion,  // Joy
            VrmExpressionRole.Emotion,  // Angry
            VrmExpressionRole.Emotion,  // Sorrow
            VrmExpressionRole.Emotion,  // Fun
            VrmExpressionRole.LookAt,   // LookUp
            VrmExpressionRole.LookAt,   // LookDown
            VrmExpressionRole.LookAt,   // LookLeft
            VrmExpressionRole.LookAt,   // LookRight
            VrmExpressionRole.Blink,    // Blink_L
            VrmExpressionRole.Blink,    // Blink_R
        };

        /// <summary>
        /// The named expression fields of VRM 1.0, in the order `VRM10ObjectExpression` declares
        /// them, with what each is for.
        /// </summary>
        private static readonly (string Field, VrmExpressionRole Role)[] Vrm10Presets =
        {
            ("Happy", VrmExpressionRole.Emotion),
            ("Angry", VrmExpressionRole.Emotion),
            ("Sad", VrmExpressionRole.Emotion),
            ("Relaxed", VrmExpressionRole.Emotion),
            ("Surprised", VrmExpressionRole.Emotion),
            ("Aa", VrmExpressionRole.Viseme),
            ("Ih", VrmExpressionRole.Viseme),
            ("Ou", VrmExpressionRole.Viseme),
            ("Ee", VrmExpressionRole.Viseme),
            ("Oh", VrmExpressionRole.Viseme),
            ("Blink", VrmExpressionRole.Blink),
            ("BlinkLeft", VrmExpressionRole.Blink),
            ("BlinkRight", VrmExpressionRole.Blink),
            ("LookUp", VrmExpressionRole.LookAt),
            ("LookDown", VrmExpressionRole.LookAt),
            ("LookLeft", VrmExpressionRole.LookAt),
            ("LookRight", VrmExpressionRole.LookAt),
            ("Neutral", VrmExpressionRole.Neutral),
        };

        /// <summary>
        /// The expressions a VRM 0.x avatar declares, followed from its blend shape proxy.
        /// </summary>
        public static List<VrmExpressionData> ReadVrm0(UnityYamlDocument proxy)
        {
            List<VrmExpressionData> expressions = new List<VrmExpressionData>();

            if (proxy == null || !proxy.TryGetTopLevelObjectReference(
                    "BlendShapeAvatar", out string avatarGuid, out long avatarFileId))
            {
                return expressions;
            }

            UnityYamlDocument avatar = Load(avatarGuid, avatarFileId);
            if (avatar == null)
            {
                return expressions;
            }

            foreach ((string guid, long fileId) in ObjectReferences(avatar, "Clips"))
            {
                UnityYamlDocument clip = Load(guid, fileId);
                if (clip != null)
                {
                    expressions.Add(ReadVrm0Clip(clip));
                }
            }

            return expressions;
        }

        /// <summary>
        /// The expressions a VRM 1.0 avatar declares, followed from its instance component. The
        /// preset ones are named fields and the rest are a list, and both are references into
        /// the same object asset.
        /// </summary>
        public static List<VrmExpressionData> ReadVrm10(UnityYamlDocument instance)
        {
            List<VrmExpressionData> expressions = new List<VrmExpressionData>();

            if (instance == null || !instance.TryGetTopLevelObjectReference(
                    "Vrm", out string objectGuid, out long objectFileId))
            {
                return expressions;
            }

            UnityYamlDocument vrm = Load(objectGuid, objectFileId);
            if (vrm == null || !vrm.TryGetTopLevelBlock("Expression", out List<string> block))
            {
                return expressions;
            }

            foreach ((string Field, VrmExpressionRole Role) preset in Vrm10Presets)
            {
                if (!TryReadReference(block, preset.Field, objectGuid,
                        out string guid, out long fileId))
                {
                    continue;
                }

                UnityYamlDocument clip = Load(guid, fileId);
                if (clip == null)
                {
                    continue;
                }

                VrmExpressionData expression = ReadVrm10Clip(clip);
                expression.Name = preset.Field;
                expression.Role = preset.Role;
                expressions.Add(expression);
            }

            foreach ((string guid, long fileId) in CustomClipReferences(block, objectGuid))
            {
                UnityYamlDocument clip = Load(guid, fileId);
                if (clip == null)
                {
                    continue;
                }

                VrmExpressionData expression = ReadVrm10Clip(clip);
                if (string.IsNullOrEmpty(expression.Name))
                {
                    expression.Name = "Expression";
                }

                expressions.Add(expression);
            }

            return expressions;
        }

        private static VrmExpressionData ReadVrm0Clip(UnityYamlDocument clip)
        {
            VrmExpressionData expression = new VrmExpressionData
            {
                Name = clip.GetTopLevelValue("BlendShapeName") ?? string.Empty,
            };

            if (clip.TryGetInt("Preset", out int preset)
                && preset >= 0 && preset < Vrm0Roles.Length)
            {
                expression.Role = Vrm0Roles[preset];
            }

            if (clip.TryGetBool("IsBinary", out bool binary))
            {
                expression.IsBinary = binary;
            }

            // A 0.x binding's weight is already on Unity's scale: UniVRM passes it straight to
            // SetBlendShapeWeight.
            ReadBindings(clip, "Values", 1f, expression);
            expression.MaterialBindingCount = CountEntries(clip, "MaterialValues");

            if (string.IsNullOrEmpty(expression.Name))
            {
                expression.Name = clip.GetTopLevelValue("m_Name") ?? "Expression";
            }

            return expression;
        }

        private static VrmExpressionData ReadVrm10Clip(UnityYamlDocument clip)
        {
            VrmExpressionData expression = new VrmExpressionData
            {
                Name = clip.GetTopLevelValue("m_Name") ?? string.Empty,
            };

            if (clip.TryGetBool("IsBinary", out bool binary))
            {
                expression.IsBinary = binary;
            }

            ReadBindings(clip, "MorphTargetBindings", Vrm10WeightToUnity, expression);
            expression.MaterialBindingCount = CountEntries(clip, "MaterialColorBindings")
                + CountEntries(clip, "MaterialUVBindings");

            return expression;
        }

        /// <summary>
        /// The morph target bindings of either format. Both write a sequence of maps holding a
        /// relative path, an index into the mesh's blendshapes, and a weight.
        /// </summary>
        private static void ReadBindings(
            UnityYamlDocument clip, string key, float weightScale, VrmExpressionData expression)
        {
            if (!clip.TryGetTopLevelBlock(key, out List<string> block))
            {
                return;
            }

            VrmMorphBinding current = null;

            foreach (string line in block)
            {
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("-"))
                {
                    current = new VrmMorphBinding();
                    expression.Bindings.Add(current);
                    trimmed = trimmed.Substring(1).TrimStart();
                }

                if (current == null)
                {
                    continue;
                }

                if (trimmed.StartsWith("RelativePath:"))
                {
                    current.Path = trimmed.Substring("RelativePath:".Length).Trim();
                }
                else if (trimmed.StartsWith("Index:")
                         && UnityYamlValues.TryParseInt(
                             trimmed.Substring("Index:".Length).Trim(), out int index))
                {
                    current.Index = index;
                }
                else if (trimmed.StartsWith("Weight:")
                         && UnityYamlValues.TryParseFloat(
                             trimmed.Substring("Weight:".Length).Trim(), out float weight))
                {
                    current.Weight = weight * weightScale;
                }
            }
        }

        private static int CountEntries(UnityYamlDocument clip, string key)
        {
            if (!clip.TryGetTopLevelBlock(key, out List<string> block))
            {
                return 0;
            }

            int count = 0;
            foreach (string line in block)
            {
                if (line.TrimStart().StartsWith("-"))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Object references in a top level sequence, as the pairs that identify an asset: its
        /// guid, and the fileID of the object within it for a sub-asset.
        /// </summary>
        private static IEnumerable<(string Guid, long FileId)> ObjectReferences(
            UnityYamlDocument document, string key)
        {
            List<(string, long)> references = new List<(string, long)>();
            if (!document.TryGetTopLevelBlock(key, out List<string> block))
            {
                return references;
            }

            foreach (string line in block)
            {
                if (TryParseReference(line, null, out string guid, out long fileId))
                {
                    references.Add((guid, fileId));
                }
            }

            return references;
        }

        private static IEnumerable<(string Guid, long FileId)> CustomClipReferences(
            List<string> block, string ownGuid)
        {
            List<(string, long)> references = new List<(string, long)>();
            bool inCustom = false;

            foreach (string line in block)
            {
                string trimmed = line.TrimStart();

                if (trimmed.StartsWith("CustomClips:"))
                {
                    inCustom = true;
                    continue;
                }

                if (!inCustom)
                {
                    continue;
                }

                if (!trimmed.StartsWith("-"))
                {
                    break;
                }

                if (TryParseReference(line, ownGuid, out string guid, out long fileId))
                {
                    references.Add((guid, fileId));
                }
            }

            return references;
        }

        private static bool TryReadReference(
            List<string> block, string key, string ownGuid, out string guid, out long fileId)
        {
            guid = null;
            fileId = 0L;

            foreach (string line in block)
            {
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith(key + ":"))
                {
                    return TryParseReference(line, ownGuid, out guid, out fileId);
                }
            }

            return false;
        }

        /// <summary>
        /// A reference with no guid of its own points inside the file it was written in, which
        /// is how VRM 1.0 stores its expressions: sub-assets of the object asset.
        /// </summary>
        private static bool TryParseReference(
            string line, string ownGuid, out string guid, out long fileId)
        {
            guid = null;
            fileId = 0L;

            if (!UnityYamlValues.TryParseFileId(line, out fileId))
            {
                return false;
            }

            int at = line.IndexOf("guid:", System.StringComparison.Ordinal);
            if (at >= 0)
            {
                string rest = line.Substring(at + "guid:".Length).Trim();
                int end = rest.IndexOf(',');
                guid = (end >= 0 ? rest.Substring(0, end) : rest).Trim();
            }
            else
            {
                guid = ownGuid;
            }

            return !string.IsNullOrEmpty(guid);
        }

        /// <summary>One object out of an asset file, by the guid of the file and its fileID.</summary>
        private static UnityYamlDocument Load(string guid, long fileId)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return null;
            }

            UnityYamlDocument first = null;
            foreach (UnityYamlDocument document in UnityYamlScanner.ScanFile(path))
            {
                if (document.ClassId != UnityYamlScanner.ClassIdMonoBehaviour)
                {
                    continue;
                }

                if (document.FileId == fileId)
                {
                    return document;
                }

                first ??= document;
            }

            // A reference to the asset's main object names a fileID the file does not use as a
            // document id, so the only MonoBehaviour in it is the one meant.
            return first;
        }
    }
}
