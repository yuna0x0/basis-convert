using UnityEngine;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Mapping
{
    /// <summary>
    /// Turns a Dynamic Bone collider into a jiggle collider.
    /// <para>
    /// Dynamic Bone picks the shape from its height rather than from a shape field: zero height
    /// is a sphere, anything more is a capsule along the chosen axis. Plane colliders are their
    /// own component type.
    /// </para>
    /// </summary>
    public static class DynamicBoneColliderToJiggleMapper
    {
        public static JiggleColliderPlan Map(DynamicBoneColliderData source)
        {
            JiggleColliderPlan plan = new JiggleColliderPlan
            {
                SourceDocumentFileId = source.DocumentFileId,
                TransformFileId = source.OwnerGameObjectFileId,
                Radius = Mathf.Max(0f, source.Radius),
                Height = Mathf.Max(0f, source.Height),
                LocalOffset = source.Center,
                CapsuleAxis = (JiggleCapsuleAxis)source.Direction,
            };

            if (source.IsPlane)
            {
                plan.Shape = JiggleColliderShape.Plane;
            }
            else if (source.Height > 0f)
            {
                plan.Shape = JiggleColliderShape.Capsule;
            }
            else
            {
                plan.Shape = JiggleColliderShape.Sphere;
            }

            if (source.Bound == DynamicBoneColliderBound.Inside)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "collider.insideBounds.dropped",
                    "This collider kept bones inside it rather than outside. Jiggle colliders "
                    + "only push bones out.");
            }

            if (!source.IsPlane
                && source.Radius2 > 0f
                && !Mathf.Approximately(source.Radius2, source.Radius))
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Approximated, "collider.taper.dropped",
                    $"This capsule tapered from {source.Radius} to {source.Radius2}. Jiggle "
                    + $"capsules have one radius, so {plan.Radius} was used for the whole length.");
            }

            return plan;
        }
    }
}
