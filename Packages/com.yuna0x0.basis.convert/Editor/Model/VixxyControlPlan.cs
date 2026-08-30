using System.Collections.Generic;

namespace yuna0x0.Basis.Convert.Model
{
    /// <summary>One object a control switches, and its state in each choice.</summary>
    public sealed class VixxyActivationPlan
    {
        /// <summary>Transform path relative to the avatar root.</summary>
        public string Path = string.Empty;

        /// <summary>Active state per choice, index 0 being off and 1 being on.</summary>
        public bool[] Choices = new bool[2];

        /// <summary>
        /// False when only one side of the toggle animated this object. The other side leaves it
        /// at whatever the avatar was authored with, so that state is read from the hierarchy
        /// rather than guessed.
        /// </summary>
        public bool BothSidesAnimated = true;
    }

    /// <summary>
    /// One HVR Vixxy control to create, rebuilt from a VRChat menu toggle.
    /// <para>
    /// Vixxy stores object switching as activations holding the object's Transform, with a bool
    /// per choice, rather than as animation. That is why only clips holding a constant can be
    /// rebuilt: there is nowhere for a curve to go.
    /// </para>
    /// </summary>
    public sealed class VixxyControlPlan
    {
        public string MenuName = string.Empty;
        public string Parameter = string.Empty;

        /// <summary>Which choice the control starts in.</summary>
        public bool DefaultOn;

        public List<VixxyActivationPlan> Activations = new List<VixxyActivationPlan>();
        public List<ConversionDiagnostic> Diagnostics = new List<ConversionDiagnostic>();
    }
}
