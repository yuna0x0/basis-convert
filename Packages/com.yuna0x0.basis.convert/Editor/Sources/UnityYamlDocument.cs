using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace yuna0x0.Basis.Convert.Sources
{
    /// <summary>
    /// One "--- !u!&lt;classId&gt; &amp;&lt;fileId&gt;" document out of a Unity YAML asset file.
    /// </summary>
    public sealed class UnityYamlDocument
    {
        public int ClassId;
        public long FileId;
        public bool Stripped;

        /// <summary>Body lines, excluding the header and the type line that follows it.</summary>
        public List<string> Lines = new List<string>();

        /// <summary>Type line, for example "MonoBehaviour:" or "GameObject:".</summary>
        public string TypeName = string.Empty;

        private static readonly Regex ScalarPattern = new Regex(
            @"^\s*(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*(?<value>.*?)\s*$",
            RegexOptions.Compiled);

        private static readonly Regex FileIdPattern = new Regex(
            @"fileID:\s*(?<id>-?\d+)",
            RegexOptions.Compiled);

        private static readonly Regex GuidPattern = new Regex(
            @"guid:\s*(?<guid>[0-9a-fA-F]{32})",
            RegexOptions.Compiled);

        /// <summary>
        /// Raw text of a top-level key, or null. Only looks at indent level 2, so nested keys of
        /// the same name do not shadow the one being asked for.
        /// </summary>
        public string GetTopLevelValue(string key)
        {
            foreach (string line in Lines)
            {
                if (IndentOf(line) != 2)
                {
                    continue;
                }

                Match match = ScalarPattern.Match(line);
                if (match.Success && match.Groups["key"].Value == key)
                {
                    return match.Groups["value"].Value;
                }
            }

            return null;
        }

        public bool TryGetTopLevelFileIdReference(string key, out long fileId)
        {
            fileId = 0;
            string raw = GetTopLevelValue(key);
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            Match match = FileIdPattern.Match(raw);
            return match.Success
                && long.TryParse(match.Groups["id"].Value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out fileId);
        }

        /// <summary>
        /// A top-level object reference such as {fileID: 123, guid: abc..., type: 3}. The guid
        /// is null for a reference inside the same file.
        /// </summary>
        public bool TryGetTopLevelObjectReference(string key, out string guid, out long fileId)
        {
            guid = null;
            fileId = 0;

            string raw = GetTopLevelValue(key);
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            Match guidMatch = GuidPattern.Match(raw);
            if (guidMatch.Success)
            {
                guid = guidMatch.Groups["guid"].Value;
            }

            Match fileIdMatch = FileIdPattern.Match(raw);
            return fileIdMatch.Success
                && long.TryParse(fileIdMatch.Groups["id"].Value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out fileId);
        }

        /// <summary>
        /// The (guid, fileId) pair identifying the script behind a MonoBehaviour document.
        /// For a loose .cs script the fileId is 11500000 and the guid identifies the script;
        /// for a type inside a DLL the fileId is a hash of the class name and the guid
        /// identifies the assembly.
        /// </summary>
        public bool TryGetScriptIdentity(out string guid, out long fileId)
        {
            guid = null;
            fileId = 0;

            string raw = GetTopLevelValue("m_Script");
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            Match guidMatch = GuidPattern.Match(raw);
            Match fileIdMatch = FileIdPattern.Match(raw);
            if (!guidMatch.Success || !fileIdMatch.Success)
            {
                return false;
            }

            guid = guidMatch.Groups["guid"].Value;
            return long.TryParse(fileIdMatch.Groups["id"].Value, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out fileId);
        }

        private static int IndentOf(string line)
        {
            int i = 0;
            while (i < line.Length && line[i] == ' ')
            {
                i++;
            }

            return i;
        }
    }
}
