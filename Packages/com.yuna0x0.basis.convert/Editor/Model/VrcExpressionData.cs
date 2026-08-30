using System.Collections.Generic;

namespace yuna0x0.Basis.Convert.Model
{
    /// <summary>VRChat's expression menu control kinds.</summary>
    public enum VrcExpressionControlType
    {
        Button = 101,
        Toggle = 102,
        SubMenu = 103,
        TwoAxisPuppet = 201,
        FourAxisPuppet = 202,
        RadialPuppet = 203,
    }

    public enum VrcExpressionParameterType
    {
        Int = 0,
        Float = 1,
        Bool = 2,
    }

    public sealed class VrcExpressionControl
    {
        public string Name = string.Empty;
        public VrcExpressionControlType Type = VrcExpressionControlType.Button;

        /// <summary>Parameter this control drives. Empty on a submenu.</summary>
        public string Parameter = string.Empty;

        public float Value = 1f;

        /// <summary>Asset guid of the submenu, for a SubMenu control.</summary>
        public string SubMenuGuid;

        public bool HasIcon;
    }

    public sealed class VrcExpressionMenu
    {
        public string Name = string.Empty;
        public string Guid = string.Empty;
        public List<VrcExpressionControl> Controls = new List<VrcExpressionControl>();
    }

    public sealed class VrcExpressionParameter
    {
        public string Name = string.Empty;
        public VrcExpressionParameterType Type = VrcExpressionParameterType.Int;
        public bool Saved;
        public float DefaultValue;
        public bool NetworkSynced = true;
    }

    /// <summary>
    /// An avatar's whole expression menu tree and its parameters, flattened.
    /// <para>
    /// Basis has neither: no menu format and no synced parameter list. This is read so a
    /// conversion can describe what is there and what rebuilding it in HVR Vixxy involves,
    /// rather than reporting only that a menu exists.
    /// </para>
    /// </summary>
    public sealed class VrcExpressionInventory
    {
        public List<VrcExpressionMenu> Menus = new List<VrcExpressionMenu>();
        public List<VrcExpressionParameter> Parameters = new List<VrcExpressionParameter>();

        public int ControlCount
        {
            get
            {
                int count = 0;
                foreach (VrcExpressionMenu menu in Menus)
                {
                    count += menu.Controls.Count;
                }

                return count;
            }
        }

        public int CountOf(VrcExpressionControlType type)
        {
            int count = 0;
            foreach (VrcExpressionMenu menu in Menus)
            {
                foreach (VrcExpressionControl control in menu.Controls)
                {
                    if (control.Type == type)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
