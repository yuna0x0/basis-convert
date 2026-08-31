using System.Collections.Generic;
using GatorDragonGames.JigglePhysics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Pipeline;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// End to end over an avatar that ships with this package.
    /// <para>
    /// Most of this suite needs a purchased avatar and skips itself without one, which means it
    /// proves nothing on anyone else's machine. This fixture is hand-written: a prefab carrying
    /// the components a VRChat avatar has, as the missing scripts they arrive as, plus an
    /// expression menu, parameters, an animator and its clips. No third-party asset is
    /// redistributed; script identities and field names are facts about a file format.
    /// </para>
    /// </summary>
    public class SampleAvatarFixtureTests
    {
        private const string FixturePath =
            "Packages/com.yuna0x0.basis.convert/Tests/Editor/Fixtures/SampleAvatar/SampleAvatar.prefab";

        /// <summary>
        /// Where a baked motion clip is written during these tests. The planner would put it
        /// beside the animation it came from, which here is inside this package; a test writes
        /// into the project instead, and deletes it again.
        /// </summary>
        private const string MotionFolder = "Assets/WatariMotionTest";

        private GameObject _instance;

        [TearDown]
        public void TearDown()
        {
            if (_instance != null)
            {
                Object.DestroyImmediate(_instance);
                _instance = null;
            }

            if (AssetDatabase.IsValidFolder(MotionFolder))
            {
                AssetDatabase.DeleteAsset(MotionFolder);
            }
        }

        private static AvatarConversionPlan Plan()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);

            // The planner would bake beside the animation, which for this fixture is inside the
            // package. Every test here redirects that, so a run leaves nothing behind in the repo.
            foreach (PlannedAuthoredMotion motion in plan.AuthoredMotions)
            {
                motion.OutputFolder = MotionFolder;
            }

            foreach (ConversionDiagnostic diagnostic in plan.AllDiagnostics())
            {
                TestContext.WriteLine($"[{diagnostic.Severity}] {diagnostic.Code}: {diagnostic.Message}");
            }

            return plan;
        }

        [Test]
        public void TheFixtureLoads()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath);
            Assert.That(prefab, Is.Not.Null, $"No prefab at {FixturePath}.");
            Assert.That(prefab.transform.Find("Tail"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Hair/HairBone1/HairBone2"), Is.Not.Null);
        }

        [Test]
        public void ThePhysBoneAndConstraintAreRead()
        {
            AvatarConversionPlan plan = Plan();

            Assert.That(plan.PhysBonesFound, Is.EqualTo(1));
            Assert.That(plan.ConstraintsFound, Is.EqualTo(1));
            Assert.That(plan.Rigs.Count, Is.EqualTo(1));
            Assert.That(plan.Rigs[0].SourceRootBone.name, Is.EqualTo("HairBone1"),
                "The rig roots at the PhysBone's root transform, not the object it sits on.");
            Assert.That(plan.Constraints.Count, Is.EqualTo(1));
            Assert.That(plan.Constraints[0].Plan.Kind,
                Is.EqualTo(BasisConstraintKind.Rotation));
        }

        [Test]
        public void TheDescriptorIsRead()
        {
            AvatarConversionPlan plan = Plan();

            Assert.That(plan.Descriptor, Is.Not.Null);
            Assert.That(plan.Descriptor.Plan.EyePosition.x, Is.EqualTo(1.6f).Within(1e-4f),
                "Basis takes the eye position from the descriptor's view position y and z.");
            Assert.That(plan.Profile.Kind, Is.EqualTo("VRChat avatar"));
        }

        [Test]
        public void TheExpressionMenuIsRead()
        {
            AvatarConversionPlan plan = Plan();

            Assert.That(plan.Expressions.Menus.Count, Is.EqualTo(1));
            Assert.That(plan.Expressions.CountOf(VrcExpressionControlType.Toggle),
                Is.EqualTo(6),
                "Three toggles and three controls sharing a selector parameter.");
            Assert.That(plan.Expressions.CountOf(VrcExpressionControlType.RadialPuppet),
                Is.EqualTo(1));
        }

        [Test]
        public void TheToggleIsTracedAndSwitchesTheRightWayRound()
        {
            AvatarConversionPlan plan = Plan();

            ResolvedToggle tail = plan.Toggles.Find(toggle => toggle.Parameter == "Tail");
            Assert.That(tail, Is.Not.Null, "The Tail layer is a plain two-state toggle.");
            Assert.That(tail.WhenOn.Deactivated, Does.Contain("Tail"));
            Assert.That(tail.WhenOff.Deactivated, Is.Empty,
                "The off side animates nothing, which is the shape that used to invert.");

            PlannedVixxyControl control =
                plan.VixxyControls.Find(c => c.Plan.Parameter == "Tail");
            Assert.That(control, Is.Not.Null);

            VixxyActivationPlan activation = control.Plan.Activations[0];
            Assert.That(activation.Path, Is.EqualTo("Tail"));
            Assert.That(activation.Choices[0], Is.True, "Off leaves it as the avatar authored it.");
            Assert.That(activation.Choices[1], Is.False, "On hides it, as the clip says.");
        }

        [Test]
        public void ASelectorBecomesOneControlWithAChoicePerValue()
        {
            // Three menu controls share HairStyle and pick different values from it, and its
            // layer holds one state per value. Vixxy holds that as one control with three
            // choices rather than three separate toggles.
            AvatarConversionPlan plan = Plan();

            ResolvedToggle selector = plan.Toggles.Find(toggle => toggle.Parameter == "HairStyle");
            Assert.That(selector, Is.Not.Null);
            Assert.That(selector.IsSelector, Is.True);
            Assert.That(selector.Choices.Count, Is.EqualTo(3));

            PlannedVixxyControl control =
                plan.VixxyControls.Find(c => c.Plan.Parameter == "HairStyle");
            Assert.That(control, Is.Not.Null, "The selector rebuilds as a Vixxy control.");
            Assert.That(control.Plan.ChoiceNames,
                Is.EqualTo(new[] {"Hair_Long", "Hair_Braid", "Hair_Short"}),
                "Choices are named by the menu entries that select them, in value order.");
            Assert.That(control.Plan.ChoiceValues, Is.EqualTo(new[] {0, 1, 2}));

            foreach (VixxyActivationPlan activation in control.Plan.Activations)
            {
                Assert.That(activation.Choices.Length, Is.EqualTo(3),
                    $"{activation.Path} needs a state for every choice.");
            }
        }

        [Test]
        public void ASelectorIsWrittenWithItsChoices()
        {
            AvatarConversionPlan plan = Plan();
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));

            AvatarConverter.Apply(plan, _instance);

            HVR.Vixxy.HVRVixxyControl written = null;
            foreach (HVR.Vixxy.HVRVixxyControl control in
                     _instance.GetComponentsInChildren<HVR.Vixxy.HVRVixxyControl>(true))
            {
                if (control.choices != null && control.choices.Length == 3)
                {
                    written = control;
                    break;
                }
            }

            Assert.That(written, Is.Not.Null, "The selector's control has three choices.");
            Assert.That(written.choices[1].title, Is.EqualTo("Hair_Braid"));
            Assert.That(written.choices[2].value, Is.EqualTo(2f),
                "A choice carries the parameter value that selects it.");
        }

        [Test]
        public void ARadialsLabelsAreNotReadAsMoreParameters()
        {
            // A puppet's labels are a list of their own further down the same control. Reading
            // them as subParameters would invent parameters the menu never named.
            AvatarConversionPlan plan = Plan();

            VrcExpressionControl radial = null;
            foreach (VrcExpressionMenu menu in plan.Expressions.Menus)
            {
                foreach (VrcExpressionControl control in menu.Controls)
                {
                    if (control.Type == VrcExpressionControlType.RadialPuppet)
                    {
                        radial = control;
                    }
                }
            }

            Assert.That(radial, Is.Not.Null);
            Assert.That(radial.SubParameters, Is.EqualTo(new[] {"TailSize"}));
        }

        [Test]
        public void ARadialPuppetBecomesASliderBetweenItsEnds()
        {
            // The menu entry names its parameter under subParameters rather than as its own,
            // and the layer holds a blend tree rather than states to switch between.
            AvatarConversionPlan plan = Plan();

            ResolvedToggle puppet = plan.Toggles.Find(toggle => toggle.Parameter == "TailSize");
            Assert.That(puppet, Is.Not.Null, "A radial puppet drives a float through a blend tree.");
            Assert.That(puppet.IsSlider, Is.True);
            Assert.That(puppet.Choices.Count, Is.EqualTo(2), "The two ends of the range.");
            Assert.That(puppet.Choices[0].Effects.Deactivated, Does.Contain("Tail"));
            Assert.That(puppet.Choices[1].Effects.Activated, Does.Contain("Tail"));

            PlannedVixxyControl control =
                plan.VixxyControls.Find(c => c.Plan.Parameter == "TailSize");
            Assert.That(control, Is.Not.Null);
            Assert.That(control.Plan.IsSlider, Is.True,
                "Vixxy shows a continuous control as a slider and interpolates between choices.");
        }

        [Test]
        public void APuppetsMenuItemIsWrittenAsASlider()
        {
            AvatarConversionPlan plan = Plan();
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));

            AvatarConverter.Apply(plan, _instance);

            bool foundSlider = false;
            foreach (HVR.Vixxy.HVRVixxyMenuItem item in
                     _instance.GetComponentsInChildren<HVR.Vixxy.HVRVixxyMenuItem>(true))
            {
                SerializedObject serialized = new SerializedObject(item);
                if (serialized.FindProperty("presentation").enumValueIndex
                    == (int)HVR.Vixxy.HVRVixxyControlPresentation.Slider)
                {
                    foundSlider = true;
                }

                serialized.Dispose();
            }

            Assert.That(foundSlider, Is.True, "The puppet's menu item is shown as a slider.");
        }

        /// <summary>The movement labelled this, on whichever component carries it.</summary>
        private static BasisAuthoredMotion.Movement MovementNamed(GameObject root, string label)
        {
            foreach (BasisAuthoredMotion component in
                     root.GetComponents<BasisAuthoredMotion>())
            {
                foreach (BasisAuthoredMotion.Movement movement in component.movements)
                {
                    if (movement.label == label)
                    {
                        return movement;
                    }
                }
            }

            return null;
        }

        [Test]
        public void ALayerNothingSwitchesBecomesAuthoredMotion()
        {
            // Nothing steers this layer, so it plays from the moment the avatar loads. Basis has
            // no animator layers on an avatar, so it can only be replayed as authored motion.
            AvatarConversionPlan plan = Plan();

            PlannedAuthoredMotion motion =
                plan.AuthoredMotions.Find(planned => planned.Plan.Label == "TailIdle");

            Assert.That(motion, Is.Not.Null, "The idle layer is ambient motion.");
            Assert.That(motion.Plan.Paths, Does.Contain("Tail"));
            Assert.That(motion.Plan.Loop, Is.True);
            Assert.That(motion.SourceClip, Is.Not.Null, "The clip is baked when the plan runs.");
            Assert.That(AvatarConversionPlanner.Plan(FixturePath).AuthoredMotions[0].OutputFolder,
                Is.EqualTo(
                    "Packages/com.yuna0x0.basis.convert/Tests/Editor/Fixtures/SampleAvatar/"
                    + "Watari Motion"),
                "A baked clip goes in a folder of ours beside the animation it came from.");
            Assert.That(plan.AllDiagnostics().HasCode("motion.baked"), Is.True);
        }

        [Test]
        public void AToggleThatAnimatesBecomesAMotionTheControlSwitches()
        {
            // Vixxy holds a value per choice, not a curve, so this used to be reported and
            // dropped whole. The animation becomes a motion, and the control switches it.
            AvatarConversionPlan plan = Plan();

            PlannedVixxyControl control =
                plan.VixxyControls.Find(c => c.Plan.Parameter == "Wag");

            Assert.That(control, Is.Not.Null, "The toggle is rebuilt rather than dropped.");
            Assert.That(control.Plan.Motions.Count, Is.EqualTo(1));
            Assert.That(control.Motions.Count, Is.EqualTo(1));
            Assert.That(control.Motions[0].Plan.Paths, Does.Contain("Tail"));
            Assert.That(plan.AuthoredMotions, Does.Contain(control.Motions[0]),
                "A switched motion is written and counted with the rest.");

            VixxyActivationPlan activation =
                control.Plan.Activations.Find(a => a.MotionIndex == 0);
            Assert.That(activation, Is.Not.Null);
            Assert.That(activation.Choices[0], Is.False, "Off does not play it.");
            Assert.That(activation.Choices[1], Is.True, "On does.");
            Assert.That(plan.AllDiagnostics().HasCode("motion.switched"), Is.True);
        }

        [Test]
        public void AControlSwitchesTheMotionComponentItself()
        {
            AvatarConversionPlan plan = Plan();

            _instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));

            AvatarConverter.Apply(plan, _instance);

            BasisAuthoredMotion.Movement movement = MovementNamed(_instance, "Tail_Wag");
            Assert.That(movement, Is.Not.Null, "The toggle's animation was baked.");
            Assert.That(movement.bakedClip, Is.Not.Null);

            // The activation list is internal to Vixxy's assembly, so it is read the same way
            // it is written.
            BasisAuthoredMotion switched = null;
            foreach (HVR.Vixxy.HVRVixxyControl candidate in
                     _instance.GetComponents<HVR.Vixxy.HVRVixxyControl>())
            {
                SerializedObject serialized = new SerializedObject(candidate);
                SerializedProperty activations = serialized.FindProperty("activations");

                for (int i = 0; i < activations.arraySize; i++)
                {
                    Object held = activations.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("component").objectReferenceValue;

                    if (held is BasisAuthoredMotion motion)
                    {
                        switched = motion;
                    }
                }

                serialized.Dispose();
            }

            Assert.That(switched, Is.Not.Null,
                "A Vixxy activation holds the motion component, which is a type Vixxy permits.");

            // The control starts off, so the motion it switches starts disabled rather than
            // playing on an avatar nobody has touched yet.
            Assert.That(switched.enabled, Is.False);
        }

        [Test]
        public void AControlStartsWhereTheAvatarDeclaresItsParameterDefaults()
        {
            // The fixture declares every parameter defaulting to 0, so every control starts at
            // its first choice. A parameter declared otherwise carries across the same way.
            AvatarConversionPlan plan = Plan();

            PlannedVixxyControl tail = plan.VixxyControls.Find(c => c.Plan.Parameter == "Tail");
            Assert.That(tail, Is.Not.Null);
            Assert.That(tail.Plan.DefaultValue, Is.EqualTo(0f));
            Assert.That(tail.Plan.ChoiceValues[0], Is.EqualTo(0),
                "The default names a choice value, which is what Vixxy compares against.");
        }

        [Test]
        public void AToggleGuardedByAVrchatParameterIsRebuilt()
        {
            // The layer waits on IsLocal as well as its own parameter. Basis has nothing that
            // drives it, so the control switches regardless and says so.
            AvatarConversionPlan plan = Plan();

            ResolvedToggle ear = plan.Toggles.Find(toggle => toggle.Parameter == "Ear");
            Assert.That(ear, Is.Not.Null, "A guard is not a second steering parameter.");
            Assert.That(ear.GuardedBy, Is.EqualTo(new[] {"IsLocal"}));

            Assert.That(plan.VixxyControls.Find(c => c.Plan.Parameter == "Ear"), Is.Not.Null);
            Assert.That(plan.AllDiagnostics().HasCode("vixxy.builtinGuard"), Is.True);
        }

        [Test]
        public void TheAmbientLayerIsNotReadAsAToggle()
        {
            // It has no parameter, so nothing in the menu can be tied to it. Reading it as a
            // toggle as well would rebuild the same motion twice.
            AvatarConversionPlan plan = Plan();

            foreach (ResolvedToggle toggle in plan.Toggles)
            {
                Assert.That(toggle.LayerName, Is.Not.EqualTo("TailIdle"));
            }
        }

        [Test]
        public void ConvertingBakesTheClipAndAddsTheComponent()
        {
            AvatarConversionPlan plan = Plan();

            _instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));

            ConversionResult result = AvatarConverter.Apply(plan, _instance);

            Assert.That(result.AuthoredMotionsWritten, Is.EqualTo(plan.AuthoredMotions.Count));
            Assert.That(result.MotionAssets.Count, Is.EqualTo(plan.AuthoredMotions.Count));

            BasisAuthoredMotion.Movement movement = MovementNamed(_instance, "TailIdle");
            Assert.That(movement, Is.Not.Null, "The ambient layer's own component.");
            Assert.That(movement.kind, Is.EqualTo(BasisAuthoredMotion.Movement.Kind.Sequence));
            Assert.That(movement.loop, Is.True);
            Assert.That(movement.sequenceRoot, Is.EqualTo(_instance.transform));
            Assert.That(movement.bakedClip, Is.Not.Null);

            BasisMotionClip baked = movement.bakedClip;
            Assert.That(baked.paths, Is.EqualTo(new[] {"Tail"}));
            Assert.That(baked.transformCount, Is.EqualTo(1));
            Assert.That(baked.frameCount, Is.GreaterThan(1));
            Assert.That(baked.rotationSamples.Length,
                Is.EqualTo(baked.transformCount * baked.frameCount));

            // The whole point of baking is that the pose moves. A clip sampled without the
            // animation applied would hold the same rotation on every frame.
            Assert.That(baked.rotationSamples[0],
                Is.Not.EqualTo(baked.rotationSamples[baked.frameCount / 2]),
                "The sampled rotation changes over the clip.");
        }

        [Test]
        public void ConvertingTwiceReplacesTheControlsRatherThanStackingThem()
        {
            // Everything Vixxy sits on the avatar root rather than on a transform of its own, so
            // the rule that protects hand-made components elsewhere says nothing here. What a
            // re-conversion replaces is matched by the names it is about to write.
            AvatarConversionPlan plan = Plan();

            _instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));

            AvatarConverter.Apply(plan, _instance);
            int first = _instance.GetComponents<HVR.Vixxy.HVRVixxyControl>().Length;
            Assert.That(first, Is.GreaterThan(0));

            AvatarConverter.RemoveReplaceable(plan, _instance, "Re-convert");
            AvatarConverter.Apply(plan, _instance);

            Assert.That(_instance.GetComponents<HVR.Vixxy.HVRVixxyControl>().Length,
                Is.EqualTo(first), "A second conversion replaces the controls it wrote.");
            Assert.That(_instance.GetComponents<HVR.Vixxy.HVRVixxyMenuItem>().Length,
                Is.EqualTo(first), "One menu item per control, not two.");
        }

        [Test]
        public void AControlSomebodyElseAddedIsLeftAlone()
        {
            AvatarConversionPlan plan = Plan();

            _instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));

            // Something the user set up by hand, on the same object ours go on.
            HVR.Vixxy.HVRVixxyControl mine =
                _instance.AddComponent<HVR.Vixxy.HVRVixxyControl>();
            HVR.Vixxy.HVRVixxyMenuItem item =
                _instance.AddComponent<HVR.Vixxy.HVRVixxyMenuItem>();

            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty("title").stringValue = "Something I made";
            serialized.FindProperty("control").objectReferenceValue = mine;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Dispose();

            AvatarConverter.Apply(plan, _instance);

            List<Component> replaceable = AvatarConverter.FindReplaceable(plan, _instance);

            Assert.That(replaceable, Has.No.Member(mine));
            Assert.That(replaceable, Has.No.Member(item));
        }

        [Test]
        public void ConvertingTwiceWritesOneMotionClip()
        {
            // The baked clip is an asset, so it survives an undo and a second conversion. Both
            // runs write the same path, which is what keeps a re-convert from stacking copies.
            AvatarConversionPlan plan = Plan();

            _instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));

            ConversionResult first = AvatarConverter.Apply(plan, _instance);
            AvatarConverter.RemoveReplaceable(plan, _instance, "Re-convert");
            ConversionResult second = AvatarConverter.Apply(plan, _instance);

            Assert.That(second.MotionAssets, Is.EqualTo(first.MotionAssets));
            Assert.That(_instance.GetComponents<BasisAuthoredMotion>().Length,
                Is.EqualTo(plan.AuthoredMotions.Count),
                "Removing what a re-convert replaces takes the previous motions with it.");

            string[] assets = AssetDatabase.FindAssets("t:BasisMotionClip", new[] {MotionFolder});
            Assert.That(assets.Length, Is.EqualTo(plan.AuthoredMotions.Count),
                "One clip per motion, written over rather than beside on the second run.");
        }

        [Test]
        public void ConvertingWritesTheRigAndTheConstraint()
        {
            AvatarConversionPlan plan = Plan();
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));

            ConversionResult result = AvatarConverter.Apply(plan, _instance);

            Assert.That(result.RigsWritten, Is.EqualTo(1));
            Assert.That(result.ConstraintsWritten, Is.EqualTo(1));
            Assert.That(result.DescriptorWritten, Is.True);

            JiggleRig[] rigs = _instance.GetComponentsInChildren<JiggleRig>(true);
            Assert.That(rigs.Length, Is.EqualTo(1));
            Assert.That(rigs[0].GetJiggleRigData().rootBone.name, Is.EqualTo("HairBone1"));
        }
    }
}
