// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using NUnit.Framework;

    public sealed class BattleEncounterSessionTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings, IBattleMonsterFactory
        {
            public readonly List<(int NativeIndex, int ActorId, string InstanceId)> Created = new();

            public AttackTiming Attack(string source, string target) => new(0, 3, new[] { 1 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => new(0, 3, new[] { 1 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => 0;

            public int DamageDivisor(string actor) => 1;

            public BattleMonsterCreation Prepare(int nativeIndex, int actorId, string instanceId)
            {
                BattleParticipant participant = Participant(instanceId, BattleSide.Enemy, (BattleSlot)(nativeIndex - 5), actorId);
                return new BattleMonsterCreation(nativeIndex, participant, new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = instanceId,
                    NormalAttackMode = 901,
                    StealableMoney = actorId,
                    PermanentStates = new[] { BattleState.PoisonResist }
                }));
            }

            public Action<BattleMonsterCreation> OnCommit;

            public void Commit(BattleMonsterCreation creation)
            {
                OnCommit?.Invoke(creation);
                Created.Add((creation.NativeIndex, creation.Participant.ActorId, creation.Participant.InstanceId));
            }
        }

        private sealed class Rules : IBattleRules
        {
            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => BattleSession.FullGauge;

            public int GaugePerStep(BattleParticipant actor) => BattleSession.FullGauge;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1) => new(0, false, false);
        }

        private static BattleParticipant Participant(string id, BattleSide side, BattleSlot slot, int actorId) => new()
        {
            InstanceId = id,
            ActorId = actorId,
            Side = side,
            Slot = slot,
            Level = 20,
            Attributes = new[] { 500, 100, 100, 20, 10, 3, 0, 0, 0, 0, 0, 0 },
            Vitals = new[] { 500, 100, 100 }
        };

        private static BattleScenario Scenario(string variant, int enemy = 701) => new()
        {
            Variant = variant,
            Scene = "encounter-fixture",
            DataFingerprint = "encounter-fixture",
            Participants = new List<BattleParticipant>
            {
                Participant("A", BattleSide.Ally, BattleSlot.Water, 101),
                Participant("E", BattleSide.Enemy, BattleSlot.Center, enemy)
            }
        };

        private static BattleSession Session(BattleScenario scenario, Catalog catalog = null, params BattleAbilityDefinition[] skills)
        {
            var definitions = new BattleDefinitions(scenario.Variant, scenario.DataFingerprint, skills, Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Ally ? actor.InstanceId == "A" ? 0 : 1 : -1,
                    NormalAttackMode = 901
                })));
            var session = new BattleSession(new BattleSetup(scenario), catalog ?? new Catalog(), new Rules(), definitions);
            session.SetAutomatic(BattleSide.Enemy, false);
            return session;
        }

        private static BattleAbilityDefinition Magic(int id, int power, int special = 0) => new(new BattleAbilityData
        {
            Id = id,
            IsMagic = true,
            SkillTypeId = 2,
            Range = BattleTargetRange.OpponentSingle,
            SpecialEffect = special,
            Handler = BattleSpecialRules.AbilityHandler("PAL3", id, false, true, special),
            Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, power) },
            Costs = new[] { new BattleCost(2, BattleValueMode.Absolute, 7) }
        });

        private static void Learn(BattleParticipant actor, int id, int level = 4) => actor.Skills.Add(new BattleSkill { Id = id, Level = level, Mastered = true });

        private static void Advance(BattleSession session, int count, bool defend = false)
        {
            for (int index = 0; index < count && session.Status == BattleSessionStatus.Running; index++)
            {
                if (defend)
                    while (session.WaitingActor is { } waiting)
                        Assert.That(session.Submit(waiting, BattleAction.Defend), Is.True);
                session.Step();
            }
            Assert.That(session.Status, Is.Not.EqualTo(BattleSessionStatus.Faulted), session.Result?.Reason);
        }

        private sealed class ReplacementEffects : IBattleEffectDirector, IBattleSpiritCaptureLayout, IBattleMovementAudio
        {
            public readonly List<string> Calls = new();

            public Func<int> CurrentActor;

            private long _nextHandle;

            public long Start(BattleEffectRequest request) => ++_nextHandle;

            public bool IsRunning(long handle) => false;

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
            }

            public void Stop(long handle)
            {
            }

            public BattleSpiritPoint CaptureSpiritOrigin(string actorId, BattleMovementPoint world) => new(400, 300);

            public BattleSpiritRect SpiritWheelTarget(int coreElement) => new(722, 300, 750, 345);

            public void PlayMovementSound(string actor, string sound, bool loop) => Calls.Add("play:" + sound);

            public void StopMovementSound(string actor) => Calls.Add("stop:" + CurrentActor());

            public void StopBodySounds()
            {
            }
        }

        [Test]
        public void SummonReusingCapturedSlotsClearsOnlyTheirCurrentCaptureMarker()
        {
            BattleScenario scenario = Scenario("PAL3A", 720);
            Learn(scenario.Participants[0], 4102);
            Learn(scenario.Participants[1], 274);
            for (int slot = 1; slot <= 3; slot += 2)
            {
                BattleParticipant enemy = Participant("captured" + slot, BattleSide.Enemy, (BattleSlot)slot, slot == 1 ? 7364 : 7365);
                enemy.Vitals[0] = 1;
                scenario.Participants.Add(enemy);
            }
            var capture = new BattleAbilityDefinition(new BattleAbilityData { Id = 4102, Range = BattleTargetRange.OpponentAll });
            var definitions = new BattleDefinitions("PAL3A", scenario.DataFingerprint,
                new[] { capture, Magic(274, 0, 29) }, Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Ally ? 0 : -1,
                    Boss = actor.InstanceId == "E",
                    PhysicalAttackElementValues = new[] { 1, 0, 0, 0, 0 }
                })));
            var catalog = new Catalog();
            var effects = new ReplacementEffects();
            var session = new BattleSession(new BattleSetup(scenario), catalog, new Rules(), definitions, effects);
            session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 4102)), Is.True);
            while (session.WaitingActor != "E")
                Assert.That(session.Submit(session.WaitingActor, BattleAction.Defend), Is.True);
            Advance(session, 2);
            Assert.That(session.SpiritOrbs, Has.Count.EqualTo(2));
            Assert.That(session.Submit(new BattleCommand("E", BattleAction.Skill, "A", 274)), Is.True);
            Advance(session, 2);
            Assert.That(catalog.Created, Has.Count.EqualTo(2));
            Assert.That(session.SpiritOrbs, Has.Count.EqualTo(2), "Previously emitted spirit orbs still belong to the capture.");
            Assert.That(session.Events.Count(entry => entry.Detail == "original spirit capture"), Is.EqualTo(2));
            Assert.That(catalog.Created.Any(created => created.InstanceId.StartsWith("captured")), Is.True);
            foreach (var created in catalog.Created)
            {
                Assert.That(session.MonsterGeneration(created.InstanceId), Is.EqualTo(1));
                Assert.That(session.WasSpiritCaptured(created.InstanceId), Is.False, created.InstanceId);
            }
            foreach (BattleParticipant enemy in scenario.Participants.Where(actor => actor.InstanceId.StartsWith("captured") &&
                catalog.Created.All(created => created.InstanceId != actor.InstanceId)))
                Assert.That(session.WasSpiritCaptured(enemy.InstanceId), Is.True);
        }

        [Test]
        public void ReplacingAMovingMonsterStopsItsOldAudioInTheSameTick()
        {
            BattleScenario scenario = Scenario("PAL3", 738);
            var catalog = new Catalog();
            var effects = new ReplacementEffects();
            var definitions = new BattleDefinitions("PAL3", scenario.DataFingerprint, Array.Empty<BattleAbilityDefinition>(),
                Array.Empty<BattleAbilityDefinition>(), scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Ally ? 0 : -1
                })));
            var session = new BattleSession(new BattleSetup(scenario), catalog, new Rules(), definitions, effects);
            effects.CurrentActor = () => session.CaptureParticipant("E").ActorId;
            catalog.OnCommit = creation => effects.Calls.Add("commit:" + creation.Participant.ActorId);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            object enemy = typeof(BattleSession).GetMethod("ActorAtNativeIndex", flags).Invoke(session, new object[] { 10 });
            var movement = new BattleMovement("PAL3", new BattleMovementPlan(BattleMovementKind.Teleport,
                new BattleMovementPoint(100, 0, 0), new BattleMovementPoint(0, 0, 0)));
            enemy.GetType().GetField("Movement", flags | System.Reflection.BindingFlags.Public).SetValue(enemy, movement);
            typeof(BattleSession).GetMethod("AdvanceBodyMovement", flags).Invoke(session, new object[] { enemy, true, null });
            Assert.That(effects.Calls, Is.EqualTo(new[] { "play:we014" }));
            long tick = session.Tick;
            typeof(BattleSession).GetMethod("CreateOriginalMonster", flags).Invoke(session, new object[] { 10, 739, 42L });
            Assert.That(effects.Calls, Is.EqualTo(new[] { "play:we014", "commit:739", "stop:738" }));
            Assert.That(session.Tick, Is.EqualTo(tick));
            Assert.That(session.CaptureParticipant("E").ActorId, Is.EqualTo(739));
            Assert.That(session.Events.Last(entry => entry.Detail == "native enemy slot 10 created actor 739").ActionId, Is.EqualTo(42));
            typeof(BattleSession).GetMethod("SyncBodyMovementSound", flags).Invoke(session, new[] { enemy });
            Assert.That(effects.Calls.Count(call => call.StartsWith("stop:")), Is.EqualTo(1));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void NativeZeroRoundLimitCanWinBeforeAnyEnemyDies(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.RoundLimit = 0;
            BattleSession session = Session(scenario);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(session.Result.Winner, Is.EqualTo(BattleSide.Ally));
            Assert.That(session.Actors.Single(actor => actor.Side == BattleSide.Enemy).Hp, Is.EqualTo(500));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void CenterGaugeRoundsEndTheEncounterAtTheConfiguredLimit(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.RoundLimit = 2;
            BattleSession session = Session(scenario);
            Advance(session, 40, true);
            Assert.That(session.BossRounds, Is.EqualTo(2));
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(session.Result.Winner, Is.EqualTo(BattleSide.Ally));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void APartyRoundLimitDoesNotCountAnEnemyOutsideTheCenter(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.RoundLimit = 1;
            scenario.Participants[1].Slot = BattleSlot.Water;
            BattleSession session = Session(scenario);
            Advance(session, 30, true);
            Assert.That(session.BossRounds, Is.Zero);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running));
            Assert.That(session.Actors.Any(actor => actor.Rounds > 1), Is.True);
        }

        [TestCase("PAL3", 210)]
        [TestCase("PAL3A", 308)]
        public void FireballPaysItsMagicCostBeforeSelfDestruction(string variant, int magicId)
        {
            BattleScenario scenario = Scenario(variant, 7312);
            Learn(scenario.Participants[1], magicId);
            BattleSession session = Session(scenario, null, Magic(magicId, -10));
            Assert.That(session.Submit("A", BattleAction.Defend), Is.True);
            Assert.That(session.Submit(new BattleCommand("E", BattleAction.Skill, "A", magicId)), Is.True);
            Advance(session, 3);
            BattleActorSnapshot enemy = session.Actors.Single(actor => actor.Id == "E");
            Assert.That(enemy.Mp, Is.EqualTo(93));
            Assert.That(enemy.Hp, Is.Zero);
            int cost = session.Events.ToList().FindIndex(entry => entry.Detail == "skill cost " + magicId);
            int suicide = session.Events.ToList().FindIndex(entry => entry.Detail.Contains("fireball self-destruction"));
            Assert.That(cost, Is.GreaterThanOrEqualTo(0));
            Assert.That(suicide, Is.GreaterThan(cost));
        }

        [Test]
        public void ChongLouReusesTheNativeSlotAndLaterQueuedTargetBindsToTheNewForm()
        {
            BattleScenario scenario = Scenario("PAL3", 738);
            scenario.Participants.Insert(1, Participant("B", BattleSide.Ally, BattleSlot.Fire, 104));
            Learn(scenario.Participants[0], 9991);
            Learn(scenario.Participants[1], 9992, 1);
            var catalog = new Catalog();
            BattleSession session = Session(scenario, catalog, Magic(9991, -100000), Magic(9992, -30));
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 9991)), Is.True);
            Assert.That(session.Submit(new BattleCommand("B", BattleAction.Skill, "E", 9992)), Is.True);
            Advance(session, 3, true);
            session.SetAutomatic(BattleSide.Enemy, false);
            Advance(session, 100, true);
            Assert.That(catalog.Created.Single(), Is.EqualTo((10, 739, "E")));
            Assert.That(session.CaptureParticipant("E").ActorId, Is.EqualTo(739));
            Assert.That(session.MonsterGeneration("E"), Is.EqualTo(1));
            Assert.That(session.ActiveScene, Is.EqualTo("z39"));
            Assert.That(session.StealPools.Single(pool => pool.Actor == "E").Money, Is.EqualTo(739));
            Assert.That(session.Events.Any(entry => entry.Source == "B" && entry.Target == "E" && entry.HpBefore == 500 && entry.HpAfter < 500), Is.True);
        }

        [TestCase("PAL3", 7710)]
        [TestCase("PAL3A", 274)]
        public void SummonedEnemiesHaveTheirOwnStealPoolsAndCanTakeManualTurns(string variant, int skillId)
        {
            BattleScenario scenario = Scenario(variant);
            Learn(scenario.Participants[1], skillId);
            var catalog = new Catalog();
            BattleSession session = Session(scenario, catalog, Magic(skillId, 0, 29));
            Assert.That(session.Submit("A", BattleAction.Defend), Is.True);
            Assert.That(session.Submit(new BattleCommand("E", BattleAction.Skill, "A", skillId)), Is.True);
            Advance(session, 2);
            Assert.That(catalog.Created, Has.Count.EqualTo(2));
            Assert.That(session.Actors.Count(actor => actor.Side == BattleSide.Enemy), Is.EqualTo(3));
            foreach (var created in catalog.Created)
            {
                Assert.That(session.CaptureParticipant(created.InstanceId).ActorId, Is.EqualTo(created.ActorId));
                Assert.That(session.MonsterGeneration(created.InstanceId), Is.EqualTo(1));
                Assert.That(session.StealPools.Single(pool => pool.Actor == created.InstanceId).Money, Is.EqualTo(created.ActorId));
                Assert.That(session.GetAiProfile(created.InstanceId), Is.EqualTo(63));
            }

            session.SetAutomatic(BattleSide.Enemy, false);
            string summoned = catalog.Created[0].InstanceId;
            for (int tick = 0; tick < 50 && session.WaitingActor != summoned; tick++)
            {
                if (session.WaitingActor is { } waiting)
                    Assert.That(session.Submit(waiting, BattleAction.Defend), Is.True);
                else
                    Advance(session, 1);
            }
            Assert.That(session.WaitingActor, Is.EqualTo(summoned));
            Assert.That(session.Submit(summoned, BattleAction.Attack, "A"), Is.True);
            Assert.That(session.Inputs.Last().Actor, Is.EqualTo(summoned));
            Assert.That(session.Inputs.Last().Action, Is.EqualTo(BattleAction.Attack));
        }

        [TestCase("PAL3", true)]
        [TestCase("PAL3A", false)]
        public void DefeatReportsTheIndependentGameOverPolicy(string variant, bool gameOver)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.GameOverOnDefeat = gameOver;
            scenario.Participants[0].Vitals[0] = 0;
            BattleSession session = Session(scenario);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(session.Result.Winner, Is.EqualTo(BattleSide.Enemy));
            Assert.That(session.Result.GameOver, Is.EqualTo(gameOver));
        }
    }
}
