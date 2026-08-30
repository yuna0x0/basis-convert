using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

namespace yuna0x0.Basis.Convert.Sources
{
    /// <summary>One animator layer that behaves as an on/off switch for a single parameter.</summary>
    public sealed class FxToggleLayer
    {
        public string LayerName = string.Empty;
        public string Parameter = string.Empty;
        public AnimationClip WhenOff;
        public AnimationClip WhenOn;

        /// <summary>States found beyond the two an on/off switch needs.</summary>
        public int ExtraStates;
    }

    /// <summary>
    /// Finds the animator layers behind an avatar's menu toggles.
    /// <para>
    /// Unlike the rest of the source data, an AnimatorController is a native Unity type, so it is
    /// read through the editor API rather than out of YAML. Its scripts are not missing because
    /// there are none: nothing here belongs to the VRChat SDK.
    /// </para>
    /// </summary>
    public static class FxControllerReader
    {
        public static List<FxToggleLayer> FindToggleLayers(
            AnimatorController controller, ICollection<string> parameters)
        {
            List<FxToggleLayer> found = new List<FxToggleLayer>();
            if (controller == null || parameters == null || parameters.Count == 0)
            {
                return found;
            }

            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                AnimatorStateMachine machine = layer.stateMachine;
                if (machine == null)
                {
                    continue;
                }

                // A layer is a toggle for a parameter only when that parameter is the only one
                // steering it. Gesture layers commonly carry a second condition on an unrelated
                // parameter, and treating those as that parameter's own layer picks up the wrong
                // clips entirely.
                HashSet<string> steering = SteeringParameters(machine);
                if (steering.Count != 1)
                {
                    continue;
                }

                foreach (string parameter in parameters)
                {
                    if (!steering.Contains(parameter))
                    {
                        continue;
                    }

                    FxToggleLayer toggle = ReadLayer(layer, machine, parameter);
                    if (toggle != null)
                    {
                        found.Add(toggle);
                    }
                }
            }

            return found;
        }

        /// <summary>Every parameter any transition in the layer tests.</summary>
        private static HashSet<string> SteeringParameters(AnimatorStateMachine machine)
        {
            HashSet<string> parameters = new HashSet<string>();

            foreach (ChildAnimatorState child in machine.states)
            {
                foreach (AnimatorStateTransition transition in child.state.transitions)
                {
                    Collect(transition.conditions, parameters);
                }
            }

            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
            {
                Collect(transition.conditions, parameters);
            }

            foreach (AnimatorTransition transition in machine.entryTransitions)
            {
                Collect(transition.conditions, parameters);
            }

            return parameters;
        }

        private static void Collect(AnimatorCondition[] conditions, HashSet<string> into)
        {
            if (conditions == null)
            {
                return;
            }

            foreach (AnimatorCondition condition in conditions)
            {
                if (!string.IsNullOrEmpty(condition.parameter))
                {
                    into.Add(condition.parameter);
                }
            }
        }

        private static FxToggleLayer ReadLayer(
            AnimatorControllerLayer layer, AnimatorStateMachine machine, string parameter)
        {
            AnimatorState on = null;
            AnimatorState off = null;
            bool mentionsParameter = false;

            foreach (ChildAnimatorState child in machine.states)
            {
                foreach (AnimatorStateTransition transition in child.state.transitions)
                {
                    Classify(transition.conditions, transition.destinationState, parameter,
                        ref mentionsParameter, ref on, ref off);
                }
            }

            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
            {
                Classify(transition.conditions, transition.destinationState, parameter,
                    ref mentionsParameter, ref on, ref off);
            }

            foreach (AnimatorTransition transition in machine.entryTransitions)
            {
                Classify(transition.conditions, transition.destinationState, parameter,
                    ref mentionsParameter, ref on, ref off);
            }

            if (!mentionsParameter)
            {
                return null;
            }

            // Whatever the layer starts in is the off state unless a transition said otherwise.
            off ??= machine.defaultState;

            if (on == null || off == null || on == off)
            {
                return null;
            }

            return new FxToggleLayer
            {
                LayerName = layer.name,
                Parameter = parameter,
                WhenOn = on.motion as AnimationClip,
                WhenOff = off.motion as AnimationClip,
                ExtraStates = Mathf.Max(0, machine.states.Length - 2),
            };
        }

        /// <summary>
        /// A condition that turns the parameter on points at the on state, one that turns it off
        /// points at the off state. Equals and NotEqual cover int parameters, where a menu
        /// control selects one value out of several.
        /// </summary>
        private static void Classify(
            AnimatorCondition[] conditions, AnimatorState destination, string parameter,
            ref bool mentionsParameter, ref AnimatorState on, ref AnimatorState off)
        {
            if (destination == null || conditions == null)
            {
                return;
            }

            foreach (AnimatorCondition condition in conditions)
            {
                if (condition.parameter != parameter)
                {
                    continue;
                }

                mentionsParameter = true;

                switch (condition.mode)
                {
                    case AnimatorConditionMode.If:
                    case AnimatorConditionMode.Equals:
                    case AnimatorConditionMode.Greater:
                        on ??= destination;
                        break;

                    case AnimatorConditionMode.IfNot:
                    case AnimatorConditionMode.NotEqual:
                    case AnimatorConditionMode.Less:
                        off ??= destination;
                        break;
                }
            }
        }
    }
}
