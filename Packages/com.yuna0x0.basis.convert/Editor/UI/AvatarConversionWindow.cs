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
using yuna0x0.Basis.Convert.Rig;
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
        private bool _showOptions = true;
        private bool _showRigs = true;
        private bool _showConstraints;
        private bool _showToggles;
        private bool _showDiagnostics = true;
        private bool _showTuning;
        private bool _showRig = true;

        /// <summary>
        /// Per item control and the tuning weights, hidden by default. The common case is the
        /// handful of checkboxes above them.
        /// </summary>
        private bool _advanced;

        /// <summary>
        /// Which kinds of thing to write. Held by the window rather than by the plan so a choice
        /// survives rescanning, and remembered between sessions.
        /// </summary>
        private readonly ConversionOptions _options = new ConversionOptions();

        private const string PrefsPrefix = "yuna0x0.basis.convert.options.";

        /// <summary>
        /// The two parts of the mapping that are judgement calls rather than conversions.
        /// Exposed so they can be adjusted and rescanned without editing code.
        /// </summary>
        private readonly JiggleMappingProfile _profile = JiggleMappingProfile.Default;

        [MenuItem(ProductInfo.ToolsMenu + "Convert Avatar")]
        public static void Open()
        {
            AvatarConversionWindow window = GetWindow<AvatarConversionWindow>();
            window.titleContent = new GUIContent("Basis Convert");
            window.minSize = new Vector2(420f, 360f);
            window.Show();
        }

        [MenuItem(ProductInfo.GameObjectMenu + "Convert Avatar", false, 30)]
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
            LoadOptions();

            if (_target == null && Selection.activeGameObject != null)
            {
                SetTarget(Selection.activeGameObject);
            }
        }

        private void LoadOptions()
        {
            _advanced = EditorPrefs.GetBool(PrefsPrefix + "advanced", false);
            _options.Physics = EditorPrefs.GetBool(PrefsPrefix + "physics", true);
            _options.Colliders = EditorPrefs.GetBool(PrefsPrefix + "colliders", true);
            _options.Constraints = EditorPrefs.GetBool(PrefsPrefix + "constraints", true);
            _options.Descriptor = EditorPrefs.GetBool(PrefsPrefix + "descriptor", true);
            _options.Toggles = EditorPrefs.GetBool(PrefsPrefix + "toggles", true);
        }

        private void SaveOptions()
        {
            EditorPrefs.SetBool(PrefsPrefix + "advanced", _advanced);
            EditorPrefs.SetBool(PrefsPrefix + "physics", _options.Physics);
            EditorPrefs.SetBool(PrefsPrefix + "colliders", _options.Colliders);
            EditorPrefs.SetBool(PrefsPrefix + "constraints", _options.Constraints);
            EditorPrefs.SetBool(PrefsPrefix + "descriptor", _options.Descriptor);
            EditorPrefs.SetBool(PrefsPrefix + "toggles", _options.Toggles);
        }

        public void SetTarget(GameObject target)
        {
            _target = target;
            Rescan();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Avatar to Basis", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Reads the avatar's prefab directly, so no source SDK needs installing.",
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

            // Narrowing the conversion narrows what is reported with it, so the diagnostics are
            // regrouped whenever the selection changes rather than only on a rescan.
            using (EditorGUI.ChangeCheckScope changed = new EditorGUI.ChangeCheckScope())
            {
                DrawOptions();
                DrawRig();
                DrawDiagnostics();
                DrawRigs();
                DrawConstraints();
                DrawToggles();
                DrawTuning();

                if (changed.changed)
                {
                    _groups = ConversionReport.Group(_plan);
                }
            }

            EditorGUILayout.EndScrollView();

            DrawActions();
        }

        private void DrawSummary()
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Detected", _plan.Profile.Kind, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(" ", string.Join(", ", _plan.Profile.Signals()),
                EditorStyles.miniLabel);

            if (_plan.Profile.LooksInconsistent)
            {
                EditorGUILayout.HelpBox(
                    "This has a humanoid rig but nothing convertible on it. If you meant to "
                    + "convert an avatar, check you picked the right object: a rig with its "
                    + "physics on a child prefab looks like this.", MessageType.Warning);
            }

            EditorGUILayout.Space(2f);

            int warnings = CountOf(DiagnosticSeverity.Warning);
            int dropped = CountOf(DiagnosticSeverity.Dropped);
            int approximated = CountOf(DiagnosticSeverity.Approximated);

            string summary = $"Found {_plan.PhysBonesFound} PhysBones, "
                + $"{_plan.DynamicBonesFound} Dynamic Bones, {_plan.CollidersFound} colliders "
                + $"and {_plan.ConstraintsFound} constraints.\n"
                + $"Would create {_plan.SelectedRigCount} jiggle rigs, "
                + $"{_plan.SelectedConstraintCount} Basis constraints and "
                + $"{_plan.SelectedVixxyControlCount} Vixxy controls"
                + (_plan.DescriptorSelected
                    ? ", and set up the Basis Avatar component."
                    : ".");

            EditorGUILayout.HelpBox(summary,
                _plan.TotalSelected > 0 ? MessageType.Info : MessageType.Warning);

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
        /// Which parts of the plan get written.
        /// <para>
        /// Basic is the kinds of thing a conversion produces. Advanced adds the per item lists
        /// and the tuning weights, so narrowing a conversion to a single bone or toggle is
        /// possible without that being in the way of the common case.
        /// </para>
        /// </summary>
        private void DrawOptions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _showOptions = EditorGUILayout.Foldout(_showOptions, "What to convert", true);

                using (EditorGUI.ChangeCheckScope changed = new EditorGUI.ChangeCheckScope())
                {
                    _advanced = GUILayout.Toggle(_advanced, "Advanced", EditorStyles.miniButton,
                        GUILayout.Width(72f));

                    if (changed.changed)
                    {
                        SaveOptions();
                    }
                }
            }

            if (!_showOptions)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            using (EditorGUI.ChangeCheckScope changed = new EditorGUI.ChangeCheckScope())
            {
                _options.Physics = Category("Physics", _options.Physics,
                    Tally(_plan.SelectedRigCount, _plan.Rigs.Count, "jiggle rigs"),
                    _plan.Rigs.Count > 0);

                if (_advanced)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        _options.Colliders = Category("Colliders", _options.Colliders,
                            $"{_plan.Colliders.Count} shapes for those rigs to rest on",
                            _options.Physics && _plan.Colliders.Count > 0);
                    }
                }

                _options.Constraints = Category("Constraints", _options.Constraints,
                    Tally(_plan.SelectedConstraintCount, _plan.Constraints.Count,
                        "Basis constraints"),
                    _plan.Constraints.Count > 0);

                _options.Descriptor = Category("Avatar descriptor", _options.Descriptor,
                    _plan.Descriptor != null
                        ? "view position, visemes and blink"
                        : "none found",
                    _plan.Descriptor != null);

                _options.Toggles = Category("Menu toggles", _options.Toggles,
                    Tally(_plan.SelectedVixxyControlCount, _plan.VixxyControls.Count,
                        "Vixxy controls"),
                    _plan.VixxyControls.Count > 0);

                if (changed.changed)
                {
                    SaveOptions();
                }
            }

            if (_advanced)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField(
                    "Advanced adds a checkbox per rig, constraint and toggle, and the two "
                    + "tuning weights.", EditorStyles.wordWrappedMiniLabel);

                // Colliders only appear under Advanced, so say so here rather than let the
                // setting act on a conversion from somewhere the reader cannot see it.
                if (!_options.Colliders)
                {
                    EditorGUILayout.LabelField(
                        "Colliders are switched off under Advanced, so the rigs will be written "
                        + "without them.", EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        /// <summary>
        /// One category row: what it is on the left, how much of it there is on the right.
        /// Disabled when the avatar has none of that kind, so an unchecked box always means a
        /// choice rather than an absence.
        /// </summary>
        private static bool Category(string label, bool value, string detail, bool available)
        {
            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(!available))
            {
                bool result = EditorGUILayout.ToggleLeft(label, value && available,
                    GUILayout.Width(170f));
                EditorGUILayout.LabelField(detail, EditorStyles.miniLabel);
                return available ? result : value;
            }
        }

        private static string Tally(int selected, int total, string noun)
        {
            return selected == total
                ? $"{total} {noun}"
                : $"{selected} of {total} {noun}";
        }

        /// <summary>Select or clear a whole list at once, since avatars carry dozens of each.</summary>
        private static void DrawSelectAll(System.Action<bool> apply)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15f);

                if (GUILayout.Button("All", EditorStyles.miniButtonLeft, GUILayout.Width(42f)))
                {
                    apply(true);
                }

                if (GUILayout.Button("None", EditorStyles.miniButtonRight, GUILayout.Width(46f)))
                {
                    apply(false);
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawRig()
        {
            if (_plan.RigDiagnostics.Count == 0)
            {
                return;
            }

            _showRig = EditorGUILayout.Foldout(_showRig, "Rig", true);
            if (!_showRig)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField(
                    "What Basis's full-body IK will make of this rig. Nothing here is converted; "
                    + "these are settings on the model itself.",
                    EditorStyles.wordWrappedMiniLabel);

                foreach (ConversionDiagnostic diagnostic in _plan.RigDiagnostics)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(EditorGUI.indentLevel * 15f);
                        GUILayout.Label(IconFor(diagnostic.Severity), GUILayout.Width(20f),
                            GUILayout.Height(18f));
                        EditorGUILayout.LabelField(diagnostic.Message,
                            EditorStyles.wordWrappedMiniLabel);
                    }
                }

                DrawJawFix();
            }
        }

        /// <summary>
        /// Clearing the Jaw mapping edits the model's import settings rather than the scene, so
        /// it is offered separately from Convert and confirmed on its own.
        /// </summary>
        private void DrawJawFix()
        {
            bool jawMapped = false;
            foreach (ConversionDiagnostic diagnostic in _plan.RigDiagnostics)
            {
                if (diagnostic.Code == "rig.jawMapped")
                {
                    jawMapped = true;
                    break;
                }
            }

            if (!jawMapped)
            {
                return;
            }

            ModelImporter importer = RigReadiness.TryGetModelImporter(_plan.SourceRoot);
            using (new EditorGUI.DisabledScope(importer == null))
            {
                if (!GUILayout.Button("Clear the Jaw mapping on the model"))
                {
                    return;
                }

                bool confirmed = EditorUtility.DisplayDialog(
                    "Clear the Jaw mapping?",
                    $"This edits the humanoid rig on {importer.assetPath} and reimports it.\n\n"
                    + "Every avatar using that model is affected, and this is not covered by "
                    + "undo.",
                    "Clear it",
                    "Cancel");

                if (confirmed && RigReadiness.ClearJawMapping(importer))
                {
                    Rescan();
                }
            }
        }

        private void DrawTuning()
        {
            if (!_advanced)
            {
                return;
            }

            EditorGUILayout.Space();
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
            if (_plan.Rigs.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            _showRigs = EditorGUILayout.Foldout(_showRigs,
                $"Rigs ({Tally(_plan.SelectedRigCount, _plan.Rigs.Count, "selected")})", true);
            if (!_showRigs)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUI.DisabledScope(!_options.Physics))
            {
                if (_advanced)
                {
                    DrawSelectAll(include =>
                    {
                        foreach (PlannedJiggleRig rig in _plan.Rigs)
                        {
                            rig.Include = include;
                        }
                    });
                }

                foreach (PlannedJiggleRig rig in _plan.Rigs)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (_advanced)
                        {
                            rig.Include = EditorGUILayout.Toggle(rig.Include,
                                GUILayout.Width(24f));
                        }

                        if (GUILayout.Button(rig.Describe(), EditorStyles.linkLabel,
                                GUILayout.MinWidth(120f)))
                        {
                            EditorGUIUtility.PingObject(rig.SourceRootBone);
                        }

                        rig.Plan.Preset = (JigglePreset)EditorGUILayout.EnumPopup(
                            rig.Plan.Preset, GUILayout.Width(90f));
                        EditorGUILayout.LabelField(
                            _options.Colliders && rig.Colliders.Count > 0
                                ? $"{rig.Colliders.Count} colliders"
                                : " ",
                            GUILayout.Width(80f));
                    }
                }
            }
        }

        /// <summary>
        /// The constraints, one per row, so a single misbehaving one can be left out without
        /// giving up the rest. Advanced only: with no checkboxes there is nothing to do here
        /// that the report does not already say.
        /// </summary>
        private void DrawConstraints()
        {
            if (!_advanced || _plan.Constraints.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            _showConstraints = EditorGUILayout.Foldout(_showConstraints,
                $"Constraints ({Tally(_plan.SelectedConstraintCount, _plan.Constraints.Count, "selected")})",
                true);
            if (!_showConstraints)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUI.DisabledScope(!_options.Constraints))
            {
                DrawSelectAll(include =>
                {
                    foreach (PlannedConstraint constraint in _plan.Constraints)
                    {
                        constraint.Include = include;
                    }
                });

                foreach (PlannedConstraint constraint in _plan.Constraints)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        constraint.Include = EditorGUILayout.Toggle(constraint.Include,
                            GUILayout.Width(24f));

                        if (GUILayout.Button(constraint.Describe(), EditorStyles.linkLabel,
                                GUILayout.MinWidth(120f)))
                        {
                            EditorGUIUtility.PingObject(constraint.SourceHost);
                        }

                        EditorGUILayout.LabelField(
                            $"{constraint.Plan.Sources.Count} sources", GUILayout.Width(80f));
                    }
                }
            }
        }

        /// <summary>The menu toggles that can be rebuilt, one per row. Advanced only.</summary>
        private void DrawToggles()
        {
            if (!_advanced || _plan.VixxyControls.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            _showToggles = EditorGUILayout.Foldout(_showToggles,
                $"Menu toggles ({Tally(_plan.SelectedVixxyControlCount, _plan.VixxyControls.Count, "selected")})",
                true);
            if (!_showToggles)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUI.DisabledScope(!_options.Toggles))
            {
                DrawSelectAll(include =>
                {
                    foreach (PlannedVixxyControl control in _plan.VixxyControls)
                    {
                        control.Include = include;
                    }
                });

                foreach (PlannedVixxyControl control in _plan.VixxyControls)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        control.Include = EditorGUILayout.Toggle(control.Include,
                            GUILayout.Width(24f));
                        EditorGUILayout.LabelField(control.Plan.MenuName,
                            GUILayout.MinWidth(120f));
                        EditorGUILayout.LabelField(Describe(control.Plan),
                            EditorStyles.miniLabel);
                    }
                }
            }
        }

        private static string Describe(VixxyControlPlan plan)
        {
            int shapes = 0;
            foreach (VixxySubjectPlan subject in plan.Subjects)
            {
                shapes += subject.BlendShapes.Count;
            }

            if (plan.Activations.Count > 0 && shapes > 0)
            {
                return $"{plan.Activations.Count} objects, {shapes} blendshapes";
            }

            return plan.Activations.Count > 0
                ? $"{plan.Activations.Count} objects"
                : $"{shapes} blendshapes";
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

                using (new EditorGUI.DisabledScope(_plan.TotalSelected == 0))
                {
                    if (GUILayout.Button($"Convert {_plan.TotalSelected} components"))
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

            bool trouble = _result.TotalSkipped > 0;
            string headline = trouble
                ? $"Converted {_result.TotalWritten} components, skipped {_result.TotalSkipped}"
                : $"Converted {_result.TotalWritten} components";

            EditorGUILayout.LabelField(headline, HeadlineStyle(trouble));
            EditorGUILayout.LabelField(
                $"{_result.RigsWritten} jiggle rigs, {_result.ConstraintsWritten} constraints, "
                + $"{_result.VixxyControlsWritten} Vixxy controls"
                + (_result.DescriptorWritten ? ", Basis Avatar component." : "."),
                EditorStyles.miniLabel);

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(
                "To see the jiggle move, press Test in Editor on the Basis Avatar component. "
                + "Play mode alone does not calibrate the avatar.",
                EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>
        /// Green when the conversion came out clean, red when something was skipped. Both tones
        /// are picked per skin: the colours that read on the dark theme are washed out on the
        /// light one.
        /// </summary>
        private static GUIStyle HeadlineStyle(bool trouble)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);

            if (trouble)
            {
                style.normal.textColor = EditorGUIUtility.isProSkin
                    ? new Color(0.94f, 0.42f, 0.40f)
                    : new Color(0.65f, 0.10f, 0.10f);
            }
            else
            {
                style.normal.textColor = EditorGUIUtility.isProSkin
                    ? new Color(0.40f, 0.83f, 0.45f)
                    : new Color(0.10f, 0.50f, 0.15f);
            }

            return style;
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
            _plan.Options = _options;
            _groups = ConversionReport.Group(_plan);

            if (_plan.TotalPlanned == 0)
            {
                _blocker = "Nothing convertible was found in this prefab. Supported sources are "
                    + "VRChat PhysBones, colliders and constraints, the VRChat avatar "
                    + "descriptor, and Dynamic Bone, which plenty of avatars use without VRChat "
                    + "being involved. If the components were already stripped, there is nothing "
                    + "left to read.";
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
                + "source data from. That usually means the prefab was unpacked. Re-import the "
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
        /// <summary>
        /// Converting twice would otherwise stack a second set of components on top of the
        /// first. Offers to remove the ones sitting where this conversion is about to write, and
        /// leaves the rest of the avatar alone.
        /// </summary>
        private bool ConfirmReplacement(GameObject destination)
        {
            List<Component> replaceable = AvatarConverter.FindReplaceable(_plan, destination);
            if (replaceable.Count == 0)
            {
                return true;
            }

            int rigs = 0;
            int constraints = 0;
            foreach (Component component in replaceable)
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
                "Convert again?",
                $"{destination.name} already has {rigs} jiggle rigs and {constraints} Basis "
                + "constraints on the bones this conversion writes to.\n\nThey will be replaced. "
                + "Anything elsewhere on the avatar is left alone.\n\nThis is undoable.",
                "Replace",
                "Cancel");

            if (!replace)
            {
                return false;
            }

            AvatarConverter.RemoveReplaceable(_plan, destination,
                $"{ProductInfo.Name}: replace converted components");
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
