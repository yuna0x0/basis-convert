using System.Collections.Generic;

namespace yuna0x0.Basis.Convert.Sources
{
    /// <summary>An animator controller Modular Avatar merges into the avatar at build time.</summary>
    public sealed class MaMergeAnimatorData
    {
        public long OwnerGameObjectFileId;

        /// <summary>Asset guid of the controller. Native Unity asset, so it is loaded, not parsed.</summary>
        public string ControllerGuid = string.Empty;

        /// <summary>VRChat's animation layer slot, where 5 is FX. Same numbering as the descriptor.</summary>
        public int LayerType;

        /// <summary>
        /// 0 addresses objects relative to the component's own object, 1 relative to the avatar
        /// root. Read off a real gimmick pack: its clips animate `Melody` and `Key/Armature`,
        /// which are children of the object holding the component, with this set to 0.
        /// </summary>
        public int PathMode;
    }

    /// <summary>A menu entry Modular Avatar installs into the avatar's expression menu.</summary>
    public sealed class MaMenuItemData
    {
        public long OwnerGameObjectFileId;

        /// <summary>Label. Empty means the object's own name is used.</summary>
        public string Name = string.Empty;

        /// <summary>VRChat's control type: 101 button, 102 toggle, 103 submenu, 104 and up puppets.</summary>
        public int ControlType;

        public string Parameter = string.Empty;

        public bool IsToggle => ControlType == ControlTypeToggle;

        public const int ControlTypeToggle = 102;
    }

    /// <summary>
    /// Reads the Modular Avatar components a conversion cares about.
    /// <para>
    /// Modular Avatar ships loose scripts, so its components are identified by guid alone and
    /// their fields are read from YAML like every other source component. That way the package is
    /// never a dependency, and clothing reads the same whether or not Modular Avatar is installed
    /// in the project being converted into.
    /// </para>
    /// </summary>
    public static class ModularAvatarDocumentReader
    {
        public static MaMergeAnimatorData ReadMergeAnimator(UnityYamlDocument document)
        {
            MaMergeAnimatorData data = new MaMergeAnimatorData();

            if (document.TryGetTopLevelFileIdReference("m_GameObject", out long owner))
            {
                data.OwnerGameObjectFileId = owner;
            }

            if (document.TryGetTopLevelObjectReference("animator", out string guid, out long _))
            {
                data.ControllerGuid = guid;
            }

            if (document.TryGetInt("layerType", out int layerType))
            {
                data.LayerType = layerType;
            }

            if (document.TryGetInt("pathMode", out int pathMode))
            {
                data.PathMode = pathMode;
            }

            return data;
        }

        public static MaMenuItemData ReadMenuItem(UnityYamlDocument document)
        {
            MaMenuItemData data = new MaMenuItemData();

            if (document.TryGetTopLevelFileIdReference("m_GameObject", out long owner))
            {
                data.OwnerGameObjectFileId = owner;
            }

            if (!document.TryGetTopLevelBlock("Control", out List<string> control))
            {
                return data;
            }

            bool inParameter = false;

            foreach (string line in control)
            {
                int indent = IndentOf(line);
                string trimmed = line.Trim();

                if (indent == 4)
                {
                    inParameter = trimmed.StartsWith("parameter:");

                    if (trimmed.StartsWith("name:"))
                    {
                        data.Name = ValueOf(trimmed);
                    }
                    else if (trimmed.StartsWith("type:")
                             && int.TryParse(ValueOf(trimmed), out int type))
                    {
                        data.ControlType = type;
                    }

                    continue;
                }

                // The control's parameter is a block of its own holding a single name.
                if (inParameter && indent == 6 && trimmed.StartsWith("name:"))
                {
                    data.Parameter = ValueOf(trimmed);
                }
            }

            return data;
        }

        private static string ValueOf(string trimmed)
        {
            int colon = trimmed.IndexOf(':');
            return colon < 0 ? string.Empty : trimmed.Substring(colon + 1).Trim();
        }

        private static int IndentOf(string line)
        {
            int indent = 0;
            while (indent < line.Length && line[indent] == ' ')
            {
                indent++;
            }

            return indent;
        }
    }
}
