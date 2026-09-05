using System.Collections.Generic;

namespace yuna0x0.Basis.Convert.Model
{
    /// <summary>When a VRCHeadChop bone is scaled away. Basis has no such condition.</summary>
    public enum VrcHeadChopCondition
    {
        Always = 0,
        VrOnly = 1,
        NonVrOnly = 2,
    }

    public sealed class VrcHeadChopBoneData
    {
        public long TransformFileId;

        /// <summary>0 scales the bone away entirely, 1 leaves it as it is.</summary>
        public float ScaleFactor;

        public VrcHeadChopCondition Condition = VrcHeadChopCondition.Always;
    }

    /// <summary>One VRCHeadChop, read out of prefab YAML.</summary>
    public sealed class VrcHeadChopData
    {
        public long DocumentFileId;
        public long OwnerGameObjectFileId;

        /// <summary>Multiplied into every bone's own factor. 1 means each bone keeps its own.</summary>
        public float GlobalScaleFactor = 1f;

        public List<VrcHeadChopBoneData> Bones = new List<VrcHeadChopBoneData>();
    }

    public sealed class HeadChopTargetPlan
    {
        public long TransformFileId;

        /// <summary>What Basis multiplies the bone's scale by while the wearer is in first person.</summary>
        public float Scale;
    }

    /// <summary>A BasisHeadChop to write, as plain data.</summary>
    public sealed class BasisHeadChopPlan
    {
        public long SourceDocumentFileId;
        public List<HeadChopTargetPlan> Targets = new List<HeadChopTargetPlan>();
        public List<ConversionDiagnostic> Diagnostics = new List<ConversionDiagnostic>();
    }
}
