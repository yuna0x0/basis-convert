using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace yuna0x0.Basis.Convert.Pipeline
{
    /// <summary>
    /// One prefab a conversion reads from, and where in the hierarchy its contents sit.
    /// <para>
    /// An avatar is rarely one prefab. Clothing, hair and accessories are prefabs of their own,
    /// carrying their own physics, dropped onto the avatar in a scene or nested inside its
    /// prefab. The component data for each lives in its own file, so each is read separately and
    /// its results placed at the path where that prefab sits.
    /// </para>
    /// </summary>
    public sealed class ConversionSource
    {
        public string AssetPath = string.Empty;

        /// <summary>Root of the prefab asset, which is the space this source's transforms are in.</summary>
        public GameObject Root;

        /// <summary>
        /// Sibling-index path from the hierarchy that was scanned to where this prefab sits in
        /// it. Empty for the avatar itself.
        /// </summary>
        public int[] PathInHierarchy = new int[0];

        public string Name => Root != null ? Root.name : System.IO.Path.GetFileNameWithoutExtension(AssetPath);

        public bool IsPrimary => PathInHierarchy.Length == 0;

        /// <summary>
        /// Whether the conversion writes what was read from this prefab. Cleared from the
        /// window, for a prop parented onto an avatar that was not meant to be converted with
        /// it.
        /// </summary>
        public bool Include = true;

        public static ConversionSource ForAsset(string assetPath)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            return root == null
                ? null
                : new ConversionSource { AssetPath = assetPath, Root = root };
        }

        /// <summary>
        /// The prefab this one is a variant of, or null when it is not a variant of one. A
        /// variant's own file holds only what it overrides; everything inherited lives in the
        /// base file and is not read from here.
        /// <para>
        /// A prefab made from an FBX is a variant of that model, which is the ordinary way an
        /// avatar prefab is built and carries nothing to miss: a model holds the imported
        /// hierarchy and no authored components. Only a variant of another prefab is returned.
        /// </para>
        /// </summary>
        public string BaseAssetPath()
        {
            if (Root == null
                || PrefabUtility.GetPrefabAssetType(Root) != PrefabAssetType.Variant)
            {
                return null;
            }

            GameObject baseAsset = PrefabUtility.GetCorrespondingObjectFromSource(Root);
            if (baseAsset == null
                || PrefabUtility.GetPrefabAssetType(baseAsset) == PrefabAssetType.Model)
            {
                return null;
            }

            return AssetDatabase.GetAssetPath(baseAsset);
        }
    }

    /// <summary>
    /// Finds every prefab a hierarchy is built from.
    /// <para>
    /// The scanned object is the first source, and every prefab instance under it is another.
    /// That covers both ways an avatar is assembled: clothing dropped into a scene, and clothing
    /// nested inside the avatar's own prefab.
    /// </para>
    /// </summary>
    public static class ConversionSourceDiscovery
    {
        public static List<ConversionSource> Discover(GameObject hierarchyRoot)
        {
            List<ConversionSource> sources = new List<ConversionSource>();
            if (hierarchyRoot == null)
            {
                return sources;
            }

            Transform root = hierarchyRoot.transform;
            Add(sources, root, root);

            foreach (Transform candidate in hierarchyRoot.GetComponentsInChildren<Transform>(true))
            {
                if (candidate != root && PrefabUtility.IsAnyPrefabInstanceRoot(candidate.gameObject))
                {
                    Add(sources, root, candidate);
                }
            }

            return sources;
        }

        private static void Add(List<ConversionSource> sources, Transform root, Transform at)
        {
            string assetPath = AssetPathOf(at.gameObject);
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
            {
                return;
            }

            int[] path = at == root ? new int[0] : TransformIndexPath.Of(root, at);
            if (path == null)
            {
                return;
            }

            // A prefab whose own root is where it sits produces the same source twice when the
            // scanned object is itself an instance root.
            foreach (ConversionSource existing in sources)
            {
                if (Same(existing.PathInHierarchy, path))
                {
                    return;
                }
            }

            sources.Add(new ConversionSource
            {
                AssetPath = assetPath,
                Root = asset,
                PathInHierarchy = path,
            });
        }

        /// <summary>
        /// The file holding the component data for an object: the prefab asset it is part of, or
        /// the asset its instance comes from.
        /// </summary>
        public static string AssetPathOf(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            if (PrefabUtility.IsPartOfPrefabAsset(target))
            {
                return AssetDatabase.GetAssetPath(target);
            }

            return PrefabUtility.IsPartOfPrefabInstance(target)
                ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target)
                : null;
        }

        private static bool Same(int[] left, int[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
