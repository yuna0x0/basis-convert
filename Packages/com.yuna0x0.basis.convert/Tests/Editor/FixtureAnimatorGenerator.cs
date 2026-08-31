using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// Builds the animator and clips the sample avatar fixture uses.
    /// <para>
    /// The rest of that fixture is hand-written YAML, because the components it carries belong to
    /// an SDK that cannot be installed here. An animator controller is a native Unity asset with
    /// no such problem, and hand-writing a state machine is a good way to produce a file that
    /// looks right and does not load, so it is generated through the editor API instead.
    /// </para>
    /// <para>
    /// Run from <c>Tools/Watari/Development/Regenerate Test Fixtures</c> after changing what the
    /// fixture is meant to contain. The generated assets are committed; this is not run by tests.
    /// </para>
    /// </summary>
    public static class FixtureAnimatorGenerator
    {
        private const string Folder =
            "Packages/com.yuna0x0.basis.convert/Tests/Editor/Fixtures/SampleAvatar";

        [MenuItem(ProductInfo.ToolsMenu + "Development/Regenerate Test Fixtures")]
        public static void Generate()
        {
            AnimationClip tailOn = Clip("Tail", active: false);
            AnimationClip hairLong = Clip("Hair", active: true);
            AnimationClip hairBraid = Clip("Hair", active: false);
            AnimationClip hairShort = Clip("Body", active: false);

            Save(tailOn, "TailOn");
            Save(hairLong, "HairLong");
            Save(hairBraid, "HairBraid");
            Save(hairShort, "HairShort");

            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath($"{Folder}/SampleFx.controller");

            controller.AddParameter("Tail", AnimatorControllerParameterType.Bool);
            controller.AddParameter("HairStyle", AnimatorControllerParameterType.Int);

            // A plain on/off toggle: one parameter, two states.
            AnimatorControllerLayer tail = controller.layers[0];
            tail.name = "Tail";
            AnimatorState tailOff = tail.stateMachine.AddState("Off");
            AnimatorState tailOnState = tail.stateMachine.AddState("On", new Vector3(0f, 100f, 0f));
            tailOnState.motion = tailOn;
            tail.stateMachine.defaultState = tailOff;

            AnimatorStateTransition toOn = tailOff.AddTransition(tailOnState);
            toOn.AddCondition(AnimatorConditionMode.If, 0f, "Tail");
            AnimatorStateTransition toOff = tailOnState.AddTransition(tailOff);
            toOff.AddCondition(AnimatorConditionMode.IfNot, 0f, "Tail");
            controller.layers = new[] {tail};

            // A selector: one int parameter choosing between three states, which is what a menu
            // with several controls sharing a parameter produces.
            controller.AddLayer("HairStyle");
            AnimatorControllerLayer hair = controller.layers[1];
            hair.defaultWeight = 1f;

            AnimatorState[] states =
            {
                hair.stateMachine.AddState("Long"),
                hair.stateMachine.AddState("Braid", new Vector3(0f, 100f, 0f)),
                hair.stateMachine.AddState("Short", new Vector3(0f, 200f, 0f)),
            };

            states[0].motion = hairLong;
            states[1].motion = hairBraid;
            states[2].motion = hairShort;
            hair.stateMachine.defaultState = states[0];

            for (int value = 0; value < states.Length; value++)
            {
                AnimatorStateTransition transition =
                    hair.stateMachine.AddAnyStateTransition(states[value]);
                transition.AddCondition(AnimatorConditionMode.Equals, value, "HairStyle");
                transition.duration = 0f;
                transition.canTransitionToSelf = false;
            }

            // A radial puppet: a float blending between two motions, which is a blend tree with
            // no transitions rather than states the parameter switches between.
            controller.AddParameter("TailSize", AnimatorControllerParameterType.Float);
            controller.AddLayer("TailSize");
            AnimatorControllerLayer size = controller.layers[2];
            size.defaultWeight = 1f;

            AnimationClip small = Clip("Tail", active: false);
            AnimationClip large = Clip("Tail", active: true);
            Save(small, "TailSmall");
            Save(large, "TailLarge");

            BlendTree tree = new BlendTree
            {
                name = "TailSize",
                blendParameter = "TailSize",
                blendType = BlendTreeType.Simple1D,
            };

            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(small, 0f);
            tree.AddChild(large, 1f);

            AnimatorState sizeState = size.stateMachine.AddState("Size");
            sizeState.motion = tree;
            size.stateMachine.defaultState = sizeState;

            controller.layers = new[] {tail, hair, size};

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Wrote the sample avatar's animator to {Folder}.");
        }

        /// <summary>One clip switching one object, which is the shape a menu toggle animates.</summary>
        private static AnimationClip Clip(string path, bool active)
        {
            AnimationClip clip = new AnimationClip {name = path};
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive"),
                AnimationCurve.Constant(0f, 1f / 60f, active ? 1f : 0f));
            return clip;
        }

        private static void Save(AnimationClip clip, string name)
        {
            AssetDatabase.CreateAsset(clip, $"{Folder}/{name}.anim");
        }
    }
}
