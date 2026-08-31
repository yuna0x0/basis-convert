using System.Collections.Generic;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Mapping
{
    /// <summary>
    /// Turns a Dynamic Bone into jiggle rigs.
    /// <para>
    /// This maps more cleanly than PhysBones do. Dynamic Bone's damping is damping, and jiggle's
    /// drag is damping, on the same 0 to 1 scale; its inert is jiggle's ignoreRootMotion, again
    /// on the same scale. Only the return-to-pose force needs judgement, and even that is a
    /// combination of two settings that both mean the same thing.
    /// </para>
    /// <para>
    /// One component can drive several roots, and each becomes its own rig, since they are
    /// separate chains rather than branches of one.
    /// </para>
    /// </summary>
    public static class DynamicBoneToJiggleMapper
    {
        public static List<JiggleRigPlan> Map(
            DynamicBoneData source, JiggleMappingProfile profile = null)
        {
            profile ??= JiggleMappingProfile.Default;

            List<long> roots = ResolveRoots(source);
            List<JiggleRigPlan> plans = new List<JiggleRigPlan>();

            foreach (long root in roots)
            {
                plans.Add(MapOne(source, root, profile, roots.Count));
            }

            return plans;
        }

        /// <summary>
        /// The roots this component drives. Dynamic Bone uses the single Root, plus any extra
        /// Roots, and falls back to the object it sits on when neither is set.
        /// </summary>
        private static List<long> ResolveRoots(DynamicBoneData source)
        {
            List<long> roots = new List<long>();

            if (source.RootFileId != 0L)
            {
                roots.Add(source.RootFileId);
            }

            foreach (long extra in source.RootFileIds)
            {
                if (extra != 0L && !roots.Contains(extra))
                {
                    roots.Add(extra);
                }
            }

            if (roots.Count == 0)
            {
                roots.Add(0L);
            }

            return roots;
        }

        private static JiggleRigPlan MapOne(
            DynamicBoneData source, long rootFileId, JiggleMappingProfile profile, int rootCount)
        {
            JiggleRigPlan plan = new JiggleRigPlan
            {
                SourcePhysBoneDocumentFileId = source.DocumentFileId,
                RootBoneFileId = rootFileId,
                ExcludedTransformFileIds = new List<long>(source.ExclusionFileIds),
                ColliderSourceFileIds = new List<long>(source.ColliderFileIds),
            };

            List<ConversionDiagnostic> log = plan.Diagnostics;
            JiggleParameterPlan parameters = plan.Parameters;

            if (rootCount > 1)
            {
                log.Add(DiagnosticSeverity.Mapped, "dynamicbone.multipleRoots",
                    $"This Dynamic Bone drives {rootCount} separate chains. Each became its own "
                    + "jiggle rig, sharing these settings.");
            }

            // Elasticity restores the bone to its animated pose and stiffness preserves its
            // original orientation. Both hold the chain toward the pose, jiggle's stiffness, so
            // they combine the same way pull and stiffness do for PhysBones.
            float stiffness = Mathf.Clamp01(
                source.Elasticity.Value * profile.PullToStiffness
                + source.Stiffness.Value * profile.StiffnessToStiffness);

            parameters.Stiffness = new JiggleCurvedFloatPlan(stiffness, source.Elasticity.Curve);
            log.Add(DiagnosticSeverity.Approximated, "dynamicbone.elasticity.stiffness",
                $"elasticity {source.Elasticity.Value} and stiffness {source.Stiffness.Value} "
                + $"became jiggle stiffness {stiffness}. Check by eye.");

            // Damping and drag are both damping on the same scale, so this one is direct.
            parameters.Drag = new JiggleCurvedFloatPlan(
                Mathf.Clamp01(source.Damping.Value), source.Damping.Curve);
            log.Add(DiagnosticSeverity.Mapped, "dynamicbone.damping.drag",
                $"damping {source.Damping.Value} became jiggle drag, with its curve if it had "
                + "one. Both are damping on the same scale.");

            parameters.IgnoreRootMotion = Mathf.Clamp01(source.Inert.Value);
            log.Add(DiagnosticSeverity.Mapped, "dynamicbone.inert.ignoreRootMotion",
                $"inert {source.Inert.Value} became ignoreRootMotion. Both describe how much "
                + "the chain ignores the character moving.");

            MapRadius(source, parameters, log);
            MapGravity(source, parameters, log);
            ReportUnmappable(source, log);

            return plan;
        }

        private static void MapRadius(
            DynamicBoneData source, JiggleParameterPlan parameters, List<ConversionDiagnostic> log)
        {
            float radius = Mathf.Max(0f, source.Radius.Value);
            bool collides = radius > 0f;

            parameters.CollisionRadius = new JiggleCurvedFloatPlan(radius, source.Radius.Curve);
            parameters.CollisionToggle = collides;

            if (collides)
            {
                log.Add(DiagnosticSeverity.Mapped, "dynamicbone.radius.collisionRadius",
                    $"radius {radius} became collisionRadius, with its curve if it had one.");
            }
        }

        private static void MapGravity(
            DynamicBoneData source, JiggleParameterPlan parameters, List<ConversionDiagnostic> log)
        {
            // Dynamic Bone's gravity is a vector; jiggle's is a multiplier on world gravity.
            // Only the downward part has anywhere to go.
            float magnitude = source.Gravity.magnitude;
            if (Mathf.Approximately(magnitude, 0f))
            {
                parameters.Gravity = new JiggleCurvedFloatPlan(0f);
                return;
            }

            float downward = -source.Gravity.y;
            parameters.Gravity = new JiggleCurvedFloatPlan(Mathf.Max(0f, downward));

            if (downward <= 0f || !Mathf.Approximately(magnitude, Mathf.Abs(downward)))
            {
                log.Add(DiagnosticSeverity.Approximated, "dynamicbone.gravity.direction",
                    $"Gravity was {source.Gravity}, which does not point straight down. Jiggle "
                    + "scales world gravity rather than taking a direction, so only the downward "
                    + $"part was kept, as {parameters.Gravity.Value.Value}.");
            }
            else
            {
                log.Add(DiagnosticSeverity.Mapped, "dynamicbone.gravity",
                    $"gravity {source.Gravity} became a jiggle gravity multiplier of "
                    + $"{parameters.Gravity.Value.Value}.");
            }
        }

        private static void ReportUnmappable(
            DynamicBoneData source, List<ConversionDiagnostic> log)
        {
            if (source.Force != Vector3.zero)
            {
                log.Add(DiagnosticSeverity.Dropped, "dynamicbone.force.dropped",
                    $"The constant force {source.Force} was dropped. Jiggle has gravity but no "
                    + "arbitrary force.");
            }

            if (source.FreezeAxis != DynamicBoneFreezeAxis.None)
            {
                log.Add(DiagnosticSeverity.Dropped, "dynamicbone.freezeAxis.dropped",
                    $"Freeze Axis was {source.FreezeAxis}, which flattens the chain's motion onto "
                    + "a plane. Jiggle has no equivalent, so the bones move freely.");
            }

            if (!Mathf.Approximately(source.Friction.Value, 0f))
            {
                log.Add(DiagnosticSeverity.Dropped, "dynamicbone.friction.dropped",
                    $"Friction {source.Friction.Value} was dropped. It slows bones after they "
                    + "touch a collider, which jiggle does not model separately from drag.");
            }

            if (!Mathf.Approximately(source.BlendWeight, 1f))
            {
                log.Add(DiagnosticSeverity.Dropped, "dynamicbone.blendWeight.dropped",
                    $"Blend Weight {source.BlendWeight} was dropped. Jiggle has no overall blend "
                    + "between the animated pose and the simulation.");
            }

            if (source.EndOffset != Vector3.zero || !Mathf.Approximately(source.EndLength, 0f))
            {
                log.Add(DiagnosticSeverity.Dropped, "dynamicbone.endpoint.dropped",
                    "The chain's end offset or length was dropped. Jiggle derives its own chain "
                    + "endpoint, so the last bone may behave differently.");
            }
        }
    }
}
