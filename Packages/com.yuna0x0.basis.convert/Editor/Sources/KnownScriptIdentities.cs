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

        // UniVRM, both formats. VRM 0.x puts a group of chains on one component; VRM 1.0 puts
        // parameters on each bone and lists the chains on the avatar's own component. The guids
        // were read from UniVRM's .meta files and are unchanged across its last several
        // releases. See agent/research/vrm-spring-bones.md.
        VrmSpringBone,
        VrmSpringBoneColliderGroup,
        VrmBlendShapeProxy,
        Vrm10Instance,
        Vrm10SpringBoneJoint,
        Vrm10SpringBoneCollider,
        Vrm10SpringBoneColliderGroup,

        // Modular Avatar, all of it. Naming every component keeps them out of the unknown
        // script report, and lets each be reported for what it does on Basis. The GUIDs were
        // read from Modular Avatar's own .meta files, not derived.
        //
        // Rearranges the hierarchy or the meshes, which is platform-independent work that
        // Modular Avatar does on Basis as well as anywhere else.
        MaMergeArmature,
        MaBoneProxy,
        MaMeshSettings,
        MaBlendshapeSync,
        MaParameters,
        MaMoveTo,
        MaReplaceObject,
        MaScaleAdjuster,
        MaOutfitRoot,
        MaRemoveVertexColor,
        MaMeshCutter,
        MaFloorAdjuster,
        MaWorldScaleObject,
        MaPlatformFilter,
        MaConvertConstraints,

        // Builds menus or merges animator layers, which target structures only VRChat has.
        MaMenuItem,
        MaMenuInstaller,
        MaMenuGroup,
        MaMergeAnimator,
        MaMergeBlendTree,

        // Reacts to a menu item or an object's state by changing something else. Object Toggle
        // is rebuilt; the rest are reported.
        MaObjectToggle,
        MaShapeChanger,
        MaMaterialSetter,
        MaMaterialSwap,

        // Specific to VRChat's own systems, and inert on Basis.
        MaGlobalCollider,
        MaPBBlocker,
        MaVisibleHeadAccessory,
        MaWorldFixedObject,
        MaMmdLayerControl,
        MaSyncParameterSequence,
        MaRenameVRChatCollisionTags,
        MaVRChatSettings,
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

                // UniVRM, read from its own .meta files in the package cache.
                { ("00ea06e1753e16f4ca870c39c067c86b", LooseScriptFileId), SourceComponentKind.VrmSpringBone },
                { ("646b65a4a57afd34d8c4ed557efb46a5", LooseScriptFileId), SourceComponentKind.VrmSpringBoneColliderGroup },
                { ("5b678c1df50cfb547990db24a32856da", LooseScriptFileId), SourceComponentKind.VrmBlendShapeProxy },
                { ("bfba4ccd3f854e64f868ce83553071a9", LooseScriptFileId), SourceComponentKind.Vrm10Instance },
                { ("0a942e03b39600e41a1b161e958048f7", LooseScriptFileId), SourceComponentKind.Vrm10SpringBoneJoint },
                { ("35bfb658269b2af478e501de243deda6", LooseScriptFileId), SourceComponentKind.Vrm10SpringBoneCollider },
                { ("177ea458e237fee41b0902e3006c744b", LooseScriptFileId), SourceComponentKind.Vrm10SpringBoneColliderGroup },

                // Modular Avatar, read off clothing prefabs in the reference library and
                // identified by their serialized fields rather than by a published list.
                { ("2df373bf91cf30b4bbd495e11cb1a2ec", LooseScriptFileId), SourceComponentKind.MaMergeArmature },
                { ("42581d8044b64899834d3d515ab3a144", LooseScriptFileId), SourceComponentKind.MaBoneProxy },
                { ("560fdafd46c74b2db6422fdf0e7f2363", LooseScriptFileId), SourceComponentKind.MaMeshSettings },
                { ("6fd7cab7d93b403280f2f9da978d8a4f", LooseScriptFileId), SourceComponentKind.MaBlendshapeSync },
                { ("71a96d4ea0c344f39e277d82035bf9bd", LooseScriptFileId), SourceComponentKind.MaParameters },
                { ("4e6bb6a99e499d2489ccf296662fa3cd", LooseScriptFileId), SourceComponentKind.MaMoveTo },
                { ("7e949680c0864ee7b441d9b2c93b890b", LooseScriptFileId), SourceComponentKind.MaReplaceObject },
                { ("09a660aa9d4e47d992adcac5a05dd808", LooseScriptFileId), SourceComponentKind.MaScaleAdjuster },
                { ("1895bf16884f4064f8e9550e7493c205", LooseScriptFileId), SourceComponentKind.MaOutfitRoot },
                { ("dc5f8bfae24244aeaedcd6c2bb7264f9", LooseScriptFileId), SourceComponentKind.MaRemoveVertexColor },
                { ("762726b8618cac7419e39bdc2b572b3d", LooseScriptFileId), SourceComponentKind.MaMeshCutter },
                { ("ba18e6eae93342fd8774b3f3f132928a", LooseScriptFileId), SourceComponentKind.MaFloorAdjuster },
                { ("e113c01563a14226b5e863befe6fe769", LooseScriptFileId), SourceComponentKind.MaWorldScaleObject },
                { ("8c8a67d5c01849629fa90c3b2eded93f", LooseScriptFileId), SourceComponentKind.MaPlatformFilter },
                { ("e362b3df8a3d478c82bf5ffe18f622e6", LooseScriptFileId), SourceComponentKind.MaConvertConstraints },

                { ("3b29d45007c5493d926d2cd45a489529", LooseScriptFileId), SourceComponentKind.MaMenuItem },
                { ("7ef83cb0c23d4d7c9d41021e544a1978", LooseScriptFileId), SourceComponentKind.MaMenuInstaller },
                { ("97e46a47dd8a425eb4ce9411defe313d", LooseScriptFileId), SourceComponentKind.MaMenuGroup },
                { ("1bb122659f724ebf85fe095ac02dc339", LooseScriptFileId), SourceComponentKind.MaMergeAnimator },
                { ("229dd561ca024a6588e388160921a70f", LooseScriptFileId), SourceComponentKind.MaMergeBlendTree },

                { ("a162bb8ec7e24a5abcf457887f1df3fa", LooseScriptFileId), SourceComponentKind.MaObjectToggle },
                { ("2db441f589c3407bb6fb5f02ff8ab541", LooseScriptFileId), SourceComponentKind.MaShapeChanger },
                { ("0adf335711644e34b6c635e94ae61fa7", LooseScriptFileId), SourceComponentKind.MaMaterialSetter },
                { ("b259b73280ead4e4fbbdafc5e29175d1", LooseScriptFileId), SourceComponentKind.MaMaterialSwap },

                { ("49bb23f95a7baca4186efa68bc5891b6", LooseScriptFileId), SourceComponentKind.MaGlobalCollider },
                { ("a5bf908a199a4648845ebe2fd3b5a4bd", LooseScriptFileId), SourceComponentKind.MaPBBlocker },
                { ("33dac8cfeaeb4c399ddd90597f849f70", LooseScriptFileId), SourceComponentKind.MaVisibleHeadAccessory },
                { ("0e2d9f1d69e34b92a96e6cc162770fad", LooseScriptFileId), SourceComponentKind.MaWorldFixedObject },
                { ("d1d979d3cedd4ddd969f414e2ea04fb8", LooseScriptFileId), SourceComponentKind.MaMmdLayerControl },
                { ("934543afe4744213b5621aa13a67e3b4", LooseScriptFileId), SourceComponentKind.MaSyncParameterSequence },
                { ("04802bf95b218724a9f4b97003067857", LooseScriptFileId), SourceComponentKind.MaRenameVRChatCollisionTags },
                { ("89c938d7d8a741df99f2eda501b3a6fe", LooseScriptFileId), SourceComponentKind.MaVRChatSettings },
            };

        /// <summary>
        /// True for the Modular Avatar components that do their job on Basis, which are the ones
        /// that only rearrange the hierarchy. Nothing needs converting for these.
        /// </summary>
        public static bool IsHandledByModularAvatar(SourceComponentKind kind)
        {
            return kind >= SourceComponentKind.MaMergeArmature
                && kind <= SourceComponentKind.MaConvertConstraints;
        }

        /// <summary>
        /// Modular Avatar components that build menus or merge animator layers. Both target
        /// structures only VRChat has, so what they add does nothing on Basis unless it is
        /// rebuilt.
        /// </summary>
        public static bool IsModularAvatarMenuOrAnimator(SourceComponentKind kind)
        {
            return kind >= SourceComponentKind.MaMenuItem
                && kind <= SourceComponentKind.MaMergeBlendTree;
        }

        /// <summary>
        /// Modular Avatar components that react to a menu item or an object's state. Object
        /// Toggle is rebuilt as a Vixxy control; the others are reported.
        /// </summary>
        public static bool IsModularAvatarReactive(SourceComponentKind kind)
        {
            return kind >= SourceComponentKind.MaObjectToggle
                && kind <= SourceComponentKind.MaMaterialSwap;
        }

        /// <summary>
        /// Modular Avatar components tied to VRChat's own systems, which have nothing to act on
        /// under Basis.
        /// </summary>
        public static bool IsModularAvatarVrchatOnly(SourceComponentKind kind)
        {
            return kind >= SourceComponentKind.MaGlobalCollider
                && kind <= SourceComponentKind.MaVRChatSettings;
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
