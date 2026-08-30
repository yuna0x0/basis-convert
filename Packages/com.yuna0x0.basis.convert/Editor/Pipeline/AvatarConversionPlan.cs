using System.Collections.Generic;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Writers;

namespace yuna0x0.Basis.Convert.Pipeline
{
    /// <summary>One rig the conversion intends to produce, and where it will go.</summary>
    public sealed class PlannedJiggleRig
    {
        public JiggleRigPlan Plan;

        /// <summary>Bone the rig is rooted at, in the source hierarchy.</summary>
        public Transform SourceRootBone;

        /// <summary>The transform carrying the source component, in the source hierarchy.</summary>
        public Transform SourceHost;

        public List<Transform> SourceExcludedTransforms = new List<Transform>();
        public List<PlannedJiggleCollider> Colliders = new List<PlannedJiggleCollider>();

        /// <summary>Everything the mapping reported, plus anything the planner added.</summary>
        public IEnumerable<ConversionDiagnostic> Diagnostics => Plan.Diagnostics;

        public string Describe()
        {
            return SourceRootBone != null ? SourceRootBone.name : "(unresolved)";
        }
    }

    public sealed class PlannedJiggleCollider
    {
        public JiggleColliderPlan Plan;
        public Transform SourceTransform;
    }

    /// <summary>The avatar descriptor the conversion intends to produce.</summary>
    public sealed class PlannedAvatarDescriptor
    {
        public BasisAvatarPlan Plan;
        public Transform SourceRoot;
        public SkinnedMeshRenderer SourceVisemeMesh;
        public SkinnedMeshRenderer SourceBlinkMesh;
    }

    /// <summary>One Basis constraint the conversion intends to produce, and where it will go.</summary>
    public sealed class PlannedConstraint
    {
        public BasisConstraintPlan Plan;

        /// <summary>
        /// The transform the component will sit on, which is the transform the constraint drives.
        /// </summary>
        public Transform SourceHost;

        public List<Transform> SourceTransforms = new List<Transform>();
        public Transform SourceWorldUpObject;

        public string Describe()
        {
            return SourceHost != null
                ? $"{SourceHost.name} ({Plan.Kind})"
                : $"(unresolved {Plan.Kind})";
        }
    }

    /// <summary>
    /// The result of reading and mapping an avatar, before anything is written. This is what a
    /// dry run shows.
    /// </summary>
    public sealed class AvatarConversionPlan
    {
        public string SourceAssetPath;

        /// <summary>Root of the hierarchy the plan was read from.</summary>
        public GameObject SourceRoot;

        public List<PlannedJiggleRig> Rigs = new List<PlannedJiggleRig>();

        /// <summary>
        /// Every collider the avatar defines, mapped once. Rigs reference entries from here
        /// rather than each mapping its own copy, so a collider shared by twenty PhysBones is
        /// reported once rather than twenty times.
        /// </summary>
        public List<PlannedJiggleCollider> Colliders = new List<PlannedJiggleCollider>();

        public List<PlannedConstraint> Constraints = new List<PlannedConstraint>();

        /// <summary>The avatar descriptor, when the source had one.</summary>
        public PlannedAvatarDescriptor Descriptor;

        /// <summary>Diagnostics about the avatar as a whole, rather than one component.</summary>
        public List<ConversionDiagnostic> Diagnostics = new List<ConversionDiagnostic>();

        /// <summary>
        /// What the humanoid rig looks like to Basis's full-body IK. Not a conversion, so these
        /// are kept apart from the component diagnostics.
        /// </summary>
        public List<ConversionDiagnostic> RigDiagnostics = new List<ConversionDiagnostic>();

        public int PhysBonesFound;
        public int CollidersFound;
        public int ConstraintsFound;
        public int DynamicBonesFound;

        /// <summary>Components identified in the file but not tied to a live transform.</summary>
        public int Unresolved;

        public int TotalPlanned =>
            Rigs.Count + Constraints.Count + (Descriptor != null ? 1 : 0);

        public IEnumerable<ConversionDiagnostic> AllDiagnostics()
        {
            foreach (ConversionDiagnostic diagnostic in Diagnostics)
            {
                yield return diagnostic;
            }

            foreach (PlannedJiggleRig rig in Rigs)
            {
                foreach (ConversionDiagnostic diagnostic in rig.Plan.Diagnostics)
                {
                    yield return diagnostic;
                }

            }

            foreach (PlannedJiggleCollider collider in Colliders)
            {
                foreach (ConversionDiagnostic diagnostic in collider.Plan.Diagnostics)
                {
                    yield return diagnostic;
                }
            }

            foreach (PlannedConstraint constraint in Constraints)
            {
                foreach (ConversionDiagnostic diagnostic in constraint.Plan.Diagnostics)
                {
                    yield return diagnostic;
                }
            }

            if (Descriptor != null)
            {
                foreach (ConversionDiagnostic diagnostic in Descriptor.Plan.Diagnostics)
                {
                    yield return diagnostic;
                }
            }

            foreach (ConversionDiagnostic diagnostic in RigDiagnostics)
            {
                yield return diagnostic;
            }
        }

        public int CountOf(DiagnosticSeverity severity)
        {
            int count = 0;
            foreach (ConversionDiagnostic diagnostic in AllDiagnostics())
            {
                if (diagnostic.Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
