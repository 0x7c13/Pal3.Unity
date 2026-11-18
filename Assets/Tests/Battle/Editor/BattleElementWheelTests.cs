// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using NUnit.Framework;

    public sealed class BattleElementWheelTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings, IBattleAbilityEffects, IBattleElementWheelTimings, IBattleAbilityBodyAnimations
        {
            public AttackTiming Attack(string actor, string target) => new(0, 3, new[] { 1 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => new(0, 3, new[] { 1 });

            public AttackTiming ElementWheel(string actor, BattleAbilityDefinition ability) => new(0, 3, new[] { 1 });

            public BattleAbilityStageProfile ElementWheelStage(string actor, BattleAbilityDefinition ability) => new(BattleAbilityStageKind.ElementWheel,
                new BattleAbilityEffectBinding(-1, 0), new BattleAbilityEffectBinding(-1, 0), .02f, BattleAbilityWorkBoundary.WorkTime, 0, false);

            public BattleAbilityStageProfile AbilityStage(string actor, BattleAbilityDefinition ability) => new(BattleAbilityStageKind.Magic,
                new BattleAbilityEffectBinding(-1, 0), new BattleAbilityEffectBinding(-1, 0), .02f, BattleAbilityWorkBoundary.WorkTime, 0, false);

            public BattlePhysicalAnimation ElementWheelBody(string actor, BattleAbilityDefinition ability) => null;

            public BattlePhysicalAnimation AbilityBody(string actor, BattleAbilityDefinition ability) => null;

            public int? AbilityNativeAction(string actor, BattleAbilityDefinition ability) => null;

            public BattlePhysicalAnimation AbilityMovementBody(string actor, BattleAbilityDefinition ability, BattleMovementKind kind, bool returning) => null;

            public IReadOnlyList<BattleMovementPlan> AbilityMovement(BattleActorSnapshot source, BattleActorSnapshot target,
                BattleAbilityDefinition ability, BattleDefinitions definitions, bool returning) => Array.Empty<BattleMovementPlan>();

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 1;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor == "N" ? 0 : 1;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class Rules : IBattleRules
        {
            private readonly bool _refillController;

            public Rules(bool refillController = false) => _refillController = refillController;

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) =>
                actor.Side == BattleSide.Ally ? BattleSession.FullGauge : 0;

            public int GaugePerStep(BattleParticipant actor) => _refillController && actor.InstanceId == "N" ? BattleSession.FullGauge : 0;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) => new(-1, false, false);
        }

        private static BattleParticipant Participant(string id, int actorId, BattleSide side, BattleSlot slot) => new()
        {
            InstanceId = id,
            ActorId = actorId,
            Side = side,
            Slot = slot,
            Attributes = new[] { 500, 100, 100, 10, 10, 3, 0, 0, 0, 0, 0, 0 },
            Vitals = new[] { 500, 100, 0 }
        };

        private static (BattleSession Session, BattleSetup Setup, BattleDefinitions Definitions) Fixture(int charge = 9, string variant = "PAL3A",
            BattleState? state = null, bool learned = false, bool refillController = false)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "wheel-fixture", DataFingerprint = "wheel", Seed = 12345 };
            scenario.Pal3A.SpiritWheel.Charges = Enumerable.Repeat(charge, 5).ToArray();
            BattleParticipant nangong = Participant("N", 101, BattleSide.Ally, BattleSlot.Water);
            if (state.HasValue)
                nangong.Statuses.Add(new BattleStatus { Id = (int)state.Value, Rounds = 20 });
            if (learned)
                for (int id = 264; id <= 268; id++)
                    nangong.Skills.Add(new BattleSkill { Id = id, Level = 1, Uses = 12, Mastered = true });
            scenario.Participants.Add(nangong);
            scenario.Participants.Add(Participant("W", 102, BattleSide.Ally, BattleSlot.Fire));
            scenario.Participants.Add(Participant("E", 7311, BattleSide.Enemy, BattleSlot.Water));
            var definitions = new BattleDefinitions(variant, "wheel", Enumerable.Range(264, 5).Select(id =>
                new BattleAbilityDefinition(new BattleAbilityData
                {
                    Id = id,
                    IsMagic = true,
                    SkillTypeId = 0,
                    Range = BattleTargetRange.None,
                    ElementMask = 1 << (id == 266 ? 3 : id == 267 ? 2 : id - 264),
                    Costs = new[] { new BattleCost(2, BattleValueMode.Absolute, 5) }
                })), Array.Empty<BattleAbilityDefinition>(), scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.InstanceId == "N" ? 0 : actor.InstanceId == "W" ? 1 : -1
                })));
            var setup = new BattleSetup(scenario);
            var session = new BattleSession(setup, new Catalog(), new Rules(refillController), definitions);
            session.SetAutomatic(BattleSide.Ally, false);
            session.SetAutomatic(BattleSide.Enemy, false);
            return (session, setup, definitions);
        }

        private static uint RandomState(BattleSession session) => ((BattleRandom)typeof(BattleSession)
            .GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session)).State;

        private static void Resolve(BattleSession session)
        {
            for (int tick = 0; tick < 20 && session.Actors.Single(actor => actor.Id == "N").ActionId != 0; tick++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running), session.Result?.Reason);
            Assert.That(session.Actors.Single(actor => actor.Id == "N").ActionId, Is.Zero, "The wheel has no name/lineup wait.");
        }

        [TestCase(264, 0, 1)]
        [TestCase(265, 1, 2)]
        [TestCase(266, 3, 4)]
        [TestCase(267, 2, 3)]
        [TestCase(268, 4, 5)]
        public void ManualWheelUsesItsDedicatedNoTargetStageAndClearsOnlyItsCoreElement(int id, int coreElement, int field)
        {
            BattleSession session = Fixture(state: BattleState.Seal).Session;
            Assert.That(session.CanSubmitElementWheel("N", id), Is.Null);
            Assert.That(session.GetTargetCandidates(new BattleCommand("N", BattleAction.Skill, null, id)), Is.Empty);
            int count = session.Inputs.Count;
            uint random = RandomState(session);
            Assert.That(session.SubmitElementWheel("N", id), Is.True);
            BattleInput input = session.Inputs.Last();
            Assert.That((input.Kind, input.Actor, input.Target, input.Action, input.AbilityId, input.FromTestAi),
                Is.EqualTo((BattleInputKind.ElementWheel, "N", (string)null, BattleAction.Skill, id, false)));
            Assert.That(session.Inputs.Count, Is.EqualTo(count + 1));
            Assert.That(session.Actors.Single(actor => actor.Id == "N").CastGauge, Is.Zero);
            Assert.That(session.Pal3AState.SpiritWheel.Charges[coreElement], Is.EqualTo(9), "Cost occurs at work, not submission.");
            Resolve(session);
            int[] expected = Enumerable.Repeat(9, 5).ToArray();
            expected[coreElement] = 0;
            Assert.That(session.Pal3AState.SpiritWheel.Charges, Is.EqualTo(expected));
            Assert.That((session.FiveSpiritElement, session.FiveSpiritForce), Is.EqualTo((field, 1)));
            Assert.That(session.Actors.Single(actor => actor.Id == "N").Mp, Is.Zero);
            Assert.That(session.Actors.Single(actor => actor.Id == "N").Skills, Is.Empty);
            Assert.That(session.Events.Count(entry => entry.Detail.Contains("consumed", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(session.Events.Any(entry => entry.Detail.StartsWith("skill cost", StringComparison.Ordinal)), Is.False);
            Assert.That(RandomState(session), Is.EqualTo(random));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(8)]
        public void IncompleteElementCannotSubmitAndQueriesNeverChangeTheSession(int charge)
        {
            BattleSession session = Fixture(charge).Session;
            uint random = RandomState(session);
            int count = session.Inputs.Count;
            for (int query = 0; query < 10; query++)
                for (int id = 264; id <= 268; id++)
                    Assert.That(session.CanSubmitElementWheel("N", id), Is.Not.Null);
            Assert.That(session.SubmitElementWheel("N", 264), Is.False);
            Assert.That(session.Pal3AState.SpiritWheel.Charges, Is.All.EqualTo(charge));
            Assert.That(RandomState(session), Is.EqualTo(random));
            Assert.That(session.Inputs.Count, Is.EqualTo(count));
        }

        [TestCase("N", 0)]
        [TestCase("N", 263)]
        [TestCase("N", 269)]
        [TestCase("W", 264)]
        [TestCase("E", 264)]
        [TestCase("missing", 264)]
        [TestCase(null, 264)]
        public void ManualWheelRejectsInvalidSkillAndAnyOtherController(string actor, int id)
        {
            BattleSession session = Fixture().Session;
            int inputs = session.Inputs.Count;
            Assert.That(session.CanSubmitElementWheel(actor, id), Is.Not.Null);
            Assert.That(session.SubmitElementWheel(actor, id), Is.False);
            Assert.That(session.Inputs.Count, Is.EqualTo(inputs));
        }

        [TestCase(BattleState.Paralysis)]
        [TestCase(BattleState.Sleep)]
        [TestCase(BattleState.Chaos)]
        [TestCase(BattleState.Madness)]
        public void AbnormalControllersCannotUseTheManualWheel(BattleState state)
        {
            Assert.That(Fixture(state: state).Session.CanSubmitElementWheel("N", 264), Is.Not.Null);
        }

        [Test]
        public void CastingFromTheWheelKeepsTheLastOrdinaryCommand()
        {
            BattleSession session = Fixture(refillController: true).Session;
            Assert.That(session.Submit("N", BattleAction.Defend), Is.True);
            Assert.That(session.Submit("W", BattleAction.Defend), Is.True);
            Resolve(session);
            for (int tick = 0; tick < 500 && session.WaitingActor != "N"; tick++)
                session.Step();
            Assert.That(session.SubmitElementWheel("N", 264), Is.True);
            Resolve(session);
            for (int tick = 0; tick < 500 && session.WaitingActor != "N"; tick++)
                session.Step();

            Assert.That(session.RequestAiRepeat("N"), Is.True);
            Assert.That(session.Inputs.Last(input => input.Kind == BattleInputKind.Command).Action, Is.EqualTo(BattleAction.Defend));
        }

        [Test]
        public void ManualEligibilityDoesNotGrantOrdinarySkillAccessOrFakeProficiency()
        {
            BattleSession session = Fixture(learned: true).Session;
            Assert.That(session.CanSubmitElementWheel("N", 264), Is.Null);
            Assert.That(session.CanSubmit(new BattleCommand("N", BattleAction.Skill, null, 264)), Is.Not.Null);
            Assert.That(session.Submit(new BattleCommand("N", BattleAction.Skill, null, 264)), Is.False);
            Assert.That(session.SubmitElementWheel("N", 264), Is.True);
            Resolve(session);
            Assert.That(session.Actors.Single(actor => actor.Id == "N").Skills.Select(skill => (skill.Id, skill.Level, skill.Uses)),
                Is.EqualTo(Enumerable.Range(264, 5).Select(id => (id, 1, 12))));
        }

        [Test]
        public void Pal3AndNonCurrentControllersCannotUseTheWheel()
        {
            Assert.That(Fixture(variant: "PAL3").Session.CanSubmitElementWheel("N", 264), Is.Not.Null);
            BattleSession session = Fixture().Session;
            session.SetAiProfile("N", 32);
            Assert.That(session.WaitingActor, Is.EqualTo("W"));
            Assert.That(session.CanSubmitElementWheel("N", 264), Is.Not.Null);
        }

        [Test]
        public void EnemyAiFieldMagicRetainsItsMpCostAndCannotConsumeThePlayerWheel()
        {
            BattleSession session = Fixture().Session;
            object enemy = ((IDictionary)typeof(BattleSession).GetField("_byId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session))["E"];
            Type actorType = enemy.GetType();
            actorType.GetField("Ready").SetValue(enemy, true);
            actorType.GetField("Gauge").SetValue(enemy, BattleSession.FullGauge);
            actorType.GetField("Mp").SetValue(enemy, 100);
            var command = new BattleCommand("E", BattleAction.Skill, null, 266);
            Assert.That((long)typeof(BattleSession).GetMethod("SubmitAiCommand", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(session, new object[] { command, true }), Is.GreaterThan(0));
            for (int tick = 0; tick < 300 && session.Actors.Single(actor => actor.Id == "E").ActionId != 0; tick++)
                session.Step();
            Assert.That(session.Actors.Single(actor => actor.Id == "E").ActionId, Is.Zero);
            Assert.That((session.FiveSpiritElement, session.FiveSpiritForce), Is.EqualTo((4, 2)));
            Assert.That(session.Actors.Single(actor => actor.Id == "E").Mp, Is.EqualTo(95));
            Assert.That(session.Pal3AState.SpiritWheel.Charges, Is.All.EqualTo(9));
            Assert.That(session.Inputs.Last().Kind, Is.EqualTo(BattleInputKind.Command));
            Assert.That(session.Inputs.Last().FromTestAi, Is.True);
        }

        [Test]
        public void EndedAndAlreadySubmittedInputsCannotUseTheWheel()
        {
            BattleSession session = Fixture().Session;
            Assert.That(session.SubmitElementWheel("N", 264), Is.True);
            Assert.That(session.SubmitElementWheel("N", 265), Is.False);
            session.Abort("test stop");
            Assert.That(session.SubmitElementWheel("N", 265), Is.False);
        }

        [Test]
        public void WheelUsesNativeChargeGateDuringBeastFormAndRejectsPendingTransition()
        {
            BattleSession session = Fixture().Session;
            object state = ((IDictionary)typeof(BattleSession).GetField("_pal3AForms", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session))["N"];
            state.GetType().GetField("Kind", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(state, BattlePal3AFormKind.NangongFireBeast);
            Assert.That(session.CanSubmitElementWheel("N", 264), Is.Null, "The released wheel panel does not check the beast-form bit.");
            Assert.That(session.CanSubmit(new BattleCommand("N", BattleAction.Skill, null, 264)), Is.Not.Null);
            state.GetType().GetField("Pending", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(state, true);
            Assert.That(session.CanSubmitElementWheel("N", 264), Is.Not.Null);
        }

        [Test]
        public void DedicatedInputReplaysWithoutTheAiQualificationChannel()
        {
            var fixture = Fixture();
            BattleSession session = fixture.Session;
            for (int id = 264; id <= 268; id++)
                Assert.That(session.CanSubmitElementWheel("N", id), Is.Null);
            Assert.That(session.SubmitElementWheel("N", 266), Is.True);
            Resolve(session);
            BattleSession replay = BattleReplay.Play(fixture.Setup, new Catalog(), session.Inputs, session.Tick, new Rules(), fixture.Definitions);
            Assert.That(replay.Pal3AState.SpiritWheel.Charges, Is.EqualTo(session.Pal3AState.SpiritWheel.Charges));
            Assert.That((replay.FiveSpiritElement, replay.FiveSpiritForce, RandomState(replay)),
                Is.EqualTo((session.FiveSpiritElement, session.FiveSpiritForce, RandomState(session))));
            Assert.That(replay.Events.Select(entry => (entry.Tick, entry.Kind, entry.Source, entry.Target, entry.Detail)),
                Is.EqualTo(session.Events.Select(entry => (entry.Tick, entry.Kind, entry.Source, entry.Target, entry.Detail))));
        }
    }
}
