using System.Collections.Generic;
using UnityEngine;

namespace yuna0x0.Basis.Convert.Model
{
    /// <summary>How VRChat drives mouth movement.</summary>
    public enum VrcLipSyncStyle
    {
        Default = 0,
        JawFlapBone = 1,
        JawFlapBlendShape = 2,
        VisemeBlendShape = 3,
        VisemeParameterOnly = 4,
    }

    /// <summary>How VRChat drives eyelids.</summary>
    public enum VrcEyelidType
    {
        None = 0,
        Bones = 1,
        Blendshapes = 2,
    }

    /// <summary>Which playable layer a controller was assigned to.</summary>
    public enum VrcAnimationLayer
    {
        Base = 0,
        Additive = 1,
        Gesture = 2,
        Action = 3,
        FX = 4,
        Sitting = 5,
        TPose = 6,
        IKPose = 7,
    }

    /// <summary>One entry of baseAnimationLayers or specialAnimationLayers.</summary>
    public sealed class VrcAnimationLayerEntry
    {
        public VrcAnimationLayer Layer;

        /// <summary>False when the layer is left on VRChat's stock controller.</summary>
        public bool IsCustom;
    }

    /// <summary>
    /// The parts of a VRCAvatarDescriptor that have a counterpart in Basis. Expression menus,
    /// animation layers and collider configuration are deliberately not read here: they belong
    /// to systems Basis does not have, and pretending otherwise would produce a descriptor that
    /// looks complete and is not.
    /// </summary>
    public sealed class VrcAvatarDescriptorData
    {
        public long DocumentFileId;
        public long OwnerGameObjectFileId;

        /// <summary>Where the eyes sit, in avatar-local space.</summary>
        public Vector3 ViewPosition;

        public VrcLipSyncStyle LipSync = VrcLipSyncStyle.Default;
        public long VisemeSkinnedMeshFileId;

        /// <summary>
        /// Fifteen blendshape names in VRChat's viseme order, which is the same order Basis uses.
        /// </summary>
        public List<string> VisemeBlendShapes = new List<string>();

        public bool EnableEyeLook;
        public VrcEyelidType EyelidType = VrcEyelidType.None;
        public long EyelidsSkinnedMeshFileId;

        /// <summary>
        /// Blendshape indices for blink, looking up and looking down, in that order. Unity
        /// serializes this as a hex byte blob rather than a list.
        /// </summary>
        public List<int> EyelidsBlendshapes = new List<int>();

        public long LeftEyeFileId;
        public long RightEyeFileId;

        /// <summary>
        /// The expression and animation systems, recorded only so a conversion can say they
        /// exist and will not come across. Basis has no playable layers and no expression menu
        /// format; HVR Vixxy replaces both, and is authored by hand.
        /// </summary>
        public bool HasExpressionsMenu;

        public bool HasExpressionParameters;

        public List<VrcAnimationLayerEntry> AnimationLayers =
            new List<VrcAnimationLayerEntry>();
    }
}
