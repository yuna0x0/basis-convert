using System.Collections.Generic;
using System.IO;
using Basis.Scripts.BasisSdk.Constraints;
using GatorDragonGames.JigglePhysics;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Mapping;
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
    public sealed class AvatarConversionWindow : EditorWindow
    {
        private GameObject _target;
        private string _sourceAssetPath;
        private string _blocker;

        private AvatarConversionPlan _plan;
        private ConversionResult _result;
        private List<DiagnosticGroup> _groups;

        private Vector2 _scroll;
        private bool _showRigs = true;
        private bool _showDiagnostics = true;
        private bool _showTuning;

        /// <summary>
        /// The two parts of the mapping that are judgement calls rather than conversions.
        /// Exposed so they can be adjusted and rescanned without editing code.
        /// </summary>
        private readonly JiggleMappingProfile _profile = JiggleMappingProfile.Default;

        [MenuItem(ProductInfo.ToolsMenu + "Convert VRChat Avatar")]
        public static void Open()
        {
            AvatarConversionWindow window = GetWindow<AvatarConversionWindow>();
            window.titleContent = new GUIContent("Basis Convert");
            window.minSize = new Vector2(420f, 360f);
            window.Show();
        }

        [MenuItem(ProductInfo.GameObjectMenu + "Convert VRChat Avatar", false, 30)]
        private static void OpenFromHierarchy(MenuCommand command)
        {
            Open();
            if (command.context is GameObject selected)
            {
                GetWindow<AvatarConversionWindow>().SetTarget(selected);
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
            EditorGUILayout.LabelField("VRChat avatar to Basis", EditorStyles.boldLabel);
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
            DrawTuning();
            DrawDiagnostics();
            DrawRigs();
            EditorGUILayout.EndScrollView();

            DrawActions();
        }

        private void DrawSummary()
        {
            EditorGUILayout.Space();

            int warnings = CountOf(DiagnosticSeverity.Warning);
            int dropped = CountOf(DiagnosticSeverity.Dropped);
            int approximated = CountOf(DiagnosticSeverity.Approximated);

            string summary = $"Found {_plan.PhysBonesFound} PhysBones, {_plan.CollidersFound} "
                + $"colliders and {_plan.ConstraintsFound} constraints.\n"
                + $"Would create {_plan.Rigs.Count} jiggle rigs and "
                + $"{_plan.Constraints.Count} Basis constraints.";

            EditorGUILayout.HelpBox(summary,
                _plan.TotalPlanned > 0 ? MessageType.Info : MessageType.Warning);

            if (_plan.Unresolved > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{_plan.Unresolved} PhysBones could not be tied to a bone and will be "
                    + "skipped.", MessageType.Warning);
            }

            if (warnings > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{warnings} things need attention before you rely on the result.",
                    MessageType.Warning);
            }

            if (dropped + approximated > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{dropped} settings have no Basis equivalent and {approximated} were "
                    + "approximated. Conversion is not lossless.", MessageType.Info);
            }

        }

        private int CountOf(DiagnosticSeverity severity)
        {
            int count = 0;
            foreach (DiagnosticGroup group in _groups)
            {
                if (group.Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Components of the kinds a conversion produces that are already on the target,
        /// whether written by an earlier conversion or added by hand.
        /// </summary>
        private static List<Component> ExistingConverted(GameObject target)
        {
            List<Component> existing = new List<Component>();
            if (target == null || PrefabUtility.IsPartOfPrefabAsset(target))
            {
                return existing;
            }

            existing.AddRange(target.GetComponentsInChildren<JiggleRig>(true));
            existing.AddRange(target.GetComponentsInChildren<BasisConstraintBase>(true));
            return existing;
        }

        private void DrawTuning()
        {
            _showTuning = EditorGUILayout.Foldout(_showTuning, "Tuning", true);
            if (!_showTuning)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField(
                    "Everything else is a direct mapping. These two are fits between settings "
                    + "that do not have the same meaning or scale, so they are the ones worth "
                    + "adjusting if the result feels wrong. Rescan to apply.",
                    EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.LabelField("Stiffness, from PhysBone pull and stiffness",
                    EditorStyles.miniBoldLabel);
                _profile.PullToStiffness = EditorGUILayout.Slider(
                    "Pull weight", _profile.PullToStiffness, 0f, 2f);
                _profile.StiffnessToStiffness = EditorGUILayout.Slider(
                    "Stiffness weight", _profile.StiffnessToStiffness, 0f, 2f);

                EditorGUILayout.LabelField("Drag, from PhysBone spring",
                    EditorStyles.miniBoldLabel);
                _profile.DragAtNoSpring = EditorGUILayout.Slider(
                    "Drag at spring 0", _profile.DragAtNoSpring, 0f, 1f);
                _profile.DragAtFullSpring = EditorGUILayout.Slider(
                    "Drag at spring 1", _profile.DragAtFullSpring, 0f, 1f);

                EditorGUILayout.LabelField(
                    "Higher stiffness holds bones closer to their animated pose. Higher drag "
                    + "settles them sooner.", EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawDiagnostics()
        {
            _showDiagnostics = EditorGUILayout.Foldout(_showDiagnostics,
                "What will not come across cleanly", true);
            if (!_showDiagnostics)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                DrawDiagnosticSection(DiagnosticSeverity.Warning, "Needs attention");
                DrawDiagnosticSection(DiagnosticSeverity.Dropped, "Not carried over");
                DrawDiagnosticSection(DiagnosticSeverity.Approximated,
                    "Approximated, check by eye");
                DrawDiagnosticSection(DiagnosticSeverity.Mapped, "Mapped directly");
            }
        }

        private void DrawDiagnosticSection(DiagnosticSeverity severity, string heading)
        {
            List<DiagnosticGroup> section = _groups.FindAll(group => group.Severity == severity);
            if (section.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(heading, EditorStyles.miniBoldLabel);

            foreach (DiagnosticGroup group in section)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(EditorGUI.indentLevel * 15f);
                    GUILayout.Label(IconFor(severity), GUILayout.Width(20f),
                        GUILayout.Height(18f));
                    EditorGUILayout.LabelField($"{group.Code}  x{group.Count}",
                        EditorStyles.boldLabel);
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField(group.Example, EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        /// <summary>
        /// Unity's own console icons, so severity reads the same here as everywhere else in the
        /// editor. Warnings and losses share the warning icon because both are things the reader
        /// should notice; the section headings carry the difference between them.
        /// </summary>
        private static GUIContent IconFor(DiagnosticSeverity severity)
        {
            string icon = severity switch
            {
                DiagnosticSeverity.Warning => "console.warnicon.sml",
                DiagnosticSeverity.Dropped => "console.warnicon.sml",
                _ => "console.infoicon.sml",
            };

            return EditorGUIUtility.IconContent(icon);
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
                        if (GUILayout.Button(rig.Describe(), EditorStyles.linkLabel,
                                GUILayout.MinWidth(120f)))
                        {
                            EditorGUIUtility.PingObject(rig.SourceRootBone);
                        }

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
            DrawResult();

            EditorGUILayout.Space(6f);
            Separator();
            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rescan"))
                {
                    Rescan();
                }

                using (new EditorGUI.DisabledScope(_plan.TotalPlanned == 0))
                {
                    if (GUILayout.Button($"Convert {_plan.TotalPlanned} components"))
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

        /// <summary>
        /// The outcome of the last conversion, kept apart from the scan's warnings so the two
        /// are not read as one block.
        /// </summary>
        private void DrawResult()
        {
            if (_result == null)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            Separator();
            EditorGUILayout.Space(4f);

            string headline = _result.TotalSkipped > 0
                ? $"Converted {_result.TotalWritten} components, skipped {_result.TotalSkipped}"
                : $"Converted {_result.TotalWritten} components";

            EditorGUILayout.LabelField(headline, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"{_result.RigsWritten} jiggle rigs, {_result.ConstraintsWritten} constraints.",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(
                "To see the jiggle move, press Test in Editor on the Basis Avatar component. "
                + "Play mode alone does not calibrate the avatar.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private static void Separator()
        {
            Rect line = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(line, new Color(0f, 0f, 0f, 0.25f));
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

            _plan = AvatarConversionPlanner.Plan(_sourceAssetPath, _profile);
            _groups = ConversionReport.Group(_plan);

            if (_plan.PhysBonesFound == 0 && _plan.ConstraintsFound == 0)
            {
                _blocker = "No convertible VRChat components were found in this avatar's "
                    + "prefab. If they were already stripped, or the avatar came from somewhere "
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

            if (!ConfirmReplacement(destination))
            {
                return;
            }

            _result = AvatarConverter.Apply(_plan, destination,
                $"{ProductInfo.Name}: convert avatar");

            _groups = ConversionReport.Group(_plan);
        }

        /// <summary>
        /// Converting twice would otherwise stack a second set of components on top of the
        /// first. Offers to remove what is there, and says plainly what that includes: without
        /// per-component bookkeeping there is no way to tell a previous conversion's output from
        /// components added by hand.
        /// </summary>
        private static bool ConfirmReplacement(GameObject destination)
        {
            List<Component> existing = ExistingConverted(destination);
            if (existing.Count == 0)
            {
                return true;
            }

            int rigs = 0;
            int constraints = 0;
            foreach (Component component in existing)
            {
                if (component is JiggleRig)
                {
                    rigs++;
                }
                else
                {
                    constraints++;
                }
            }

            bool replace = EditorUtility.DisplayDialog(
                "Replace existing components?",
                $"{destination.name} already has {rigs} jiggle rigs and {constraints} Basis "
                + "constraints.\n\nConverting again would add a second set on top of them. "
                + "Replacing removes every jiggle rig and Basis constraint under this avatar "
                + "first, including any you added or edited by hand.\n\nThis is undoable.",
                "Replace",
                "Cancel");

            if (!replace)
            {
                return false;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName($"{ProductInfo.Name}: replace converted components");

            foreach (Component component in existing)
            {
                if (component != null)
                {
                    Undo.DestroyObjectImmediate(component);
                }
            }

            return true;
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
    }
}
