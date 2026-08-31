using System.Collections.Generic;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Mapping
{
    /// <summary>
    /// Turns a VRM spring chain into a jiggle rig.
    /// <para>
    /// VRM and jiggle describe a hanging chain the same way, so most of this is direct. The one
    /// judgement call is stiffness: VRM's is a force with no upper bound, jiggle's runs from 0
    /// to 1, so anything above 1 is as stiff as jiggle goes.
    /// </para>
    /// <para>
    /// VRM 1.0 carries parameters per joint. Jiggle evaluates its parameters over normalized
    /// distance from the chain root, which is the same axis, so a chain whose joints differ
    /// becomes a curve rather than an average.
    /// </para>
    /// </summary>
    public static class VrmSpringBoneToJiggleMapper
    {
        public static JiggleRigPlan Map(VrmSpringChainData source)
        {
            JiggleRigPlan plan = new JiggleRigPlan();
            List<ConversionDiagnostic> log = plan.Diagnostics;

            if (source == null || source.Joints.Count == 0)
            {
                log.Add(DiagnosticSeverity.Warning, "vrm.noJoints",
                    "A VRM spring chain named no joints, so there was nothing to convert.");
                return plan;
            }

            List<VrmSpringJointData> joints = WithParameters(source.Joints);
            JiggleParameterPlan parameters = plan.Parameters;

            // A chain's tail joint carries no parameters of its own: it says where the chain
            // ends rather than how it behaves.
            if (joints.Count == 0)
            {
                joints.Add(source.Joints[0]);
            }

            float[] stiffness = new float[joints.Count];
            float[] drag = new float[joints.Count];
            float[] radius = new float[joints.Count];
            float[] gravity = new float[joints.Count];
            bool clamped = false;
            bool sideways = false;

            for (int i = 0; i < joints.Count; i++)
            {
                VrmSpringJointData joint = joints[i];

                clamped |= joint.Stiffness > 1f;
                stiffness[i] = Mathf.Clamp01(joint.Stiffness);
                drag[i] = Mathf.Clamp01(joint.DragForce);
                radius[i] = Mathf.Max(0f, joint.Radius);

                // VRM's gravity is a direction and a magnitude; jiggle's is a multiplier on
                // world gravity, so only the downward part has anywhere to go.
                Vector3 pull = joint.GravityDir.normalized * joint.GravityPower;
                gravity[i] = Mathf.Max(0f, -pull.y);
                sideways |= joint.GravityPower > 0f
                    && !Mathf.Approximately(pull.magnitude, Mathf.Abs(pull.y));
            }

            parameters.Stiffness = Curved(stiffness);
            log.Add(DiagnosticSeverity.Approximated, "vrm.stiffness",
                $"stiffness force {joints[0].Stiffness} became jiggle stiffness "
                + $"{stiffness[0]}. VRM measures it as a force with no upper bound and jiggle "
                + "runs from 0 to 1, so this is a fit rather than a conversion.");

            if (clamped)
            {
                log.Add(DiagnosticSeverity.Approximated, "vrm.stiffness.clamped",
                    "A joint's stiffness force was above 1, which is stiffer than jiggle can "
                    + "express. It was written as fully stiff.");
            }

            parameters.Drag = Curved(drag);
            log.Add(DiagnosticSeverity.Mapped, "vrm.drag",
                $"drag force {joints[0].DragForce} became jiggle drag. Both are damping on the "
                + "same 0 to 1 scale.");

            parameters.Gravity = Curved(gravity);
            if (sideways)
            {
                log.Add(DiagnosticSeverity.Approximated, "vrm.gravity.direction",
                    "Gravity did not point straight down. Jiggle scales world gravity rather "
                    + "than taking a direction, so only the downward part carried across.");
            }

            bool collides = radius[0] > 0f;
            parameters.CollisionRadius = Curved(radius);
            parameters.CollisionToggle = collides;

            if (collides)
            {
                log.Add(DiagnosticSeverity.Mapped, "vrm.radius",
                    $"joint radius {joints[0].Radius} became jiggle collisionRadius. Both are "
                    + "metres.");
            }

            if (source.CenterFileId != 0L)
            {
                log.Add(DiagnosticSeverity.Dropped, "vrm.center.dropped",
                    "The chain named a centre transform. VRM simulates relative to it so hair "
                    + "does not lag behind a moving avatar. Jiggle has no equivalent; its own "
                    + "root motion handling covers some of the same ground.");
            }

            return plan;
        }

        /// <summary>
        /// A parameter and its falloff. Jiggle evaluates <c>value * curve(t)</c> over normalized
        /// distance from the root, so a chain whose joints agree needs no curve at all, and one
        /// whose joints differ becomes the ratios between them.
        /// </summary>
        private static JiggleCurvedFloatPlan Curved(float[] values)
        {
            float first = values[0];
            bool varies = false;

            for (int i = 1; i < values.Length; i++)
            {
                if (!Mathf.Approximately(values[i], first))
                {
                    varies = true;
                    break;
                }
            }

            if (!varies || values.Length < 2)
            {
                return new JiggleCurvedFloatPlan(first);
            }

            // The curve is a ratio, so it needs something to be a ratio of. A chain starting at
            // zero is described by its largest value instead.
            float scale = first;
            if (Mathf.Approximately(scale, 0f))
            {
                foreach (float value in values)
                {
                    scale = Mathf.Max(scale, value);
                }
            }

            if (Mathf.Approximately(scale, 0f))
            {
                return new JiggleCurvedFloatPlan(0f);
            }

            Keyframe[] keys = new Keyframe[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                keys[i] = new Keyframe(i / (float)(values.Length - 1), values[i] / scale);
            }

            return new JiggleCurvedFloatPlan(scale, new AnimationCurve(keys));
        }

        private static List<VrmSpringJointData> WithParameters(List<VrmSpringJointData> joints)
        {
            List<VrmSpringJointData> kept = new List<VrmSpringJointData>();
            foreach (VrmSpringJointData joint in joints)
            {
                if (joint.HasParameters)
                {
                    kept.Add(joint);
                }
            }

            return kept;
        }
    }
}
