// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using Pal3.Battle;

    public sealed class BattleFleeTests
    {
        private sealed class Catalog : IBattleCatalog, IBattlePhysicalTimings, IBattleAbilityTimings, IBattleStageGeometry
        {
            private readonly string _variant;

            internal Catalog(string variant) => _variant = variant;

            public int Priority(string actor) => actor == "A" ? 0 : actor == "B" ? 1 : 5;

            public int GuardEntry(string actor) => 2;

            public int Hurt(string actor) => 5;

            public int SpImpact(string actor) => 0;

            public int DamageDivisor(string actor) => 1;

            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => new(0, 5, new[] { 2 });

            public BattlePhysicalStageProfile Physical(string actor)
            {
                var animations = Enum.GetValues(typeof(BattlePose)).Cast<BattlePose>().ToDictionary(pose => pose,
                    pose => new BattlePhysicalAnimation(pose.ToString(), 5, 10, new[] { new BattlePhysicalMarker(4, BattlePhysicalMarkerKind.Over) }));
                return new BattlePhysicalStageProfile(_variant, actor == "E" ? 701 : 101, actor == "E" ? -1 : actor == "A" ? 0 : 1, 1, 1, animations);
            }

            public BattleMovementPoint InitialHome(string actor) => actor == "A" ? new(0, 0, 0) : actor == "B" ? new(100, 0, 0) : new(200, 0, 0);

            public BattleMovementPoint FormationHome(string actor, BattleSlot slot) => InitialHome(actor);

            public IReadOnlyList<BattleMovementPlan> PlanFormationMove(string actor, BattleMovementPoint from, BattleMovementPoint to, bool crossing) => new[] { new BattleMovementPlan(BattleMovementKind.Run, from, to) };

            public BattleAttackPlan PlanAttack(string source, string target, BattleMovementPoint from, BattleMovementPoint to) => new(Attack(source, target), from, from, to);
        }

        private sealed class ReadyRules : IBattleRules
        {
            private readonly string _ready;

            internal ReadyRules(string ready) => _ready = ready;

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => actor.InstanceId == _ready ? BattleSession.FullGauge - (_ready == "E" ? 1 : 0) : 0;

            public int GaugePerStep(BattleParticipant actor) => 1;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1) => new(-1, false, false);
        }

        private static BattleParticipant Participant(string id, BattleSide side) => new()
        {
            InstanceId = id,
            ActorId = side == BattleSide.Ally ? id == "A" ? 101 : 104 : 701,
            Side = side,
            Slot = id == "B" ? BattleSlot.Fire : BattleSlot.Water,
            Level = 10,
            Attributes = new[] { 100, 100, 100, 10, 10, 3, 10, 0, 0, 0, 0, 0 },
            Vitals = new[] { 100, 100, 100 }
        };

        private static BattleSession Session(string variant, bool script = false, bool enemyFlee = false, bool secondAlly = false)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "portable", DataFingerprint = "flee-fixture", ScriptBattle = script, Seed = 1 };
            scenario.Participants.Add(Participant("A", BattleSide.Ally));
            scenario.Participants.Add(Participant("E", BattleSide.Enemy));
            if (secondAlly)
                scenario.Participants.Add(Participant("B", BattleSide.Ally));
            scenario.Items.Add(new BattleItem { Id = 6114, Count = 2 });
            var item = new BattleAbilityData
            {
                Id = 6114,
                IsItem = true,
                Range = BattleTargetRange.OwnSingle,
                Handler = BattleSpecialHandler.ForceFlee
            };
            var definitions = new BattleDefinitions(variant, "flee-fixture", Array.Empty<BattleAbilityDefinition>(), new[] { new BattleAbilityDefinition(item) }, scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
            {
                InstanceId = actor.InstanceId,
                Identity = actor.Side == BattleSide.Enemy ? -1 : actor.InstanceId == "A" ? 0 : 1,
                EscapePercent = enemyFlee && actor.Side == BattleSide.Enemy ? 100 : 0
            })));
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(variant), new ReadyRules(enemyFlee ? "E" : "A"), definitions);
            session.SetAutomatic(BattleSide.Enemy, false);
            session.SetAutomatic(BattleSide.Ally, false);
            return session;
        }

        private static BattleActorSnapshot Actor(BattleSession session, string id) => session.Actors.Single(actor => actor.Id == id);

        private static void Steps(BattleSession session, int count)
        {
            for (int i = 0; i < count; i++)
                session.Step();
            Assert.That(session.Status, Is.Not.EqualTo(BattleSessionStatus.Faulted), session.Result?.Reason);
        }

        private static void Until(BattleSession session, Func<bool> condition, int limit = 1200)
        {
            while (!condition() && session.Status == BattleSessionStatus.Running && limit-- > 0)
                session.Step();
            Assert.That(condition(), Is.True, session.Result?.Reason);
        }

        private static object NativeActor(BattleSession session, string id) => ((System.Collections.IDictionary)typeof(BattleSession).GetField("_byId", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(session))[id];

        private static object Invoke(BattleSession session, string name, params object[] arguments) => typeof(BattleSession).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(session, arguments);

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void LastEnemyDeathAtTheFleeWaitBoundaryReleasesTheAttemptAndCompletesVictory(string variant)
        {
            var scenario = new BattleScenario
            {
                Variant = variant,
                Scene = "portable",
                DataFingerprint = "flee-boundary",
                Encounter = BattleEncounter.AllyAmbush
            };
            scenario.Participants.Add(Participant("A", BattleSide.Ally));
            scenario.Participants.Add(Participant("B", BattleSide.Ally));
            scenario.Participants.Add(Participant("E", BattleSide.Enemy));
            scenario.Participants[1].Attributes[3] = 1000;
            var definitions = new BattleDefinitions(variant, scenario.DataFingerprint,
                Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Enemy ? -1 : actor.InstanceId == "A" ? 0 : 1
                })));
            var catalog = new BattleSessionTests.Catalog { Timing = new AttackTiming(0, 300, new[] { 263 }) };
            var setup = new BattleSetup(scenario);
            var session = new BattleSession(setup, catalog, definitions: definitions);
            session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            Assert.That(session.Submit("B", BattleAction.Attack, "E"), Is.True);
            Steps(session, 263);
            Assert.That(Actor(session, "E").Hp, Is.Zero);
            Assert.That(session.StageTasks.Any(task => task.Kind == "flee wait" && task.Finished), Is.True);
            Until(session, () => session.Result != null);
            Assert.That(session.Result.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(session.Result.Winner, Is.EqualTo(BattleSide.Ally));
            Assert.That(session.Result.Escaped, Is.False);
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Escaped || entry.Detail == "escape failed"), Is.False);
            Assert.That(session.StageTasks, Is.Empty);
            Assert.That(((System.Collections.IDictionary)typeof(BattleSession).GetField("_bodyStageOwners", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session)).Count, Is.Zero);
            BattleSession replay = BattleReplay.Play(setup, catalog, session.Inputs, session.Tick, definitions: definitions);
            Assert.That(replay.Result.Winner, Is.EqualTo(BattleSide.Ally));
            Assert.That(replay.Result.Escaped, Is.False);
            Assert.That(replay.Events.Select(entry => (entry.Tick, entry.Kind, entry.RandomState)),
                Is.EqualTo(session.Events.Select(entry => (entry.Tick, entry.Kind, entry.RandomState))));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void SuccessWaitsForBothSlotsThenRunsThePartyBeforeEnding(string variant)
        {
            BattleSession session = Session(variant, secondAlly: true);
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            Steps(session, 1);
            Assert.That(Actor(session, "A").Pose, Is.EqualTo(BattlePose.Fleeing));
            Assert.That(Actor(session, "A").CastGauge, Is.EqualTo(44));
            Until(session, () => session.FleeResult?.Success == true);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running));
            Assert.That(Actor(session, "A").Position.X, Is.LessThan(0));
            Assert.That(Actor(session, "B").Position.X, Is.LessThan(100));
            Assert.That(Actor(session, "A").Pose, Is.EqualTo(BattlePose.Moving));
            int enemyGauge = Actor(session, "E").Gauge;
            Steps(session, 10);
            Assert.That(Actor(session, "E").Gauge, Is.EqualTo(enemyGauge));
            Until(session, () => session.Result != null);
            Assert.That(session.Result.Escaped, Is.True);
            Assert.That(Actor(session, "A").Position.X, Is.EqualTo(-800));
            Assert.That(Actor(session, "A").Position.Z, Is.EqualTo(-800));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void BusyBodyRepeatsTheNativeEscapeRollUntilItCanRun(string variant)
        {
            BattleSession session = Session(variant);
            typeof(BattleSession).GetField("_fleeAlwaysSucceeds", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, true);
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            Until(session, () => session.StageTasks.Any(task => task.Kind == "flee wait" && task.Finished));
            object actor = NativeActor(session, "A");
            actor.GetType().GetField("HurtUntil").SetValue(actor, (int)session.SimulationTick + 4);

            Steps(session, 3);
            BattleEvent[] success = session.Events.Where(entry => entry.Kind == BattleEventKind.Escaped).ToArray();
            Assert.That(success.Select(entry => entry.RandomState), Is.EqualTo(new uint[] { 2745024, 3357800067, 415139642 }));
            Assert.That(Actor(session, "A").Position, Is.EqualTo(Actor(session, "A").Home));
            Steps(session, 1);
            Assert.That(Actor(session, "A").Pose, Is.EqualTo(BattlePose.Moving));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ScriptFailureConsumesProbabilityAndKeepsAThreeSecondMessage(string variant)
        {
            BattleSession session = Session(variant, script: true);
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            Until(session, () => session.FleeResult != null);
            Assert.That(session.FleeResult.Success, Is.False);
            Assert.That(session.Events.Last(entry => entry.Detail == "escape failed").RandomState, Is.EqualTo(2745024));
            float started = session.FleeResult.StartedAtSeconds;
            Until(session, () => BattleStageScheduler.TimeAt(session.SimulationTick + 1) - started > 3f);
            Assert.That(session.FleeResult, Is.Not.Null);
            Steps(session, 1);
            Assert.That(session.FleeResult, Is.Null);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void NegativeHpRequestInterruptsTheWaitBodyAndSchedulesFailure(string variant)
        {
            BattleSession session = Session(variant);
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            Steps(session, 5);
            Invoke(session, "ChangeHp", NativeActor(session, "A"), -1, null, 0L, "native negative HP test", false, false, false);
            Assert.That(Actor(session, "A").CurrentAction, Is.EqualTo(BattleAction.Flee));
            Assert.That(Actor(session, "A").CastGauge, Is.Zero);
            Assert.That(session.FleeResult, Is.Null);
            Until(session, () => session.FleeResult != null);
            Assert.That(session.FleeResult.Success, Is.False);
            Assert.That(Actor(session, "A").CurrentAction, Is.Null);
            Assert.That(Actor(session, "A").Gauge, Is.GreaterThan(0));
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        [TestCase("PAL3", true)]
        [TestCase("PAL3A", true)]
        public void GhostEscapeWaterUsesAnIndependentWaitThreadAndScriptBattlesDoNotSetItsFlag(string variant, bool script)
        {
            BattleSession session = Session(variant, script: script);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "A", 6114)), Is.True);
            Until(session, () => session.FleeResult != null);
            Assert.That(session.FleeAlwaysSucceeds, Is.EqualTo(!script));
            Assert.That(session.Inventory.Single().Count, Is.EqualTo(1));
            Assert.That(session.FleeResult.Success, Is.EqualTo(!script));
            Assert.That(session.Events.Any(entry => entry.Detail == "item consumed when its action starts"), Is.True);
            if (!script)
            {
                Assert.That(session.Tick, Is.LessThan(100));
                Until(session, () => session.Result != null);
                Assert.That(session.Result.Escaped, Is.True);
            }
        }

        [TestCase("PAL3", false, 1u)]
        [TestCase("PAL3A", false, 1u)]
        [TestCase("PAL3", true, 2745024u)]
        [TestCase("PAL3A", true, 1u)]
        public void ProbabilityEndpointsPreserveThePublishedVariantRandomOrder(string variant, bool highChance, uint expectedSeed)
        {
            BattleSession session = Session(variant, script: true);
            object actor = NativeActor(session, "A");
            object enemy = NativeActor(session, "E");
            actor.GetType().GetField("Level").SetValue(actor, highChance ? 1 : 100);
            enemy.GetType().GetField("Level").SetValue(enemy, highChance ? 100 : 1);
            ((int[])actor.GetType().GetField("Attributes").GetValue(actor))[6] = 0;
            ((int[])enemy.GetType().GetField("Attributes").GetValue(enemy))[6] = 100;
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            Until(session, () => session.FleeResult != null);
            Assert.That(session.Events.Last(entry => entry.Detail == "escape failed").RandomState, Is.EqualTo(expectedSeed));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void CancellingThePartyRunReleasesItsSharedBodiesAndResumesTheGauges(string variant)
        {
            BattleSession session = Session(variant, secondAlly: true);
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            Until(session, () => session.FleeResult?.Success == true);
            Steps(session, 5);
            Assert.That(Actor(session, "A").Position, Is.Not.EqualTo(Actor(session, "A").Home));
            Assert.That(Actor(session, "B").Position, Is.Not.EqualTo(Actor(session, "B").Home));
            Assert.That(session.CancelAction("A"), Is.True);
            Assert.That(Actor(session, "A").Position, Is.EqualTo(Actor(session, "A").Home));
            Assert.That(Actor(session, "B").Position, Is.EqualTo(Actor(session, "B").Home));
            Assert.That(((System.Collections.IDictionary)typeof(BattleSession).GetField("_bodyStageOwners", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session)).Count, Is.Zero);
            Steps(session, 10);
            Assert.That(Actor(session, "A").Movement, Is.Null);
            Assert.That(Actor(session, "B").Movement, Is.Null);
            Assert.That(Actor(session, "A").Gauge, Is.GreaterThan(0));
            Assert.That(Actor(session, "B").Gauge, Is.GreaterThan(0));
            Assert.That(Actor(session, "E").Gauge, Is.GreaterThan(264));
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running));
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        [TestCase("PAL3", true)]
        [TestCase("PAL3A", true)]
        public void StoppingThePartyRunClearsThePauseAndAllBodiesIdempotently(string variant, bool fault)
        {
            BattleSession session = Session(variant, secondAlly: true);
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            Until(session, () => session.FleeResult?.Success == true);
            if (fault)
                session.Fault("flee fault test");
            else
                session.Abort();
            Assert.That(typeof(BattleSession).GetField("_fleeAtbPaused", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session), Is.False);
            Assert.That(((System.Collections.IDictionary)typeof(BattleSession).GetField("_bodyStageOwners", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session)).Count, Is.Zero);
            Invoke(session, "StopFleeStages");
            Assert.That(session.Status, Is.EqualTo(fault ? BattleSessionStatus.Faulted : BattleSessionStatus.Aborted));
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        [TestCase("PAL3", true)]
        [TestCase("PAL3A", true)]
        public void ACommittedPartyEscapeContinuesAfterLethalDamageUnlessTheWholePartyIsDead(string variant, bool allDead)
        {
            BattleSession session = Session(variant, secondAlly: true);
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            Until(session, () => session.FleeResult?.Success == true);
            Invoke(session, "ChangeHp", NativeActor(session, "A"), -1000, null, 0L, "pending lethal during committed escape", false, false, false);
            if (allDead)
                Invoke(session, "ChangeHp", NativeActor(session, "B"), -1000, null, 0L, "last party member killed during escape", false, false, false);
            Until(session, () => session.Result != null);
            Assert.That(session.Result.Escaped, Is.EqualTo(!allDead));
            Assert.That(session.Result.Winner, Is.EqualTo(allDead ? (BattleSide?)BattleSide.Enemy : null));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Cancelled && entry.Source == "A" && entry.Detail == "physical stage received an end request"), Is.False);
        }

        [TestCase("PAL3", BattleState.Chaos, false)]
        [TestCase("PAL3A", BattleState.Chaos, false)]
        [TestCase("PAL3", BattleState.Madness, false)]
        [TestCase("PAL3A", BattleState.Madness, false)]
        [TestCase("PAL3", BattleState.Chaos, true)]
        [TestCase("PAL3A", BattleState.Chaos, true)]
        [TestCase("PAL3", BattleState.Madness, true)]
        [TestCase("PAL3A", BattleState.Madness, true)]
        public void UncontrollableStatesResetOnlyOrdinaryEscapeWaiting(string variant, BattleState state, bool forced)
        {
            BattleSession session = Session(variant);
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            Steps(session, 5);
            typeof(BattleSession).GetField("_fleeAlwaysSucceeds", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, forced);
            int before = Actor(session, "A").CastGauge;
            Invoke(session, "ApplyState", NativeActor(session, "A"), state, false, 99, true, null, 0L, true);
            Assert.That(Actor(session, "A").CastGauge, Is.EqualTo(forced ? before : 0));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Interrupted && entry.Source == "A"), Is.EqualTo(!forced));
            Until(session, () => session.FleeResult != null);
            Assert.That(session.FleeResult.Success, Is.EqualTo(forced));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void LethalDamageDuringWaitingStillFinishesTheNativeFailureMessage(string variant)
        {
            BattleSession session = Session(variant, secondAlly: true);
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            Steps(session, 5);
            Invoke(session, "ChangeHp", NativeActor(session, "A"), -1000, null, 0L, "lethal request while waiting", false, false, false);
            Until(session, () => session.FleeResult != null);
            Assert.That(session.FleeResult.Success, Is.False);
            Assert.That(Actor(session, "A").CurrentAction, Is.Null);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Cancelled && entry.Source == "A" && entry.Detail == "physical stage received an end request"), Is.False);
        }

        [TestCase("PAL3", BattleState.Paralysis)]
        [TestCase("PAL3A", BattleState.Paralysis)]
        [TestCase("PAL3", BattleState.Sleep)]
        [TestCase("PAL3A", BattleState.Sleep)]
        [TestCase("PAL3", BattleState.Chaos)]
        [TestCase("PAL3A", BattleState.Chaos)]
        [TestCase("PAL3", BattleState.Madness)]
        [TestCase("PAL3A", BattleState.Madness)]
        public void UncontrollableStatesDoNotRevokeACommittedPartyEscape(string variant, BattleState state)
        {
            BattleSession session = Session(variant, secondAlly: true);
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            Until(session, () => session.FleeResult?.Success == true);
            Invoke(session, "ApplyState", NativeActor(session, "A"), state, false, 99, true, null, 0L, true);
            Assert.That(Actor(session, "A").CurrentAction, Is.EqualTo(BattleAction.Flee));
            Assert.That(Actor(session, "A").CastGauge, Is.Zero);
            Until(session, () => session.Result != null);
            Assert.That(session.Result.Escaped, Is.True);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void EnemyArrivalClearsNativeStatesAndModelLifeOnlyInPal3A(string variant)
        {
            BattleSession session = Session(variant, enemyFlee: true);
            Steps(session, 1);
            object enemy = NativeActor(session, "E");
            var states = (Dictionary<BattleState, BattleStatusInstance>)enemy.GetType().GetField("States").GetValue(enemy);
            states[BattleState.Barrier] = new BattleStatusRules(variant).Create(BattleState.Barrier, 100, 99, true);
            int rounds = Actor(session, "E").Rounds;
            Until(session, () => Actor(session, "E").Escaped);
            Assert.That(Actor(session, "E").Rounds, Is.EqualTo(rounds));
            Assert.That(Actor(session, "E").States.Any(state => state.State == BattleState.Barrier), Is.EqualTo(variant == "PAL3"));
            Assert.That(Actor(session, "E").ModelAliveFlag, Is.EqualTo(variant == "PAL3"));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void EnemyBecomesFleeingBeforeWalkingAndFledOnlyAfterReachingItsDestination(string variant)
        {
            BattleSession session = Session(variant, enemyFlee: true);
            Steps(session, 1);
            BattleActorSnapshot before = Actor(session, "E");
            Assert.That(before.Fleeing, Is.True);
            Assert.That(before.Escaped, Is.False);
            Assert.That(before.Pose, Is.EqualTo(BattlePose.Moving));
            Invoke(session, "ChangeHp", NativeActor(session, "E"), -1, null, 0L, "pending damage can hit a fleeing target", false, false, false);
            Assert.That(Actor(session, "E").Hp, Is.EqualTo(99));
            Until(session, () => Actor(session, "E").Escaped);
            Assert.That(Actor(session, "E").Position.X, Is.EqualTo(800));
            Assert.That(Actor(session, "E").Position.Z, Is.EqualTo(800));
            Until(session, () => session.Result != null);
            Assert.That(session.Result.Winner, Is.EqualTo(BattleSide.Ally));
        }
    }
}
