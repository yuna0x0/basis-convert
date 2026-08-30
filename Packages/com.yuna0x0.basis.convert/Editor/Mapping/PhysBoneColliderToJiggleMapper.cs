using UnityEngine;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Mapping
{
    /// <summary>
    /// Turns a VRCPhysBoneCollider into a jiggle collider.
    /// <para>
    /// The shapes line up: both systems offer sphere, capsule and plane. The difference is
    /// orientation. VRChat places a collider with an arbitrary rotation quaternion, while jiggle
    /// orients a capsule along one of the three local axes, so a rotated capsule is snapped to
    /// the axis it points closest to and the residual is reported.
    /// </para>
    /// </summary>
    public static class PhysBoneColliderToJiggleMapper
    {
        /// <summary>Angle past which snapping a capsule to an axis is worth reporting.</summary>
        private const float AxisSnapToleranceDegrees = 5f;

        public static JiggleColliderPlan Map(PhysBoneColliderData source)
        {
            JiggleColliderPlan plan = new JiggleColliderPlan
            {
                SourceDocumentFileId = source.DocumentFileId,
                TransformFileId = source.RootTransformFileId,
                Radius = Mathf.Max(0f, source.Radius),
                Height = Mathf.Max(0f, source.Height),
                LocalOffset = source.Position,
            };

            if (source.Radius < 0f)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "collider.radius.negative",
                    $"Collider radius was {source.Radius}. Clamped to 0.");
            }

            switch (source.ShapeType)
            {
                case PhysBoneColliderShape.Sphere:
                    plan.Shape = JiggleColliderShape.Sphere;
                    break;

                case PhysBoneColliderShape.Capsule:
                    plan.Shape = JiggleColliderShape.Capsule;
                    plan.CapsuleAxis = NearestAxis(source.Rotation, plan.Diagnostics);
                    break;

                case PhysBoneColliderShape.Plane:
                    plan.Shape = JiggleColliderShape.Plane;
                    break;
            }

            if (source.InsideBounds)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "collider.insideBounds.dropped",
                    "Inside Bounds was set, which keeps bones within the collider rather than "
                    + "outside it. Jiggle colliders only push bones out.");
            }

            if (source.BonesAsSpheres)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "collider.bonesAsSpheres.dropped",
                    "Bones As Spheres was set and has no jiggle equivalent.");
            }

            return plan;
        }

        /// <summary>
        /// VRChat capsules run along their local Y before rotation. Jiggle picks an axis instead
        /// of a rotation, so take the axis the rotated Y lands closest to.
        /// </summary>
        private static JiggleCapsuleAxis NearestAxis(
            Quaternion rotation, System.Collections.Generic.List<ConversionDiagnostic> log)
        {
            Vector3 direction = rotation * Vector3.up;

            float x = Mathf.Abs(direction.x);
            float y = Mathf.Abs(direction.y);
            float z = Mathf.Abs(direction.z);

            JiggleCapsuleAxis axis;
            Vector3 snapped;
            if (x >= y && x >= z)
            {
                axis = JiggleCapsuleAxis.X;
                snapped = Vector3.right;
            }
            else if (y >= z)
            {
                axis = JiggleCapsuleAxis.Y;
                snapped = Vector3.up;
            }
            else
            {
                axis = JiggleCapsuleAxis.Z;
                snapped = Vector3.forward;
            }

            float residual = Vector3.Angle(direction, direction.x * snapped.x
                + direction.y * snapped.y + direction.z * snapped.z >= 0f ? snapped : -snapped);

            if (residual > AxisSnapToleranceDegrees)
            {
                log.Add(DiagnosticSeverity.Approximated, "collider.capsuleRotation.snapped",
                    $"The capsule was rotated {residual:0.#} degrees away from the "
                    + $"{axis} axis. Jiggle orients capsules along an axis rather than by "
                    + "rotation, so it was snapped to that axis.");
            }

            return axis;
        }
    }
}
