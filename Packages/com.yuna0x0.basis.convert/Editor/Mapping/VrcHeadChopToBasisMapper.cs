using UnityEngine;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Mapping
{
    /// <summary>
    /// Turns a VRCHeadChop into a Basis head chop.
    /// <para>
    /// Both scale named bones away while the wearer looks out of their own head, with the same
    /// factor: 0 hides the bone, 1 leaves it. VRChat multiplies a global factor into each bone's
    /// own and can limit a bone to VR or to desktop; Basis has one factor per bone and applies
    /// it always, so the product is written and a condition is reported.
    /// </para>
    /// </summary>
    public static class VrcHeadChopToBasisMapper
    {
        public static BasisHeadChopPlan Map(VrcHeadChopData source)
        {
            BasisHeadChopPlan plan = new BasisHeadChopPlan
            {
                SourceDocumentFileId = source.DocumentFileId,
            };

            float global = Mathf.Clamp01(source.GlobalScaleFactor);

            foreach (VrcHeadChopBoneData bone in source.Bones)
            {
                if (bone.TransformFileId == 0)
                {
                    continue;
                }

                plan.Targets.Add(new HeadChopTargetPlan
                {
                    TransformFileId = bone.TransformFileId,
                    Scale = Mathf.Clamp01(bone.ScaleFactor) * global,
                });

                if (bone.Condition != VrcHeadChopCondition.Always)
                {
                    plan.Diagnostics.Add(DiagnosticSeverity.Approximated,
                        "headChop.condition.dropped",
                        "A head chop bone was scaled away only "
                        + (bone.Condition == VrcHeadChopCondition.VrOnly ? "in VR" : "on desktop")
                        + ". Basis applies a head chop whenever the wearer is in first person, so "
                        + "it is scaled away in both.");
                }
            }

            return plan;
        }
    }
}
