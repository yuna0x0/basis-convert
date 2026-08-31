using UnityEngine;

namespace yuna0x0.Basis.Convert.Model
{
    public enum VrmConstraintKind
    {
        /// <summary>Copies the source's rotation, as a delta from its rest pose.</summary>
        Rotation = 0,

        /// <summary>Turns the object so one of its axes points at the source.</summary>
        Aim = 1,

        /// <summary>Copies the source's rotation about one axis only.</summary>
        Roll = 2,
    }

    /// <summary>
    /// The axis an aim constraint points at its source, in the order VRM's own enum declares
    /// them.
    /// </summary>
    public enum VrmAimAxis
    {
        PositiveX = 0,
        NegativeX = 1,
        PositiveY = 2,
        NegativeY = 3,
        PositiveZ = 4,
        NegativeZ = 5,
    }

    /// <summary>One VRM 1.0 node constraint, read out of prefab YAML.</summary>
    public sealed class VrmConstraintData
    {
        public long DocumentFileId;

        /// <summary>The object the constraint drives, which is the one it sits on.</summary>
        public long OwnerGameObjectFileId;

        public VrmConstraintKind Kind;

        /// <summary>The transform it follows.</summary>
        public long SourceTransformFileId;

        public float Weight = 1f;

        /// <summary>Aim constraints only.</summary>
        public VrmAimAxis AimAxis = VrmAimAxis.PositiveX;

        /// <summary>Roll constraints only: 0 is X, 1 is Y, 2 is Z.</summary>
        public int RollAxis;

        /// <summary>The local axis an aim constraint points, as a vector.</summary>
        public Vector3 AimVector => AimAxis switch
        {
            VrmAimAxis.PositiveX => Vector3.right,
            VrmAimAxis.NegativeX => Vector3.left,
            VrmAimAxis.PositiveY => Vector3.up,
            VrmAimAxis.NegativeY => Vector3.down,
            VrmAimAxis.PositiveZ => Vector3.forward,
            _ => Vector3.back,
        };
    }
}
