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
        public List<JiggleRig> Written = new List<JiggleRig>();
        public List<ConversionDiagnostic> Diagnostics = new List<ConversionDiagnostic>();

        public int TotalWritten =>
            RigsWritten + ConstraintsWritten + VixxyControlsWritten
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

            Transform source = plan.SourceRoot.transform;
            Transform target = targetRoot.transform;

            foreach (PlannedJiggleRig planned in plan.SelectedRigs())
            {
                if (!TryTranslate(source, target, planned.SourceHost, out Transform host)
                    || !TryTranslate(source, target, planned.SourceRootBone, out Transform root))
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
                    if (TryTranslate(source, target, excluded, out Transform translated))
                    {
                        resolved.ExcludedTransforms.Add(translated);
                    }
                }

                if (plan.Options.Colliders)
                {
                    foreach (PlannedJiggleCollider collider in planned.Colliders)
                    {
                        if (!TryTranslate(source, target, collider.SourceTransform,
                                out Transform translated))
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
                if (!TryTranslate(source, target, planned.SourceHost, out Transform host))
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
                        TryTranslate(source, target, sourceTransform, out Transform translated)
                            ? translated
                            : null);
                }

                if (TryTranslate(source, target, planned.SourceWorldUpObject,
                        out Transform worldUp))
                {
                    resolved.WorldUpObject = worldUp;
                }

                BasisConstraintWriter.Write(resolved, undoName);
                result.ConstraintsWritten++;
            }

            WriteDescriptor(plan, source, target, undoName, result);
            WriteVixxyControls(plan, source, target, undoName, result);

            Undo.CollapseUndoOperations(group);
            return result;
        }

        private static void WriteDescriptor(
            AvatarConversionPlan plan, Transform source, Transform target, string undoName,
            ConversionResult result)
        {
            if (!plan.DescriptorSelected)
            {
                return;
            }

            if (!TryTranslate(source, target, plan.Descriptor.SourceRoot, out Transform root))
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
                VisemeMesh = TranslateRenderer(source, target, plan.Descriptor.SourceVisemeMesh),
                BlinkMesh = TranslateRenderer(source, target, plan.Descriptor.SourceBlinkMesh),
            };

            BasisAvatarWriter.Write(resolved, undoName);
            result.DescriptorWritten = true;
        }

        private static void WriteVixxyControls(
            AvatarConversionPlan plan, Transform source, Transform target, string undoName,
            ConversionResult result)
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
                    if (!TryTranslate(source, target, sourceTarget, out Transform translated))
                    {
                        ok = false;
                        break;
                    }

                    resolved.Targets.Add(translated);
                }

                foreach (Renderer renderer in planned.SourceRenderers)
                {
                    Renderer translated = TranslateRenderer(source, target, renderer);
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

        private static GameObject targetRootOf(Transform target) => target.gameObject;

        private static T TranslateRenderer<T>(Transform source, Transform target, T renderer)
            where T : Renderer
        {
            if (renderer == null)
            {
                return null;
            }

            return TryTranslate(source, target, renderer.transform, out Transform translated)
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

            Transform source = plan.SourceRoot.transform;
            Transform target = targetRoot.transform;
            HashSet<Transform> seen = new HashSet<Transform>();

            foreach (PlannedJiggleRig planned in plan.SelectedRigs())
            {
                if (TryTranslate(source, target, planned.SourceHost, out Transform host)
                    && seen.Add(host))
                {
                    found.AddRange(host.GetComponents<JiggleRig>());
                }
            }

            seen.Clear();
            foreach (PlannedConstraint planned in plan.SelectedConstraints())
            {
                if (TryTranslate(source, target, planned.SourceHost, out Transform host)
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

        private static bool TryTranslate(
            Transform sourceRoot, Transform targetRoot, Transform sourceTransform,
            out Transform translated)
        {
            translated = null;
            if (sourceTransform == null)
            {
                return false;
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
