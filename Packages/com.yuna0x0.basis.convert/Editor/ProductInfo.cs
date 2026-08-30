namespace yuna0x0.Basis.Convert
{
    /// <summary>
    /// Names shown in the editor, in one place.
    /// <para>
    /// Menus live under our own product name rather than Basis's menu. Basis's trademark policy
    /// asks third parties not to imply affiliation, and the surrounding ecosystem does the same:
    /// NDMF uses Tools/NDM Framework, Modular Avatar uses Tools/Modular Avatar, and Unity's own
    /// guidance is to nest under an existing menu or under Tools rather than add a top-level one.
    /// </para>
    /// </summary>
    public static class ProductInfo
    {
        public const string Name = "Watari";

        /// <summary>What it is, for anywhere the name alone does not say enough.</summary>
        public const string Tagline = "Converter for Basis";

        public const string ToolsMenu = "Tools/" + Name + "/";
        public const string GameObjectMenu = "GameObject/" + Name + "/";
    }
}
