using System.Collections.Generic;
using Basis.Scripts.BasisSdk;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Writers
{
    public sealed class ResolvedHeadChopTarget
    {
        public Transform Transform;
        public float Scale;
    }

    public sealed class ResolvedHeadChop
    {
        public BasisHeadChopPlan Plan;
        public GameObject Host;
        public List<ResolvedHeadChopTarget> Targets = new List<ResolvedHeadChopTarget>();
    }

    /// <summary>
    /// Puts a <see cref="BasisHeadChop"/> on the object the VRChat one sat on. A second
    /// conversion rewrites the one it wrote rather than adding another.
    /// </summary>
    public static class BasisHeadChopWriter
    {
        public static BasisHeadChop Write(ResolvedHeadChop chop, string undoName = "Convert head chop")
        {
            if (chop?.Host == null)
            {
                throw new System.ArgumentException("A host is required", nameof(chop));
            }

            BasisHeadChop component = chop.Host.GetComponent<BasisHeadChop>();
            if (component == null)
            {
                component = Undo.AddComponent<BasisHeadChop>(chop.Host);
            }
            else
            {
                Undo.RecordObject(component, undoName);
            }

            Undo.SetCurrentGroupName(undoName);

            BasisHeadChop.HeadChopTarget[] targets =
                new BasisHeadChop.HeadChopTarget[chop.Targets.Count];
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = new BasisHeadChop.HeadChopTarget
                {
                    Target = chop.Targets[i].Transform,
                    Scale = chop.Targets[i].Scale,
                };
            }

            component.Targets = targets;
            EditorUtility.SetDirty(component);
            return component;
        }
    }
}
