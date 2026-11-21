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
    using NUnit.Framework;
    using Pal3.Battle;

    // Expected behavior comes from the released EXE traces under
    // workspace/Analysis/Battle-Stage4/review-{ability-clock,lethal-gp,death,
    // block-death,absorb-writer,scheduler,physical-display}-native.json.
    // Reflection supplies native writer/body/stage ports without adding product APIs.
    public sealed class BattleNativeLifecycleTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings, IBattleStageGeometry
        {
            private readonly Dictionary<string, BattleParticipant> _actors;
            internal AttackTiming AbilityTiming = new(0, 20, new[] { 5 });
            internal AttackTiming PhysicalTiming = new(0, 5, new[] { 1 });
            internal int HurtTicks = 10;

            internal Catalog(BattleScenario scenario) => _actors = scenario.Participants.ToDictionary(actor => actor.InstanceId);

            public AttackTiming Attack(string source, string target) => PhysicalTiming;

            public AttackTiming Ability(string source, BattleAbilityDefinition ability) => AbilityTiming;

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => HurtTicks;

            public int SpImpact(string actor) => 7;

            public int Priority(string actor) => actor switch { "A" => 0, "B" => 1, "C" => 4, _ => 5 };

            public int DamageDivisor(string actor) => 1;

            public BattleMovementPoint InitialHome(string actor) => FormationHome(actor, _actors[actor].Slot);

            public BattleMovementPoint FormationHome(string actor, BattleSlot slot) =>
                new((_actors[actor].Side == BattleSide.Enemy ? 1000 : 0) + 100 * (int)slot, 0, 0);

            public BattleAttackPlan PlanAttack(string source, string target, BattleMovementPoint sourceHome, BattleMovementPoint targetHome) =>
                new(PhysicalTiming, sourceHome, sourceHome, targetHome);

            public IReadOnlyList<BattleMovementPlan> PlanFormationMove(string actor, BattleMovementPoint from,
                BattleMovementPoint to, bool midpoint) => new[] { new BattleMovementPlan(BattleMovementKind.Run, from, to) };
        }

        private sealed class ReadyRules : IBattleRules
        {
            private readonly HashSet<string> _ready;

            internal ReadyRules(IEnumerable<string> ready) => _ready = new HashSet<string>(ready);

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) =>
                _ready.Contains(actor.InstanceId) ? BattleSession.FullGauge : 0;

            public int GaugePerStep(BattleParticipant actor) => 0;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) => new(-10, false, false);
        }

        private sealed class OccupiedJob : IBattleStageJob
        {
            public bool Advance(BattleStageContext context) => false;
        }

        private static BattleScenario Scenario(string variant, bool party = false)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "fixture", DataFingerprint = "native-lifecycle", Seed = 1 };
            scenario.Participants.Add(Participant("A", BattleSide.Ally, BattleSlot.Water));
            if (party)
            {
                scenario.Participants.Add(Participant("B", BattleSide.Ally, BattleSlot.Fire));
                scenario.Participants.Add(Participant("C", BattleSide.Ally, BattleSlot.Earth));
            }
            scenario.Participants.Add(Participant("E", BattleSide.Enemy, BattleSlot.Water));
            return scenario;
        }

        private static BattleParticipant Participant(string id, BattleSide side, BattleSlot slot) => new()
        {
            InstanceId = id,
            ActorId = side == BattleSide.Ally ? 101 : 701,
            Side = side,
            Slot = slot,
            Attributes = new[] { 500, 100, 200, 100, 0, 3, 0, 0, 0, 0, 0, 0 },
            Vitals = new[] { 500, 10, 0 }
        };

        private static BattleAbilityData Technique(int id = 4305) => new()
        {
            Id = id,
            Name = "native lifecycle work",
            SkillTypeId = 0,
            Range = BattleTargetRange.OpponentSingle,
            Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -20) }
        };

        private static BattleSession Session(BattleScenario scenario, out Catalog catalog,
            BattleAbilityData ability = null, params string[] ready)
        {
            if (ability != null)
                scenario.Participants[0].Skills.Add(new BattleSkill { Id = ability.Id, Level = 4, Mastered = true });
            catalog = new Catalog(scenario);
            var definitions = new BattleDefinitions(scenario.Variant, scenario.DataFingerprint,
                ability == null ? Array.Empty<BattleAbilityDefinition>() : new[] { new BattleAbilityDefinition(ability) },
                Array.Empty<BattleAbilityDefinition>(), scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.InstanceId switch { "A" => 0, "B" => 1, "C" => 4, _ => -1 },
                    NormalAttackMode = 924
                })));
            return new BattleSession(new BattleSetup(scenario), catalog, new ReadyRules(ready.Length == 0 ? new[] { "A" } : ready), definitions);
        }

        private static BattleActorSnapshot Actor(BattleSession session, string id) => session.Actors.Single(actor => actor.Id == id);

        private static object NativeActor(BattleSession session, string id) =>
            ((IDictionary)typeof(BattleSession).GetField("_byId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session))[id];

        private static object Invoke(BattleSession session, string name, params object[] arguments) =>
            typeof(BattleSession).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, arguments);

        private static int WriteHp(BattleSession session, string id, int requested, bool showAction = false) =>
            (int)Invoke(session, "ChangeHp", NativeActor(session, id), requested, null, 0L, "native writer port", showAction, false, false);

        private static int WriteResource(BattleSession session, string id, int resource, int requested, bool showNumber = false) =>
            (int)Invoke(session, "ChangeResource", NativeActor(session, id), resource, requested, null, 0L, "native writer port", showNumber);

        private static BattleStageScheduler Stage(BattleSession session) =>
            (BattleStageScheduler)typeof(BattleSession).GetField("_stage", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);

        private static void Steps(BattleSession session, int count)
        {
            for (int i = 0; i < count; i++)
                session.Step();
            Assert.That(session.Status, Is.Not.EqualTo(BattleSessionStatus.Faulted), session.Result?.Reason);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AbilityWorkUsesStageTimeWhileItsBodyIsHurt(string variant)
        {
            BattleAbilityData ability = Technique();
            BattleSession session = Session(Scenario(variant), out _, ability);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", ability.Id)), Is.True);
            Steps(session, 2);
            WriteHp(session, "A", -1, true);
            Steps(session, 3);
            Assert.That(session.Events.Single(entry => entry.Target == "E" && entry.Resource == 0 && entry.Kind == BattleEventKind.Hit).Tick, Is.EqualTo(5));
            Assert.That(Actor(session, "A").Pose, Is.EqualTo(BattlePose.Hurt));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void DelayedBodyActivationDoesNotRestartTheActWorkClock(string variant)
        {
            BattleAbilityData ability = Technique();
            BattleSession session = Session(Scenario(variant), out _, ability);
            WriteHp(session, "A", -1, true);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", ability.Id)), Is.True);
            Steps(session, 11);
            Assert.That(session.Events.Single(entry => entry.Target == "E" && entry.Kind == BattleEventKind.Hit).Tick, Is.EqualTo(11));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void BodyCanReturnToIdleWhileTheStageStillWaits(string variant)
        {
            BattleAbilityData ability = Technique();
            BattleSession session = Session(Scenario(variant), out Catalog catalog, ability);
            catalog.AbilityTiming = new AttackTiming(0, 5, new[] { 2 }, minimumEndTick: 20, bodyAnimationTicks: 2);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", ability.Id)), Is.True);
            Steps(session, 4);
            Assert.That(Actor(session, "A").CurrentAction, Is.EqualTo(BattleAction.Skill));
            Assert.That(Actor(session, "A").Pose, Is.EqualTo(BattlePose.Idle));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void LethalPhysicalDamageStillPaysSameCallSpBeforeTheNextStatePass(string variant)
        {
            BattleScenario scenario = Scenario(variant, true);
            scenario.Participants[0].Vitals[0] = 1;
            BattleSession session = Session(scenario, out _, ready: new[] { "E" });
            Assert.That(session.Submit("E", BattleAction.Attack, "A"), Is.True);
            Steps(session, 1);
            Assert.That((Actor(session, "A").Hp, Actor(session, "A").Sp, Actor(session, "A").AliveFlag), Is.EqualTo((0, 17, true)));
            Steps(session, 1);
            Assert.That(Actor(session, "A").AliveFlag, Is.False);
            Assert.That(Actor(session, "A").ModelAliveFlag, Is.True);
            Assert.That(Actor(session, "A").Pose, Is.EqualTo(BattlePose.Hurt));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void SameFrameHealingCanSaveAnHpZeroActorBeforeFixState(string variant)
        {
            BattleSession session = Session(Scenario(variant, true), out _);
            WriteHp(session, "B", -1000);
            Assert.That(Actor(session, "B").AliveFlag, Is.True);
            Assert.That(WriteResource(session, "B", 1, 7), Is.EqualTo(7));
            WriteHp(session, "B", 1);
            Steps(session, 1);
            Assert.That((Actor(session, "B").Hp, Actor(session, "B").Sp, Actor(session, "B").AliveFlag), Is.EqualTo((1, 17, true)));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void HpZeroWithAliveFlagRepeatsTheNativeDeathWriter(string variant)
        {
            BattleSession session = Session(Scenario(variant, true), out _);
            WriteHp(session, "B", -1000);
            WriteHp(session, "B", 0);
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Death && entry.Target == "B"), Is.EqualTo(2));
            Assert.That(Actor(session, "B").AliveFlag, Is.True);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void DeathEndRequestDoesNotCancelPreviouslyRegisteredNumbers(string variant)
        {
            BattleSession session = Session(Scenario(variant, true), out _);
            WriteHp(session, "B", -1);
            WriteHp(session, "B", -1000);
            Steps(session, 1);
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.NumberShown && entry.Target == "B"), Is.EqualTo(2));
            Assert.That(session.StageTasks.Count(task => task.Owner == "B"), Is.EqualTo(2));
            Steps(session, 100);
            Assert.That(session.StageTasks.Count(task => task.Owner == "B"), Is.EqualTo(2));
            Steps(session, 1);
            Assert.That(session.StageTasks.Count(task => task.Owner == "B"), Is.Zero);
        }

        [TestCase("PAL3", 0, false, 41)]
        [TestCase("PAL3", 5, false, 41)]
        [TestCase("PAL3", 100, false, 41)]
        [TestCase("PAL3", 0, true, 41)]
        [TestCase("PAL3A", 0, false, 0)]
        [TestCase("PAL3A", 5, false, 5)]
        [TestCase("PAL3A", 100, false, 100)]
        [TestCase("PAL3A", 0, true, 161)]
        [TestCase("PAL3A", 5, true, 161)]
        [TestCase("PAL3A", 100, true, 161)]
        public void MpAbsorptionUsesTheDisplayAllocationDependentNativeReturn(string variant, int targetMp, bool poolFull, int expectedAbsorption)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants.Last().Vitals[2] = targetMp;
            BattleAbilityData ability = Technique(variant == "PAL3" ? 4304 : 4504);
            ability.Handler = BattleSpecialHandler.ReturnedMpAbsorb;
            ability.Attributes = new[] { new BattleAttributeEffect(2, BattleValueMode.Absolute, variant == "PAL3" ? -40 : -150) };
            BattleSession session = Session(scenario, out _, ability);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", ability.Id)), Is.True);
            Steps(session, 1);
            if (poolFull)
            {
                BattleStageScheduler stage = Stage(session);
                while (stage.Count < BattleStageScheduler.DefaultCapacity)
                    stage.Add("occupied native slot", "fixture", new OccupiedJob(), BattleStageScheduler.TimeAt(session.Tick));
            }
            Steps(session, 5);
            Assert.That(Actor(session, "A").Mp, Is.EqualTo(expectedAbsorption));
            Assert.That(Actor(session, "E").Mp, Is.EqualTo(Math.Max(0, targetMp - (variant == "PAL3" ? 41 : 161))));
            BattleEvent returned = session.Events.Single(entry => entry.Detail == "native returned MP absorption");
            Assert.That(returned.RequestedDelta, Is.EqualTo(expectedAbsorption));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AFullHpRecoveryNumberKeepsFormationLockedUntilItsStageDeletion(string variant)
        {
            BattleSession session = Session(Scenario(variant, true), out _, ready: new[] { "C" });
            WriteHp(session, "A", 1);
            Assert.That(session.CanBeginFormation("C"), Is.Not.Null);
            Steps(session, 126);
            Assert.That(session.CanBeginFormation("C"), Is.Not.Null);
            Steps(session, 1);
            Assert.That(session.CanBeginFormation("C"), Is.Null);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ABodyOnlyBlockDoesNotBecomeAStageTaskForFormation(string variant)
        {
            BattleSession session = Session(Scenario(variant, true), out _, ready: new[] { "C" });
            Invoke(session, "PushBodyBlock", NativeActor(session, "A"), false);
            Assert.That(Actor(session, "A").Pose, Is.EqualTo(BattlePose.Hurt));
            Assert.That(session.CanBeginFormation("C"), Is.Null);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ZeroGpDoesNotCreateAStageNumberEvenWhenDisplayWasRequested(string variant)
        {
            BattleSession session = Session(Scenario(variant), out _);
            Assert.That(WriteResource(session, "A", 1, 0, true), Is.Zero);
            Assert.That(session.StageTasks, Is.Empty);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void HpRecoveryDisplaysTheClampedAmount(string variant)
        {
            BattleSession session = Session(Scenario(variant), out _);
            Assert.That(WriteHp(session, "A", 10), Is.Zero);
            Steps(session, 1);
            BattleEvent number = session.Events.Single(entry => entry.Kind == BattleEventKind.NumberShown);
            Assert.That(number.RequestedDelta, Is.Zero);
            Assert.That(number.DurationSeconds, Is.EqualTo(2.5f));
        }

        [TestCase("PAL3", true, false, 5)]
        [TestCase("PAL3A", true, false, 5)]
        [TestCase("PAL3", true, true, 10)]
        [TestCase("PAL3A", true, true, 10)]
        [TestCase("PAL3", false, false, 10)]
        [TestCase("PAL3A", false, false, 10)]
        public void PositiveHpReturnAlsoDependsOnSuccessfulNumberRegistration(string variant, bool modelAlive, bool poolFull, int returned)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[0].Vitals[0] = 495;
            BattleSession session = Session(scenario, out _);
            NativeActor(session, "A").GetType().GetField("ModelAlive").SetValue(NativeActor(session, "A"), modelAlive);
            if (poolFull)
            {
                BattleStageScheduler stage = Stage(session);
                while (stage.Count < BattleStageScheduler.DefaultCapacity)
                    stage.Add("occupied native slot", "fixture", new OccupiedJob(), 0f);
            }
            Assert.That(WriteHp(session, "A", 10), Is.EqualTo(returned));
            Assert.That(Actor(session, "A").Hp, Is.EqualTo(500));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ResurrectionRestoresBothLifeFlagsBeforeTheHpWriter(string variant)
        {
            BattleScenario scenario = Scenario(variant, true);
            scenario.Participants[1].Vitals[0] = 0;
            BattleSession session = Session(scenario, out _);
            Invoke(session, "ApplyState", NativeActor(session, "B"), BattleState.Death, true, null, false, null, 0L, true);
            Assert.That((Actor(session, "B").Hp, Actor(session, "B").AliveFlag, Actor(session, "B").ModelAliveFlag), Is.EqualTo((0, true, true)));
            Assert.That(WriteHp(session, "B", 10), Is.EqualTo(10));
            Assert.That(session.StageTasks.Count(task => task.Owner == "B"), Is.EqualTo(1));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void FormationCloseStillReleasesBodiesWhenTheNativeStagePoolIsFull(string variant)
        {
            BattleSession session = Session(Scenario(variant, true), out _, ready: new[] { "C" });
            Assert.That(session.BeginFormation("C"), Is.True);
            Assert.That(session.SwapFormation("A", BattleSlot.Fire), Is.True);
            Steps(session, 3);
            float current = Actor(session, "A").Position.X;
            BattleStageScheduler stage = Stage(session);
            while (stage.Count < BattleStageScheduler.DefaultCapacity)
                stage.Add("occupied native slot", "fixture", new OccupiedJob(), BattleStageScheduler.TimeAt(session.Tick));
            Assert.That(session.EndFormation("C"), Is.True);
            Assert.That(session.ActiveFormationController, Is.Null);
            Assert.That(Actor(session, "A").Movement, Is.Null);
            Assert.That(Actor(session, "A").Position.X, Is.EqualTo(current));
            Assert.That(Actor(session, "C").Gauge, Is.Zero);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ADeathEndRequestDoesNotWaitForTheOrdinaryAbilityMinimumEnd(string variant)
        {
            BattleAbilityData ability = Technique();
            BattleSession session = Session(Scenario(variant, true), out Catalog catalog, ability);
            catalog.AbilityTiming = new AttackTiming(0, 50, new[] { 40 }, minimumEndTick: 100);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", ability.Id)), Is.True);
            Steps(session, 1);
            WriteHp(session, "A", -1000);
            Steps(session, 9);
            Assert.That(Actor(session, "A").CurrentAction, Is.Null);
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit && entry.Target == "E"), Is.False);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void APhysicalNonnegativeResultCreatesItsNativeMissNumber(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[0].Attributes[3] = 0;
            scenario.Participants[1].Attributes[4] = 1000;
            BattleSession session = Session(scenario, out _);
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Steps(session, 2);
            BattleEvent number = session.Events.Single(entry => entry.Kind == BattleEventKind.NumberShown);
            Assert.That((number.Target, number.DurationSeconds, number.Detail), Is.EqualTo(("E", 1f, "native miss")));
            Assert.That(number.Tick, Is.EqualTo(2));
        }
    }
}
