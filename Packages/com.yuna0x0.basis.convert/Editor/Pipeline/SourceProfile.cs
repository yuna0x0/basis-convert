using System.Collections.Generic;

namespace yuna0x0.Basis.Convert.Pipeline
{
    /// <summary>
    /// What kind of thing was scanned, in the terms a reader would recognise.
    /// <para>
    /// Shown before converting so a mistaken selection is obvious: pointing the tool at a piece
    /// of clothing rather than the avatar wearing it otherwise looks like a working conversion
    /// that quietly did a fraction of the work.
    /// </para>
    /// </summary>
    public sealed class SourceProfile
    {
        public bool HasVrchatDescriptor;
        public bool HasVrchatComponents;
        public bool HasDynamicBone;
        public bool HasVrmSpringBones;
        public bool HasHumanoidRig;

        /// <summary>A short name for what this appears to be.</summary>
        public string Kind
        {
            get
            {
                if (HasVrchatDescriptor)
                {
                    return "VRChat avatar";
                }

                if (HasVrmSpringBones && HasHumanoidRig)
                {
                    return "VRM avatar";
                }

                if (HasHumanoidRig)
                {
                    return HasVrchatComponents
                        ? "Humanoid avatar with VRChat components"
                        : HasDynamicBone
                            ? "Humanoid avatar with Dynamic Bone"
                            : "Humanoid avatar";
                }

                if (HasVrchatComponents || HasDynamicBone || HasVrmSpringBones)
                {
                    // Not "prop": in Basis that names BasisProp, a spawnable content type this
                    // does not produce.
                    return "Clothing or accessory";
                }

                return "Nothing recognised";
            }
        }

        /// <summary>The individual signals, so the guess above can be checked.</summary>
        public IEnumerable<string> Signals()
        {
            yield return HasHumanoidRig ? "humanoid rig" : "no humanoid rig";

            if (HasVrchatDescriptor)
            {
                yield return "VRChat avatar descriptor";
            }

            if (HasVrchatComponents)
            {
                yield return "VRChat components";
            }

            if (HasDynamicBone)
            {
                yield return "Dynamic Bone";
            }

            if (HasVrmSpringBones)
            {
                yield return "VRM spring bones";
            }
        }

        public string Describe()
        {
            return $"{Kind} ({string.Join(", ", Signals())})";
        }

        /// <summary>
        /// Whether what was found matches what this looks like. A humanoid avatar with no
        /// convertible components, or components on something with no rig, is worth a second
        /// look before converting.
        /// </summary>
        public bool LooksInconsistent =>
            HasHumanoidRig && !HasVrchatComponents && !HasDynamicBone && !HasVrchatDescriptor
            && !HasVrmSpringBones;
    }
}
