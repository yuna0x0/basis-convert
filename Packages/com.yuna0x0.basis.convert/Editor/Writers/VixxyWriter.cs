using System.Collections.Generic;
using HVR.Vixxy;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Writers
{
    public sealed class ResolvedVixxyControl
    {
        public VixxyControlPlan Plan;

        /// <summary>Where the control component goes, normally the avatar root.</summary>
        public GameObject Host;

        /// <summary>Transforms to switch, in the same order as the plan's activations.</summary>
        public List<Transform> Targets = new List<Transform>();
    }

    /// <summary>
    /// Creates an HVR Vixxy control and its menu item.
    /// <para>
    /// Vixxy stores object switching as activations, each holding the object's Transform and one
    /// bool per choice. Its own comment explains the shape: "To toggle a GameObject, provide the
    /// Transform instead. It makes things easier as GameObject is not a component."
    /// </para>
    /// <para>
    /// Most of the control's fields are internal to its assembly, so they are written through
    /// SerializedObject. The orchestrator is deliberately left alone: a control finds its own at
    /// runtime through VixxySetup.EnsureInitialized.
    /// </para>
    /// </summary>
    public static class VixxyWriter
    {
        public static HVRVixxyControl Write(
            ResolvedVixxyControl control, string undoName = "Convert menu toggle")
        {
            if (control?.Host == null)
            {
                throw new System.ArgumentException("A host is required", nameof(control));
            }

            VixxyControlPlan plan = control.Plan;

            HVRVixxyControl component = Undo.AddComponent<HVRVixxyControl>(control.Host);
            Undo.SetCurrentGroupName(undoName);

            // choices and defaultValue are public; everything else here is not.
            component.choices = new[]
            {
                new HVRVixxyChoiceControl { title = "OFF", value = 0f },
                new HVRVixxyChoiceControl { title = "ON", value = 1f },
            };
            component.defaultValue = plan.DefaultOn ? 1f : 0f;

            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty activations = serialized.FindProperty("activations");

            int written = 0;
            activations.arraySize = control.Targets.Count;

            for (int i = 0; i < control.Targets.Count; i++)
            {
                Transform target = control.Targets[i];
                if (target == null || i >= plan.Activations.Count)
                {
                    continue;
                }

                SerializedProperty entry = activations.GetArrayElementAtIndex(written);
                entry.FindPropertyRelative("component").objectReferenceValue = target;

                SerializedProperty choices = entry.FindPropertyRelative("choices");
                choices.arraySize = 2;
                choices.GetArrayElementAtIndex(0).boolValue = plan.Activations[i].Choices[0];
                choices.GetArrayElementAtIndex(1).boolValue = plan.Activations[i].Choices[1];

                written++;
            }

            activations.arraySize = written;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Dispose();

            WriteMenuItem(control.Host, component, plan, undoName);

            EditorUtility.SetDirty(component);
            return component;
        }

        private static void WriteMenuItem(
            GameObject host, HVRVixxyControl control, VixxyControlPlan plan, string undoName)
        {
            HVRVixxyMenuItem item = Undo.AddComponent<HVRVixxyMenuItem>(host);
            Undo.SetCurrentGroupName(undoName);

            SerializedObject serialized = new SerializedObject(item);

            SerializedProperty title = serialized.FindProperty("title");
            if (title != null)
            {
                title.stringValue = plan.MenuName;
            }

            SerializedProperty titleSelection = serialized.FindProperty("titleSelection");
            if (titleSelection != null)
            {
                // Use the title written above rather than the object's name, which is the
                // avatar root here and would label every control identically.
                titleSelection.enumValueIndex =
                    (int)HVRVixxyTitleSelection.UseCustomTitle;
            }

            SerializedProperty linked = serialized.FindProperty("control");
            if (linked != null)
            {
                linked.objectReferenceValue = control;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Dispose();
            EditorUtility.SetDirty(item);
        }
    }
}
