using System.Collections.Generic;
using NUnit.Framework;
using yuna0x0.Basis.Convert.Mapping;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// A jiggle rig holds at most 32 colliders. Both physics sources can reference more than
    /// that, and the extras are silently dropped at runtime, so the conversion has to say so.
    /// </summary>
    public class ColliderLimitTests
    {
        private const int JiggleColliderLimit = 32;

        [Test]
        public void APhysBoneCanReferenceMoreCollidersThanARigHolds()
        {
            PhysBoneData source = new PhysBoneData();
            for (int i = 0; i < JiggleColliderLimit + 5; i++)
            {
                source.ColliderFileIds.Add(1000 + i);
            }

            JiggleRigPlan plan = PhysBoneToJiggleMapper.Map(source);

            Assert.That(plan.ColliderSourceFileIds.Count,
                Is.EqualTo(JiggleColliderLimit + 5),
                "The mapper records every referenced collider; the planner reports the overflow.");
        }

        [Test]
        public void ADynamicBoneCanReferenceMoreCollidersThanARigHolds()
        {
            DynamicBoneData source = new DynamicBoneData { RootFileId = 20L };
            for (int i = 0; i < JiggleColliderLimit + 5; i++)
            {
                source.ColliderFileIds.Add(1000 + i);
            }

            List<JiggleRigPlan> plans = DynamicBoneToJiggleMapper.Map(source);

            Assert.That(plans.Count, Is.EqualTo(1));
            Assert.That(plans[0].ColliderSourceFileIds.Count,
                Is.EqualTo(JiggleColliderLimit + 5));
        }
    }
}
