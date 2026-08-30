using System;
using System.Collections.Generic;
using UnityEngine;

namespace yuna0x0.Basis.Convert.Model
{
    public enum BasisConstraintKind
    {
        Position,
        Rotation,
        Scale,
        Parent,
        Aim,
        LookAt,
    }

    /// <summary>Which axes a constraint drives. Values match Basis's own axis flags.</summary>
    [Flags]
    public enum ConstraintAxes : byte
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4,
        All = X | Y | Z,
    }

    /// <summary>How an up direction is resolved. Values match Basis's own world-up enum.</summary>
    public enum ConstraintWorldUp : byte
    {
        SceneUp = 0,
        ObjectUp = 1,
        ObjectRotationUp = 2,
        Vector = 3,
        None = 4,
    }

    public sealed class BasisConstraintSourcePlan
    {
        public long TransformFileId;
        public float Weight = 1f;

        /// <summary>Parent constraints only; other kinds have no per-source offset.</summary>
        public Vector3 PositionOffset;

        /// <summary>Parent constraints only; other kinds have no per-source offset.</summary>
        public Vector3 RotationOffset;
    }

    /// <summary>
    /// One Basis constraint to create. Transform references stay as file identifiers so the
    /// mapping can be produced and tested without a scene.
    /// </summary>
    public sealed class BasisConstraintPlan
    {
        public long SourceDocumentFileId;

        /// <summary>
        /// Where the component goes. Usually the object the VRChat constraint sat on, but a
        /// VRChat constraint can drive a different transform, and Basis constraints always drive
        /// their own object, so in that case the component is placed on the driven transform.
        /// </summary>
        public long HostFileId;

        public BasisConstraintKind Kind;

        public bool Active = true;
        public float Weight = 1f;
        public bool Locked = true;

        public List<BasisConstraintSourcePlan> Sources = new List<BasisConstraintSourcePlan>();

        public Vector3 TranslationAtRest;
        public Vector3 TranslationOffset;
        public ConstraintAxes TranslationAxis = ConstraintAxes.All;

        public Vector3 RotationAtRest;
        public Vector3 RotationOffset;
        public ConstraintAxes RotationAxis = ConstraintAxes.All;

        public Vector3 ScaleAtRest = Vector3.one;
        public Vector3 ScaleOffset = Vector3.one;
        public ConstraintAxes ScaleAxis = ConstraintAxes.All;

        public Vector3 AimVector = Vector3.forward;
        public Vector3 UpVector = Vector3.up;
        public ConstraintWorldUp WorldUpType = ConstraintWorldUp.SceneUp;
        public Vector3 WorldUpVector = Vector3.up;
        public long WorldUpTransformFileId;

        public float Roll;
        public bool UseUpObject;

        public List<ConversionDiagnostic> Diagnostics = new List<ConversionDiagnostic>();
    }
}
