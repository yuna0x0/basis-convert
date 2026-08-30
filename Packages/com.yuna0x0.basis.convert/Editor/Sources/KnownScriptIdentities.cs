using System.Collections.Generic;

namespace yuna0x0.Basis.Convert.Sources
{
    public enum SourceComponentKind
    {
        Unknown = 0,
        VrcPhysBone,
        VrcPhysBoneCollider,
        VrcAvatarDescriptor,
        VrcPositionConstraint,
        VrcRotationConstraint,
        VrcAimConstraint,
        VrcParentConstraint,
        VrcScaleConstraint,
        VrcLookAtConstraint,
        VrcExpressionsMenu,
        VrcExpressionParameters,
        VrcPipelineManager,
        VrcContactReceiver,
        VrcContactSender,
        DynamicBone,
        DynamicBoneCollider,
        DynamicBonePlaneCollider,
    }

    /// <summary>
    /// Maps a MonoBehaviour's (script guid, script fileId) pair onto the component type it was,
    /// so components whose script is missing can still be identified.
    /// <para>
    /// Two shapes exist. A loose .cs script always has fileId 11500000 and is identified by its
    /// own guid. A type compiled into a DLL, which is how the VRChat SDK ships, is identified by
    /// the assembly's guid plus a fileId derived from the class name, so one guid covers many
    /// types.
    /// </para>
    /// <para>
    /// Values below were read off real assets written by VRChat SDK 3.10.3. Guids are stable in
    /// practice but are not a contract, which is why an unrecognised identity is reported rather
    /// than skipped: that is how this table is meant to grow.
    /// </para>
    /// </summary>
    public static class KnownScriptIdentities
    {
        public const long LooseScriptFileId = 11500000L;

        public const string GuidVrcPhysBoneAssembly = "2a2c05204084d904aa4945ccff20d8e5";
        public const string GuidVrcConstraintAssembly = "58e2f01a24261a14cb82e6d3399e8b16";
        public const string GuidVrcSdk3AAssembly = "67cc4cb7839cd3741b63733d5adf0442";
        public const string GuidVrcCoreEditorAssembly = "4ecd63eff847044b68db9453ce219299";
        public const string GuidVrcContactAssembly = "80f1b8067b0760e4bb45023bc2e9de66";

        private static readonly Dictionary<(string Guid, long FileId), SourceComponentKind> Table =
            new Dictionary<(string, long), SourceComponentKind>
            {
                { (GuidVrcPhysBoneAssembly, 1661641543L), SourceComponentKind.VrcPhysBone },
                { (GuidVrcPhysBoneAssembly, -1631200402L), SourceComponentKind.VrcPhysBoneCollider },

                { (GuidVrcConstraintAssembly, 1116338486L), SourceComponentKind.VrcPositionConstraint },
                { (GuidVrcConstraintAssembly, 1788371120L), SourceComponentKind.VrcRotationConstraint },
                { (GuidVrcConstraintAssembly, -926596935L), SourceComponentKind.VrcAimConstraint },

                // Computed rather than observed: the reference avatars do not use these three.
                // Unity derives a DLL type's fileID from "s\0\0\0" + namespace + name, hashed
                // with MD4, taking the first four bytes little-endian. Reproducing that for the
                // ten identities above, all of which were read off real assets, gives exactly the
                // values listed, so the same derivation is trusted for these.
                { (GuidVrcConstraintAssembly, 575728033L), SourceComponentKind.VrcParentConstraint },
                { (GuidVrcConstraintAssembly, 41250163L), SourceComponentKind.VrcScaleConstraint },
                { (GuidVrcConstraintAssembly, -372946275L), SourceComponentKind.VrcLookAtConstraint },

                { (GuidVrcSdk3AAssembly, 542108242L), SourceComponentKind.VrcAvatarDescriptor },
                { (GuidVrcSdk3AAssembly, -340790334L), SourceComponentKind.VrcExpressionsMenu },
                { (GuidVrcSdk3AAssembly, -1506855854L), SourceComponentKind.VrcExpressionParameters },

                { (GuidVrcCoreEditorAssembly, -1427037861L), SourceComponentKind.VrcPipelineManager },

                { (GuidVrcContactAssembly, -1450912254L), SourceComponentKind.VrcContactReceiver },
                { (GuidVrcContactAssembly, -802764141L), SourceComponentKind.VrcContactSender },

                { ("f9ac8d30c6a0d9642a11e5be4c440740", LooseScriptFileId), SourceComponentKind.DynamicBone },
                { ("baedd976e12657241bf7ff2d1c685342", LooseScriptFileId), SourceComponentKind.DynamicBoneCollider },
                { ("4e535bdf3689369408cc4d078260ef6a", LooseScriptFileId), SourceComponentKind.DynamicBonePlaneCollider },
            };

        public static SourceComponentKind Resolve(string guid, long fileId)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return SourceComponentKind.Unknown;
            }

            return Table.TryGetValue((guid, fileId), out SourceComponentKind kind)
                ? kind
                : SourceComponentKind.Unknown;
        }
    }
}
