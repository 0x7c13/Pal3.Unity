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

    public sealed class BattleAiSessionTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings
        {
            public readonly Dictionary<int, AttackTiming> Timings = new();

            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public AttackTiming Ability(string source, BattleAbilityDefinition ability) => Timings.TryGetValue(ability.Id, out AttackTiming timing) ? timing : new(0, 5, new[] { 2 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => 0;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class Effects : IBattleEffectDirector
        {
            private long _next;
            public readonly List<BattleEffectRequest> Requests = new();

            public long Start(BattleEffectRequest request)
            {
                Requests.Add(request);
                return ++_next;
            }

            public bool IsRunning(long handle) => false;

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
            }

            public void Stop(long handle)
            {
            }
        }

        private sealed class Rules : IBattleRules
        {
            private readonly bool _allyReady, _enemyReady;
            private readonly int _speed;
            private readonly BattleBasicRules _original = new();

            public Rules(bool allyReady, bool enemyReady, int speed = 1)
            {
                _allyReady = allyReady;
                _enemyReady = enemyReady;
                _speed = speed;
            }

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) =>
                (actor.Side == BattleSide.Ally ? _allyReady : _enemyReady) ? BattleSession.FullGauge : 0;

            public int GaugePerStep(BattleParticipant actor) => actor.Side == BattleSide.Enemy && !_enemyReady ? 0 : _speed;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) => _original.Hit(source, target, guarding, random, targetAtHome, damageDivisor);
        }

        private static BattleParticipant Actor(string id, BattleSide side, BattleSlot slot, int hp = 1000, int maximum = 1000) => new()
        {
            InstanceId = id,
            ActorId = side == BattleSide.Ally ? 101 : 701,
            Side = side,
            Slot = slot,
            Attributes = new[] { maximum, 100, 100, 60, 10, 3, 0, 0, 0, 0, 0, 0 },
            Vitals = new[] { hp, 100, 100 }
        };

        private static BattleScenario Scenario(string variant = "PAL3", int seed = 1)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "portable", DataFingerprint = "ai-port", Seed = seed };
            scenario.Participants.Add(Actor("A", BattleSide.Ally, BattleSlot.Water));
            scenario.Participants.Add(Actor("E", BattleSide.Enemy, BattleSlot.Water));
            return scenario;
        }

        private static BattleAbilityDefinition Ability(int id, int power, bool magic, BattleTargetRange range, int cost = 0,
            bool revive = false, bool item = false, int element = 1) => new(new BattleAbilityData
            {
                Id = id,
                Name = "native AI port fixture",
                IsMagic = magic,
                IsItem = item,
                ItemType = item ? power < 0 ? 2 : 1 : 0,
                SkillTypeId = magic ? power < 0 ? 2 : 3 : 0,
                Range = range,
                ElementMask = element,
                Attributes = power == 0 ? Array.Empty<BattleAttributeEffect>() : new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, power) },
                States = revive ? new[] { new BattleStatusEffect(BattleState.Death, true) } : Array.Empty<BattleStatusEffect>(),
                Costs = cost == 0 ? Array.Empty<BattleCost>() : new[] { new BattleCost(magic ? 2 : 1, BattleValueMode.Absolute, cost) }
            });

        private static (BattleSession Session, BattleSetup Setup, BattleDefinitions Definitions, Catalog Catalog, Rules Rules) Create(
            BattleScenario scenario, IEnumerable<BattleAbilityDefinition> abilities, bool allyReady, bool enemyReady, int speed = 1, IBattleEffectDirector effects = null)
        {
            BattleAbilityDefinition[] all = abilities.ToArray();
            var definitions = new BattleDefinitions(scenario.Variant, scenario.DataFingerprint, all.Where(ability => !ability.IsItem), all.Where(ability => ability.IsItem),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Ally ? actor.InstanceId == "A" ? 0 : 1 : -1
                })));
            var setup = new BattleSetup(scenario);
            var catalog = new Catalog();
            var rules = new Rules(allyReady, enemyReady, speed);
            return (new BattleSession(setup, catalog, rules, definitions, effects), setup, definitions, catalog, rules);
        }

        private static BattleActorSnapshot Snapshot(BattleSession session, string id) => session.Actors.Single(actor => actor.Id == id);

        private static void AdvanceUntil(BattleSession session, Func<bool> condition, int maximum = 1000)
        {
            for (int step = 0; step < maximum && !condition() && session.Status == BattleSessionStatus.Running; step++)
                session.Step();
            Assert.That(session.Status, Is.Not.EqualTo(BattleSessionStatus.Faulted), session.Result?.Reason);
            Assert.That(condition(), Is.True);
        }

        private static string Trace(BattleSession session) => string.Join("\n", session.Events.Select(entry =>
            $"{entry.Tick}|{entry.Kind}|{entry.Source}|{entry.Target}|{entry.ActionId}|{entry.AbilityId}|{entry.RequestedDelta}|{entry.ActualDelta}|{entry.RandomState}|{entry.Detail}"));

        private static void Replay((BattleSession Session, BattleSetup Setup, BattleDefinitions Definitions, Catalog Catalog, Rules Rules) fixture, IBattleEffectDirector effects = null)
        {
            BattleSession replay = BattleReplay.Play(fixture.Setup, fixture.Catalog, fixture.Session.Inputs, fixture.Session.Tick, fixture.Rules, fixture.Definitions, effects);
            Assert.That(Trace(replay), Is.EqualTo(Trace(fixture.Session)));
            Assert.That(replay.Inventory.Select(item => item.Id + ":" + item.Count), Is.EqualTo(fixture.Session.Inventory.Select(item => item.Id + ":" + item.Count)));
            Assert.That(replay.FiveSpiritElement, Is.EqualTo(fixture.Session.FiveSpiritElement));
            Assert.That(replay.FiveSpiritForce, Is.EqualTo(fixture.Session.FiveSpiritForce));
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        [TestCase("PAL3", true)]
        [TestCase("PAL3A", true)]
        public void MagicSelectorsPreserveExplicitNativeSlotsWithoutDroppingOtherMagic(string variant, bool miscellaneous)
        {
            BattleScenario scenario = Scenario(variant);
            foreach (int id in new[] { 264, 312, 316, 9001 })
                scenario.Participants[0].Skills.Add(new BattleSkill
                {
                    Id = id,
                    Level = 4,
                    Mastered = true,
                    NativeMagicGroup = id is 312 or 316 ? 0 : -1,
                    NativeMagicSlot = id == 316 ? 0 : id == 312 ? 1 : -1
                });
            BattleAbilityDefinition Magic(int id) => new(new BattleAbilityData
            {
                Id = id,
                IsMagic = true,
                SkillTypeId = id == 264 ? 0 : miscellaneous ? 1 : 2,
                ApplicableIdentities = id is 312 or 316 ? new[] { 0 } : Array.Empty<int>(),
                ElementMask = 1,
                Range = id is 264 or 9001 ? BattleTargetRange.OpponentAll :
                    miscellaneous ? BattleTargetRange.OwnSingle : BattleTargetRange.OpponentSingle,
                Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -50) }
            });
            var fixture = Create(scenario, new[] { 264, 312, 316, 9001 }.Select(Magic), true, false);
            var ai = (BattleAi)typeof(BattleSession).GetField("_ai", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fixture.Session);
            Assert.That(ai.Roles[0].Skills.Select(skill => skill.Id), Is.EqualTo(new[] { 264, 316, 312, 9001 }));
            if (miscellaneous)
            {
                var expectedRandom = new BattleRandom(unchecked((int)fixture.Session.Events.Last().RandomState), variant);
                int expected = new[] { 316, 312 }[expectedRandom.Next() % 2];
                Assert.That(ai.SelectMiscMagic(0, false), Is.EqualTo(expected));
            }
            else
                Assert.That(ai.SelectMagic(0, true, false, 50), Is.EqualTo(316), "Equal power retains the first captured native slot.");
        }

        // Released GetFightSkillRange: PAL3 4f0830, PAL3A 4e1efe.
        // A tests signed HP, independently of the later FixState alive flag.
        [TestCase("PAL3", 0, false, true)]
        [TestCase("PAL3", 0, true, true)]
        [TestCase("PAL3", 1, false, true)]
        [TestCase("PAL3A", 0, false, false)]
        [TestCase("PAL3A", 0, true, false)]
        [TestCase("PAL3A", 1, false, true)]
        public void RepeatGroupRangeRetainsTheReleasedCorpseAndHpBoundaries(string variant, int hp, bool alive, bool included)
        {
            var fixture = Create(Scenario(variant), new[] { Ability(9001, -10, true, BattleTargetRange.OpponentAll) }, true, false);
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var actors = (IDictionary)typeof(BattleSession).GetField("_byId", fields).GetValue(fixture.Session);
            object enemy = actors["E"];
            enemy.GetType().GetField("Hp").SetValue(enemy, hp);
            enemy.GetType().GetField("Alive").SetValue(enemy, alive);
            var port = (BattleAiPort)typeof(BattleSession).GetField("_aiPort", fields).GetValue(fixture.Session);
            var command = new BattleAiCommand { Kind = 0x105, AbilityId = 9001, Source = 0, Target = 5 };
            port.ExpandRepeatRange(command);
            Assert.That(command.MultipleTargets, Is.True);
            Assert.That(command.TargetMask[5], Is.EqualTo(included));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void EnemyUsesItsFourthRecoverySlotAndSharesTheRecordedRandomStream(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[1].Vitals[0] = 400;
            for (int slot = 0; slot < 4; slot++)
                scenario.Participants[1].Skills.Add(new BattleSkill { Id = 9001 + slot, Level = slot == 3 ? 4 : slot + 1, Mastered = true });
            BattleAbilityDefinition[] spells = new[] { 10, 40, 100, 400 }.Select((power, slot) => Ability(9001 + slot, power, true, BattleTargetRange.OwnSingle, cost: 5)).ToArray();
            var fixture = Create(scenario, spells, false, true);
            fixture.Session.Step();
            BattleEvent command = fixture.Session.Events.First(entry => entry.Kind == BattleEventKind.Command);
            Assert.That(command.Source, Is.EqualTo("E"));
            Assert.That(command.Target, Is.EqualTo("E"));
            Assert.That(fixture.Session.Inputs.First(input => input.Kind == BattleInputKind.Command).AbilityId, Is.EqualTo(9004));
            Assert.That(command.RandomState, Is.EqualTo(1030492215u));
            AdvanceUntil(fixture.Session, () => Snapshot(fixture.Session, "E").Hp > 400);
            Assert.That(Snapshot(fixture.Session, "E").Mp, Is.EqualTo(95));
            Replay(fixture);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void NegativeProfileUsesAnEnabledRecoveryTechniqueBeforeAttacking(string variant)
        {
            BattleScenario scenario = Scenario(variant, 107);
            scenario.Participants[0].Vitals[0] = 100;
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = 9001, Level = 4, Mastered = true });
            var fixture = Create(scenario, new[] { Ability(9001, 20, false, BattleTargetRange.OwnSingle, cost: 10) }, true, false);
            fixture.Session.SetAutomatic(BattleSide.Enemy, false);
            fixture.Session.SetAiProfile("A", 1 | 4 | 16 | 32);
            fixture.Session.Step();
            BattleEvent command = fixture.Session.Events.First(entry => entry.Kind == BattleEventKind.Command);
            Assert.That(fixture.Session.Inputs.First(input => input.Kind == BattleInputKind.Command).AbilityId, Is.EqualTo(9001));
            Assert.That(command.Target, Is.EqualTo("A"));
            AdvanceUntil(fixture.Session, () => Snapshot(fixture.Session, "A").Hp > 100);
            Assert.That(Snapshot(fixture.Session, "A").Sp, Is.EqualTo(90));
            Replay(fixture);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void RepeatKeepsTheManualGroupMaskAfterAnUnselectedEnemyRevives(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants.Add(Actor("E2", BattleSide.Enemy, BattleSlot.Fire, 0));
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = 9001, Level = 4, Mastered = true });
            scenario.Participants[1].Skills.Add(new BattleSkill { Id = 9002, Level = 4, Mastered = true });
            var fixture = Create(scenario, new[]
            {
                Ability(9001, -30, true, BattleTargetRange.OpponentAll),
                Ability(9002, 50, true, BattleTargetRange.OwnSingle, revive: true)
            }, true, true, BattleSession.FullGauge);
            fixture.Catalog.Timings[9002] = new AttackTiming(0, 5, new[] { 1 });
            BattleSession session = fixture.Session;
            session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 9001)), Is.True);
            Assert.That(session.Submit(new BattleCommand("E", BattleAction.Skill, "E2", 9002)), Is.True);
            for (int step = 0; step < 100 && session.WaitingActor != "A"; step++)
            {
                if (session.WaitingActor is { } waiting)
                    Assert.That(session.Submit(waiting, BattleAction.Defend), Is.True);
                session.Step();
            }
            Assert.That(session.WaitingActor, Is.EqualTo("A"));
            Assert.That(Snapshot(session, "E2").Hp, Is.Positive);
            int revivedHp = Snapshot(session, "E2").Hp;
            int originalEnemyHp = Snapshot(session, "E").Hp;
            Assert.That(session.RequestAiRepeat("A"), Is.True);
            AdvanceUntil(session, () => Snapshot(session, "E").Hp < originalEnemyHp);
            Assert.That(Snapshot(session, "E2").Hp, Is.EqualTo(revivedHp));
            Replay(fixture);
        }

        [TestCase("PAL3", "E2")]
        [TestCase("PAL3A", "E")]
        public void FUsesTheReleasedRatioPrecisionAndReplaysItsInput(string variant, string target)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[1].Attributes[0] = 100;
            scenario.Participants[1].Vitals[0] = 20;
            scenario.Participants.Add(Actor("E2", BattleSide.Enemy, BattleSlot.Fire, 20, 100));
            var fixture = Create(scenario, Array.Empty<BattleAbilityDefinition>(), true, false);
            fixture.Session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(fixture.Session.RequestAiNormal("A"), Is.True);
            Assert.That(fixture.Session.Events.Last(entry => entry.Kind == BattleEventKind.Command).Target, Is.EqualTo(target));
            Assert.That(fixture.Session.Inputs.Any(input => input.Kind == BattleInputKind.AiNormal), Is.True);
            Assert.That((fixture.Session.GetAiProfile("A") & 1), Is.EqualTo(0));
            Replay(fixture);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void RRepeatsTheRememberedDefenseWithoutRunningOrdinaryAi(string variant)
        {
            var fixture = Create(Scenario(variant), Array.Empty<BattleAbilityDefinition>(), true, false, BattleSession.FullGauge);
            fixture.Session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(fixture.Session.Submit("A", BattleAction.Defend), Is.True);
            AdvanceUntil(fixture.Session, () => fixture.Session.WaitingActor == "A");
            Assert.That(fixture.Session.RequestAiRepeat("A"), Is.True);
            Assert.That(fixture.Session.Events.Last(entry => entry.Kind == BattleEventKind.Command).Detail, Does.StartWith("Defend"));
            Replay(fixture);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void NegativeProfileRevivesFromInventoryBeforeSpendingAnAttack(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants.Add(Actor("B", BattleSide.Ally, BattleSlot.Fire, 0));
            scenario.Items.Add(new BattleItem { Id = 6201, Count = 1 });
            var fixture = Create(scenario, new[] { Ability(6201, 100, false, BattleTargetRange.OwnSingle, revive: true, item: true) }, true, false);
            fixture.Session.SetAutomatic(BattleSide.Enemy, false);
            fixture.Session.SetAiProfile("A", 1 | 8 | 16 | 32);
            fixture.Session.Step();
            BattleEvent command = fixture.Session.Events.First(entry => entry.Kind == BattleEventKind.Command);
            Assert.That(command.Target, Is.EqualTo("B"));
            Assert.That(fixture.Session.Inputs.First(input => input.Kind == BattleInputKind.Command).AbilityId, Is.EqualTo(6201));
            AdvanceUntil(fixture.Session, () => Snapshot(fixture.Session, "B").Hp > 0);
            Assert.That(fixture.Session.Inventory, Is.Empty);
            Replay(fixture);
        }

        [TestCase("PAL3", 7711)]
        [TestCase("PAL3A", 7714)]
        public void SingleAttackConversionCanSubmitAnOriginalSpecialIdOutsideTheFourSlots(string variant, int original)
        {
            BattleScenario scenario = Scenario(variant, 107);
            scenario.Participants[1].Skills.Add(new BattleSkill { Id = original, Level = 4, Mastered = true });
            BattleAbilityDefinition[] spells = new[] { Ability(original, -20, true, BattleTargetRange.OpponentSingle, cost: 5) }
                .Concat(new[] { 207, 215, 232, 224, 240 }.Select(id => Ability(id, -20, true, BattleTargetRange.OpponentSingle))).ToArray();
            var fixture = Create(scenario, spells, false, true, 100);
            fixture.Session.SetAiProfile("E", 2 | 32);
            AdvanceUntil(fixture.Session, () => fixture.Session.Events.Any(entry => entry.Kind == BattleEventKind.Command), 100);
            int chosen = fixture.Session.Inputs.First(input => input.Kind == BattleInputKind.Command).AbilityId;
            Assert.That(new[] { 207, 215, 232, 224, 240 }, Does.Contain(chosen));
            Assert.That(scenario.Participants[1].Skills.Any(skill => skill.Id == chosen), Is.False);
            AdvanceUntil(fixture.Session, () => fixture.Session.Events.Any(entry => entry.Kind is BattleEventKind.Hit or BattleEventKind.Miss), 1000);
            Replay(fixture);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ChaosRunsWithoutTheEnabledFlagAndRetainsTheOriginalSamplingFailure(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[0].Statuses.Add(new BattleStatus { Id = (int)BattleState.Chaos, Rounds = 10 });
            var fixture = Create(scenario, Array.Empty<BattleAbilityDefinition>(), true, false);
            fixture.Session.SetAutomatic(BattleSide.Enemy, false);
            fixture.Session.Step();
            Assert.That(fixture.Session.Events.Any(entry => entry.Kind == BattleEventKind.Command), Is.False);
            fixture.Session.SetAiProfile("A", 1);
            Assert.That(fixture.Session.Events.Last().RandomState, Is.EqualTo(1957819393u));
            Replay(fixture);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void MadnessCanSubmitARealFriendlyPhysicalAttackWithAttackDisabled(string variant)
        {
            BattleScenario scenario = Scenario(variant, 2);
            scenario.Participants[0].Statuses.Add(new BattleStatus { Id = (int)BattleState.Madness, Rounds = 10 });
            scenario.Participants.Add(Actor("B", BattleSide.Ally, BattleSlot.Fire));
            var fixture = Create(scenario, Array.Empty<BattleAbilityDefinition>(), true, false);
            fixture.Session.SetAutomatic(BattleSide.Enemy, false);
            fixture.Session.Step();
            BattleInput command = fixture.Session.Inputs.First(input => input.Kind == BattleInputKind.Command);
            Assert.That(command.Action, Is.EqualTo(BattleAction.Attack));
            Assert.That(command.Actor, Is.EqualTo("A"));
            Assert.That(command.Target, Is.EqualTo("B"));
            Replay(fixture);
        }

        [Test]
        public void FiveSpiritDedicatedMagicWaitAcceptsItsOriginalNoneRangeAndSetsTheFieldAtWork()
        {
            BattleScenario scenario = Scenario("PAL3A", 107);
            scenario.Participants[1].Skills.Add(new BattleSkill { Id = 264, Level = 4, Mastered = true });
            var field = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = 264,
                IsMagic = true,
                SkillTypeId = 0,
                Range = BattleTargetRange.None,
                ElementMask = 1,
                Costs = new[] { new BattleCost(2, BattleValueMode.Absolute, 5) }
            });
            var fixture = Create(scenario, new[] { field }, false, true);
            fixture.Session.Step();
            Assert.That(fixture.Session.Inputs.Last(input => input.Kind == BattleInputKind.Command).AbilityId, Is.EqualTo(264));
            Assert.That(fixture.Session.FiveSpiritElement, Is.EqualTo(0));
            AdvanceUntil(fixture.Session, () => fixture.Session.FiveSpiritElement == 1);
            Assert.That(fixture.Session.FiveSpiritForce, Is.EqualTo(2));
            Assert.That(Snapshot(fixture.Session, "E").Mp, Is.EqualTo(95));
            Replay(fixture);
        }

        [Test]
        public void FullMpBossUsesItsDedicatedMagicAndStartsEffect559()
        {
            BattleScenario scenario = Scenario("PAL3", 107);
            scenario.Participants.Add(Actor("B", BattleSide.Ally, BattleSlot.Fire, 0));
            scenario.Participants[1].ActorId = 739;
            scenario.Participants[1].Skills.Add(new BattleSkill { Id = 7709, Level = 4, Mastered = true });
            var effects = new Effects();
            var fixture = Create(scenario, new[] { Ability(7709, -360, true, BattleTargetRange.OpponentAll, cost: 80) }, false, true, effects: effects);
            fixture.Session.SetAiProfile("E", 32);
            fixture.Session.Step();
            Assert.That(fixture.Session.Inputs.Last(input => input.Kind == BattleInputKind.Command).AbilityId, Is.EqualTo(7709));
            Assert.That(effects.Requests.Single(request => request.GroupId == 559).Targets, Is.EqualTo(new[] { "A", "B" }));
            AdvanceUntil(fixture.Session, () => Snapshot(fixture.Session, "E").Mp == 20);
            Replay(fixture, new Effects());
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3A", true)]
        public void FRetainsTheNativeInvisibleTargetDifferenceIncludingRejectedInputReplay(string variant, bool accepted)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[1].Statuses.Add(new BattleStatus { Id = (int)BattleState.Invisible, Rounds = 5 });
            var fixture = Create(scenario, Array.Empty<BattleAbilityDefinition>(), true, false);
            fixture.Session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(fixture.Session.RequestAiNormal("A"), Is.EqualTo(accepted));
            Assert.That(fixture.Session.Inputs.Count(input => input.Kind == BattleInputKind.Command), Is.EqualTo(accepted ? 1 : 0));
            Assert.That(fixture.Session.Inputs.Last(input => input.Kind == BattleInputKind.AiNormal).Actor, Is.EqualTo("A"));
            Replay(fixture);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AReadyManualPlayerBlocksLaterEnemyDispatchEvenWhenSystemAutoOnlyIsSet(string variant)
        {
            var fixture = Create(Scenario(variant, 107), Array.Empty<BattleAbilityDefinition>(), true, true);
            fixture.Session.Step();
            Assert.That(fixture.Session.Inputs.Any(input => input.Kind == BattleInputKind.Command), Is.False);
            fixture.Session.SetAiAutoOnly(true);
            fixture.Session.Step();
            Assert.That(fixture.Session.Inputs.Any(input => input.Kind == BattleInputKind.Command), Is.False);
            Assert.That(fixture.Session.Submit("A", BattleAction.Defend), Is.True);
            fixture.Session.Step();
            Assert.That(fixture.Session.Inputs.Any(input => input.Kind == BattleInputKind.Command && input.Actor == "E"), Is.True);
            Replay(fixture);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void WorstStatePreservesTheReleasedRoleZeroLifeGateAndNullCureField(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[0].Vitals[0] = 0;
            scenario.Participants.Add(Actor("B", BattleSide.Ally, BattleSlot.Fire));
            scenario.Participants.Add(Actor("C", BattleSide.Ally, BattleSlot.Wind));
            scenario.Participants[2].Skills.Add(new BattleSkill { Id = 9001, Level = 4, Mastered = true });
            scenario.Participants[3].Statuses.Add(new BattleStatus { Id = (int)BattleState.Paralysis, Rounds = 10 });
            var spell = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = 9001,
                IsMagic = true,
                SkillTypeId = 1,
                ElementMask = 1,
                Range = BattleTargetRange.OwnSingle,
                SuccessRate = 2,
                Costs = new[] { new BattleCost(2, BattleValueMode.Absolute, 5) }
            });
            var fixture = Create(scenario, new[] { spell }, true, false);
            fixture.Session.SetAutomatic(BattleSide.Enemy, false);
            fixture.Session.SetAiProfile("B", 2 | 16 | 32);
            fixture.Session.Step();
            BattleInput command = fixture.Session.Inputs.First(input => input.Kind == BattleInputKind.Command);
            Assert.That(command.Actor, Is.EqualTo("B"));
            Assert.That(command.Target, Is.EqualTo("C"));
            Assert.That(command.AbilityId, Is.EqualTo(9001));
            AdvanceUntil(fixture.Session, () => Snapshot(fixture.Session, "B").Mp == 95);
            Assert.That(Snapshot(fixture.Session, "C").States.Any(state => state.State == BattleState.Paralysis), Is.True);
            Replay(fixture);
        }

        [Test]
        public void ASealedFiveSpiritAttemptQueuesThenCancelsAtItsFirstMagicWaitPass()
        {
            BattleScenario scenario = Scenario("PAL3A", 107);
            scenario.Participants[1].Skills.Add(new BattleSkill { Id = 264, Level = 4, Mastered = true });
            scenario.Participants[1].Statuses.Add(new BattleStatus { Id = (int)BattleState.Seal, Rounds = 10 });
            var field = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = 264,
                IsMagic = true,
                SkillTypeId = 0,
                Range = BattleTargetRange.None,
                ElementMask = 1,
                Costs = new[] { new BattleCost(2, BattleValueMode.Absolute, 5) }
            });
            var fixture = Create(scenario, new[] { field }, false, true);
            fixture.Session.SetAiProfile("E", 32);
            fixture.Session.Step();
            Assert.That(fixture.Session.Inputs.Last(input => input.Kind == BattleInputKind.Command).AbilityId, Is.EqualTo(264));
            fixture.Session.Step();
            Assert.That(fixture.Session.Events.Any(entry => entry.Kind == BattleEventKind.Cancelled && entry.Source == "E"), Is.True);
            Assert.That(fixture.Session.FiveSpiritElement, Is.EqualTo(0));
            Assert.That(Snapshot(fixture.Session, "E").Mp, Is.EqualTo(100));
            Assert.That(Snapshot(fixture.Session, "E").Ready, Is.False);
            Replay(fixture);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void SealAppliedAfterMagicActStartsDoesNotCancelTheAct(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = 9001, Level = 4, Mastered = true });
            scenario.Participants[1].Skills.Add(new BattleSkill { Id = 9002, Level = 4, Mastered = true });
            var seal = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = 9002,
                SkillTypeId = 0,
                Range = BattleTargetRange.OpponentSingle,
                States = new[] { new BattleStatusEffect(BattleState.Seal, false) }
            });
            var fixture = Create(scenario, new[] { Ability(9001, -20, true, BattleTargetRange.OpponentSingle), seal }, true, true);
            fixture.Catalog.Timings[9001] = new AttackTiming(0, 30, new[] { 20 });
            fixture.Session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(fixture.Session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 9001)), Is.True);
            fixture.Session.Step();
            Assert.That(fixture.Session.Submit(new BattleCommand("E", BattleAction.Skill, "A", 9002)), Is.True);
            AdvanceUntil(fixture.Session, () => Snapshot(fixture.Session, "A").States.Any(state => state.State == BattleState.Seal));
            Assert.That(fixture.Session.Events.Any(entry => entry.Kind == BattleEventKind.Cancelled && entry.Source == "A"), Is.False);
            AdvanceUntil(fixture.Session, () => fixture.Session.Events.Any(entry => entry.Kind == BattleEventKind.Hit && entry.Source == "A"));
            Assert.That(fixture.Session.Events.Any(entry => entry.Kind == BattleEventKind.Cancelled && entry.Source == "A"), Is.False);
            Replay(fixture);
        }

        [Test]
        public void AFieldForbiddenMagicIsExcludedBeforeRankingTheLegalSpells()
        {
            BattleScenario scenario = Scenario("PAL3A");
            scenario.FiveSpiritElement = 1;
            scenario.FiveSpiritForce = 2;
            scenario.Participants[0].Vitals[0] = 100;
            foreach (int id in new[] { 9001, 9010, 9011 })
                scenario.Participants[0].Skills.Add(new BattleSkill { Id = id, Level = 4, Mastered = true });
            var fixture = Create(scenario, new[]
            {
                Ability(9001, 20, false, BattleTargetRange.OwnSingle, cost: 10),
                Ability(9010, 400, true, BattleTargetRange.OwnSingle, cost: 5),
                Ability(9011, 10, true, BattleTargetRange.OwnSingle, cost: 5, element: 2)
            }, true, false);
            fixture.Session.SetAutomatic(BattleSide.Enemy, false);
            fixture.Session.SetAiProfile("A", 63);
            fixture.Session.Step();
            Assert.That(fixture.Session.Inputs.Last(input => input.Kind == BattleInputKind.Command).AbilityId, Is.EqualTo(9011));
            AdvanceUntil(fixture.Session, () => Snapshot(fixture.Session, "A").Hp > 100);
            Assert.That(Snapshot(fixture.Session, "A").Mp, Is.EqualTo(95));
            Assert.That(Snapshot(fixture.Session, "A").Sp, Is.EqualTo(100));
            Replay(fixture);
        }

        [Test]
        public void NativeFieldDenialDuringRankingInvalidatesDifferentSavedCommand()
        {
            BattleScenario scenario = Scenario("PAL3A");
            scenario.FiveSpiritElement = 1;
            scenario.FiveSpiritForce = 2;
            scenario.Participants[0].InstanceId = "B";
            scenario.Participants[0].ActorId = 102;
            scenario.Participants[0].Vitals[0] = 100;
            foreach (int id in new[] { 318, 302 })
                scenario.Participants[0].Skills.Add(new BattleSkill { Id = id, Level = 4, Mastered = true });
            var fixture = Create(scenario, new[]
            {
                Ability(318, 100, true, BattleTargetRange.OwnSingle, cost: 5),
                Ability(302, 40, true, BattleTargetRange.OwnSingle, cost: 5, element: 4)
            }, true, false, BattleSession.FullGauge);
            var session = fixture.Session;
            session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(session.Submit("B", BattleAction.Defend), Is.True);
            session.SetAiProfile("B", 63);
            AdvanceUntil(session, () => session.Inputs.Any(input => input.Kind == BattleInputKind.Command && input.AbilityId == 302));
            session.SetAiProfile("B", 31);
            AdvanceUntil(session, () => session.WaitingActor == "B");
            Assert.That(session.RequestAiRepeat("B"), Is.True);
            Replay(fixture);
            Assert.That(session.Inputs.Last().Action, Is.EqualTo(BattleAction.Attack));
        }

        [Test]
        public void VisitingAForbiddenMagicRowRecordsTheRepeatInvalidation()
        {
            BattleScenario scenario = Scenario("PAL3A");
            scenario.FiveSpiritElement = 1;
            scenario.FiveSpiritForce = 2;
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = 318, Level = 4, Mastered = true });
            var magic = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = 318,
                ApplicableIdentities = new[] { 0 },
                SkillTypeId = 3,
                IsMagic = true,
                Range = BattleTargetRange.OwnSingle,
                ElementMask = 1
            });
            var fixture = Create(scenario, new[] { magic }, true, false, BattleSession.FullGauge);
            var session = fixture.Session;
            session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(session.Submit("A", BattleAction.Defend), Is.True);
            AdvanceUntil(session, () => session.WaitingActor == "A");
            int before = session.RecordedInputCount;
            Assert.That(session.CanSubmit(new BattleCommand("A", BattleAction.Skill, "A", 318)), Is.Not.Null);
            Assert.That(session.RecordedInputCount, Is.EqualTo(before));
            Assert.That(session.InspectMagicMenuAbility("A", 318), Is.True);
            Assert.That(session.Inputs.Last().Kind, Is.EqualTo(BattleInputKind.MagicMenuFieldDenial));
            Assert.That(session.InspectMagicMenuAbility("A", 318), Is.False);
            Assert.That(session.RecordedInputCount, Is.EqualTo(before + 1));
            Assert.That(session.RequestAiRepeat("A"), Is.True);
            Assert.That(session.Inputs.Last().Action, Is.EqualTo(BattleAction.Attack));
            Replay(fixture);
        }

        [Test]
        public void EnemyFieldDeniesManualMagicAndInvalidatesARepeatBeforeFallingBack()
        {
            BattleScenario scenario = Scenario("PAL3A");
            scenario.FiveSpiritElement = 1;
            scenario.FiveSpiritForce = 2;
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = 9001, Level = 4, Mastered = true });
            var fixture = Create(scenario, new[] { Ability(9001, -10, true, BattleTargetRange.OpponentSingle) }, true, false);
            var session = fixture.Session;
            var command = new BattleCommand("A", BattleAction.Skill, "E", 9001);
            Assert.That(session.CanSubmit(command), Does.Contain("五灵阵法"));
            Assert.That(session.Submit(command), Is.False);
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var saved = (IDictionary)typeof(BattleSession).GetField("_aiLastCommands", fields).GetValue(session);
            saved[0] = new BattleAiCommand { Source = 0, Kind = 0x105, AbilityId = 9001, Target = 5 };
            Assert.That(session.RequestAiRepeat("A"), Is.True);
            Assert.That(session.Inputs.Last().Action, Is.EqualTo(BattleAction.Attack));
            Assert.That(saved.Contains(0), Is.False);
        }

        [TestCase("PAL3", false, "E2")]
        [TestCase("PAL3A", false, "E3")]
        [TestCase("PAL3", true, "E2")]
        [TestCase("PAL3A", true, "E2")]
        public void RRetargetsAbsoluteHpAndAppliesTheOriginalMagicVersusNormalBackFilter(string variant, bool magic, string expected)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[1].Vitals[0] = 1;
            scenario.Participants.Add(Actor("E2", BattleSide.Enemy, BattleSlot.Fire, 10));
            scenario.Participants.Add(Actor("E3", BattleSide.Enemy, BattleSlot.Center, 500));
            if (magic)
                scenario.Participants[0].Skills.Add(new BattleSkill { Id = 9001, Level = 4, Mastered = true });
            var fixture = Create(scenario, magic ? new[] { Ability(9001, -20, true, BattleTargetRange.OpponentSingle) } : Array.Empty<BattleAbilityDefinition>(),
                true, false, BattleSession.FullGauge);
            fixture.Session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(fixture.Session.Submit(new BattleCommand("A", magic ? BattleAction.Skill : BattleAction.Attack, "E", magic ? 9001 : 0)), Is.True);
            AdvanceUntil(fixture.Session, () => Snapshot(fixture.Session, "E").Hp == 0 && fixture.Session.WaitingActor == "A");
            Assert.That(fixture.Session.RequestAiRepeat("A"), Is.True);
            Assert.That(fixture.Session.Inputs.Last(input => input.Kind == BattleInputKind.Command).Target, Is.EqualTo(expected));
            Replay(fixture);
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        public void Pal3ARepeatRetargetsEnemyDebuffsAndThrowablesAfterTheOriginalTargetDies(int skillType, bool item)
        {
            const int id = 9001;
            BattleScenario scenario = Scenario("PAL3A");
            scenario.Participants[1].Vitals[0] = 1;
            scenario.Participants.Add(Actor("B", BattleSide.Ally, BattleSlot.Fire));
            scenario.Participants.Add(Actor("E2", BattleSide.Enemy, BattleSlot.Fire));
            if (item)
                scenario.Items.Add(new BattleItem { Id = id, Count = 2 });
            else
                scenario.Participants[0].Skills.Add(new BattleSkill { Id = id, Level = 4, Mastered = true });
            var ability = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = id,
                IsItem = item,
                ItemType = item ? 2 : 0,
                IsMagic = skillType != 0,
                SkillTypeId = skillType,
                Range = BattleTargetRange.OpponentSingle,
                SuccessRate = 100,
                States = new[] { new BattleStatusEffect(BattleState.DefenseDecrease, false) }
            });
            var fixture = Create(scenario, new[] { ability }, true, false, BattleSession.FullGauge);
            fixture.Session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(fixture.Session.Submit(new BattleCommand("A", item ? BattleAction.Item : BattleAction.Skill, "E", id)), Is.True);
            Assert.That(fixture.Session.Submit("B", BattleAction.Attack, "E"), Is.True);
            fixture.Session.SetAiProfile("B", 32);
            for (int step = 0; step < 80 && (Snapshot(fixture.Session, "E").Hp > 0 || fixture.Session.WaitingActor != "A"); step++)
                fixture.Session.Step();
            Assert.That((Snapshot(fixture.Session, "E").Hp, fixture.Session.WaitingActor), Is.EqualTo((0, "A")), Trace(fixture.Session));

            Assert.That(fixture.Session.RequestAiRepeat("A"), Is.True);
            Assert.That(fixture.Session.Inputs.Last(input => input.Kind == BattleInputKind.Command).Target, Is.EqualTo("E2"));
            Replay(fixture);
        }

        [Test]
        public void AutoOnlyChangesTheSystemFlagAndKeepsEveryActorCapability()
        {
            var fixture = Create(Scenario(), Array.Empty<BattleAbilityDefinition>(), false, false);
            int before = fixture.Session.GetAiProfile("A");
            fixture.Session.SetAiAutoOnly(true);
            Assert.That(fixture.Session.IsAutomatic(BattleSide.Ally), Is.True);
            Assert.That(fixture.Session.GetAiProfile("A"), Is.EqualTo(before));
            Replay(fixture);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FiveSpiritFieldExpiresAfterFiveSuccessfulActionsOfItsAlivePlayerAnchor(bool endWithAiControl)
        {
            BattleScenario scenario = Scenario("PAL3A");
            scenario.FiveSpiritElement = 1;
            scenario.FiveSpiritForce = 2;
            var fixture = Create(scenario, Array.Empty<BattleAbilityDefinition>(), true, false, BattleSession.FullGauge);
            fixture.Session.SetAutomatic(BattleSide.Enemy, false);
            for (int action = 1; action <= 5; action++)
            {
                if (action == 5 && endWithAiControl)
                {
                    int profile = fixture.Session.GetAiProfile("A");
                    Assert.That(fixture.Session.CommitAiControl("A", profile ^ 1), Is.True, "Editing attack preference keeps the turn.");
                    Assert.That(fixture.Session.FiveSpiritElement, Is.EqualTo(1));
                    Assert.That(fixture.Session.CommitAiControl("A", profile ^ 32), Is.True);
                }
                else
                    Assert.That(fixture.Session.Submit("A", BattleAction.Defend), Is.True);
                Assert.That(fixture.Session.FiveSpiritElement, Is.EqualTo(action < 5 ? 1 : 0));
                if (action < 5)
                    AdvanceUntil(fixture.Session, () => fixture.Session.WaitingActor == "A");
            }
            Assert.That(fixture.Session.FiveSpiritForce, Is.EqualTo(0));
            Replay(fixture);
        }

        [TestCase("A", 0, 1, -201)]
        [TestCase("A", 1, 1, -261)]
        [TestCase("A", 1, 16, -101)]
        [TestCase("E", 0, 1, -103)]
        [TestCase("E", 1, 1, -134)]
        [TestCase("E", 1, 16, -52)]
        public void DynamicFieldChangesRealMagicPowerForEitherForceAndReplays(string source, int field, int mask, int expectedPower)
        {
            BattleScenario scenario = Scenario("PAL3A");
            scenario.SceneElement = 2;
            scenario.FiveSpiritElement = field;
            scenario.FiveSpiritForce = source == "A" ? 1 : 2;
            BattleParticipant caster = scenario.Participants.Single(actor => actor.InstanceId == source);
            caster.Level = 20;
            caster.Slot = BattleSlot.Center;
            caster.Skills.Add(new BattleSkill { Id = 9001, Level = 4, Mastered = true });
            var fixture = Create(scenario, new[] { Ability(9001, -101, true, BattleTargetRange.OpponentSingle, element: mask) }, source == "A", source == "E");
            fixture.Session.SetAutomatic(BattleSide.Enemy, false);
            string target = source == "A" ? "E" : "A";
            Assert.That(fixture.Session.Submit(new BattleCommand(source, BattleAction.Skill, target, 9001)), Is.True);
            AdvanceUntil(fixture.Session, () => Snapshot(fixture.Session, target).Hp < 1000);
            Assert.That(Snapshot(fixture.Session, target).Hp, Is.EqualTo(1000 + expectedPower));
            BattleEvent damage = fixture.Session.Events.Last(entry => entry.Kind == BattleEventKind.Hit && entry.Source == source);
            Assert.That(damage.RandomState, Is.EqualTo(3357800067u));
            Replay(fixture);
        }

        [TestCase("PAL3", 1766988168u)]
        [TestCase("PAL3A", 1924036713u)]
        public void FailedReadyEnemyAttemptsOnlyOneNativeDecisionPerControlFrame(string variant, uint expectedState)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants.Add(Actor("B", BattleSide.Ally, BattleSlot.Fire));
            scenario.Participants.Add(Actor("E2", BattleSide.Enemy, BattleSlot.Fire));
            var fixture = Create(scenario, Array.Empty<BattleAbilityDefinition>(), false, true);
            fixture.Session.SetAiProfile("E", 32);
            fixture.Session.SetAiProfile("E2", 0);
            fixture.Session.Step();
            Assert.That(fixture.Session.Inputs.Any(input => input.Kind == BattleInputKind.Command), Is.False);
            fixture.Session.SetAiProfile("E", 0);
            Assert.That(fixture.Session.Events.Last().RandomState, Is.EqualTo(expectedState));
            Replay(fixture);
        }
    }
}
