using UnityEngine;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Mapping
{
    /// <summary>
    /// Turns a VRCAvatarDescriptor into what a `BasisAvatar` needs.
    /// <para>
    /// The viseme lists line up exactly: both systems keep fifteen blendshapes in the order
    /// sil, PP, FF, TH, DD, kk, CH, SS, nn, RR, aa, E, ih, oh, ou, so the mapping is positional
    /// rather than by name.
    /// </para>
    /// <para>
    /// The rest of the descriptor, expression menus, parameters and the playable animation
    /// layers, has no counterpart in Basis at all and is not touched here.
    /// </para>
    /// </summary>
    public static class VrcAvatarDescriptorToBasisMapper
    {
        public const int VisemeCount = 15;

        public static BasisAvatarPlan Map(VrcAvatarDescriptorData source)
        {
            BasisAvatarPlan plan = new BasisAvatarPlan
            {
                SourceDocumentFileId = source.DocumentFileId,
                AvatarRootFileId = source.OwnerGameObjectFileId,

                // Basis keeps height above the root and forward offset; VRChat's sideways
                // component has nowhere to go, and is zero on a symmetric avatar anyway.
                EyePosition = new Vector2(source.ViewPosition.y, source.ViewPosition.z),
            };

            if (!Mathf.Approximately(source.ViewPosition.x, 0f))
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "descriptor.viewPosition.sideways",
                    $"The view position was offset sideways by {source.ViewPosition.x}. Basis "
                    + "stores only height and forward offset, so that part was dropped.");
            }

            MapVisemes(source, plan);
            MapBlink(source, plan);

            plan.Diagnostics.Add(DiagnosticSeverity.Mapped, "descriptor.autoSetup",
                "The animator, human scale, renderer list and mouth position were left for "
                + "Basis to fill in when the Basis Avatar inspector is first opened. It does not "
                + "overwrite values that are already set, so what came from VRChat stays.");

            return plan;
        }

        private static void MapVisemes(VrcAvatarDescriptorData source, BasisAvatarPlan plan)
        {
            if (source.LipSync != VrcLipSyncStyle.VisemeBlendShape)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "descriptor.lipSync.unsupported",
                    $"Lip sync was set to {source.LipSync}. Basis drives visemes from blendshapes "
                    + "only, so nothing was carried over and the avatar will not lip sync until "
                    + "viseme blendshapes are assigned by hand.");
                return;
            }

            plan.VisemeMeshFileId = source.VisemeSkinnedMeshFileId;

            if (source.VisemeSkinnedMeshFileId == 0L)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "descriptor.visemeMesh.missing",
                    "Lip sync was set to blendshapes but no mesh was assigned to drive them.");
                return;
            }

            for (int i = 0; i < VisemeCount; i++)
            {
                plan.VisemeBlendShapeNames.Add(
                    i < source.VisemeBlendShapes.Count ? source.VisemeBlendShapes[i] : string.Empty);
            }

            if (source.VisemeBlendShapes.Count != VisemeCount)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "descriptor.visemes.count",
                    $"The avatar listed {source.VisemeBlendShapes.Count} viseme blendshapes "
                    + $"rather than {VisemeCount}. The missing ones were left unset.");
            }
            else
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Mapped, "descriptor.visemes",
                    "All fifteen visemes carried across. Both systems keep them in the same "
                    + "order, so they map position for position.");
            }
        }

        private static void MapBlink(VrcAvatarDescriptorData source, BasisAvatarPlan plan)
        {
            switch (source.EyelidType)
            {
                case VrcEyelidType.Blendshapes:
                    plan.BlinkMeshFileId = source.EyelidsSkinnedMeshFileId;

                    // The array is blink, looking up, looking down. Basis has blink only.
                    if (source.EyelidsBlendshapes.Count > 0)
                    {
                        plan.BlinkBlendShapeIndex = source.EyelidsBlendshapes[0];
                    }

                    if (source.EyelidsBlendshapes.Count > 1)
                    {
                        plan.Diagnostics.Add(DiagnosticSeverity.Dropped,
                            "descriptor.eyelids.lookUpDown",
                            "The looking up and looking down eyelid blendshapes were dropped. "
                            + "Basis drives blink only, and moves the eyes with bones.");
                    }

                    break;

                case VrcEyelidType.Bones:
                    plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "descriptor.eyelids.bones",
                        "Eyelids were driven by bones. Basis blinks with a blendshape only, so "
                        + "this was not carried over.");
                    break;

                default:
                    plan.Diagnostics.Add(DiagnosticSeverity.Mapped, "descriptor.eyelids.none",
                        "The avatar had no eyelid setup, so blinking was left unset.");
                    break;
            }
        }
    }
}
