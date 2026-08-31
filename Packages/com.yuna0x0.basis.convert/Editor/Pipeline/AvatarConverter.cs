using System.Collections.Generic;
using Basis.Scripts.BasisSdk.Constraints;
using GatorDragonGames.JigglePhysics;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Writers;

namespace yuna0x0.Basis.Convert.Pipeline
{
    public sealed class ConversionResult
    {
        public int RigsWritten;
        public int RigsSkipped;
        public int ConstraintsWritten;
        public int ConstraintsSkipped;
        public bool DescriptorWritten;
        public int VixxyControlsWritten;
        public int AuthoredMotionsWritten;

        /// <summary>Baked motion clips written to the project, which an undo does not remove.</summary>
        public List<string> MotionAssets = new List<string>();
        public List<JiggleRig> Written = new List<JiggleRig>();
        public List<ConversionDiagnostic> Diagnostics = new List<ConversionDiagnostic>();

        public int TotalWritten =>
            RigsWritten + ConstraintsWritten + VixxyControlsWritten + AuthoredMotionsWritten
            + (DescriptorWritten ? 1 : 0);
        public int TotalSkipped => RigsSkipped + ConstraintsSkipped;
    }

    /// <summary>
    /// Applies a plan, producing real components.
    /// <para>
    /// The plan is read from a prefab asset, but the components are written onto whichever
    /// hierarchy is being converted, usually a scene instance of that prefab. The two have the
    /// same shape, so each transform is located in the target by the sibling-index path it has
    /// in the source. Names are not used, since avatars repeat bone names.
    /// </para>
    /// </summary>
    public static class AvatarConverter
    {
        public static ConversionResult Apply(
            AvatarConversionPlan plan, GameObject targetRoot, string undoName = "Convert PhysBones")
        {
            ConversionResult result = new ConversionResult();

            if (plan == null || targetRoot == null)
            {
                result.Diagnostics.Add(DiagnosticSeverity.Warning, "apply.noTarget",
                    "A plan and a target hierarchy are both required.");
                return result;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            int group = Undo.GetCurrentGroup();

            Transform target = targetRoot.transform;
            Dictionary<ConversionSource, Transform> roots = LocateSources(plan, target, result);

            foreach (PlannedJiggleRig planned in plan.SelectedRigs())
            {
                if (!TryTranslate(plan, roots, target, planned.Source, planned.SourceHost,
                        out Transform host)
                    || !TryTranslate(plan, roots, target, planned.Source, planned.SourceRootBone,
                        out Transform root))
                {
                    result.RigsSkipped++;
                    result.Diagnostics.Add(DiagnosticSeverity.Warning, "apply.unresolved",
                        $"The rig for {planned.Describe()} has no counterpart in the target "
                        + "hierarchy and was skipped.");
                    continue;
                }

                ResolvedJiggleRig resolved = new ResolvedJiggleRig
                {
                    Plan = planned.Plan,
                    Host = host.gameObject,
                    RootBone = root,
                };

                foreach (Transform excluded in planned.SourceExcludedTransforms)
                {
                    if (TryTranslate(plan, roots, target, planned.Source, excluded,
                            out Transform translated))
                    {
                        resolved.ExcludedTransforms.Add(translated);
                    }
                }

                if (plan.Options.Colliders)
                {
                    foreach (PlannedJiggleCollider collider in planned.Colliders)
                    {
                        if (!TryTranslate(plan, roots, target, collider.Source ?? planned.Source,
                                collider.SourceTransform, out Transform translated))
                        {
                            continue;
                        }

                        resolved.Colliders.Add(new ResolvedJiggleCollider
                        {
                            Plan = collider.Plan,
                            Transform = translated,
                        });
                    }
                }

                result.Written.Add(JiggleRigWriter.Write(resolved, undoName));
                result.RigsWritten++;
            }

            foreach (PlannedConstraint planned in plan.SelectedConstraints())
            {
                if (!TryTranslate(plan, roots, target, planned.Source, planned.SourceHost,
                        out Transform host))
                {
                    result.ConstraintsSkipped++;
                    result.Diagnostics.Add(DiagnosticSeverity.Warning, "apply.unresolved",
                        $"The constraint for {planned.Describe()} has no counterpart in the "
                        + "target hierarchy and was skipped.");
                    continue;
                }

                ResolvedBasisConstraint resolved = new ResolvedBasisConstraint
                {
                    Plan = planned.Plan,
                    Host = host.gameObject,
                };

                foreach (Transform sourceTransform in planned.SourceTransforms)
                {
                    resolved.Sources.Add(
                        TryTranslate(plan, roots, target, planned.Source, sourceTransform,
                            out Transform translated)
                            ? translated
                            : null);
                }

                if (TryTranslate(plan, roots, target, planned.Source, planned.SourceWorldUpObject,
                        out Transform worldUp))
                {
                    resolved.WorldUpObject = worldUp;
                }

                BasisConstraintWriter.Write(resolved, undoName);
                result.ConstraintsWritten++;
            }

            WriteDescriptor(plan, roots, target, undoName, result);
            WriteVixxyControls(plan, roots, target, undoName, result);
            WriteAuthoredMotions(plan, roots, target, undoName, result);

            Undo.CollapseUndoOperations(group);
            return result;
        }

        private static void WriteDescriptor(
            AvatarConversionPlan plan, Dictionary<ConversionSource, Transform> roots,
            Transform target, string undoName, ConversionResult result)
        {
            if (!plan.DescriptorSelected)
            {
                return;
            }

            if (!TryTranslate(plan, roots, target, plan.Descriptor.Source,
                    plan.Descriptor.SourceRoot, out Transform root))
            {
                result.Diagnostics.Add(DiagnosticSeverity.Warning, "apply.descriptorUnresolved",
                    "The avatar descriptor has no counterpart in the target hierarchy and was "
                    + "skipped.");
                return;
            }

            ResolvedBasisAvatar resolved = new ResolvedBasisAvatar
            {
                Plan = plan.Descriptor.Plan,
                Root = root.gameObject,
                VisemeMesh = TranslateRenderer(plan, roots, target, plan.Descriptor.Source,
                    plan.Descriptor.SourceVisemeMesh),
                BlinkMesh = TranslateRenderer(plan, roots, target, plan.Descriptor.Source,
                    plan.Descriptor.SourceBlinkMesh),
            };

            BasisAvatarWriter.Write(resolved, undoName);
            result.DescriptorWritten = true;
        }

        private static void WriteVixxyControls(
            AvatarConversionPlan plan, Dictionary<ConversionSource, Transform> roots,
            Transform target, string undoName, ConversionResult result)
        {
            foreach (PlannedVixxyControl planned in plan.SelectedVixxyControls())
            {
                ResolvedVixxyControl resolved = new ResolvedVixxyControl
                {
                    Plan = planned.Plan,
                    Host = targetRootOf(target),
                };

                bool ok = true;
                foreach (Transform sourceTarget in planned.SourceTargets)
                {
                    if (!TryTranslate(plan, roots, target, planned.Source, sourceTarget,
                            out Transform translated))
                    {
                        ok = false;
                        break;
                    }

                    resolved.Targets.Add(translated);
                }

                foreach (Renderer renderer in planned.SourceRenderers)
                {
                    Renderer translated =
                        TranslateRenderer(plan, roots, target, planned.Source, renderer);
                    if (translated == null)
                    {
                        ok = false;
                        break;
                    }

                    resolved.Renderers.Add(translated);
                }

                if (!ok)
                {
                    result.Diagnostics.Add(DiagnosticSeverity.Warning, "apply.vixxyUnresolved",
                        $"'{planned.Plan.MenuName}' switches an object with no counterpart in "
                        + "the target hierarchy and was skipped.");
                    continue;
                }

                VixxyWriter.Write(resolved, undoName);
                result.VixxyControlsWritten++;
            }
        }

        /// <summary>
        /// Writes the avatar's ambient motion, baking a clip for each one.
        /// <para>
        /// The bake poses the hierarchy being converted, which is why this runs against the
        /// target rather than the prefab the plan was read from: a clip's paths have to resolve
        /// against a live object for its rotations to be sampled at all.
        /// </para>
        /// </summary>
        private static void WriteAuthoredMotions(
            AvatarConversionPlan plan, Dictionary<ConversionSource, Transform> roots,
            Transform target, string undoName, ConversionResult result)
        {
            foreach (PlannedAuthoredMotion planned in plan.SelectedAuthoredMotions())
            {
                Transform root = target;
                if (planned.Source != null && !roots.TryGetValue(planned.Source, out root))
                {
                    result.Diagnostics.Add(DiagnosticSeverity.Warning, "apply.motionUnresolved",
                        $"'{planned.Describe()}' came from a prefab that is no longer where it "
                        + "was scanned, so its motion was not written.");
                    continue;
                }

                WrittenAuthoredMotion written = AuthoredMotionWriter.Write(
                    new ResolvedAuthoredMotion
                    {
                        Plan = planned.Plan,
                        Host = root.gameObject,
                        Root = root,
                        Clip = planned.SourceClip,
                        OutputFolder = planned.OutputFolder,
                    },
                    undoName);

                result.Diagnostics.AddRange(written.Diagnostics);

                if (written.Component == null)
                {
                    continue;
                }

                result.AuthoredMotionsWritten++;
                if (!string.IsNullOrEmpty(written.AssetPath))
                {
                    result.MotionAssets.Add(written.AssetPath);
                }
            }
        }

        private static GameObject targetRootOf(Transform target) => target.gameObject;

        private static T TranslateRenderer<T>(
            AvatarConversionPlan plan, Dictionary<ConversionSource, Transform> roots,
            Transform target, ConversionSource source, T renderer)
            where T : Renderer
        {
            if (renderer == null)
            {
                return null;
            }

            return TryTranslate(plan, roots, target, source, renderer.transform,
                out Transform translated)
                ? translated.GetComponent<T>()
                : null;
        }

        /// <summary>
        /// Components of the kinds a conversion writes, on exactly the transforms this plan
        /// would write to, with the current options applied.
        /// <para>
        /// This is how converting twice avoids stacking a second set on top of the first. It
        /// needs no stored state: the plan already knows every transform it targets, so the
        /// previous output can be found by looking there. Anything on a transform the plan does
        /// not touch is left alone, which is what protects rigs added by hand elsewhere on the
        /// avatar, and what leaves an earlier conversion's output intact where the options have
        /// since narrowed what gets written.
        /// </para>
        /// </summary>
        public static List<Component> FindReplaceable(
            AvatarConversionPlan plan, GameObject targetRoot)
        {
            List<Component> found = new List<Component>();
            if (plan == null || targetRoot == null || plan.SourceRoot == null)
            {
                return found;
            }

            Transform target = targetRoot.transform;
            Dictionary<ConversionSource, Transform> roots =
                LocateSources(plan, target, new ConversionResult());
            HashSet<Transform> seen = new HashSet<Transform>();

            foreach (PlannedJiggleRig planned in plan.SelectedRigs())
            {
                if (TryTranslate(plan, roots, target, planned.Source, planned.SourceHost,
                        out Transform host)
                    && seen.Add(host))
                {
                    found.AddRange(host.GetComponents<JiggleRig>());
                }
            }

            if (plan.SelectedAuthoredMotionCount > 0)
            {
                found.AddRange(targetRoot.GetComponents<BasisAuthoredMotion>());
            }

            seen.Clear();
            foreach (PlannedConstraint planned in plan.SelectedConstraints())
            {
                if (TryTranslate(plan, roots, target, planned.Source, planned.SourceHost,
                        out Transform host)
                    && seen.Add(host))
                {
                    found.AddRange(host.GetComponents<BasisConstraintBase>());
                }
            }

            return found;
        }

        /// <summary>Removes what <see cref="FindReplaceable"/> finds, under one undo step.</summary>
        public static int RemoveReplaceable(
            AvatarConversionPlan plan, GameObject targetRoot, string undoName)
        {
            List<Component> replaceable = FindReplaceable(plan, targetRoot);

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);

            int removed = 0;
            foreach (Component component in replaceable)
            {
                if (component != null)
                {
                    Undo.DestroyObjectImmediate(component);
                    removed++;
                }
            }

            return removed;
        }

        /// <summary>
        /// Where each prefab the plan was read from sits in the hierarchy being converted.
        /// <para>
        /// The avatar's own prefab is the target root. Clothing and accessories sit somewhere
        /// under it, at the path they were found at, and their transforms are in their own
        /// prefab's space, so each needs its own root to translate against.
        /// </para>
        /// </summary>
        private static Dictionary<ConversionSource, Transform> LocateSources(
            AvatarConversionPlan plan, Transform target, ConversionResult result)
        {
            Dictionary<ConversionSource, Transform> roots =
                new Dictionary<ConversionSource, Transform>();

            foreach (ConversionSource source in plan.Sources)
            {
                if (source.IsPrimary)
                {
                    roots[source] = target;
                    continue;
                }

                if (TransformIndexPath.TryResolve(target, source.PathInHierarchy,
                        out Transform at))
                {
                    roots[source] = at;
                    continue;
                }

                result.Diagnostics.Add(DiagnosticSeverity.Warning, "apply.sourceUnresolved",
                    $"{source.Name} is not where it was when this was scanned, so nothing read "
                    + "from it was written. Rescan and convert again.");
            }

            return roots;
        }

        /// <summary>
        /// Locates a transform of one source prefab in the hierarchy being converted.
        /// <para>
        /// An item with no source recorded belongs to the avatar itself, which is what a plan
        /// built by hand and everything resolved against the avatar root produces.
        /// </para>
        /// </summary>
        private static bool TryTranslate(
            AvatarConversionPlan plan, Dictionary<ConversionSource, Transform> roots,
            Transform target, ConversionSource source, Transform sourceTransform,
            out Transform translated)
        {
            translated = null;
            if (sourceTransform == null)
            {
                return false;
            }

            Transform sourceRoot;
            Transform targetRoot;

            if (source == null)
            {
                if (plan.SourceRoot == null)
                {
                    return false;
                }

                sourceRoot = plan.SourceRoot.transform;
                targetRoot = target;
            }
            else
            {
                if (!roots.TryGetValue(source, out targetRoot) || source.Root == null)
                {
                    return false;
                }

                sourceRoot = source.Root.transform;
            }

            if (sourceRoot == targetRoot)
            {
                translated = sourceTransform;
                return true;
            }

            int[] path = TransformIndexPath.Of(sourceRoot, sourceTransform);
            return path != null && TransformIndexPath.TryResolve(targetRoot, path, out translated);
        }
    }
}
