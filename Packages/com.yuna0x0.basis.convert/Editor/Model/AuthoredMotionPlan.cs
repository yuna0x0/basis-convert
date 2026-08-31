using System.Collections.Generic;

namespace yuna0x0.Basis.Convert.Model
{
    /// <summary>
    /// One movement a `BasisAuthoredMotion` will replay, planned from an animator layer that
    /// plays on its own.
    /// <para>
    /// Basis has no animator layers on an avatar, so animation that runs unprompted has nowhere
    /// to go except authored motion, which replays a baked clip from a batched job. What is
    /// planned here is the movement's configuration; the baking itself needs a scene and happens
    /// when the plan is applied.
    /// </para>
    /// </summary>
    public sealed class AuthoredMotionPlan
    {
        /// <summary>Author-facing name of the movement, taken from the layer it came from.</summary>
        public string Label = string.Empty;

        public bool Loop = true;

        /// <summary>Playback rate, where 1 is the speed the clip was authored at.</summary>
        public float Speed = 1f;

        /// <summary>
        /// Transform paths the clip turns, relative to the avatar root. These are what the bake
        /// records a rotation for, and what the movement drives.
        /// </summary>
        public List<string> Paths = new List<string>();

        public List<ConversionDiagnostic> Diagnostics = new List<ConversionDiagnostic>();
    }
}
