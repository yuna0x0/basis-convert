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

        // Modular Avatar. Its hierarchy components do their job on Basis, so they are
        // identified in order to be left alone knowingly rather than reported as unknown. The
        // menu and animator ones target VRChat structures Basis does not have.
        MaMergeArmature,
        MaBoneProxy,
        MaMeshSettings,
        MaBlendshapeSync,
        MaMergeAnimator,
        MaMenuItem,
        MaMenuInstaller,
        MaParameters,
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

                // Modular Avatar, read off clothing prefabs in the reference library and
                // identified by their serialized fields rather than by a published list.
                { ("2df373bf91cf30b4bbd495e11cb1a2ec", LooseScriptFileId), SourceComponentKind.MaMergeArmature },
                { ("42581d8044b64899834d3d515ab3a144", LooseScriptFileId), SourceComponentKind.MaBoneProxy },
                { ("560fdafd46c74b2db6422fdf0e7f2363", LooseScriptFileId), SourceComponentKind.MaMeshSettings },
                { ("6fd7cab7d93b403280f2f9da978d8a4f", LooseScriptFileId), SourceComponentKind.MaBlendshapeSync },
                { ("1bb122659f724ebf85fe095ac02dc339", LooseScriptFileId), SourceComponentKind.MaMergeAnimator },
                { ("3b29d45007c5493d926d2cd45a489529", LooseScriptFileId), SourceComponentKind.MaMenuItem },
                { ("7ef83cb0c23d4d7c9d41021e544a1978", LooseScriptFileId), SourceComponentKind.MaMenuInstaller },
                { ("71a96d4ea0c344f39e277d82035bf9bd", LooseScriptFileId), SourceComponentKind.MaParameters },
            };

        /// <summary>
        /// True for the Modular Avatar components that do their job on Basis, which are the ones
        /// that only rearrange the hierarchy. Nothing needs converting for these.
        /// </summary>
        public static bool IsHandledByModularAvatar(SourceComponentKind kind)
        {
            return kind == SourceComponentKind.MaMergeArmature
                || kind == SourceComponentKind.MaBoneProxy
                || kind == SourceComponentKind.MaMeshSettings
                || kind == SourceComponentKind.MaBlendshapeSync
                || kind == SourceComponentKind.MaParameters;
        }

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
