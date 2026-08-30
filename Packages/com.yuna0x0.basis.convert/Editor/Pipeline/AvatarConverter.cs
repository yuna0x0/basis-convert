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
        public List<JiggleRig> Written = new List<JiggleRig>();
        public List<ConversionDiagnostic> Diagnostics = new List<ConversionDiagnostic>();

        public int TotalWritten => RigsWritten + ConstraintsWritten;
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

            foreach (PlannedJiggleRig planned in plan.Rigs)
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

                result.Written.Add(JiggleRigWriter.Write(resolved, undoName));
                result.RigsWritten++;
            }

            foreach (PlannedConstraint planned in plan.Constraints)
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

            Undo.CollapseUndoOperations(group);
            return result;
        }

        /// <summary>
        /// Components of the kinds a conversion writes, on exactly the transforms this plan
        /// would write to.
        /// <para>
        /// This is how converting twice avoids stacking a second set on top of the first. It
        /// needs no stored state: the plan already knows every transform it targets, so the
        /// previous output can be found by looking there. Anything on a transform the plan does
        /// not touch is left alone, which is what protects rigs added by hand elsewhere on the
        /// avatar.
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

            foreach (PlannedJiggleRig planned in plan.Rigs)
            {
                if (TryTranslate(source, target, planned.SourceHost, out Transform host)
                    && seen.Add(host))
                {
                    found.AddRange(host.GetComponents<JiggleRig>());
                }
            }

            seen.Clear();
            foreach (PlannedConstraint planned in plan.Constraints)
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
