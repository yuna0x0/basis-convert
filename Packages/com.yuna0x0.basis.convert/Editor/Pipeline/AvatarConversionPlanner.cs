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
        /// <summary>Reads one prefab. What a conversion of a bare avatar reads.</summary>
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

            ConversionSource source = ConversionSource.ForAsset(prefabAssetPath);
            if (source == null)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "avatar.notLoaded",
                    $"{prefabAssetPath} did not load as a prefab.");
                return plan;
            }

            plan.Sources.Add(source);
            plan.SourceRoot = source.Root;

            HashSet<string> unknownIdentities = new HashSet<string>();
            ReadSource(plan, source, profile, unknownIdentities);
            Finish(plan, unknownIdentities);
            return plan;
        }

        /// <summary>
        /// Reads a whole hierarchy, which is normally an avatar with clothing and accessories on
        /// it. Each prefab it is built from is read separately, because that is where each one's
        /// component data lives, and its results are placed where that prefab sits.
        /// </summary>
        public static AvatarConversionPlan Plan(GameObject hierarchyRoot,
            JiggleMappingProfile profile = null)
        {
            profile ??= JiggleMappingProfile.Default;

            AvatarConversionPlan plan = new AvatarConversionPlan();
            List<ConversionSource> sources = ConversionSourceDiscovery.Discover(hierarchyRoot);

            if (sources.Count == 0)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "avatar.noPrefab",
                    "Nothing here is linked to a prefab, so there is no file to read the source "
                    + "data from.");
                return plan;
            }

            plan.Sources.AddRange(sources);
            plan.SourceAssetPath = sources[0].AssetPath;
            plan.SourceRoot = sources[0].Root;

            HashSet<string> unknownIdentities = new HashSet<string>();
            foreach (ConversionSource source in sources)
            {
                ReadSource(plan, source, profile, unknownIdentities);
            }

            if (sources.Count > 1)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Mapped, "source.severalPrefabs",
                    $"This avatar is built from {sources.Count} prefabs. Each was read from its "
                    + "own file and its components placed where that prefab sits: "
                    + Names(sources) + ".");
            }

            Finish(plan, unknownIdentities);
            return plan;
        }

        /// <summary>
        /// The prefabs by name, capped. A gimmick pack nests the same prefab a dozen times, so
        /// the whole list is unreadable and mostly repetition.
        /// </summary>
        private static string Names(List<ConversionSource> sources)
        {
            const int limit = 6;
            List<string> names = new List<string>();

            foreach (ConversionSource source in sources)
            {
                if (!names.Contains(source.Name))
                {
                    names.Add(source.Name);
                }

                if (names.Count == limit)
                {
                    break;
                }
            }

            int remaining = sources.Count - names.Count;
            return remaining > 0
                ? string.Join(", ", names) + $" and {remaining} more"
                : string.Join(", ", names);
        }

        /// <summary>Reads one prefab into a plan, tagging what it finds with where it came from.</summary>
        private static void ReadSource(AvatarConversionPlan plan, ConversionSource source,
            JiggleMappingProfile profile, HashSet<string> unknownIdentities)
        {
            List<UnityYamlDocument> documents = UnityYamlScanner.ScanFile(source.AssetPath);
            PrefabObjectResolver resolver =
                PrefabObjectResolver.Create(source.AssetPath, documents);

            if (resolver.Root == null)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "avatar.notLoaded",
                    $"{source.AssetPath} did not load as a prefab.");
                return;
            }

            Dictionary<long, PlannedJiggleCollider> colliders =
                MapColliders(documents, resolver, plan);

            foreach (PlannedJiggleCollider collider in colliders.Values)
            {
                collider.Source = source;
            }

            plan.ModularAvatarToggles.AddRange(
                ModularAvatarToggleResolver.Resolve(documents, resolver, source));

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
                        constraint.Source = source;
                        plan.Constraints.Add(constraint);
                    }

                    continue;
                }

                if (kind == SourceComponentKind.VrcContactReceiver
                    || kind == SourceComponentKind.VrcContactSender)
                {
                    plan.ContactsFound++;
                    continue;
                }

                if (KnownScriptIdentities.IsHandledByModularAvatar(kind))
                {
                    plan.ModularAvatarHierarchyFound++;
                    continue;
                }

                if (kind == SourceComponentKind.MaMergeAnimator
                    || kind == SourceComponentKind.MaMenuItem
                    || kind == SourceComponentKind.MaMenuInstaller)
                {
                    plan.ModularAvatarMenuFound++;
                    continue;
                }

                if (kind == SourceComponentKind.DynamicBone)
                {
                    plan.DynamicBonesFound++;
                    PlanDynamicBone(document, resolver, colliders, profile, plan, source);
                    continue;
                }

                if (kind == SourceComponentKind.VrcAvatarDescriptor)
                {
                    // The avatar's own descriptor is the one that counts. Clothing prefabs are
                    // often shipped with a descriptor of their own for previewing.
                    PlannedAvatarDescriptor descriptor = PlanDescriptor(document, resolver, plan);
                    if (descriptor != null && plan.Descriptor == null)
                    {
                        descriptor.Source = source;
                        plan.Descriptor = descriptor;
                    }

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

                rig.Source = source;
                plan.Rigs.Add(rig);
            }

        }

        /// <summary>What is worked out once, after every prefab has been read.</summary>
        private static void Finish(AvatarConversionPlan plan, HashSet<string> unknownIdentities)
        {
            if (plan.SourceRoot == null)
            {
                return;
            }

            if (plan.ModularAvatarHierarchyFound > 0)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Mapped, "modularAvatar.hierarchy",
                    $"{plan.ModularAvatarHierarchyFound} Modular Avatar components rearrange the "
                    + "hierarchy: merged armatures, bone proxies, mesh settings and blendshape "
                    + "sync. Those run on Basis, so they are left to Modular Avatar rather than "
                    + "converted.");
            }

            if (plan.ModularAvatarMenuFound > 0)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "modularAvatar.menus",
                    $"{plan.ModularAvatarMenuFound} Modular Avatar components build menus and "
                    + "merge animator layers. Both target structures VRChat has and Basis does "
                    + "not, so anything they add does nothing on Basis and is not converted yet.");
            }

            if (plan.ContactsFound > 0)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "contacts.dropped",
                    $"{plan.ContactsFound} VRChat contact senders and receivers were found. Basis "
                    + "has no contact system, so anything driven by touch does not come across.");
            }

            BuildProfile(plan);
            EnsureAvatarComponent(plan);
            LoadExpressions(plan);

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
            JiggleMappingProfile profile, AvatarConversionPlan plan, ConversionSource source)
        {
            DynamicBoneData bone = DynamicBoneDocumentReader.ReadBone(document);

            if (!resolver.TryResolveTransform(bone.OwnerGameObjectFileId, out Transform host))
            {
                plan.Unresolved++;
                plan.Diagnostics.Add(DiagnosticSeverity.Warning, "dynamicbone.unresolved",
                    $"A Dynamic Bone at &{document.FileId} could not be tied to a transform and "
                    + "was skipped.");
                return;
            }

            foreach (JiggleRigPlan rigPlan in DynamicBoneToJiggleMapper.Map(bone, profile))
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
                planned.Source = source;
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

        private static void BuildProfile(AvatarConversionPlan plan)
        {
            Animator animator = plan.SourceRoot == null
                ? null
                : plan.SourceRoot.GetComponentInChildren<Animator>(true);

            plan.Profile = new SourceProfile
            {
                HasVrchatDescriptor = plan.Descriptor != null,
                HasVrchatComponents = plan.PhysBonesFound > 0 || plan.ConstraintsFound > 0
                    || plan.ContactsFound > 0,
                HasDynamicBone = plan.DynamicBonesFound > 0,
                HasHumanoidRig = animator != null && animator.avatar != null
                    && animator.avatar.isHuman,
            };
        }

        /// <summary>
        /// An avatar that never came from VRChat has no descriptor, but if it has a humanoid rig
        /// it still needs a Basis Avatar component to be usable. Dynamic Bone in particular is an
        /// ordinary Unity asset that plenty of avatars use without VRChat ever being involved.
        /// <para>
        /// Nothing is known about visemes or blink in that case, so the component is created
        /// empty and Basis fills what it can when its inspector is first opened.
        /// </para>
        /// </summary>
        private static void EnsureAvatarComponent(AvatarConversionPlan plan)
        {
            if (plan.Descriptor != null || plan.SourceRoot == null)
            {
                return;
            }

            Animator animator = plan.SourceRoot.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
            {
                return;
            }

            BasisAvatarPlan descriptorPlan = new BasisAvatarPlan
            {
                AvatarRootFileId = 0L,
            };

            descriptorPlan.Diagnostics.Add(DiagnosticSeverity.Mapped, "descriptor.noSource",
                "This avatar has a humanoid rig but no VRChat descriptor, so a Basis Avatar "
                + "component was added empty. Open its inspector once and Basis fills in the "
                + "animator, scale, renderers, eye and mouth positions itself. Visemes and blink "
                + "have to be assigned by hand, since there was nothing to read them from.");

            plan.Descriptor = new PlannedAvatarDescriptor
            {
                Plan = descriptorPlan,
                SourceRoot = plan.SourceRoot.transform,
            };
        }

        /// <summary>
        /// Reads the expression menu tree and parameters, and says what rebuilding them means.
        /// None of it converts: Basis has no menu format and no synced parameter list.
        /// </summary>
        private static void LoadExpressions(AvatarConversionPlan plan)
        {
            if (plan.Descriptor == null)
            {
                return;
            }

            VrcAvatarDescriptorData source = plan.Descriptor.SourceData;
            if (source == null)
            {
                return;
            }

            plan.Expressions = ExpressionInventoryLoader.Load(
                source.ExpressionsMenuGuid, source.ExpressionParametersGuid);

            VrcExpressionInventory inventory = plan.Expressions;
            if (inventory.ControlCount == 0 && inventory.Parameters.Count == 0)
            {
                return;
            }

            int toggles = inventory.CountOf(VrcExpressionControlType.Toggle);
            int buttons = inventory.CountOf(VrcExpressionControlType.Button);
            int subMenus = inventory.CountOf(VrcExpressionControlType.SubMenu);
            int puppets = inventory.CountOf(VrcExpressionControlType.TwoAxisPuppet)
                + inventory.CountOf(VrcExpressionControlType.FourAxisPuppet)
                + inventory.CountOf(VrcExpressionControlType.RadialPuppet);

            plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "expressions.menu",
                $"The expression menu has {inventory.ControlCount} controls across "
                + $"{inventory.Menus.Count} menus: {toggles} toggles, {buttons} buttons, "
                + $"{subMenus} submenus, {puppets} puppets. Basis has no menu format, so each of "
                + "these is rebuilt as an HVR Vixxy control with a menu item.");

            if (inventory.Parameters.Count > 0)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "expressions.parameters",
                    $"{inventory.Parameters.Count} expression parameters were declared. Vixxy "
                    + "controls hold their own state, so there is no parameter list to recreate, "
                    + "but anything driven by these has to be rebuilt control by control.");
            }

            ResolveToggles(plan, source);

            if (puppets > 0)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Dropped, "expressions.puppets",
                    $"{puppets} of the controls are puppets. Vixxy offers a slider for a control "
                    + "with several choices, which covers a radial puppet; the two and four axis "
                    + "ones have no direct equivalent.");
            }
        }

        /// <summary>
        /// Ties the menu's toggles to the animator layers behind them, and says how many could be
        /// rebuilt as they stand.
        /// </summary>
        private static void ResolveToggles(
            AvatarConversionPlan plan, VrcAvatarDescriptorData source)
        {
            string fxGuid = null;
            foreach (VrcAnimationLayerEntry layer in source.AnimationLayers)
            {
                if (layer.Layer == VrcAnimationLayer.FX)
                {
                    fxGuid = layer.ControllerGuid;
                    break;
                }
            }

            if (string.IsNullOrEmpty(fxGuid))
            {
                return;
            }

            plan.Toggles = ToggleResolver.Resolve(plan.Expressions, fxGuid);
            if (plan.Toggles.Count == 0)
            {
                return;
            }

            int simple = 0;
            foreach (ResolvedToggle toggle in plan.Toggles)
            {
                if (toggle.IsSimple)
                {
                    simple++;
                }
            }

            BuildVixxyControls(plan, plan.Toggles, plan.SourceRoot.transform,
                plan.Sources.Count > 0 ? plan.Sources[0] : null);
            BuildModularAvatarControls(plan);

            int toggleControls = plan.Expressions.CountOf(VrcExpressionControlType.Toggle);

            plan.Diagnostics.Add(DiagnosticSeverity.Mapped, "expressions.togglesResolved",
                $"{plan.Toggles.Count} of {toggleControls} menu toggles were traced to an "
                + $"animator layer, and {simple} of those only switch objects on and off, set "
                + "blendshapes or set material properties, which is what a Vixxy control holds. "
                + "The rest animate over time or drive something else and need rebuilding by "
                + "hand.");
        }

        /// <summary>
        /// Turns the toggles Modular Avatar would install into Vixxy controls. Each belongs to
        /// the prefab it came from, and its paths are resolved inside that prefab.
        /// </summary>
        private static void BuildModularAvatarControls(AvatarConversionPlan plan)
        {
            int before = plan.VixxyControls.Count;

            foreach (ModularAvatarToggle toggle in plan.ModularAvatarToggles)
            {
                if (toggle.Source?.Root == null)
                {
                    continue;
                }

                BuildVixxyControls(plan, new[] { toggle.Toggle },
                    toggle.Source.Root.transform, toggle.Source);
            }

            int rebuilt = plan.VixxyControls.Count - before;
            if (plan.ModularAvatarToggles.Count > 0)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Mapped, "modularAvatar.togglesRebuilt",
                    $"{rebuilt} of {plan.ModularAvatarToggles.Count} Modular Avatar menu toggles "
                    + "were rebuilt as Vixxy controls. They would otherwise do nothing on Basis, "
                    + "which has no expression menu for Modular Avatar to install them into.");
            }
        }

        /// <summary>
        /// Turns the toggles that can be rebuilt into Vixxy controls, resolving each switched
        /// object's path against the prefab the toggle belongs to.
        /// </summary>
        private static void BuildVixxyControls(AvatarConversionPlan plan,
            IEnumerable<ResolvedToggle> toggles, Transform root, ConversionSource source)
        {
            foreach (ResolvedToggle toggle in toggles)
            {
                VixxyControlPlan control = ToggleToVixxyMapper.Map(toggle);

                foreach (ConversionDiagnostic diagnostic in control.Diagnostics)
                {
                    plan.Diagnostics.Add(diagnostic);
                }

                // A control may switch nothing and only set blendshapes, which is how a body
                // shape slider is built.
                if (control.Activations.Count == 0 && control.Subjects.Count == 0)
                {
                    continue;
                }

                PlannedVixxyControl planned = new PlannedVixxyControl { Plan = control };
                bool resolved = true;

                foreach (VixxyActivationPlan activation in control.Activations)
                {
                    Transform target = root.Find(activation.Path);
                    if (target == null)
                    {
                        plan.Diagnostics.Add(DiagnosticSeverity.Warning, "vixxy.targetMissing",
                            $"'{control.MenuName}' switches {activation.Path}, which is not in "
                            + "this avatar. The clip was authored against a different hierarchy.");
                        resolved = false;
                        break;
                    }

                    // Whatever the toggle did not animate stays as the avatar was authored.
                    if (!activation.BothSidesAnimated)
                    {
                        bool authored = target.gameObject.activeSelf;
                        if (activation.Choices[0] == activation.Choices[1])
                        {
                            activation.Choices[activation.Choices[1] ? 0 : 1] = authored;
                        }
                    }

                    planned.SourceTargets.Add(target);
                }

                if (resolved)
                {
                    resolved = ResolveSubjects(plan, control, planned, root);
                }

                if (resolved)
                {
                    planned.Source = source;
                    plan.VixxyControls.Add(planned);
                }
            }

            if (plan.VixxyControls.Count > 0)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Mapped, "vixxy.rebuilt",
                    $"{plan.VixxyControls.Count} menu toggles were rebuilt as Vixxy controls, "
                    + "each with a menu item. The rest are listed above with why they were not.");
            }
        }

        /// <summary>
        /// Resolves each blendshape subject to its renderer, and fills in the weight for whichever
        /// side of the toggle did not set it from what the avatar was authored with.
        /// </summary>
        private static bool ResolveSubjects(
            AvatarConversionPlan plan, VixxyControlPlan control, PlannedVixxyControl planned,
            Transform root)
        {
            foreach (VixxySubjectPlan subject in control.Subjects)
            {
                Transform target = root.Find(subject.Path);
                Renderer renderer = target == null ? null : target.GetComponent<Renderer>();

                if (renderer == null)
                {
                    plan.Diagnostics.Add(DiagnosticSeverity.Warning, "vixxy.rendererMissing",
                        $"'{control.MenuName}' sets {subject.Path}, which is not a renderer in "
                        + "this avatar.");
                    return false;
                }

                if (subject.BlendShapes.Count > 0)
                {
                    SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
                    if (skinned == null || skinned.sharedMesh == null)
                    {
                        plan.Diagnostics.Add(DiagnosticSeverity.Warning, "vixxy.rendererMissing",
                            $"'{control.MenuName}' sets blendshapes on {subject.Path}, which is "
                            + "not a skinned mesh in this avatar.");
                        return false;
                    }

                    FillBlendShapeDefaults(skinned, subject);
                }

                if (subject.MaterialProperties.Count > 0)
                {
                    // Vixxy resolves the component by class name, so it has to be the type that
                    // is actually there rather than the one the clip was authored against.
                    subject.RendererTypeName = renderer.GetType().FullName;
                    FillMaterialDefaults(plan, control, renderer, subject);
                }

                planned.SourceRenderers.Add(renderer);
            }

            return true;
        }

        private static void FillBlendShapeDefaults(
            SkinnedMeshRenderer renderer, VixxySubjectPlan subject)
        {
            foreach (VixxyBlendShapePlan shape in subject.BlendShapes)
            {
                if (shape.BothSidesAnimated)
                {
                    continue;
                }

                int index = renderer.sharedMesh.GetBlendShapeIndex(shape.ShapeName);
                float authored = index >= 0 ? renderer.GetBlendShapeWeight(index) : 0f;

                // Whichever side the clip did not set keeps the authored weight.
                if (Mathf.Approximately(shape.Choices[0], shape.Choices[1]))
                {
                    shape.Choices[shape.Choices[1] != 0f ? 0 : 1] = authored;
                }
            }
        }

        /// <summary>
        /// Fills in the channels neither side of the toggle set, from the material as authored.
        /// A clip that sets only the red channel of a colour, or only sets it in one state, is
        /// the common case rather than the exception.
        /// </summary>
        private static void FillMaterialDefaults(
            AvatarConversionPlan plan, VixxyControlPlan control, Renderer renderer,
            VixxySubjectPlan subject)
        {
            Material material = renderer.sharedMaterial;

            foreach (VixxyMaterialPropertyPlan property in subject.MaterialProperties)
            {
                Vector4 authored = AuthoredValue(material, property);

                for (int channel = 0; channel < property.Channels; channel++)
                {
                    if (!property.SetWhenOff[channel])
                    {
                        Vector4 off = property.Choices[0];
                        off[channel] = authored[channel];
                        property.Choices[0] = off;
                    }

                    if (!property.SetWhenOn[channel])
                    {
                        Vector4 on = property.Choices[1];
                        on[channel] = authored[channel];
                        property.Choices[1] = on;
                    }
                }
            }

            // Vixxy sets material properties through a MaterialPropertyBlock, which the renderer
            // applies to all of its materials at once.
            if (renderer.sharedMaterials.Length > 1)
            {
                plan.Diagnostics.Add(DiagnosticSeverity.Approximated, "vixxy.materialBlock",
                    $"'{control.MenuName}' sets material properties on {subject.Path}, which has "
                    + $"{renderer.sharedMaterials.Length} materials. Vixxy applies them through a "
                    + "MaterialPropertyBlock, so every material on that renderer is affected.");
            }
        }

        /// <summary>What the material holds for a property, or zero when it does not have it.</summary>
        private static Vector4 AuthoredValue(
            Material material, VixxyMaterialPropertyPlan property)
        {
            if (material == null || !material.HasProperty(property.PropertyName))
            {
                return Vector4.zero;
            }

            switch (property.Kind)
            {
                case VixxyMaterialPropertyKind.Colour:
                    return material.GetColor(property.PropertyName);
                case VixxyMaterialPropertyKind.Vector:
                    return material.GetVector(property.PropertyName);
                default:
                    return new Vector4(material.GetFloat(property.PropertyName), 0f, 0f, 0f);
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
                SourceData = source,
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
