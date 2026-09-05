using NUnit.Framework;
using yuna0x0.Basis.Convert.Sources;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// Every component Modular Avatar 1.18.7 ships is named, so none of them comes back as an
    /// unknown script. The guids are the ones in its .meta files.
    /// </summary>
    public class ModularAvatarIdentityTests
    {
        [TestCase("660848d04d7443b5b6fcfb627e6be5ea", SourceComponentKind.MaVertexFilterByAxis)]
        [TestCase("f8e2c9a1b3d44c6d9a7e5f2c1b8d3e4f", SourceComponentKind.MaVertexFilterByBone)]
        [TestCase("96a7b00b1dae4a02b61b29bf02241063", SourceComponentKind.MaVertexFilterByMask)]
        [TestCase("da7788c69fae9ff4abae088a0dc92c5b", SourceComponentKind.MaVertexFilterByShape)]
        [TestCase("8c38d6a064dbe9b91f24ee30e85c3c4f", SourceComponentKind.MaVertexFilterByUVTile)]
        [TestCase("a8d5b07828ba4eefb9acc305478369d0", SourceComponentKind.MaMoveIndependently)]
        [TestCase("762726b8618cac7419e39bdc2b572b3d", SourceComponentKind.MaMeshCutter)]
        public void MeshAndHierarchyComponentsAreHandledByModularAvatar(
            string guid, SourceComponentKind expected)
        {
            SourceComponentKind kind =
                KnownScriptIdentities.Resolve(guid, KnownScriptIdentities.LooseScriptFileId);

            Assert.That(kind, Is.EqualTo(expected));
            Assert.That(KnownScriptIdentities.IsHandledByModularAvatar(kind), Is.True,
                "Mesh and hierarchy work is done by Modular Avatar itself on Basis.");
        }
    }
}
