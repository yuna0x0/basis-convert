using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Pipeline;
using yuna0x0.Basis.Convert.Reporting;
using yuna0x0.Basis.Convert.Writers;

namespace yuna0x0.Basis.Convert.UI
{
    /// <summary>
    /// Scan an avatar, show what a conversion would produce, then convert.
    /// <para>
    /// Nothing is written until Convert is pressed, and what it writes is one undo step.
    /// </para>
    /// </summary>
    public sealed class PhysBoneConversionWindow : EditorWindow
    {
        private GameObject _target;
        private string _sourceAssetPath;
        private string _blocker;

        private AvatarJigglePlan _plan;
        private ConversionResult _result;
        private List<DiagnosticGroup> _groups;

        private Vector2 _scroll;
        private bool _showRigs = true;
        private bool _showDiagnostics = true;

        [MenuItem(ProductInfo.ToolsMenu + "Convert VRChat PhysBones")]
        public static void Open()
        {
            PhysBoneConversionWindow window = GetWindow<PhysBoneConversionWindow>();
            window.titleContent = new GUIContent("PhysBones to Jiggle");
            window.minSize = new Vector2(420f, 360f);
            window.Show();
        }

        [MenuItem(ProductInfo.GameObjectMenu + "Convert VRChat PhysBones", false, 30)]
        private static void OpenFromHierarchy(MenuCommand command)
        {
            Open();
            if (command.context is GameObject selected)
            {
                GetWindow<PhysBoneConversionWindow>().SetTarget(selected);
            }
        }

        private void OnEnable()
        {
            if (_target == null && Selection.activeGameObject != null)
            {
                SetTarget(Selection.activeGameObject);
            }
        }

        public void SetTarget(GameObject target)
        {
            _target = target;
            Rescan();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("VRChat PhysBones to Basis Jiggle Physics",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Reads the avatar's prefab directly, so the VRChat SDK is not needed.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();

            using (EditorGUI.ChangeCheckScope changed = new EditorGUI.ChangeCheckScope())
            {
                _target = (GameObject)EditorGUILayout.ObjectField(
                    "Avatar", _target, typeof(GameObject), true);

                if (changed.changed)
                {
                    Rescan();
                }
            }

            if (!string.IsNullOrEmpty(_blocker))
            {
                EditorGUILayout.HelpBox(_blocker, MessageType.Info);
                return;
            }

            if (_plan == null)
            {
                return;
            }

            DrawSummary();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawDiagnostics();
            DrawRigs();
            EditorGUILayout.EndScrollView();

            DrawActions();
        }

        private void DrawSummary()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    $"{_plan.PhysBonesFound} PhysBones, {_plan.CollidersFound} colliders");
                EditorGUILayout.LabelField($"{_plan.Rigs.Count} jiggle rigs would be created");

                if (_plan.Unresolved > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"{_plan.Unresolved} PhysBones could not be tied to a bone and will be "
                        + "skipped.", MessageType.Warning);
                }

                if (_result != null)
                {
                    EditorGUILayout.LabelField(
                        $"Converted: {_result.RigsWritten} written, {_result.RigsSkipped} skipped",
                        EditorStyles.boldLabel);
                }
            }
        }

        private void DrawDiagnostics()
        {
            _showDiagnostics = EditorGUILayout.Foldout(_showDiagnostics,
                $"What will not come across cleanly ({_groups.Count} kinds)", true);
            if (!_showDiagnostics)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (DiagnosticGroup group in _groups)
                {
                    if (group.Severity == DiagnosticSeverity.Mapped)
                    {
                        continue;
                    }

                    EditorGUILayout.LabelField($"{Prefix(group.Severity)} {group.Code}",
                        $"x{group.Count}");
                    EditorGUILayout.LabelField(group.Example,
                        EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        private void DrawRigs()
        {
            EditorGUILayout.Space();
            _showRigs = EditorGUILayout.Foldout(_showRigs, $"Rigs ({_plan.Rigs.Count})", true);
            if (!_showRigs)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (PlannedJiggleRig rig in _plan.Rigs)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(rig.Describe(), GUILayout.MinWidth(120f));
                        rig.Plan.Preset = (JigglePreset)EditorGUILayout.EnumPopup(
                            rig.Plan.Preset, GUILayout.Width(90f));
                        EditorGUILayout.LabelField(
                            rig.Colliders.Count > 0 ? $"{rig.Colliders.Count} colliders" : " ",
                            GUILayout.Width(80f));
                    }
                }
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rescan"))
                {
                    Rescan();
                }

                using (new EditorGUI.DisabledScope(_plan.Rigs.Count == 0))
                {
                    if (GUILayout.Button($"Convert {_plan.Rigs.Count} rigs"))
                    {
                        Convert();
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy report"))
                {
                    EditorGUIUtility.systemCopyBuffer = ConversionReport.Write(_plan, _result);
                }

                if (GUILayout.Button("Save report"))
                {
                    SaveReport();
                }
            }

            EditorGUILayout.LabelField(
                "Convert writes components you can tune by hand. One undo reverts all of it.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private void Rescan()
        {
            _plan = null;
            _result = null;
            _groups = null;
            _sourceAssetPath = null;
            _blocker = null;

            if (_target == null)
            {
                _blocker = "Pick the avatar to convert.";
                return;
            }

            _sourceAssetPath = ResolveSourceAssetPath(_target, out _blocker);
            if (string.IsNullOrEmpty(_sourceAssetPath))
            {
                return;
            }

            _plan = AvatarJigglePlanner.Plan(_sourceAssetPath);
            _groups = ConversionReport.Group(_plan);

            if (_plan.PhysBonesFound == 0)
            {
                _blocker = "No VRChat PhysBones were found in this avatar's prefab. If its "
                    + "components were already stripped, or the avatar came from somewhere "
                    + "other than VRChat, there is nothing here to convert yet.";
            }
        }

        /// <summary>
        /// The prefab whose file holds the source data. The components themselves are missing
        /// scripts, so the data has to come from the asset on disk rather than from the objects.
        /// </summary>
        private static string ResolveSourceAssetPath(GameObject target, out string blocker)
        {
            blocker = null;

            if (PrefabUtility.IsPartOfPrefabAsset(target))
            {
                return AssetDatabase.GetAssetPath(target);
            }

            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }
            }

            blocker = "This object is not linked to a prefab, so there is no file to read the "
                + "VRChat data from. That usually means the prefab was unpacked. Re-import the "
                + "avatar and convert it before unpacking, or drag the original prefab in here "
                + "instead.";
            return null;
        }

        private void Convert()
        {
            GameObject destination = _target;

            if (PrefabUtility.IsPartOfPrefabAsset(_target))
            {
                destination = (GameObject)PrefabUtility.InstantiatePrefab(_target);
                Undo.RegisterCreatedObjectUndo(destination, "Instantiate avatar");
                Selection.activeGameObject = destination;
            }

            _result = AvatarJiggleConverter.Apply(_plan, destination,
                $"{ProductInfo.Name}: PhysBones to Jiggle");

            _groups = ConversionReport.Group(_plan);
        }

        private void SaveReport()
        {
            string suggested = _plan.SourceRoot != null
                ? _plan.SourceRoot.name + "-conversion-report.md"
                : "conversion-report.md";

            string path = EditorUtility.SaveFilePanel(
                "Save conversion report", string.Empty, suggested, "md");

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, ConversionReport.Write(_plan, _result));
            }
        }

        private static string Prefix(DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Warning => "!",
                DiagnosticSeverity.Dropped => "-",
                DiagnosticSeverity.Approximated => "~",
                _ => "+",
            };
        }
    }
}
