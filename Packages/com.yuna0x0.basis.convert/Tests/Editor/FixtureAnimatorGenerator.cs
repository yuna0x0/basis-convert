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

            // A toggle whose clip animates over time rather than switching something. Vixxy
            // cannot hold that as a value per choice, so it becomes a motion the control
            // switches on.
            controller.AddParameter("Wag", AnimatorControllerParameterType.Bool);
            controller.AddLayer("Wag");
            AnimatorControllerLayer wag = controller.layers[3];
            wag.defaultWeight = 1f;

            AnimationClip wagOff = Clip("Tail", active: true);
            AnimationClip wagOn = SwayClip("Tail");
            Save(wagOff, "WagOff");
            Save(wagOn, "WagOn");

            AnimatorState wagOffState = wag.stateMachine.AddState("Off");
            AnimatorState wagOnState = wag.stateMachine.AddState("On", new Vector3(0f, 100f, 0f));
            wagOffState.motion = wagOff;
            wagOnState.motion = wagOn;
            wag.stateMachine.defaultState = wagOffState;

            AnimatorStateTransition wagToOn = wagOffState.AddTransition(wagOnState);
            wagToOn.AddCondition(AnimatorConditionMode.If, 0f, "Wag");
            AnimatorStateTransition wagToOff = wagOnState.AddTransition(wagOffState);
            wagToOff.AddCondition(AnimatorConditionMode.IfNot, 0f, "Wag");

            // A toggle guarded by one of VRChat's own parameters, which is how a gimmick that
            // only runs for the wearer is written. Nothing on Basis drives IsLocal.
            controller.AddParameter("Ear", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsLocal", AnimatorControllerParameterType.Bool);
            controller.AddLayer("Ear");
            AnimatorControllerLayer ear = controller.layers[4];
            ear.defaultWeight = 1f;

            AnimationClip earOn = Clip("Hair", active: false);
            Save(earOn, "EarOn");

            AnimatorState earOffState = ear.stateMachine.AddState("Off");
            AnimatorState earOnState = ear.stateMachine.AddState("On", new Vector3(0f, 100f, 0f));
            earOnState.motion = earOn;
            ear.stateMachine.defaultState = earOffState;

            AnimatorStateTransition earToOn = earOffState.AddTransition(earOnState);
            earToOn.AddCondition(AnimatorConditionMode.If, 0f, "Ear");
            earToOn.AddCondition(AnimatorConditionMode.If, 0f, "IsLocal");
            AnimatorStateTransition earToOff = earOnState.AddTransition(earOffState);
            earToOff.AddCondition(AnimatorConditionMode.IfNot, 0f, "Ear");

            // Ambient motion: a layer with nothing to switch it, holding a looping clip that
            // turns a bone. This is what a swaying tail or a twitching ear is authored as.
            controller.AddLayer("TailIdle");
            AnimatorControllerLayer idle = controller.layers[5];
            idle.defaultWeight = 1f;

            AnimationClip sway = SwayClip("Tail");
            Save(sway, "TailIdle");

            AnimatorState swayState = idle.stateMachine.AddState("Idle");
            swayState.motion = sway;
            idle.stateMachine.defaultState = swayState;

            controller.layers = new[] {tail, hair, size, wag, ear, idle};

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Wrote the sample avatar's animator to {Folder}.");
        }

        /// <summary>
        /// A mesh with named blendshapes, for the VRM expression fixtures.
        /// <para>
        /// VRM names a blendshape by its position in the mesh, so testing that at all needs a
        /// real mesh with real shapes on it. Two triangles and two shapes is enough, and a mesh
        /// is a native Unity asset with no missing script to hand-write.
        /// </para>
        /// </summary>
        [MenuItem(ProductInfo.ToolsMenu + "Development/Regenerate VRM Face Mesh")]
        public static void GenerateFaceMesh()
        {
            const string folder =
                "Packages/com.yuna0x0.basis.convert/Tests/Editor/Fixtures/SampleVrmAvatar";

            Mesh mesh = new Mesh {name = "SampleFace"};
            mesh.vertices = new[]
            {
                new Vector3(-0.1f, 0f, 0f),
                new Vector3(0.1f, 0f, 0f),
                new Vector3(0f, 0.2f, 0f),
            };

            mesh.triangles = new[] {0, 1, 2};
            mesh.normals = new[] {Vector3.back, Vector3.back, Vector3.back};

            // The order matters: it is what an expression refers to.
            mesh.AddBlendShapeFrame("Smile", 100f,
                new[] {Vector3.up * 0.01f, Vector3.up * 0.01f, Vector3.zero},
                null, null);

            mesh.AddBlendShapeFrame("BrowUp", 100f,
                new[] {Vector3.zero, Vector3.zero, Vector3.up * 0.02f},
                null, null);

            AssetDatabase.CreateAsset(mesh, $"{folder}/SampleFace.mesh");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Wrote the VRM face mesh to {folder}, guid "
                + AssetDatabase.AssetPathToGUID($"{folder}/SampleFace.mesh"));
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

        /// <summary>
        /// A looping clip that turns one bone back and forth, which is what ambient motion looks
        /// like: a curve that moves over time rather than one holding a state.
        /// </summary>
        private static AnimationClip SwayClip(string path)
        {
            AnimationClip clip = new AnimationClip {name = path + " Idle"};

            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, -8f), new Keyframe(1f, 8f), new Keyframe(2f, -8f));

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.z"),
                curve);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            return clip;
        }

        private static void Save(AnimationClip clip, string name)
        {
            AssetDatabase.CreateAsset(clip, $"{Folder}/{name}.anim");
        }
    }
}
