// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Linq;
    using Battle;
    using NUnit.Framework;

    public sealed class BattleSessionTests
    {
        public sealed class Catalog : IBattleCatalog
        {
            public AttackTiming Timing = new(0, 10, new[] { 5 });
            public int HurtTicks = 2;
            public AttackTiming EnemyTiming;

            public AttackTiming Attack(string source, string target) => source == "Enemy" && EnemyTiming != null ? EnemyTiming : Timing;

            public int GuardEntry(string actor) => 2;

            public int Hurt(string actor) => HurtTicks;

            public int SpImpact(string actor) => 8;

            public int Priority(string actor) => 0;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class ReadyRules : IBattleRules
        {
            private readonly BattleBasicRules _base = new();

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => BattleSession.FullGauge;

            public int GaugePerStep(BattleParticipant actor) => _base.GaugePerStep(actor);

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1) => _base.Hit(source, target, guarding, random, targetAtHome, damageDivisor);
        }

        public static BattleScenario Scenario()
        {
            var scenario = new BattleScenario { Scene = "synthetic", Encounter = BattleEncounter.AllyAmbush };
            foreach (BattleSide side in Enum.GetValues(typeof(BattleSide)))
                scenario.Participants.Add(new BattleParticipant
                {
                    InstanceId = side.ToString(),
                    ActorId = side == BattleSide.Ally ? 101 : 701,
                    Side = side,
                    Attributes = new[] { 100, 50, 30, 40, 10, 3, 0, 0, 0, 0, 0, 0 },
                    Vitals = new[] { 100, 0, 30 }
                });
            return scenario;
        }

        [TestCase(BattleSessionStatus.Aborted)]
        [TestCase(BattleSessionStatus.Faulted)]
        [TestCase(BattleSessionStatus.Completed)]
        public void TerminalSessionHasNoWaitingActor(BattleSessionStatus status)
        {
            var scenario = Scenario();
            if (status == BattleSessionStatus.Completed)
                scenario.Participants[1].Vitals[0] = 0;
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), new ReadyRules());
            if (status == BattleSessionStatus.Aborted)
                session.Abort();
            else if (status == BattleSessionStatus.Faulted)
                session.Fault("fixture");
            Assert.That(session.Status, Is.EqualTo(status));
            Assert.That(session.WaitingActor, Is.Null);
        }

        private static string Trace(BattleSession session) => string.Join("\n", session.Events.Select(entry =>
            $"{entry.Tick}:{entry.Kind}:{entry.Source}:{entry.Target}:{entry.ActionId}:{entry.RequestedDelta}:{entry.ActualDelta}:{entry.Critical}:{entry.Dodged}:{entry.RandomState}:{entry.Detail}"));

        [TestCase(1, 26, 14, 3, 2, 14, 1, -12)]
        [TestCase(20, 100, 20, 4, 9, 30, 1, -78)]
        [TestCase(20, 100, 20, 4, 9, 30, 12345, -77)]
        [TestCase(100, 500, 30, 5, 100, 100, 1, -641)]
        [TestCase(100, 500, 30, 5, 100, 100, 12345, -576)]
        [TestCase(1, 10, 10, 3, 1, 100, 1, 5)]
        [TestCase(1, 10, 10, 3, 1, 100, 12345, 1)]
        [TestCase(100, 2, 0, 9, 5, 1, 1, -2)]
        public void MatchesNeutralDamageVectorsExecutedFromBothOriginalExecutables(int level, int attack, int defense, int speed, int luck, int targetDefense, int seed, int expected)
        {
            // Independent native oracle: workspace/Analysis/Battle-Stage2/probe-native-rules.py.
            BattleScenario scenario = Scenario();
            BattleParticipant source = scenario.Participants[0], target = scenario.Participants[1];
            source.Level = level;
            source.Attributes[3] = attack;
            source.Attributes[4] = defense;
            source.Attributes[5] = speed;
            source.Attributes[6] = luck;
            target.Attributes[4] = targetDefense;
            Assert.That(new BattleBasicRules().NormalDamage(source, target, new BattleRandom(seed)), Is.EqualTo(expected));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ReplayReproducesCapturedInputsAndEvents(string variant)
        {
            BattleScenario scenario = Scenario();
            scenario.Variant = variant;
            scenario.DataFingerprint = "replay-fixture";
            BattleDefinitions definitions = ReplayDefinitions(scenario);
            var setup = new BattleSetup(scenario);
            var catalog = new Catalog();
            var session = new BattleSession(setup, catalog, definitions: definitions);
            session.Abort("recorded abort");
            bool created = false;
            BattleSession replay = BattleReplay.Play(setup, catalog, session.Inputs, session.Tick,
                definitions: definitions, onCreated: _ => created = true);
            Assert.That(created, Is.True);
            Assert.That(Trace(replay), Is.EqualTo(Trace(session)));
        }

        private static BattleDefinitions ReplayDefinitions(BattleScenario scenario) => new(scenario.Variant, scenario.DataFingerprint,
            Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(),
            scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
            {
                InstanceId = actor.InstanceId,
                Identity = actor.Side == BattleSide.Ally ? 0 : -1
            })));

        [Test]
        public void NativeRandomAndInitialGaugeUseIntegerRatioAndEncounterSide()
        {
            var random = new BattleRandom(1);
            Assert.That(new[] { random.Next(), random.Next(), random.Next() }, Is.EqualTo(new[] { 41, 18467, 6334 }));
            BattleScenario scenario = Scenario();
            scenario.Encounter = BattleEncounter.Normal;
            var session = new BattleSession(new BattleSetup(scenario), new Catalog());
            Assert.That(session.Actors.Select(actor => actor.Gauge), Is.EqualTo(new[] { 1955, 2517 }));
            session.Step();
            Assert.That(session.Actors.Select(actor => actor.Gauge), Is.EqualTo(new[] { 1990, 2552 }));
            scenario.Encounter = BattleEncounter.EnemyAmbush;
            session = new BattleSession(new BattleSetup(scenario), new Catalog());
            Assert.That(session.Actors.Select(actor => actor.Gauge), Is.EqualTo(new[] { 0, 11500 }));
        }

        [Test]
        public void ManualWaitStopsGaugesButNotAnAcceptedAttackAndNeverHitsBeforeMarker()
        {
            var session = new BattleSession(new BattleSetup(Scenario()), new Catalog());
            for (int i = 0; i < 10; i++)
                session.Step();
            Assert.That(session.Actors[1].Gauge, Is.Zero);
            Assert.That(session.Submit("Ally", BattleAction.Attack, "Enemy"), Is.True);
            for (int i = 0; i < 4; i++)
                session.Step();
            Assert.That(session.Actors[1].Hp, Is.EqualTo(100));
            session.Step();
            Assert.That(session.Actors[1].Hp, Is.LessThan(100));
            Assert.That(session.Actors[0].Gauge, Is.Zero);
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Hit), Is.EqualTo(1));
        }

        [Test]
        public void InvalidCommandsAndCancellationCannotRerollOrProduceDelayedHits()
        {
            var session = new BattleSession(new BattleSetup(Scenario()), new Catalog());
            string trace = Trace(session);
            Assert.That(session.Submit("Ally", BattleAction.Attack, "Ally"), Is.False);
            Assert.That(session.Submit("Enemy", BattleAction.Attack, "Ally"), Is.False);
            Assert.That(Trace(session), Is.EqualTo(trace));
            session.Submit("Ally", BattleAction.Attack, "Enemy");
            session.Step();
            Assert.That(session.CancelAction("Ally"), Is.True);
            Assert.That(session.CancelAction("Ally"), Is.False);
            for (int i = 0; i < 15; i++)
                session.Step();
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit), Is.False);
            Assert.That(session.Actors[0].Gauge, Is.LessThan(BattleSession.FullGauge));
        }

        [Test]
        public void GuardStartsAfterEntryHalvesDamageAndExpiresWhenGaugeFills()
        {
            var catalog = new Catalog();
            var session = new BattleSession(new BattleSetup(Scenario()), catalog, new ReadyRules());
            session.Submit("Ally", BattleAction.Defend);
            session.Submit("Enemy", BattleAction.Attack, "Ally");
            session.Step();
            Assert.That(session.Actors[0].Guarding, Is.False);
            session.Step();
            Assert.That(session.Actors[0].Guarding, Is.True);
            for (int i = 0; i < 3; i++)
                session.Step();
            BattleEvent hit = session.Events.Single(entry => entry.Kind == BattleEventKind.Hit);
            Assert.That(hit.ActualDelta, Is.EqualTo(-16));
            Assert.That(session.Actors[0].Sp, Is.EqualTo(8));
            for (int i = 0; i < 400 && !session.Actors[0].Ready; i++)
                session.Step();
            Assert.That(session.Actors[0].Ready, Is.True);
            Assert.That(session.Actors[0].Guarding, Is.False);
            Assert.That(session.Actors[0].Rounds, Is.EqualTo(1));
        }

        [Test]
        public void SameTickLethalHitHasStablePriorityAndCancelsDeadActorsRemainingMarkers()
        {
            BattleScenario scenario = Scenario();
            foreach (BattleParticipant actor in scenario.Participants)
                actor.Vitals[0] = 1;
            var catalog = new Catalog { Timing = new AttackTiming(0, 10, new[] { 2, 4 }) };
            var session = new BattleSession(new BattleSetup(scenario), catalog, new ReadyRules());
            session.SetAutomatic(BattleSide.Ally, true);
            session.SetAutomatic(BattleSide.Enemy, true);
            for (int i = 0; i < 10; i++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(session.Result.Winner, Is.EqualTo(BattleSide.Ally));
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Death), Is.EqualTo(1));
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Hit), Is.EqualTo(1));
            long stopped = session.Tick;
            session.Advance(10);
            Assert.That(session.Tick, Is.EqualTo(stopped));
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void FrameRateAndReplayProduceIdenticalEventsAndNaturalResult(int frameRate)
        {
            BattleScenario scenario = Scenario();
            var setup = new BattleSetup(scenario);
            var catalog = new Catalog();
            var expected = new BattleSession(setup, catalog);
            var actual = new BattleSession(setup, catalog);
            foreach (BattleSession session in new[] { expected, actual })
            {
                session.SetAutomatic(BattleSide.Ally, true);
                session.SetAutomatic(BattleSide.Enemy, true);
            }
            for (int i = 0; i < 10000 && expected.Status == BattleSessionStatus.Running; i++)
                expected.Step();
            for (int i = 0; i < 100000 && actual.Status == BattleSessionStatus.Running; i++)
                actual.Advance(1.0 / frameRate);
            Assert.That(actual.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(Trace(actual), Is.EqualTo(Trace(expected)));
            BattleSession replay = BattleReplay.Play(setup, catalog, actual.Inputs, actual.Tick);
            Assert.That(Trace(replay), Is.EqualTo(Trace(actual)));
            Assert.That(scenario.Participants[0].Vitals[0], Is.EqualTo(100));
        }

        [Test]
        public void BasicLethalHitRemovesDeadTargetsFromSnapshotsAndCommandValidation()
        {
            BattleScenario scenario = Scenario();
            scenario.Participants[1].Vitals[0] = 1;
            BattleParticipant secondEnemy = scenario.Participants[1].Copy();
            secondEnemy.InstanceId = "Enemy2";
            secondEnemy.Slot = BattleSlot.Fire;
            scenario.Participants.Add(secondEnemy);
            var session = new BattleSession(new BattleSetup(scenario), new Catalog());
            Assert.That(session.Submit("Ally", BattleAction.Attack, "Enemy"), Is.True);
            for (int i = 0; i < 10; i++)
                session.Step();
            BattleActorSnapshot dead = session.Actors.Single(actor => actor.Id == "Enemy");
            Assert.That(dead.Hp, Is.Zero);
            Assert.That(dead.Alive, Is.False);
            Assert.That(dead.ModelAliveFlag, Is.False);
            Assert.That(dead.Pose, Is.EqualTo(BattlePose.Dead));
            Assert.That(session.GetTargetCandidates(new BattleCommand("Ally", BattleAction.Attack))
                .Select(actor => actor.Id), Is.EqualTo(new[] { "Enemy2" }));
            session.SetAutomatic(BattleSide.Enemy, true);
            for (int i = 0; i < 1000 && session.WaitingActor != "Ally"; i++)
                session.Step();
            Assert.That(session.WaitingActor, Is.EqualTo("Ally"));
            Assert.That(session.CanSubmit("Ally", BattleAction.Attack, "Enemy"), Is.EqualTo("Choose a legal target for this action."));
            Assert.That(session.CanSubmit("Ally", BattleAction.Attack, "Enemy2"), Is.Null);
        }

        [Test]
        public void BasicAutomaticBattleProgressesPastFirstDefeatedEnemyAndReplays()
        {
            BattleScenario scenario = Scenario();
            scenario.Participants[1].Vitals[0] = 1;
            BattleParticipant secondEnemy = scenario.Participants[1].Copy();
            secondEnemy.InstanceId = "Enemy2";
            secondEnemy.Slot = BattleSlot.Fire;
            scenario.Participants.Add(secondEnemy);
            var setup = new BattleSetup(scenario);
            var catalog = new Catalog();
            var session = new BattleSession(setup, catalog);
            session.SetAutomatic(BattleSide.Ally, true);
            session.SetAutomatic(BattleSide.Enemy, true);
            for (int i = 0; i < 1000 && session.Status == BattleSessionStatus.Running; i++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(session.Result.Winner, Is.EqualTo(BattleSide.Ally));
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Death), Is.EqualTo(2));
            Assert.That(session.Result.Actors.Where(actor => actor.Side == BattleSide.Enemy)
                .All(actor => !actor.Alive && !actor.ModelAliveFlag), Is.True);
            Assert.That(Trace(BattleReplay.Play(setup, catalog, session.Inputs, session.Tick)), Is.EqualTo(Trace(session)));
        }

        [Test]
        public void EmptyLivingTeamsCompleteAndForcedStopIsNeverVictory()
        {
            BattleScenario scenario = Scenario();
            foreach (BattleParticipant actor in scenario.Participants)
                actor.Vitals[0] = 0;
            var session = new BattleSession(new BattleSetup(scenario), new Catalog());
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(session.Result.Winner, Is.Null);
            session = new BattleSession(new BattleSetup(Scenario()), new Catalog());
            session.Abort("test");
            Assert.That(session.Result.Status, Is.EqualTo(BattleSessionStatus.Aborted));
            Assert.That(session.Result.Winner, Is.Null);
        }

        [Test]
        public void MovingActorsWithEqualSpeedDoNotChaseEachOtherForever()
        {
            BattleScenario scenario = Scenario();
            scenario.Encounter = BattleEncounter.Normal;
            foreach (BattleParticipant actor in scenario.Participants)
                actor.Attributes[5] = 19;
            var catalog = new Catalog { Timing = new AttackTiming(5, 10, new[] { 5 }) };
            var setup = new BattleSetup(scenario);
            var session = new BattleSession(setup, catalog);
            session.SetAutomatic(BattleSide.Ally, true);
            session.SetAutomatic(BattleSide.Enemy, true);
            for (int i = 0; i < 10000 && session.Status == BattleSessionStatus.Running; i++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit), Is.True);
            Assert.That(Trace(BattleReplay.Play(setup, catalog, session.Inputs, session.Tick)), Is.EqualTo(Trace(session)));
        }

        [Test]
        public void ExternalFaultIsRecordedAndReplayedWithoutBecomingVictory()
        {
            var setup = new BattleSetup(Scenario());
            var catalog = new Catalog();
            var session = new BattleSession(setup, catalog);
            session.Step();
            session.Fault("resource failure");
            BattleSession replay = BattleReplay.Play(setup, catalog, session.Inputs, session.Tick);
            Assert.That(replay.Status, Is.EqualTo(BattleSessionStatus.Faulted));
            Assert.That(replay.Result.Winner, Is.Null);
            Assert.That(Trace(replay), Is.EqualTo(Trace(session)));
        }

        [Test]
        public void UnsupportedInitialRulesRejectRatherThanSilentlyIgnore()
        {
            BattleScenario scenario = Scenario();
            scenario.Participants[0].Statuses.Add(new BattleStatus { Id = 1 });
            Assert.Throws<NotSupportedException>(() => new BattleSession(new BattleSetup(scenario), new Catalog()));
            scenario.Participants[0].Statuses.Clear();
            scenario.RoundLimit = 2;
            Assert.Throws<NotSupportedException>(() => new BattleSession(new BattleSetup(scenario), new Catalog()));
        }

        [Test]
        public void HurtRecoveryPreventsImmediateCommandAndDefersInPlaceHitMarkers()
        {
            var setup = new BattleSetup(Scenario());
            var catalog = new Catalog { Timing = new AttackTiming(0, 20, new[] { 1, 5 }), HurtTicks = 10 };
            var session = new BattleSession(setup, catalog, new ReadyRules());
            session.SetAutomatic(BattleSide.Enemy, true);
            session.Step();
            session.Step();
            Assert.That(session.Actors[0].Pose, Is.EqualTo(BattlePose.Hurt));
            Assert.That(session.Submit("Ally", BattleAction.Attack, "Enemy"), Is.False);
            for (int i = 0; i < 9; i++)
                session.Step();
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit && entry.Source == "Ally"), Is.False);

            session = new BattleSession(setup, catalog, new ReadyRules());
            session.Submit("Ally", BattleAction.Attack, "Enemy");
            session.Submit("Enemy", BattleAction.Attack, "Ally");
            session.Step();
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Hit), Is.EqualTo(1));
            Assert.That(session.Actors[1].ActionTick, Is.Zero, "Hurt pushes an in-place action instead of letting its marker fire through recovery.");
            for (int i = 0; i < 5; i++)
                session.Step();
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit && entry.Source == "Enemy"), Is.False);
        }

        [TestCase(1, -6)]
        [TestCase(12345, -6)]
        public void NativeWeaponDivisorIsAppliedAfterWarpAndBeforeCritical(int seed, int expected)
        {
            BattleScenario scenario = Scenario();
            scenario.Participants[0].Attributes[3] = 26;
            scenario.Participants[1].Attributes[4] = 14;
            Assert.That(new BattleBasicRules().NormalDamage(scenario.Participants[0], scenario.Participants[1], new BattleRandom(seed), 2), Is.EqualTo(expected));
        }

        [Test]
        public void HurtRestartsPreHitAnimationButDiscardsMarkersAfterFirstWork()
        {
            var setup = new BattleSetup(Scenario());
            var catalog = new Catalog { Timing = new AttackTiming(0, 20, new[] { 10 }), EnemyTiming = new AttackTiming(0, 20, new[] { 1 }), HurtTicks = 10 };
            var session = new BattleSession(setup, catalog, new ReadyRules());
            session.Submit("Ally", BattleAction.Attack, "Enemy");
            session.Submit("Enemy", BattleAction.Attack, "Ally");
            for (int i = 0; i < 19; i++)
                session.Step();
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit && entry.Source == "Ally"), Is.False);
            session.Step();
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Hit && entry.Source == "Ally"), Is.EqualTo(1));

            catalog = new Catalog { Timing = new AttackTiming(0, 20, new[] { 1, 5 }), EnemyTiming = new AttackTiming(0, 20, new[] { 1 }) };
            session = new BattleSession(setup, catalog, new ReadyRules());
            session.Submit("Ally", BattleAction.Attack, "Enemy");
            session.Submit("Enemy", BattleAction.Attack, "Ally");
            for (int i = 0; i < 20; i++)
                session.Step();
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Hit && entry.Source == "Ally"), Is.EqualTo(1), "A pushed attack task is discarded after its first work marker.");
            Assert.That(session.Actors[0].ActionId, Is.Zero);
        }
    }
}
