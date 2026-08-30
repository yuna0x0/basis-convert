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

        /// <summary>Whether the conversion will write this one. Cleared from the window.</summary>
        public bool Include = true;

        /// <summary>The prefab this was read from, which its transforms belong to.</summary>
        public ConversionSource Source;

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

        /// <summary>The prefab this was read from, which its transform belongs to.</summary>
        public ConversionSource Source;
    }

    /// <summary>One Vixxy control the conversion intends to produce.</summary>
    public sealed class PlannedVixxyControl
    {
        public VixxyControlPlan Plan;

        /// <summary>Whether the conversion will write this one. Cleared from the window.</summary>
        public bool Include = true;

        /// <summary>The prefab this was read from, which its transforms belong to.</summary>
        public ConversionSource Source;

        public List<Transform> SourceTargets = new List<Transform>();

        /// <summary>
        /// Renderers the subjects name, in the same order as the plan's subjects. Blendshapes
        /// need a skinned mesh; material properties work on any renderer.
        /// </summary>
        public List<Renderer> SourceRenderers = new List<Renderer>();
    }

    /// <summary>The avatar descriptor the conversion intends to produce.</summary>
    public sealed class PlannedAvatarDescriptor
    {
        public BasisAvatarPlan Plan;

        /// <summary>Whether the conversion will write this one. Cleared from the window.</summary>
        public bool Include = true;

        /// <summary>The prefab this was read from, which its transforms belong to.</summary>
        public ConversionSource Source;

        /// <summary>Kept so the expression assets it references can be followed.</summary>
        public VrcAvatarDescriptorData SourceData;
        public Transform SourceRoot;
        public SkinnedMeshRenderer SourceVisemeMesh;
        public SkinnedMeshRenderer SourceBlinkMesh;
    }

    /// <summary>One Basis constraint the conversion intends to produce, and where it will go.</summary>
    public sealed class PlannedConstraint
    {
        public BasisConstraintPlan Plan;

        /// <summary>Whether the conversion will write this one. Cleared from the window.</summary>
        public bool Include = true;

        /// <summary>The prefab this was read from, which its transforms belong to.</summary>
        public ConversionSource Source;

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

        /// <summary>
        /// Root of the hierarchy the plan was read from, which is the avatar's own prefab.
        /// Clothing and accessories are prefabs of their own; see <see cref="Sources"/>.
        /// </summary>
        public GameObject SourceRoot;

        /// <summary>
        /// Every prefab this plan was read from, the avatar's own first. Each planned item
        /// records which one it came from, because its transforms are in that prefab's space.
        /// </summary>
        public List<ConversionSource> Sources = new List<ConversionSource>();

        /// <summary>
        /// Which parts of this plan a conversion will write. Everything, unless the window
        /// narrows it.
        /// </summary>
        public ConversionOptions Options = new ConversionOptions();

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

        /// <summary>
        /// The expression menu tree and parameters. Nothing here converts; it is read so the
        /// report can describe what rebuilding it in HVR Vixxy involves.
        /// </summary>
        public VrcExpressionInventory Expressions = new VrcExpressionInventory();

        /// <summary>
        /// Menu toggles whose animator layer and clips were found. What a Vixxy rebuild can be
        /// generated from, once emitting is implemented.
        /// </summary>
        public List<ResolvedToggle> Toggles = new List<ResolvedToggle>();

        /// <summary>Menu toggles that can be rebuilt as Vixxy controls, with their targets.</summary>
        public List<PlannedVixxyControl> VixxyControls = new List<PlannedVixxyControl>();

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
        public int ContactsFound;

        /// <summary>What kind of source this appears to be, for the reader to sanity check.</summary>
        public SourceProfile Profile = new SourceProfile();

        /// <summary>Components identified in the file but not tied to a live transform.</summary>
        public int Unresolved;

        /// <summary>Everything the plan holds, whatever the options are set to.</summary>
        public int TotalPlanned =>
            Rigs.Count + Constraints.Count + VixxyControls.Count
            + (Descriptor != null ? 1 : 0);

        /// <summary>What a conversion would write, with the current options applied.</summary>
        public int TotalSelected =>
            SelectedRigCount + SelectedConstraintCount + SelectedVixxyControlCount
            + (DescriptorSelected ? 1 : 0);

        public IEnumerable<PlannedJiggleRig> SelectedRigs()
        {
            if (!Options.Physics)
            {
                yield break;
            }

            foreach (PlannedJiggleRig rig in Rigs)
            {
                if (rig.Include)
                {
                    yield return rig;
                }
            }
        }

        public IEnumerable<PlannedConstraint> SelectedConstraints()
        {
            if (!Options.Constraints)
            {
                yield break;
            }

            foreach (PlannedConstraint constraint in Constraints)
            {
                if (constraint.Include)
                {
                    yield return constraint;
                }
            }
        }

        public IEnumerable<PlannedVixxyControl> SelectedVixxyControls()
        {
            if (!Options.Toggles)
            {
                yield break;
            }

            foreach (PlannedVixxyControl control in VixxyControls)
            {
                if (control.Include)
                {
                    yield return control;
                }
            }
        }

        public bool DescriptorSelected =>
            Options.Descriptor && Descriptor != null && Descriptor.Include;

        public int SelectedRigCount => Tally(SelectedRigs());
        public int SelectedConstraintCount => Tally(SelectedConstraints());
        public int SelectedVixxyControlCount => Tally(SelectedVixxyControls());

        private static int Tally<T>(IEnumerable<T> items)
        {
            int count = 0;
            foreach (T unused in items)
            {
                count++;
            }

            return count;
        }

        /// <summary>Everything reported about the avatar, whatever the options are set to.</summary>
        public IEnumerable<ConversionDiagnostic> AllDiagnostics() => DiagnosticsOf(false);

        /// <summary>
        /// What the conversion the options describe would report. A narrowed conversion should
        /// not be read alongside the losses of the parts it leaves out; what it leaves out is
        /// stated as such instead, by the window and by the report.
        /// </summary>
        public IEnumerable<ConversionDiagnostic> SelectedDiagnostics() => DiagnosticsOf(true);

        private IEnumerable<ConversionDiagnostic> DiagnosticsOf(bool selectedOnly)
        {
            foreach (ConversionDiagnostic diagnostic in Diagnostics)
            {
                yield return diagnostic;
            }

            foreach (PlannedJiggleRig rig in selectedOnly ? SelectedRigs() : Rigs)
            {
                foreach (ConversionDiagnostic diagnostic in rig.Plan.Diagnostics)
                {
                    yield return diagnostic;
                }
            }

            if (!selectedOnly || (Options.Physics && Options.Colliders))
            {
                foreach (PlannedJiggleCollider collider in Colliders)
                {
                    foreach (ConversionDiagnostic diagnostic in collider.Plan.Diagnostics)
                    {
                        yield return diagnostic;
                    }
                }
            }

            foreach (PlannedConstraint constraint in
                     selectedOnly ? SelectedConstraints() : Constraints)
            {
                foreach (ConversionDiagnostic diagnostic in constraint.Plan.Diagnostics)
                {
                    yield return diagnostic;
                }
            }

            if (Descriptor != null && (!selectedOnly || DescriptorSelected))
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
