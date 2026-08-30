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
    }
}
