using System.Collections.Generic;

namespace yuna0x0.Basis.Convert.Model
{
    /// <summary>
    /// What an expression is for. VRM names a handful of roles the runtime drives itself, and
    /// Basis drives the same ones from its own avatar component, so those are not rebuilt as
    /// menu controls: a viseme belongs to lip sync rather than to a menu.
    /// </summary>
    public enum VrmExpressionRole
    {
        /// <summary>An expression the author added, which is what a menu control is for.</summary>
        Custom = 0,

        /// <summary>Happy, angry, sad, relaxed, surprised, and 0.x's joy, sorrow and fun.</summary>
        Emotion = 1,

        /// <summary>The five lip sync shapes, which Basis drives from its viseme list.</summary>
        Viseme = 2,

        Blink = 3,
        LookAt = 4,

        /// <summary>The resting face, which is how the avatar already looks.</summary>
        Neutral = 5,
    }

    /// <summary>One blendshape an expression sets, and how far.</summary>
    public sealed class VrmMorphBinding
    {
        /// <summary>Transform path of the renderer, relative to the avatar root.</summary>
        public string Path = string.Empty;

        /// <summary>Index of the blendshape in the mesh, which is how VRM names it.</summary>
        public int Index;

        /// <summary>Weight on Unity's 0 to 100 scale.</summary>
        public float Weight;

        /// <summary>
        /// The blendshape's name, looked up on the mesh. VRM refers to a shape by index and
        /// Vixxy by name, so this is filled in once the renderer is known.
        /// </summary>
        public string ShapeName = string.Empty;
    }

    /// <summary>One VRM expression, from either format.</summary>
    public sealed class VrmExpressionData
    {
        public string Name = string.Empty;
        public VrmExpressionRole Role = VrmExpressionRole.Custom;

        /// <summary>True when the expression is all or nothing rather than a range.</summary>
        public bool IsBinary;

        public List<VrmMorphBinding> Bindings = new List<VrmMorphBinding>();

        /// <summary>
        /// Material bindings the expression also carries. VRM changes a material's colour or its
        /// UVs by naming the material rather than the renderer, which is not how Vixxy addresses
        /// one, so these are counted and reported rather than converted.
        /// </summary>
        public int MaterialBindingCount;
    }
}
