using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Pipeline;
using yuna0x0.Basis.Convert.Sources;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// The reader that takes VRM data from live components rather than from a file's text, which
    /// is how an imported `.vrm` is read: it is binary glTF, so there is no YAML to scan.
    /// <para>
    /// These need UniVRM installed, since without it the fixture's components are missing scripts
    /// and there is nothing live to read. They say so and skip rather than fail, the same way the
    /// tests that need a real VRChat avatar do.
    /// </para>
    /// </summary>
    public class VrmComponentReaderTests
    {
        private const string Folder =
            "Packages/com.yuna0x0.basis.convert/Tests/Editor/Fixtures/SampleVrmAvatar";

        private const string Vrm10Path = Folder + "/SampleVrm10Avatar.prefab";

        private static VrmComponentReader.Result ReadOrSkip(string path)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(asset, Is.Not.Null, path);

            VrmComponentReader.Result read = VrmComponentReader.Read(asset);
            if (!read.Any)
            {
                Assert.Ignore("UniVRM is not installed, so the fixture carries missing scripts.");
            }

            return read;
        }

        [Test]
        public void ReadsTheChainsJointsAndCollidersOfAVrm10Avatar()
        {
            VrmComponentReader.Result read = ReadOrSkip(Vrm10Path);

            Assert.That(read.Chains.Count, Is.EqualTo(1), "springs");
            Assert.That(read.Joints.Count, Is.EqualTo(3), "joint components");
            Assert.That(read.Colliders.Count, Is.EqualTo(1), "collider components");
            Assert.That(read.Groups.Count, Is.EqualTo(1), "collider groups");

            VrmSpringChainData chain = read.Chains[0];
            Assert.That(chain.IsVrm10, Is.True);
            Assert.That(chain.JointComponentFileIds.Count, Is.EqualTo(3));
            Assert.That(chain.ColliderGroupFileIds.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Every joint a spring names is one the reader also read, which is what ties a chain to
        /// its bones. Identifiers come from the asset itself, so both readers address objects the
        /// same way and the resolver needs to know nothing about where the data came from.
        /// </summary>
        [Test]
        public void EveryJointASpringNamesWasRead()
        {
            VrmComponentReader.Result read = ReadOrSkip(Vrm10Path);

            foreach (long id in read.Chains[0].JointComponentFileIds)
            {
                Assert.That(id, Is.Not.EqualTo(0L), "a joint reference resolved to no identifier");
                Assert.That(read.Joints.ContainsKey(id), Is.True, $"joint {id}");
            }

            foreach (long id in read.Chains[0].ColliderGroupFileIds)
            {
                Assert.That(read.Groups.ContainsKey(id), Is.True, $"collider group {id}");
            }
        }

        /// <summary>
        /// The two readers describe the same avatar the same way. The fixture is text, so the
        /// planner reads it as text; reading its live components has to agree, or an imported
        /// `.vrm` would convert differently from a prefab saved out of one.
        /// </summary>
        [Test]
        public void BothReadersAgreeOnTheSameAvatar()
        {
            VrmComponentReader.Result read = ReadOrSkip(Vrm10Path);
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(Vrm10Path);

            Assert.That(read.Chains.Count, Is.EqualTo(plan.VrmChainsFound), "chains");

            VrmSpringChainData chain = read.Chains[0];
            Assert.That(read.Joints[chain.JointComponentFileIds[0]].OwnerGameObjectFileId,
                Is.Not.EqualTo(0L), "the root joint's bone");

            Assert.That(read.Colliders.Count + read.Groups.Count, Is.GreaterThan(0),
                "colliders");

            Assert.That(plan.Rigs.Count, Is.EqualTo(read.Chains.Count),
                "one rig per spring, from either reader");
        }

        [Test]
        public void ReadsTheParametersOfAJoint()
        {
            VrmComponentReader.Result read = ReadOrSkip(Vrm10Path);
            VrmSpringJointData root = read.Joints[read.Chains[0].JointComponentFileIds[0]];

            Assert.That(root.Stiffness, Is.GreaterThan(0f));
            Assert.That(root.Radius, Is.GreaterThan(0f));
            Assert.That(root.DragForce, Is.InRange(0f, 1f));
        }
    }
}
