namespace yuna0x0.Basis.Convert.Mapping
{
    /// <summary>
    /// The tunable half of the PhysBone to jiggle mapping.
    /// <para>
    /// Most of the mapping is exact and lives in the mapper. Two parts are not: how PhysBone's
    /// return-to-pose forces become jiggle stiffness, and how PhysBone's wobble becomes jiggle
    /// drag. Those are collected here so they are data rather than constants buried in code, can
    /// be revised once real avatars have been compared side by side, and can be surfaced in the
    /// conversion UI.
    /// </para>
    /// </summary>
    public sealed class JiggleMappingProfile
    {
        public static JiggleMappingProfile Default => new JiggleMappingProfile();

        /// <summary>
        /// Weight on PhysBone `pull` when producing jiggle `stiffness`. Pull is the force
        /// returning bones to their rest pose, which is the closest thing PhysBone has to what
        /// jiggle calls stiffness, so it carries most of the weight.
        /// </summary>
        public float PullToStiffness = 1.0f;

        /// <summary>
        /// Weight on PhysBone `stiffness` when producing jiggle `stiffness`. PhysBone stiffness
        /// resists leaving the rest orientation, which reinforces pull rather than replacing it.
        /// Only present in Advanced integration.
        /// </summary>
        public float StiffnessToStiffness = 0.5f;

        /// <summary>Jiggle drag produced when PhysBone spring is 0, meaning no wobble at all.</summary>
        public float DragAtNoSpring = 0.6f;

        /// <summary>Jiggle drag produced when PhysBone spring is 1, meaning maximum wobble.</summary>
        public float DragAtFullSpring = 0.05f;

        /// <summary>
        /// PhysBone angle limits are in degrees; jiggle's angleLimit is 0..1 where 1 is 90
        /// degrees of deviation.
        /// </summary>
        public float AngleLimitDegreesAtOne = 90f;
    }
}
