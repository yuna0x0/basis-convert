using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using yuna0x0.Basis.Convert.Sources;

namespace yuna0x0.Basis.Convert.Pipeline
{
    /// <summary>A menu toggle Modular Avatar would install, and the prefab it came from.</summary>
    public sealed class ModularAvatarToggle
    {
        public ResolvedToggle Toggle;

        /// <summary>The prefab the toggle and everything it switches belong to.</summary>
        public ConversionSource Source;
    }

    /// <summary>
    /// Finds the toggles a piece of clothing installs through Modular Avatar.
    /// <para>
    /// Modular Avatar's hierarchy components work on Basis, but these two do not: a menu item
    /// targets VRChat's expression menu and a merged animator targets its animator layer slots,
    /// neither of which Basis has. Read together they describe a toggle completely, which is what
    /// a Vixxy control needs.
    /// </para>
    /// <para>
    /// A merged animator addresses objects relative to the object holding it, so its clip paths
    /// are rebased onto that object before anything is resolved.
    /// </para>
    /// </summary>
    public static class ModularAvatarToggleResolver
    {
        /// <summary>Paths in a merged animator's clips are relative to the component's object.</summary>
        private const int PathModeRelative = 0;

        public static List<ModularAvatarToggle> Resolve(
            List<UnityYamlDocument> documents, PrefabObjectResolver resolver,
            ConversionSource source)
        {
            List<ModularAvatarToggle> resolved = new List<ModularAvatarToggle>();
            if (documents == null || resolver == null || source?.Root == null)
            {
                return resolved;
            }

            List<MergedAnimator> animators = ReadAnimators(documents, resolver, source);
            List<MaMenuItemData> items = ReadMenuItems(documents);

            if (animators.Count == 0 || items.Count == 0)
            {
                return resolved;
            }

            // Entries are grouped by parameter for the same reason the avatar's own menu is:
            // several of them commonly share one and pick different values from it.
            Dictionary<string, List<MaMenuItemData>> byParameter =
                new Dictionary<string, List<MaMenuItemData>>();

            foreach (MaMenuItemData item in items)
            {
                if (!item.IsToggle || string.IsNullOrEmpty(item.Parameter))
                {
                    continue;
                }

                if (!byParameter.TryGetValue(item.Parameter, out List<MaMenuItemData> shared))
                {
                    shared = new List<MaMenuItemData>();
                    byParameter[item.Parameter] = shared;
                }

                shared.Add(item);
            }

            foreach (KeyValuePair<string, List<MaMenuItemData>> group in byParameter)
            {
                foreach (MergedAnimator animator in animators)
                {
                    List<FxToggleLayer> layers = FxControllerReader.FindToggleLayers(
                        animator.Controller, new[] {group.Key});

                    if (layers.Count == 0)
                    {
                        continue;
                    }

                    FxToggleLayer layer = layers[0];
                    ResolvedToggle toggle = new ResolvedToggle
                    {
                        MenuName = NameOf(group.Value, group.Key, resolver),
                        Parameter = group.Key,
                        LayerName = layer.LayerName,
                    };

                    foreach (FxParameterState state in layer.States)
                    {
                        toggle.Choices.Add(new ResolvedChoice
                        {
                            Name = ChoiceName(group.Value, state.Value, layer),
                            Value = state.Value,
                            Effects = Rebase(
                                AnimationClipReader.Read(state.Clip), animator.Prefix),
                        });
                    }

                    resolved.Add(new ModularAvatarToggle {Source = source, Toggle = toggle});
                    break;
                }
            }

            return resolved;
        }

        private sealed class MergedAnimator
        {
            public AnimatorController Controller;

            /// <summary>Path of the object holding the component, within its own prefab.</summary>
            public string Prefix = string.Empty;
        }

        private static List<MergedAnimator> ReadAnimators(
            List<UnityYamlDocument> documents, PrefabObjectResolver resolver,
            ConversionSource source)
        {
            List<MergedAnimator> animators = new List<MergedAnimator>();

            foreach (UnityYamlDocument document in documents)
            {
                if (!IsKind(document, SourceComponentKind.MaMergeAnimator))
                {
                    continue;
                }

                MaMergeAnimatorData data =
                    ModularAvatarDocumentReader.ReadMergeAnimator(document);

                AnimatorController controller = ToggleResolver.LoadController(data.ControllerGuid);
                if (controller == null)
                {
                    continue;
                }

                string prefix = string.Empty;
                if (data.PathMode == PathModeRelative
                    && resolver.TryResolveTransform(data.OwnerGameObjectFileId,
                        out Transform host))
                {
                    prefix = PathWithin(source.Root.transform, host);
                }

                animators.Add(new MergedAnimator { Controller = controller, Prefix = prefix });
            }

            return animators;
        }

        private static List<MaMenuItemData> ReadMenuItems(List<UnityYamlDocument> documents)
        {
            List<MaMenuItemData> items = new List<MaMenuItemData>();

            foreach (UnityYamlDocument document in documents)
            {
                if (IsKind(document, SourceComponentKind.MaMenuItem))
                {
                    items.Add(ModularAvatarDocumentReader.ReadMenuItem(document));
                }
            }

            return items;
        }

        private static bool IsKind(UnityYamlDocument document, SourceComponentKind kind)
        {
            return document.ClassId == UnityYamlScanner.ClassIdMonoBehaviour
                && document.TryGetScriptIdentity(out string guid, out long fileId)
                && KnownScriptIdentities.Resolve(guid, fileId) == kind;
        }

        /// <summary>
        /// A menu item with no label of its own is named after the object carrying it. Several
        /// items sharing a parameter name the choices instead, so the parameter names the
        /// control.
        /// </summary>
        private static string NameOf(
            List<MaMenuItemData> items, string parameter, PrefabObjectResolver resolver)
        {
            if (items.Count != 1)
            {
                return parameter;
            }

            MaMenuItemData item = items[0];
            if (!string.IsNullOrEmpty(item.Name))
            {
                return item.Name;
            }

            return resolver.TryResolveTransform(item.OwnerGameObjectFileId, out Transform host)
                ? host.name
                : parameter;
        }

        private static string ChoiceName(
            List<MaMenuItemData> items, int value, FxToggleLayer layer)
        {
            foreach (MaMenuItemData item in items)
            {
                if (Mathf.RoundToInt(item.Value) == value && !string.IsNullOrEmpty(item.Name))
                {
                    return item.Name;
                }
            }

            if (!layer.IsSelector)
            {
                return value == 0 ? "OFF" : "ON";
            }

            return $"{layer.Parameter} {value}";
        }

        private static string PathWithin(Transform root, Transform target)
        {
            if (target == root)
            {
                return string.Empty;
            }

            string path = target.name;
            Transform walk = target.parent;

            while (walk != null && walk != root)
            {
                path = walk.name + "/" + path;
                walk = walk.parent;
            }

            return walk == root ? path : string.Empty;
        }

        /// <summary>
        /// Moves a clip's paths from the object the animator was merged onto into the prefab's
        /// own space, which is where everything else in this source is addressed from.
        /// </summary>
        private static ClipEffects Rebase(ClipEffects effects, string prefix)
        {
            if (effects == null || string.IsNullOrEmpty(prefix))
            {
                return effects;
            }

            for (int i = 0; i < effects.Activated.Count; i++)
            {
                effects.Activated[i] = Join(prefix, effects.Activated[i]);
            }

            for (int i = 0; i < effects.Deactivated.Count; i++)
            {
                effects.Deactivated[i] = Join(prefix, effects.Deactivated[i]);
            }

            foreach (BlendShapeEffect shape in effects.BlendShapes)
            {
                shape.Path = Join(prefix, shape.Path);
            }

            foreach (MaterialPropertyEffect material in effects.MaterialProperties)
            {
                material.Path = Join(prefix, material.Path);
            }

            return effects;
        }

        private static string Join(string prefix, string path)
        {
            return string.IsNullOrEmpty(path) ? prefix : prefix + "/" + path;
        }
    }
}
