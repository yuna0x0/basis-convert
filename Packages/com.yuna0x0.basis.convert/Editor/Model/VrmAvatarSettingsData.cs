using UnityEngine;

namespace yuna0x0.Basis.Convert.Model
{
    /// <summary>
    /// What a VRM avatar says about its own eyes and about what the wearer should see.
    /// <para>
    /// Both formats carry an offset from the head to the point the camera sits at, which is the
    /// same thing VRChat calls the view position and Basis stores as the avatar's eye position.
    /// </para>
    /// </summary>
    public sealed class VrmAvatarSettingsData
    {
        /// <summary>True when the avatar declared an eye offset at all.</summary>
        public bool HasEyeOffset;

        /// <summary>Offset from the head bone to the eyes, in metres.</summary>
        public Vector3 EyeOffsetFromHead;

        /// <summary>
        /// The bone the offset is measured from. VRM 0.x names it; 1.0 always means the head.
        /// </summary>
        public long HeadBoneFileId;

        /// <summary>Renderers the avatar hides from the wearer's own view.</summary>
        /// <summary>
        /// The eyes are aimed with the look up, down, left and right expressions rather than
        /// with eye bones. Basis rotates eye bones, so such eyes do not follow gaze.
        /// </summary>
        public bool LookAtByExpression;

        public int ThirdPersonOnlyRenderers;

        /// <summary>Renderers the avatar shows only to the wearer.</summary>
        public int FirstPersonOnlyRenderers;
    }
}
