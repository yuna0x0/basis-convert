using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using yuna0x0.Basis.Convert.Mapping;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Rig;
using yuna0x0.Basis.Convert.Sources;
using yuna0x0.Basis.Convert.Writers;

namespace yuna0x0.Basis.Convert.Pipeline
{
    /// <summary>
    /// Reads an avatar prefab, maps what it finds, and reports what a conversion would produce.
    /// Changes nothing.
    /// </summary>
    public static class AvatarConversionPlanner
    {
        public static AvatarConversionPlan Plan(string prefabAssetPath,
            JiggleMappingProfile profile = null)
        {
            profile ??= JiggleMappingProfile.Default;

            AvatarConversionPlan plan = new AvatarConversionPlan { SourceAssetPath = prefabAssetPath };

            if (string.IsNullOrEmpty(prefabAssetPath)
                || !System.IO.File.Exists(prefabAssetPath))
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "avatar.missing",
                    $"No prefab at {prefabAssetPath}.");
                return plan;
            }

            List<UnityYamlDocument> documents = UnityYamlScanner.ScanFile(prefabAssetPath);
            PrefabObjectResolver resolver =
                PrefabObjectResolver.Create(prefabAssetPath, documents);

            plan.SourceRoot = resolver.Root;
            if (plan.SourceRoot == null)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "avatar.notLoaded",
                    $"{prefabAssetPath} did not load as a prefab.");
                return plan;
            }

            Dictionary<long, PlannedJiggleCollider> colliders =
                MapColliders(documents, resolver, plan);
            HashSet<string> unknownIdentities = new HashSet<string>();

            foreach (UnityYamlDocument document in documents)
            {
                if (document.ClassId != UnityYamlScanner.ClassIdMonoBehaviour
                    || !document.TryGetScriptIdentity(out string guid, out long scriptFileId))
                {
                    continue;
                }

                SourceComponentKind kind = KnownScriptIdentities.Resolve(guid, scriptFileId);
                if (kind == SourceComponentKind.Unknown)
                {
                    unknownIdentities.Add($"{guid}:{scriptFileId}");
                    continue;
                }

                if (VrcConstraintDocumentReader.TryGetKind(kind, out VrcConstraintKind constraintKind))
                {
                    plan.ConstraintsFound++;
                    PlannedConstraint constraint =
                        PlanConstraint(document, constraintKind, resolver, plan);
                    if (constraint != null)
                    {
                        plan.Constraints.Add(constraint);
                    }

                    continue;
                }

                if (kind == SourceComponentKind.DynamicBone)
                {
                    plan.DynamicBonesFound++;
                    PlanDynamicBone(document, resolver, colliders, profile, plan);
                    continue;
                }

                if (kind == SourceComponentKind.VrcAvatarDescriptor)
                {
                    plan.Descriptor = PlanDescriptor(document, resolver, plan);
                    continue;
                }

                if (kind != SourceComponentKind.VrcPhysBone)
                {
                    continue;
                }

                plan.PhysBonesFound++;
                PlannedJiggleRig rig =
                    PlanOne(document, resolver, colliders, profile, plan);
                if (rig == null)
                {
                    plan.Unresolved++;
                    continue;
                }

                plan.Rigs.Add(rig);
            }

            // Only meaningful once the descriptor is known: a prop has physics but no rig, and
            // asking it for a humanoid mapping would be noise.
            plan.RigDiagnostics = RigReadiness.Inspect(plan.SourceRoot, plan.Descriptor != null);

            // An unrecognised script is reported rather than skipped silently: VRChat ships its
            // components in DLLs, so a new SDK release can introduce identities this table has
            // never seen, and that is how the table grows.
            foreach (string identity in unknownIdentities)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "source.unknownScript",
                    $"A component with script identity {identity} was not recognised and was "
                    + "skipped.");
            }

            return plan;
        }

        /// <summary>
        /// Maps every collider in the file once. A collider referenced by many PhysBones is one
        /// mapping shared between them, so its diagnostics are reported once.
        /// </summary>
        private static Dictionary<long, PlannedJiggleCollider> MapColliders(
            List<UnityYamlDocument> documents, PrefabObjectResolver resolver,
            AvatarConversionPlan plan)
        {
            Dictionary<long, PlannedJiggleCollider> colliders =
                new Dictionary<long, PlannedJiggleCollider>();

            foreach (UnityYamlDocument document in documents)
            {
                if (document.ClassId != UnityYamlScanner.ClassIdMonoBehaviour
                    || !document.TryGetScriptIdentity(out string guid, out long scriptFileId))
                {
                    continue;
                }

                SourceComponentKind kind = KnownScriptIdentities.Resolve(guid, scriptFileId);
                JiggleColliderPlan colliderPlan;
                long transformFileId;

                switch (kind)
                {
                    case SourceComponentKind.VrcPhysBoneCollider:
                    {
                        PhysBoneColliderData data = PhysBoneDocumentReader.ReadCollider(document);
                        colliderPlan = PhysBoneColliderToJiggleMapper.Map(data);

                        // No Root Transform means the collider sits on its own object.
                        transformFileId = data.RootTransformFileId != 0L
                            ? data.RootTransformFileId
                            : data.OwnerGameObjectFileId;
                        break;
                    }

                    case SourceComponentKind.DynamicBoneCollider:
                    case SourceComponentKind.DynamicBonePlaneCollider:
                    {
                        DynamicBoneColliderData data = DynamicBoneDocumentReader.ReadCollider(
                            document, kind == SourceComponentKind.DynamicBonePlaneCollider);
                        colliderPlan = DynamicBoneColliderToJiggleMapper.Map(data);
                        transformFileId = data.OwnerGameObjectFileId;
                        break;
                    }

                    default:
                        continue;
                }

                plan.CollidersFound++;

                if (!resolver.TryResolveTransform(transformFileId, out Transform transform))
                {
                    colliderPlan.Diagnostics.Add(DiagnosticSeverity.Warning,
                        "collider.transform.unresolved",
                        "This collider could not be tied to a transform, so rigs referencing it "
                        + "will not collide with it.");
                }

                PlannedJiggleCollider planned = new PlannedJiggleCollider
                {
                    Plan = colliderPlan,
                    SourceTransform = transform,
                };

                colliders[document.FileId] = planned;
                plan.Colliders.Add(planned);
            }

            return colliders;
        }

        /// <summary>
        /// One Dynamic Bone can drive several chains, so it produces one rig per root rather
        /// than one per component.
        /// </summary>
        private static void PlanDynamicBone(
            UnityYamlDocument document, PrefabObjectResolver resolver,
            IReadOnlyDictionary<long, PlannedJiggleCollider> colliders,
            JiggleMappingProfile profile, AvatarConversionPlan plan)
        {
            DynamicBoneData source = DynamicBoneDocumentReader.ReadBone(document);

            if (!resolver.TryResolveTransform(source.OwnerGameObjectFileId, out Transform host))
            {
                plan.Unresolved++;
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "dynamicbone.unresolved",
                    $"A Dynamic Bone at &{document.FileId} could not be tied to a transform and "
                    + "was skipped.");
                return;
            }

            foreach (JiggleRigPlan rigPlan in DynamicBoneToJiggleMapper.Map(source, profile))
            {
                Transform rootBone = host;
                if (rigPlan.RootBoneFileId != 0L
                    && !resolver.TryResolveTransform(rigPlan.RootBoneFileId, out rootBone))
                {
                    rootBone = host;
                    rigPlan.Diagnostics.Add(DiagnosticSeverity.Warning,
                        "dynamicbone.rootUnresolved",
                        $"A root of the Dynamic Bone on {host.name} could not be resolved. Fell "
                        + "back to the object the component sits on.");
                }

                rigPlan.Preset = JigglePresetLibrary.GuessFrom(rootBone.name);

                PlannedJiggleRig planned = new PlannedJiggleRig
                {
                    Plan = rigPlan,
                    SourceHost = host,
                    SourceRootBone = rootBone,
                };

                AttachExclusionsAndColliders(rigPlan, planned, resolver, colliders);
                plan.Rigs.Add(planned);
            }
        }

        /// <summary>
        /// Shared by both physics sources: their exclusion and collider lists are the same shape
        /// once mapped.
        /// </summary>
        private static void AttachExclusionsAndColliders(
            JiggleRigPlan rigPlan, PlannedJiggleRig planned, PrefabObjectResolver resolver,
            IReadOnlyDictionary<long, PlannedJiggleCollider> colliders)
        {
            foreach (long excludedFileId in rigPlan.ExcludedTransformFileIds)
            {
                if (resolver.TryResolveTransform(excludedFileId, out Transform excluded))
                {
                    planned.SourceExcludedTransforms.Add(excluded);
                }
                else
                {
                    rigPlan.Diagnostics.Add(DiagnosticSeverity.Warning,
                        "physics.excludedTransform.unresolved",
                        "An excluded transform could not be resolved and was dropped.");
                }
            }

            foreach (long colliderFileId in rigPlan.ColliderSourceFileIds)
            {
                if (!colliders.TryGetValue(colliderFileId, out PlannedJiggleCollider collider))
                {
                    rigPlan.Diagnostics.Add(DiagnosticSeverity.Warning,
                        "physics.collider.unresolved",
                        "A referenced collider was not found in the file and was dropped.");
                    continue;
                }

                if (collider.SourceTransform != null)
                {
                    planned.Colliders.Add(collider);
                }
            }

            if (planned.Colliders.Count > JiggleRigDataLimits.MaxColliders)
            {
                rigPlan.Diagnostics.Add(DiagnosticSeverity.Warning, "collider.limit",
                    $"{planned.Colliders.Count} colliders were referenced but a jiggle rig "
                    + $"supports {JiggleRigDataLimits.MaxColliders}. The extras were dropped.");
            }
        }

        private static PlannedAvatarDescriptor PlanDescriptor(
            UnityYamlDocument document, PrefabObjectResolver resolver, AvatarConversionPlan plan)
        {
            VrcAvatarDescriptorData source = VrcAvatarDescriptorReader.Read(document);
            BasisAvatarPlan descriptorPlan = VrcAvatarDescriptorToBasisMapper.Map(source);

            if (!resolver.TryResolveTransform(descriptorPlan.AvatarRootFileId, out Transform root))
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "descriptor.unresolved",
                    "The avatar descriptor could not be tied to a transform and was skipped.");
                return null;
            }

            PlannedAvatarDescriptor planned = new PlannedAvatarDescriptor
            {
                Plan = descriptorPlan,
                SourceRoot = root,
                SourceVisemeMesh = ResolveRenderer(
                    resolver, descriptorPlan.VisemeMeshFileId, descriptorPlan.Diagnostics,
                    "viseme"),
                SourceBlinkMesh = ResolveRenderer(
                    resolver, descriptorPlan.BlinkMeshFileId, descriptorPlan.Diagnostics,
                    "blink"),
            };

            return planned;
        }

        private static SkinnedMeshRenderer ResolveRenderer(
            PrefabObjectResolver resolver, long fileId,
            List<ConversionDiagnostic> diagnostics, string role)
        {
            if (fileId == 0L)
            {
                return null;
            }

            if (resolver.TryResolve(fileId, out Object resolved)
                && resolved is SkinnedMeshRenderer renderer)
            {
                return renderer;
            }

            diagnostics.Add(DiagnosticSeverity.Warning, $"descriptor.{role}Mesh.unresolved",
                $"The {role} mesh could not be resolved, so it was left unset.");
            return null;
        }

        private static PlannedConstraint PlanConstraint(
            UnityYamlDocument document, VrcConstraintKind kind, PrefabObjectResolver resolver,
            AvatarConversionPlan plan)
        {
            VrcConstraintData source = VrcConstraintDocumentReader.Read(document, kind);
            BasisConstraintPlan constraintPlan = VrcConstraintToBasisMapper.Map(source);

            if (!resolver.TryResolveTransform(constraintPlan.HostFileId, out Transform host))
            {
                plan.Unresolved++;
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "constraint.unresolved",
                    $"A {kind} constraint at &{document.FileId} could not be tied to a transform "
                    + "and was skipped.");
                return null;
            }

            PlannedConstraint planned = new PlannedConstraint
            {
                Plan = constraintPlan,
                SourceHost = host,
            };

            foreach (BasisConstraintSourcePlan entry in constraintPlan.Sources)
            {
                if (resolver.TryResolveTransform(entry.TransformFileId, out Transform sourceTransform))
                {
                    planned.SourceTransforms.Add(sourceTransform);
                }
                else
                {
                    planned.SourceTransforms.Add(null);
                    constraintPlan.Diagnostics.Add(DiagnosticSeverity.Warning,
                        "constraint.source.unresolved",
                        "A constraint source could not be resolved and was dropped.");
                }
            }

            if (constraintPlan.WorldUpTransformFileId != 0L
                && resolver.TryResolveTransform(
                    constraintPlan.WorldUpTransformFileId, out Transform worldUp))
            {
                planned.SourceWorldUpObject = worldUp;
            }

            return planned;
        }

        private static PlannedJiggleRig PlanOne(
            UnityYamlDocument document,
            PrefabObjectResolver resolver,
            IReadOnlyDictionary<long, PlannedJiggleCollider> colliders,
            JiggleMappingProfile profile,
            AvatarConversionPlan plan)
        {
            PhysBoneData source = PhysBoneDocumentReader.ReadPhysBone(document);

            if (!resolver.TryResolveTransform(source.OwnerGameObjectFileId, out Transform host))
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "physbone.unresolved",
                    $"A PhysBone at &{document.FileId} could not be tied to a transform and was "
                    + "skipped.");
                return null;
            }

            // VRChat treats an empty Root Transform as "this object".
            Transform rootBone = host;
            if (source.RootTransformFileId != 0L
                && !resolver.TryResolveTransform(source.RootTransformFileId, out rootBone))
            {
                rootBone = host;
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "physbone.rootUnresolved",
                    $"The Root Transform of the PhysBone on {host.name} could not be resolved. "
                    + "Fell back to the object the component sits on.");
            }

            JiggleRigPlan rigPlan = PhysBoneToJiggleMapper.Map(source, profile);
            rigPlan.Preset = JigglePresetLibrary.GuessFrom(rootBone.name);

            PlannedJiggleRig planned = new PlannedJiggleRig
            {
                Plan = rigPlan,
                SourceHost = host,
                SourceRootBone = rootBone,
            };

            AttachExclusionsAndColliders(rigPlan, planned, resolver, colliders);

            return planned;
        }
    }

    internal static class JiggleRigDataLimits
    {
        internal const int MaxColliders =
            GatorDragonGames.JigglePhysics.JiggleRigData.MaxRuntimeJiggleColliders;
    }
}
