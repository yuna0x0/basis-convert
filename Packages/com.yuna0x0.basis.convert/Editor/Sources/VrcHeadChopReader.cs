using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Sources
{
    /// <summary>
    /// Turns a VRCHeadChop document into plain data. Its bones are a sequence of small objects,
    /// each a transform reference, a scale factor and a condition, which the generic document
    /// helpers do not cover.
    /// </summary>
    public static class VrcHeadChopReader
    {
        private static readonly Regex FieldPattern = new Regex(
            @"^\s*(?:-\s+)?(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*(?<value>.*?)\s*$",
            RegexOptions.Compiled);

        private static readonly Regex FileIdPattern = new Regex(
            @"fileID:\s*(?<id>-?\d+)", RegexOptions.Compiled);

        public static VrcHeadChopData Read(UnityYamlDocument document)
        {
            VrcHeadChopData data = new VrcHeadChopData
            {
                DocumentFileId = document.FileId,
            };

            document.TryGetTopLevelFileIdReference("m_GameObject", out data.OwnerGameObjectFileId);

            if (document.TryGetFloat("globalScaleFactor", out float global))
            {
                data.GlobalScaleFactor = global;
            }

            if (!document.TryGetTopLevelBlock("targetBones", out List<string> block))
            {
                return data;
            }

            VrcHeadChopBoneData current = null;
            foreach (string line in block)
            {
                Match match = FieldPattern.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                // A dash starts the next bone; the fields that follow it belong to that bone.
                if (line.TrimStart().StartsWith("-"))
                {
                    current = new VrcHeadChopBoneData();
                    data.Bones.Add(current);
                }

                if (current == null)
                {
                    continue;
                }

                string key = match.Groups["key"].Value;
                string value = match.Groups["value"].Value;

                switch (key)
                {
                    case "transform":
                    {
                        Match id = FileIdPattern.Match(value);
                        if (id.Success)
                        {
                            current.TransformFileId = long.Parse(id.Groups["id"].Value,
                                CultureInfo.InvariantCulture);
                        }

                        break;
                    }

                    case "scaleFactor":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                out float scale))
                        {
                            current.ScaleFactor = scale;
                        }

                        break;

                    case "applyCondition":
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                out int condition))
                        {
                            current.Condition = (VrcHeadChopCondition)condition;
                        }

                        break;
                }
            }

            return data;
        }
    }
}
