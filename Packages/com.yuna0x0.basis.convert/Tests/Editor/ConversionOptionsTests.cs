using System.Collections.Generic;
using GatorDragonGames.JigglePhysics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;
using yuna0x0.Basis.Convert.Pipeline;
using yuna0x0.Basis.Convert.Reporting;

namespace yuna0x0.Basis.Convert.Tests
{
    /// <summary>
    /// A conversion can be narrowed, by kind or one item at a time. Reading and mapping still
    /// run over the whole avatar, so what these check is that the narrowing happens between the
    /// plan and the writing, and that everything downstream of it agrees: what gets written,
    /// what counts as replaceable, what is reported.
    /// </summary>
    public class ConversionOptionsTests
    {
        private const string FixturePath =
            "Packages/com.yuna0x0.basis.convert/Tests/Editor/Fixtures/DynamicBoneChain.prefab";

        private GameObject _instance;

        [TearDown]
        public void TearDown()
        {
            if (_instance != null)
            {
                Object.DestroyImmediate(_instance);
                _instance = null;
            }
        }

        /// <summary>
        /// A plan of the shape the planner produces, built by hand so the selection logic can be
        /// checked without an avatar carrying one of everything.
        /// </summary>
        private static AvatarConversionPlan PlanWithOneOfEach()
        {
            AvatarConversionPlan plan = new AvatarConversionPlan
            {
                Rigs = new List<PlannedJiggleRig>
                {
                    new PlannedJiggleRig { Plan = new JiggleRigPlan() },
                    new PlannedJiggleRig { Plan = new JiggleRigPlan() },
                },
                Constraints = new List<PlannedConstraint>
                {
                    new PlannedConstraint { Plan = new BasisConstraintPlan() },
                    new PlannedConstraint { Plan = new BasisConstraintPlan() },
                },
                VixxyControls = new List<PlannedVixxyControl>
                {
                    new PlannedVixxyControl { Plan = new VixxyControlPlan() },
                    new PlannedVixxyControl { Plan = new VixxyControlPlan() },
                },
                Descriptor = new PlannedAvatarDescriptor { Plan = new BasisAvatarPlan() },
            };

            return plan;
        }

        [Test]
        public void ByDefaultEverythingPlannedIsSelected()
        {
            AvatarConversionPlan plan = PlanWithOneOfEach();

            Assert.That(plan.Options.IsEverything, Is.True);
            Assert.That(plan.TotalSelected, Is.EqualTo(plan.TotalPlanned));
            Assert.That(plan.TotalSelected, Is.EqualTo(7),
                "Two rigs, two constraints, two controls and the descriptor.");
        }

        [Test]
        public void TurningOffOneKindLeavesTheOthersAlone()
        {
            AvatarConversionPlan plan = PlanWithOneOfEach();
            plan.Options.Physics = false;

            Assert.That(plan.SelectedRigCount, Is.Zero);
            Assert.That(plan.SelectedConstraintCount, Is.EqualTo(2));
            Assert.That(plan.SelectedVixxyControlCount, Is.EqualTo(2));
            Assert.That(plan.DescriptorSelected, Is.True);
            Assert.That(plan.TotalPlanned, Is.EqualTo(7),
                "What was found does not change with what will be written.");
        }

        [Test]
        public void OneItemCanBeLeftOutWithoutLosingTheRestOfItsKind()
        {
            AvatarConversionPlan plan = PlanWithOneOfEach();
            plan.Rigs[0].Include = false;
            plan.Constraints[1].Include = false;
            plan.Descriptor.Include = false;

            Assert.That(plan.SelectedRigCount, Is.EqualTo(1));
            Assert.That(plan.SelectedConstraintCount, Is.EqualTo(1));
            Assert.That(plan.DescriptorSelected, Is.False);
            Assert.That(plan.TotalSelected, Is.EqualTo(4));
        }

        [Test]
        public void DiagnosticsFollowTheSelectionWithoutBeingLost()
        {
            AvatarConversionPlan plan = PlanWithOneOfEach();
            plan.Rigs[0].Plan.Diagnostics.Add(
                DiagnosticSeverity.Dropped, "test.dropped", "A setting with no equivalent.");
            plan.Options.Physics = false;

            Assert.That(Codes(plan.SelectedDiagnostics()), Does.Not.Contain("test.dropped"),
                "A loss on something the conversion will not write is not a loss.");
            Assert.That(Codes(plan.AllDiagnostics()), Contains.Item("test.dropped"),
                "The full picture still holds it.");
        }

        [Test]
        public void TheReportSaysWhatWasLeftOutRatherThanQuietlyOmittingIt()
        {
            AvatarConversionPlan plan = PlanWithOneOfEach();
            plan.Options.Constraints = false;
            plan.Rigs[0].Include = false;

            string report = ConversionReport.Write(plan);
            TestContext.WriteLine(report);

            Assert.That(report, Does.Contain("Left out by choice: constraints"));
            Assert.That(report, Does.Contain("Left out one by one: 1"));
            Assert.That(report, Does.Contain("(left out)"));
        }

        [Test]
        public void TheDescriptorReadsAsFoundRatherThanConvertedWhenItIsLeftOut()
        {
            AvatarConversionPlan plan = PlanWithOneOfEach();
            plan.Options.Descriptor = false;

            Assert.That(ConversionReport.Write(plan),
                Does.Contain("Avatar descriptor: found, left out"));
        }

        [Test]
        public void ConvertingWithPhysicsOffWritesNoRigs()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);
            plan.Options.Physics = false;

            ConversionResult result = AvatarConverter.Apply(plan, Instantiate());

            Assert.That(result.RigsWritten, Is.Zero);
            Assert.That(_instance.GetComponentsInChildren<JiggleRig>(true), Is.Empty);
        }

        [Test]
        public void ConvertingWithCollidersOffStillWritesTheRig()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);
            plan.Options.Colliders = false;

            ConversionResult result = AvatarConverter.Apply(plan, Instantiate());

            Assert.That(result.RigsWritten, Is.EqualTo(1));

            JiggleRig[] rigs = _instance.GetComponentsInChildren<JiggleRig>(true);
            Assert.That(rigs.Length, Is.EqualTo(1));
            Assert.That(rigs[0].GetJiggleRigData().jiggleColliders, Is.Empty,
                "The rig is written; its bones simply pass through the body.");
        }

        [Test]
        public void WhatIsNotSelectedIsNotTreatedAsReplaceable()
        {
            AvatarConversionPlan plan = AvatarConversionPlanner.Plan(FixturePath);
            AvatarConverter.Apply(plan, Instantiate());

            Assert.That(AvatarConverter.FindReplaceable(plan, _instance), Is.Not.Empty);

            plan.Options.Physics = false;
            Assert.That(AvatarConverter.FindReplaceable(plan, _instance), Is.Empty,
                "A narrowed conversion leaves an earlier one's output where it is rather than "
                + "clearing it away.");
        }

        private GameObject Instantiate()
        {
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(FixturePath));
            return _instance;
        }

        private static List<string> Codes(IEnumerable<ConversionDiagnostic> diagnostics)
        {
            List<string> codes = new List<string>();
            foreach (ConversionDiagnostic diagnostic in diagnostics)
            {
                codes.Add(diagnostic.Code);
            }

            return codes;
        }
    }
}
