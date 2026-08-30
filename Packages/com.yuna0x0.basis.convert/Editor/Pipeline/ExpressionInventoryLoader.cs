using System.Collections.Generic;
using UnityEditor;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Sources;

namespace yuna0x0.Basis.Convert.Pipeline
{
    /// <summary>
    /// Follows an avatar's expression menu tree and parameter list off disk.
    /// <para>
    /// Submenus are separate assets referenced by guid, so the tree is walked rather than read
    /// from one file. Guids already seen are skipped: a menu that references itself, directly or
    /// through a chain, would otherwise recurse forever.
    /// </para>
    /// </summary>
    public static class ExpressionInventoryLoader
    {
        /// <summary>Depth cap, in case a tree is pathological in some way visiting does not catch.</summary>
        private const int MaxDepth = 16;

        public static VrcExpressionInventory Load(
            string menuGuid, string parametersGuid)
        {
            VrcExpressionInventory inventory = new VrcExpressionInventory();

            HashSet<string> visited = new HashSet<string>();
            LoadMenu(menuGuid, inventory, visited, 0);

            UnityYamlDocument parameters = LoadDocument(parametersGuid);
            if (parameters != null)
            {
                inventory.Parameters = VrcExpressionReader.ReadParameters(parameters);
            }

            return inventory;
        }

        private static void LoadMenu(
            string guid, VrcExpressionInventory inventory, HashSet<string> visited, int depth)
        {
            if (string.IsNullOrEmpty(guid) || depth >= MaxDepth || !visited.Add(guid))
            {
                return;
            }

            UnityYamlDocument document = LoadDocument(guid);
            if (document == null)
            {
                return;
            }

            VrcExpressionMenu menu = VrcExpressionReader.ReadMenu(document, guid);
            inventory.Menus.Add(menu);

            foreach (VrcExpressionControl control in menu.Controls)
            {
                if (control.Type == VrcExpressionControlType.SubMenu)
                {
                    LoadMenu(control.SubMenuGuid, inventory, visited, depth + 1);
                }
            }
        }

        private static UnityYamlDocument LoadDocument(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                return null;
            }

            foreach (UnityYamlDocument document in UnityYamlScanner.ScanFile(path))
            {
                if (document.ClassId == UnityYamlScanner.ClassIdMonoBehaviour)
                {
                    return document;
                }
            }

            return null;
        }
    }
}
