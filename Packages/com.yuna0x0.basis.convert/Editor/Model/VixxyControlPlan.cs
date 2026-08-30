using System.Collections.Generic;
using UnityEngine;

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
        /// Whether each side of the toggle actually animated this object. A side that did not
        /// leaves it at whatever the avatar was authored with, read from the hierarchy later.
        /// Which side was animated has to be recorded rather than inferred from the values: a
        /// clip that switches an object off looks exactly like a side that said nothing.
        /// </summary>
        public bool SetWhenOff = true;
        public bool SetWhenOn = true;

        public bool BothSidesAnimated => SetWhenOff && SetWhenOn;
    }

    /// <summary>One blendshape a control sets, and its value in each choice.</summary>
    public sealed class VixxyBlendShapePlan
    {
        public string ShapeName = string.Empty;

        /// <summary>Weight per choice, index 0 being off and 1 being on.</summary>
        public float[] Choices = new float[2];

        /// <summary>Whether each side of the toggle actually set this shape.</summary>
        public bool SetWhenOff = true;
        public bool SetWhenOn = true;

        public bool BothSidesAnimated => SetWhenOff && SetWhenOn;
    }

    /// <summary>How a material property is held, which decides the Vixxy property type.</summary>
    public enum VixxyMaterialPropertyKind
    {
        Float,
        Colour,
        Vector,
    }

    /// <summary>One material property a control sets, and its value in each choice.</summary>
    public sealed class VixxyMaterialPropertyPlan
    {
        /// <summary>Shader property name, as the material declares it.</summary>
        public string PropertyName = string.Empty;

        public VixxyMaterialPropertyKind Kind = VixxyMaterialPropertyKind.Float;

        /// <summary>Value per choice, index 0 being off and 1 being on. A float uses x only.</summary>
        public Vector4[] Choices = new Vector4[2];

        /// <summary>
        /// Per channel, whether each side of the toggle set it. A clip commonly sets one channel
        /// of a colour, or one side of the toggle only; the rest keep what the material was
        /// authored with, read from the renderer rather than guessed.
        /// </summary>
        public bool[] SetWhenOff = new bool[4];
        public bool[] SetWhenOn = new bool[4];

        public int Channels => Kind == VixxyMaterialPropertyKind.Float ? 1 : 4;
    }

    /// <summary>What a control sets on one renderer: blendshapes, material properties, or both.</summary>
    public sealed class VixxySubjectPlan
    {
        /// <summary>Transform path of the renderer, relative to the avatar root.</summary>
        public string Path = string.Empty;

        /// <summary>
        /// Full name of the renderer type the clip named. Vixxy resolves the component by this
        /// name, and is lenient between the two renderer types for material properties.
        /// </summary>
        public string RendererTypeName = typeof(SkinnedMeshRenderer).FullName;

        public List<VixxyBlendShapePlan> BlendShapes = new List<VixxyBlendShapePlan>();

        public List<VixxyMaterialPropertyPlan> MaterialProperties =
            new List<VixxyMaterialPropertyPlan>();

        public bool IsEmpty => BlendShapes.Count == 0 && MaterialProperties.Count == 0;
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
        public List<VixxySubjectPlan> Subjects = new List<VixxySubjectPlan>();
        public List<ConversionDiagnostic> Diagnostics = new List<ConversionDiagnostic>();
    }
}
