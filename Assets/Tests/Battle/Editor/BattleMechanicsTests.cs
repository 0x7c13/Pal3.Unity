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

    // Portable fixtures. Numeric oracles are recorded independently by executing
    // the original EXEs in workspace/Analysis/Battle-Stage3, not by this session.
    public sealed partial class BattleMechanicsTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings
        {
            public readonly Dictionary<int, AttackTiming> Timings = new();
            public int Sp = 0;

            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) =>
                Timings.TryGetValue(ability.Id, out AttackTiming timing) ? timing : new AttackTiming(0, 5, new[] { 2 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => Sp;

            public int Priority(string actor) => 0;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class ReadyRules : IBattleRules
        {
            private readonly bool _bothSides;
            private readonly BattleBasicRules _original = new();

            public ReadyRules(bool bothSides = false) => _bothSides = bothSides;

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) =>
                _bothSides || actor.Side == BattleSide.Ally ? BattleSession.FullGauge : 0;

            public int GaugePerStep(BattleParticipant actor) => _original.GaugePerStep(actor);

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) => _original.Hit(source, target, guarding, random, targetAtHome, damageDivisor);
        }

        private static BattleParticipant Participant(string id, BattleSide side, BattleSlot slot = BattleSlot.Water, int hp = 500)
        {
            return new BattleParticipant
            {
                InstanceId = id,
                ActorId = side == BattleSide.Ally ? 101 : 701,
                Side = side,
                Slot = slot,
                Attributes = new[] { 500, 100, 100, 60, 10, 3, 0, 0, 0, 0, 0, 0 },
                Vitals = new[] { hp, 100, 100 }
            };
        }

        private static BattleScenario Scenario(string variant = "PAL3")
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "portable", DataFingerprint = "fixture", Encounter = BattleEncounter.AllyAmbush };
            scenario.Participants.Add(Participant("A", BattleSide.Ally));
            scenario.Participants.Add(Participant("E", BattleSide.Enemy));
            return scenario;
        }

        private static BattleAbilityData Magic(int id = 301, int power = -20, BattleTargetRange range = BattleTargetRange.OpponentSingle)
        {
            return new BattleAbilityData
            {
                Id = id,
                Name = "fixture magic",
                IsMagic = true,
                SkillTypeId = 2,
                Range = range,
                ApplicableIdentities = new[] { 0 },
                Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, power) },
                Costs = new[] { new BattleCost(2, BattleValueMode.Absolute, 10) }
            };
        }

        private static BattleAbilityData Item(int id = 6201, int power = 30, bool revive = false)
        {
            return new BattleAbilityData
            {
                Id = id,
                Name = "fixture medicine",
                IsItem = true,
                ItemType = 1,
                Range = BattleTargetRange.OwnSingle,
                Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, power) },
                States = revive ? new[] { new BattleStatusEffect(BattleState.Death, true) } : Array.Empty<BattleStatusEffect>()
            };
        }

        private static void Learn(BattleParticipant actor, int id, int level = 4, int uses = 0) =>
            actor.Skills.Add(new BattleSkill { Id = id, Level = level, Uses = uses, Mastered = true });

        private static BattleSession Session(BattleScenario scenario, IEnumerable<BattleAbilityData> abilities,
            IEnumerable<BattleActorData> actors = null, Catalog catalog = null, bool bothSidesReady = false, bool nativeGauge = false)
        {
            BattleAbilityDefinition[] captured = abilities.Select(data => new BattleAbilityDefinition(data)).ToArray();
            var overrides = (actors ?? Array.Empty<BattleActorData>()).ToDictionary(data => data.InstanceId);
            var definitions = new BattleDefinitions(scenario.Variant, scenario.DataFingerprint,
                captured.Where(ability => !ability.IsItem), captured.Where(ability => ability.IsItem),
                scenario.Participants.Select(actor => new BattleActorDefinition(overrides.TryGetValue(actor.InstanceId, out BattleActorData data)
                    ? data : new BattleActorData { InstanceId = actor.InstanceId, Identity = actor.Side == BattleSide.Ally ? 0 : -1 })));
            return new BattleSession(new BattleSetup(scenario), catalog ?? new Catalog(), nativeGauge ? null : new ReadyRules(bothSidesReady), definitions);
        }

        private static BattleActorSnapshot Actor(BattleSession session, string id) => session.Actors.Single(actor => actor.Id == id);

        private static void Steps(BattleSession session, int count)
        {
            for (int index = 0; index < count; index++)
                session.Step();
            Assert.That(session.Status, Is.Not.EqualTo(BattleSessionStatus.Faulted), session.Result?.Reason);
        }

        // Settlement follows the remaining native stage jobs, including numbers.
        private static void Complete(BattleSession session)
        {
            for (int index = 0; index < 2000 && session.Status == BattleSessionStatus.Running; index++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed), session.Result?.Reason);
        }

        private static void UntilReady(BattleSession session, string id)
        {
            for (int index = 0; index < 2000 && !Actor(session, id).Ready && session.Status == BattleSessionStatus.Running; index++)
            {
                if (session.WaitingActor != null && session.WaitingActor != id)
                    Assert.That(session.Submit(session.WaitingActor, BattleAction.Defend), Is.True);
                session.Step();
            }
            Assert.That(Actor(session, id).Ready, Is.True, session.Result?.Reason);
        }

        private static string Trace(BattleSession session) => string.Join("\n", session.Events.Select(entry =>
            $"{entry.Tick}|{entry.Kind}|{entry.Source}|{entry.Target}|{entry.ActionId}|{entry.AbilityId}|{entry.Resource}|{entry.ValueBefore}|{entry.ValueAfter}|{entry.RequestedDelta}|{entry.ActualDelta}|{entry.RandomState}|{entry.Detail}"));

        [TestCase("PAL3", 48)]
        [TestCase("PAL3A", 48)]
        [TestCase("PAL3", 99)]
        [TestCase("PAL3A", 99)]
        public void InitialMagicCastClampsFullWidthSpeedBeforeStoringTheGaugeByte(string variant, int speed)
        {
            BattleScenario scenario = Scenario(variant);
            BattleParticipant caster = scenario.Participants[0];
            caster.Attributes[5] = speed;
            Learn(caster, 301, level: 1);
            BattleSession session = Session(scenario, new[] { Magic() }, nativeGauge: true);

            UntilReady(session, "A");
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301)), Is.True);
            Steps(session, 1);
            Assert.That(Actor(session, "A").CastGauge, Is.EqualTo(255));
        }

        [TestCase("PAL3", 3357800067u)]
        [TestCase("PAL3A", 3357800067u)]
        public void ManualGroupMagicSkipsEnemiesDefeatedBeforeSubmission(string variant, uint expectedState)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Seed = 1;
            scenario.Participants[1].Vitals[0] = 0;
            scenario.Participants.Add(Participant("E1", BattleSide.Enemy, BattleSlot.Fire));
            Learn(scenario.Participants[0], 301);
            BattleSession session = Session(scenario, new[] { Magic(range: BattleTargetRange.OpponentAll) });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 301)), Is.True);
            Steps(session, 2);
            Assert.That(Actor(session, "E").Hp, Is.Zero);
            Assert.That(Actor(session, "E1").Hp, Is.LessThan(500));
            Assert.That(session.Events.Last(entry => entry.Kind == BattleEventKind.ResourceChanged && entry.Resource == 2).RandomState,
                Is.EqualTo(expectedState));
        }

        [Test]
        public void ManualGroupMagicKeepsAnEnemyDefeatedBetweenSubmissionAndImpact()
        {
            BattleScenario scenario = Scenario();
            scenario.Participants.Add(Participant("B", BattleSide.Ally, BattleSlot.Fire));
            scenario.Participants.Add(Participant("E1", BattleSide.Enemy, BattleSlot.Fire, 1));
            Learn(scenario.Participants[0], 301);
            Learn(scenario.Participants[2], 302);
            var catalog = new Catalog();
            catalog.Timings[301] = new AttackTiming(0, 5, new[] { 1 });
            BattleSession session = Session(scenario, new[] { Magic(301), Magic(302, range: BattleTargetRange.OpponentAll) }, catalog: catalog);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E1", 301)), Is.True);
            Assert.That(session.Submit(new BattleCommand("B", BattleAction.Skill, abilityId: 302)), Is.True);
            Steps(session, 2);
            Assert.That(Actor(session, "E1").Alive, Is.False);
            BattleEvent hit = session.Events.Single(entry => entry.Kind == BattleEventKind.Hit && entry.Source == "B" && entry.Target == "E");
            BattleEvent cost = session.Events.Single(entry => entry.Kind == BattleEventKind.ResourceChanged && entry.Source == "B" && entry.Resource == 2);
            // MagicPower consumes two random draws for the captured, now defeated, higher slot.
            var expected = new BattleRandom(unchecked((int)hit.RandomState));
            expected.Next();
            expected.Next();
            Assert.That(cost.RandomState, Is.EqualTo(expected.State));
        }

        [TestCase("PAL3", 1)]
        [TestCase("PAL3", 2)]
        [TestCase("PAL3A", 1)]
        [TestCase("PAL3A", 2)]
        public void ClampedResourceReturnControlsCastingInterruption(string variant, int resource)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[1].Vitals[resource] = 0;
            Learn(scenario.Participants[0], 4100);
            Learn(scenario.Participants[1], 301, 1);
            var drain = new BattleAbilityData
            {
                Id = 4100,
                Range = BattleTargetRange.OpponentSingle,
                Attributes = new[] { new BattleAttributeEffect(resource, BattleValueMode.Absolute, -10) }
            };
            BattleAbilityData waiting = Magic();
            waiting.Costs = Array.Empty<BattleCost>();
            BattleSession session = Session(scenario, new[] { drain, waiting }, bothSidesReady: true);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4100)), Is.True);
            Assert.That(session.Submit(new BattleCommand("E", BattleAction.Skill, "A", 301)), Is.True);
            Steps(session, 2);
            Assert.That(Actor(session, "E").CurrentAction, variant == "PAL3A" && resource == 2
                ? Is.EqualTo(BattleAction.Skill) : Is.Null);
            BattleEvent change = session.Events.Single(entry => entry.Kind == BattleEventKind.ResourceChanged && entry.Target == "E" && entry.Resource == resource);
            Assert.That((change.RequestedDelta, change.ActualDelta), Is.EqualTo((-10, 0)));
        }

        [TestCase(BattleAttributeSource.BaseWithEquipment, 150)]
        [TestCase(BattleAttributeSource.LevelGrowth, 500)]
        public void InitialVitalsAreValidatedAgainstDerivedMaxima(BattleAttributeSource source, int maximum)
        {
            BattleScenario scenario = Scenario();
            BattleParticipant initial = scenario.Participants[0];
            initial.AttributeSource = source;
            initial.Attributes[0] = 100;
            initial.Vitals[0] = maximum;
            var definition = new BattleActorData
            {
                InstanceId = "A",
                AttributesByLevel = new[] { Array.Empty<int>(), new[] { 450, 100, 100, 60, 10, 3, 0, 0, 0, 0, 0, 0 } },
                Equipment = new[] { new BattleEquipmentDefinition(new BattleEquipmentData
                {
                    Id = 80001, Slot = 1, Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, 50) }
                }) }
            };
            Assert.That(scenario.Validate(), Is.Empty);
            BattleSession session = Session(scenario, Array.Empty<BattleAbilityData>(), new[] { definition });
            Assert.That((Actor(session, "A").Hp, Actor(session, "A").MaxHp), Is.EqualTo((maximum, maximum)));
            initial.Vitals[0]++;
            Assert.Throws<ArgumentException>(() => Session(scenario, Array.Empty<BattleAbilityData>(), new[] { definition }));
        }

        [TestCase(BattleAction.Attack)]
        [TestCase(BattleAction.Defend)]
        [TestCase(BattleAction.Flee)]
        public void BasicActionsCannotSmuggleAnUnlearnedOrUnavailableAbility(BattleAction action)
        {
            BattleScenario scenario = Scenario();
            BattleAbilityData unavailable = Magic();
            unavailable.UnsupportedReason = "dedicated handler";
            BattleSession session = Session(scenario, new[] { unavailable });
            var command = new BattleCommand("A", action, "E", 301);
            string before = Trace(session);
            Assert.That(session.CanSubmit(command), Is.Not.Null);
            Assert.That(session.Submit(command), Is.False);
            Assert.That(session.Inputs, Is.Empty);
            Assert.That(Trace(session), Is.EqualTo(before));
            Assert.That(session.GetCostEstimate(command), Is.Empty);
        }

        [TestCase("PAL3", 0, -130)]
        [TestCase("PAL3", 25, -162)]
        [TestCase("PAL3A", 0, -150)]
        [TestCase("PAL3A", 25, -188)]
        public void CriticalDamageMatchesReleasedInstructions(string variant, int bonus, int expected)
        {
            Assert.That(new BattleEffectRules(variant).CriticalPower(-100, bonus), Is.EqualTo(expected));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void APhysicalKillGrantsSpBeforeTheFollowingStatePassMarksDeath(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[0].Vitals = new[] { 1, 0, 100 };
            BattleSession session = Session(scenario, Array.Empty<BattleAbilityData>(), catalog: new Catalog { Sp = 10 }, bothSidesReady: true);
            Assert.That(session.Submit("A", BattleAction.Defend), Is.True);
            Assert.That(session.Submit("E", BattleAction.Attack, "A"), Is.True);
            Steps(session, 2);
            Assert.That((Actor(session, "A").Hp, Actor(session, "A").Sp), Is.EqualTo((0, 10)));
            Assert.That(Actor(session, "A").Alive, Is.True);
            Steps(session, 1);
            Assert.That(Actor(session, "A").Alive, Is.False);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void RawMagicCostMustBeAvailableBeforeEquipmentSavingsAndRejectionHasNoSideEffects(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            Learn(scenario.Participants[0], 301);
            scenario.Participants[0].Vitals[2] = 9;
            var actor = new BattleActorData
            {
                InstanceId = "A",
                Identity = 0,
                Equipment = new[] { new BattleEquipmentDefinition(new BattleEquipmentData { Id = 80001, Slot = 1, MpSaving = 50 }) }
            };
            BattleSession session = Session(scenario, new[] { Magic() }, new[] { actor });
            var command = new BattleCommand("A", BattleAction.Skill, "E", 301);
            BattleCostEstimate cost = session.GetCostEstimate(command).Single();
            Assert.That((cost.Required, cost.Payable, cost.Available), Is.EqualTo((10, 5, 9)));
            string before = Trace(session);
            Assert.That(session.CanSubmit(command), Is.Not.Null);
            Assert.That(session.Submit(command), Is.False);
            Assert.That(Trace(session), Is.EqualTo(before));
            Assert.That(session.Inputs, Is.Empty);
            Assert.That((Actor(session, "A").Mp, Actor(session, "A").Ready), Is.EqualTo((9, true)));
            Assert.That(Actor(session, "A").Skills.Single().Uses, Is.Zero);
        }

        [TestCase(1, 165)]
        [TestCase(2, 83)]
        [TestCase(3, 55)]
        public void MagicSecondSlotUses11500AndPaysOnlyAtItsWorkMarker(int proficiency, int chargeTicks)
        {
            BattleScenario scenario = Scenario();
            Learn(scenario.Participants[0], 301, proficiency);
            BattleSession session = Session(scenario, new[] { Magic() });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301)), Is.True);
            Steps(session, chargeTicks - 1);
            Assert.That(Actor(session, "A").Pose, Is.EqualTo(BattlePose.Casting));
            Assert.That(Actor(session, "A").Mp, Is.EqualTo(100));
            Assert.That(Actor(session, "A").Skills.Single().Uses, Is.Zero);
            Steps(session, 1);
            Assert.That(Actor(session, "A").CastGauge, Is.EqualTo(11500));
            Assert.That(Actor(session, "A").Pose, Is.Not.EqualTo(BattlePose.Casting));
            Steps(session, 1);
            Assert.That(Actor(session, "E").Hp, Is.EqualTo(500));
            Steps(session, 1);
            Assert.That(Actor(session, "E").Hp, Is.LessThan(500));
            Assert.That(Actor(session, "A").Mp, Is.EqualTo(90));
            Assert.That(Actor(session, "A").Skills.Single().Uses, Is.EqualTo(1));
        }

        [TestCase("PAL3", null)]
        [TestCase("PAL3A", null)]
        [TestCase("PAL3", "missing")]
        [TestCase("PAL3A", "missing")]
        [TestCase("PAL3", "A")]
        [TestCase("PAL3A", "A")]
        public void WholePartyPhysicalAttackRejectsInvalidAnchorBeforePlanningOrMutation(string variant, string target)
        {
            BattleSession session = Session(Scenario(variant), Array.Empty<BattleAbilityData>(),
                new[] { new BattleActorData { InstanceId = "A", Identity = 0, NormalAttackRange = BattleTargetRange.OpponentAll } });
            var command = new BattleCommand("A", BattleAction.Attack, target);
            int inputs = session.Inputs.Count;
            string trace = Trace(session);
            BattleActorSnapshot before = Actor(session, "A");
            Assert.That(session.CanSubmit(command), Is.Not.Null);
            Assert.That(session.Submit(command), Is.False);
            Assert.That(session.Inputs.Count, Is.EqualTo(inputs));
            Assert.That(Trace(session), Is.EqualTo(trace));
            BattleActorSnapshot after = Actor(session, "A");
            Assert.That((after.Gauge, after.CastGauge, after.Ready, after.Hp, after.Sp, after.Mp),
                Is.EqualTo((before.Gauge, before.CastGauge, before.Ready, before.Hp, before.Sp, before.Mp)));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void WholePartyPhysicalAttackKeepsItsPlanningAnchorAndHitsEveryOpponent(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants.Add(Participant("E2", BattleSide.Enemy, BattleSlot.Fire));
            BattleSession session = Session(scenario, Array.Empty<BattleAbilityData>(),
                new[] { new BattleActorData { InstanceId = "A", Identity = 0, NormalAttackRange = BattleTargetRange.OpponentAll } });
            var command = new BattleCommand("A", BattleAction.Attack, "E");
            Assert.That(session.CanSubmit(command), Is.Null);
            Assert.That(session.Submit(command), Is.True);
            Assert.That(session.Inputs.Last().Target, Is.EqualTo("E"));
            Steps(session, 10);
            Assert.That(session.Events.Where(entry => entry.Kind == BattleEventKind.Hit).Select(entry => entry.Target),
                Is.EquivalentTo(new[] { "E", "E2" }));
        }

        [Test]
        public void ManualInputPausesCastingButAcceptedLevelFourMagicStillReachesItsMarker()
        {
            BattleScenario scenario = Scenario();
            scenario.Participants.Add(Participant("B", BattleSide.Ally, BattleSlot.Fire));
            Learn(scenario.Participants[0], 301, 1);
            BattleSession session = Session(scenario, new[] { Magic() });
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301));
            Steps(session, 20);
            Assert.That(session.WaitingActor, Is.EqualTo("B"));
            Assert.That(Actor(session, "A").CastGauge, Is.Zero);
            Assert.That(session.Submit("B", BattleAction.Defend), Is.True);
            Steps(session, 1);
            Assert.That(Actor(session, "A").CastGauge, Is.EqualTo(70));

            scenario.Participants[0].Skills[0].Level = 4;
            session = Session(scenario, new[] { Magic() });
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301));
            Steps(session, 2);
            Assert.That(session.WaitingActor, Is.EqualTo("B"));
            Assert.That(Actor(session, "E").Hp, Is.LessThan(500));
            Assert.That(Actor(session, "A").Mp, Is.EqualTo(90));
        }

        [TestCase("PAL3", BattleState.Seal)]
        [TestCase("PAL3", BattleState.Paralysis)]
        [TestCase("PAL3", BattleState.Sleep)]
        [TestCase("PAL3", BattleState.Chaos)]
        [TestCase("PAL3", BattleState.Madness)]
        [TestCase("PAL3A", BattleState.Seal)]
        [TestCase("PAL3A", BattleState.Paralysis)]
        [TestCase("PAL3A", BattleState.Sleep)]
        [TestCase("PAL3A", BattleState.Chaos)]
        [TestCase("PAL3A", BattleState.Madness)]
        public void ControlStatusCancelsCastingWithoutLateCostOrProficiency(string variant, BattleState state)
        {
            BattleScenario scenario = Scenario(variant);
            Learn(scenario.Participants[0], 301, 1);
            Learn(scenario.Participants[1], 4100);
            var interrupt = new BattleAbilityData
            {
                Id = 4100,
                Range = BattleTargetRange.OpponentSingle,
                States = new[] { new BattleStatusEffect(state, false) }
            };
            BattleSession session = Session(scenario, new[] { Magic(), interrupt }, bothSidesReady: true);
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301));
            Assert.That(session.Submit(new BattleCommand("E", BattleAction.Skill, "A", 4100)), Is.True);
            Steps(session, 2);
            Assert.That(Actor(session, "A").ActionId, Is.Zero);
            Assert.That(Actor(session, "A").Gauge, Is.LessThan(BattleSession.FullGauge));
            Steps(session, 250);
            Assert.That(Actor(session, "A").Mp, Is.EqualTo(100));
            Assert.That(Actor(session, "A").Skills.Single().Uses, Is.Zero);
            Assert.That(Actor(session, "E").Hp, Is.EqualTo(500));
        }

        [Test]
        public void ControlStatusOnTheFinalCastTickOnlyInterruptsBeforeTheSpellStage(
            [Values("PAL3", "PAL3A")] string variant,
            [Values(BattleState.Paralysis, BattleState.Sleep, BattleState.Chaos, BattleState.Madness, BattleState.Seal)] BattleState state,
            [Values(false, true)] bool afterStageStarts)
        {
            // A level-one caster gains 70 per step and reaches 11,500 on step 165.
            const int gaugeFullTick = 165;
            int statusTick = afterStageStarts ? gaugeFullTick + 1 : gaugeFullTick;
            BattleScenario scenario = Scenario(variant);
            Learn(scenario.Participants[0], 301, 1);
            Learn(scenario.Participants[1], 4100);
            var statusSkill = new BattleAbilityData
            {
                Id = 4100,
                Range = BattleTargetRange.OpponentSingle,
                States = new[] { new BattleStatusEffect(state, false) }
            };
            var catalog = new Catalog();
            catalog.Timings[4100] = new AttackTiming(0, statusTick + 1, new[] { statusTick });
            BattleSession session = Session(scenario, new[] { Magic(), statusSkill }, catalog: catalog, bothSidesReady: true);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301)), Is.True);
            Assert.That(session.Submit(new BattleCommand("E", BattleAction.Skill, "A", 4100)), Is.True);

            Steps(session, gaugeFullTick - 1);
            Assert.That(Actor(session, "A").CastGauge, Is.EqualTo(11480));
            Assert.That(session.StageTasks.Any(task => task.Owner == "A" && task.Kind == "ability"), Is.False);
            Steps(session, statusTick - (gaugeFullTick - 1));
            Assert.That(Actor(session, "A").States.Any(active => active.State == state), Is.True);
            Assert.That(Actor(session, "A").CurrentAction.HasValue, Is.EqualTo(afterStageStarts));
            Assert.That(session.StageTasks.Any(task => task.Owner == "A" && task.Kind == "ability"), Is.EqualTo(afterStageStarts));
            Steps(session, 10);
            Assert.That(Actor(session, "A").Skills.Single().Uses, Is.EqualTo(afterStageStarts ? 1 : 0));
            Assert.That(Actor(session, "A").Mp, Is.EqualTo(afterStageStarts ? 90 : 100));
            Assert.That(Actor(session, "E").Hp < 500, Is.EqualTo(afterStageStarts));
        }

        [TestCase("PAL3", BattleState.Paralysis)]
        [TestCase("PAL3", BattleState.Sleep)]
        [TestCase("PAL3A", BattleState.Paralysis)]
        [TestCase("PAL3A", BattleState.Sleep)]
        public void ControlStatusPreservesProgressInTheFirstGauge(string variant, BattleState state)
        {
            BattleScenario scenario = Scenario(variant);
            Learn(scenario.Participants[0], 4100);
            var technique = new BattleAbilityData
            {
                Id = 4100,
                Range = BattleTargetRange.OpponentSingle,
                States = new[] { new BattleStatusEffect(state, false) }
            };
            BattleSession session = Session(scenario, new[] { technique });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4100)), Is.True);
            Steps(session, 1);
            int firstStep = Actor(session, "E").Gauge;
            Assert.That(firstStep, Is.Positive);
            Steps(session, 1);
            BattleActorSnapshot target = Actor(session, "E");
            Assert.That(target.States.Any(active => active.State == state), Is.True);
            Assert.That(target.Gauge, Is.EqualTo(2 * firstStep));
        }

        [Test]
        public void ExplicitCancellationNeverProducesLateMagicPaymentOrHits()
        {
            BattleScenario scenario = Scenario();
            Learn(scenario.Participants[0], 301, 1);
            BattleSession session = Session(scenario, new[] { Magic() });
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301));
            Steps(session, 20);
            Assert.That(session.CancelAction("A"), Is.True);
            Assert.That(session.CancelAction("A"), Is.False);
            Steps(session, 300);
            Assert.That(Actor(session, "A").Mp, Is.EqualTo(100));
            Assert.That(Actor(session, "A").Skills.Single().Uses, Is.Zero);
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit), Is.False);
        }

        [TestCase(false, 5750)]
        [TestCase(true, 8625)]
        public void PhysicalAndRankTwoMagicHitsInterruptTheSecondSlotAtDifferentFirstSlotPositions(bool magicHit, int expectedGauge)
        {
            BattleScenario scenario = Scenario();
            Learn(scenario.Participants[0], 301, 1);
            Learn(scenario.Participants[1], 399);
            BattleAbilityData incoming = Magic(399);
            incoming.SpellRank = 2;
            BattleSession session = Session(scenario, new[] { Magic(), incoming }, bothSidesReady: true);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301)), Is.True);
            Assert.That(session.Submit(magicHit ? new BattleCommand("E", BattleAction.Skill, "A", 399) :
                new BattleCommand("E", BattleAction.Attack, "A")), Is.True);
            Steps(session, 2);
            Assert.That(Actor(session, "A").ActionId, Is.Zero);
            Assert.That(Actor(session, "A").Gauge, Is.InRange(expectedGauge, expectedGauge + 35));
            Steps(session, 200);
            Assert.That(Actor(session, "A").Mp, Is.EqualTo(100));
            Assert.That(Actor(session, "A").Skills.Single().Uses, Is.Zero);
        }

        [Test]
        public void MagicInterruptUsesTheLastNonzeroResourceResultRatherThanTheHpLoss()
        {
            BattleScenario scenario = Scenario();
            scenario.Participants[0].Vitals[2] = 50;
            Learn(scenario.Participants[0], 301, 1);
            Learn(scenario.Participants[1], 399);
            BattleAbilityData incoming = Magic(399);
            incoming.Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -20),
                new BattleAttributeEffect(2, BattleValueMode.Absolute, 10) };
            BattleSession session = Session(scenario, new[] { Magic(), incoming }, bothSidesReady: true);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301)), Is.True);
            Assert.That(session.Submit(new BattleCommand("E", BattleAction.Skill, "A", 399)), Is.True);
            Steps(session, 2);
            Assert.That(Actor(session, "A").Hp, Is.LessThan(500));
            Assert.That(Actor(session, "A").Mp, Is.GreaterThan(50));
            Assert.That(Actor(session, "A").CurrentAction, Is.EqualTo(BattleAction.Skill));
            Assert.That(Actor(session, "A").Pose, Is.EqualTo(BattlePose.Hurt));
            Assert.That(Actor(session, "A").CastGauge, Is.EqualTo(140));
        }

        [TestCase(false, 130)]
        [TestCase(true, 30)]
        public void MedicineConsumesInventoryAtStartAndRevivalClearsDeathBeforeHealing(bool revive, int expectedHp)
        {
            BattleScenario scenario = Scenario();
            scenario.Participants.Add(Participant("B", BattleSide.Ally, BattleSlot.Fire, revive ? 0 : 100));
            scenario.Items.Add(new BattleItem { Id = 6201, Count = 2 });
            BattleSession session = Session(scenario, new[] { Item(revive: revive) });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "B", 6201)), Is.True);
            Assert.That(session.Inventory.Single().Count, Is.EqualTo(2));
            Assert.That(Actor(session, "B").Hp, Is.EqualTo(revive ? 0 : 100));
            Steps(session, 2);
            Assert.That(session.Inventory.Single().Count, Is.EqualTo(1));
            Assert.That(Actor(session, "B").Hp, Is.EqualTo(expectedHp));
            if (revive)
                Assert.That(Actor(session, "B").Gauge, Is.GreaterThanOrEqualTo(11500 * 2 / 3));
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.ItemChanged), Is.EqualTo(1));
            Assert.That(scenario.Items.Single().Count, Is.EqualTo(2));
            Assert.That(scenario.Participants.Single(actor => actor.InstanceId == "B").Vitals[0], Is.EqualTo(revive ? 0 : 100));
        }

        [Test]
        public void CancelledMedicineHasOneStartConsumptionAndNoLateHealingOrSecondConsumption()
        {
            BattleScenario scenario = Scenario();
            scenario.Participants[0].Vitals[0] = 100;
            scenario.Items.Add(new BattleItem { Id = 6201, Count = 1 });
            var catalog = new Catalog();
            catalog.Timings[6201] = new AttackTiming(0, 5, new[] { 3 });
            BattleSession session = Session(scenario, new[] { Item() }, catalog: catalog);
            session.Submit(new BattleCommand("A", BattleAction.Item, "A", 6201));
            Steps(session, 1);
            Assert.That(session.Inventory, Is.Empty);
            session.CancelAction("A");
            Steps(session, 300);
            Assert.That(Actor(session, "A").Hp, Is.EqualTo(100));
            Assert.That(session.Inventory, Is.Empty);
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.ItemChanged), Is.EqualTo(1));
            UntilReady(session, "A");
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "A", 6201)), Is.False);
        }

        [TestCase("PAL3", 255, 255)]
        [TestCase("PAL3A", 255, 256)]
        [TestCase("PAL3A", 65535, 65535)]
        public void BlockedMagicStillPaysAndIncrementsOnceWithOriginalCounterWidth(string variant, int initialUses, int expectedUses)
        {
            BattleScenario scenario = Scenario(variant);
            Learn(scenario.Participants[0], 301, 4, initialUses);
            BattleAbilityData magic = Magic();
            magic.States = new[] { new BattleStatusEffect(BattleState.AttackDecrease, false) };
            BattleSession session = Session(scenario, new[] { magic }, new[] { new BattleActorData
                { InstanceId = "E", InitialStates = new[] { BattleState.Barrier } } });
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301));
            Steps(session, 5);
            Assert.That(Actor(session, "E").Hp, Is.EqualTo(500));
            Assert.That(Actor(session, "E").States.Select(status => status.State), Is.EqualTo(new[] { BattleState.Barrier }));
            Assert.That(Actor(session, "A").Mp, Is.EqualTo(90));
            Assert.That(Actor(session, "A").Skills.Single().Uses, Is.EqualTo(expectedUses));
        }

        [TestCase("PAL3", 126, 76)]
        [TestCase("PAL3A", 109, 93)]
        public void NativeAttributeStatusVectorsKeepVariantAndBankersRounding(string variant, int increased, int decreased)
        {
            var rules = new BattleStatusRules(variant);
            Assert.That(rules.ModifyAttribute(3, 101, new[] { BattleState.AttackIncrease }), Is.EqualTo(increased));
            Assert.That(rules.ModifyAttribute(3, 101, new[] { BattleState.AttackDecrease }), Is.EqualTo(decreased));
            Assert.That(rules.ModifyAttribute(5, 10, new[] { BattleState.SpeedIncrease }), Is.EqualTo(12));
            Assert.That(rules.ModifyAttribute(5, 10, new[] { BattleState.SpeedDecrease }), Is.EqualTo(8));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void NativePoisonVectorsPreserveLatentRoundsAndEarthFloatAccumulator(string variant)
        {
            var rules = new BattleStatusRules(variant);
            BattleStatusInstance water = rules.Create(BattleState.PoisonWater, 1001);
            int hp = 999;
            var deltas = new List<int>();
            for (int index = 0; index < 5; index++)
            {
                int delta = rules.TickRound(water, 1001, hp);
                deltas.Add(delta);
                hp += delta;
            }
            Assert.That(deltas, Is.EqualTo(new[] { -499, -250, -125, -62, -31 }));
            BattleStatusInstance earth = rules.Create(BattleState.PoisonEarth, 1001);
            Assert.That(Enumerable.Range(0, 8).Select(_ => rules.TickRound(earth, 1001, 999)),
                Is.EqualTo(new[] { -20, -30, -45, -68, -101, -152, -228, -342 }));
            BattleStatusInstance wind = rules.Create(BattleState.PoisonWind, 1001);
            Assert.That(Enumerable.Range(0, 3).Select(_ => rules.TickRound(wind, 1001, 999)), Is.EqualTo(new[] { 0, 0, 0 }));
            Assert.That((wind.LatentRounds, wind.RemainingRounds), Is.EqualTo((0, 5)));
            Assert.That(rules.TickRound(wind, 1001, 999), Is.EqualTo(-750));
            Assert.That(wind.RemainingRounds, Is.EqualTo(4));
        }

        [Test]
        public void PoisonCannotRefreshOrStackAndPoisonResistanceDoesNotImmunizeSeal()
        {
            var rules = new BattleStatusRules("PAL3");
            Assert.That(rules.CanAdd(BattleState.PoisonWater, new[] { BattleState.PoisonWater }, Array.Empty<BattleState>()), Is.Not.Null);
            Assert.That(rules.CanAdd(BattleState.PoisonFire, new[] { BattleState.PoisonWater }, Array.Empty<BattleState>()), Is.Not.Null);
            Assert.That(rules.CanAdd(BattleState.PoisonFire, new[] { BattleState.PoisonResist }, Array.Empty<BattleState>()), Is.Not.Null);
            Assert.That(rules.CanAdd(BattleState.Seal, new[] { BattleState.PoisonResist }, Array.Empty<BattleState>()), Is.Null);
            Assert.That(rules.ReplacedBy(BattleState.Barrier), Does.Contain(BattleState.Reflection));
            Assert.That(rules.ReplacedBy(BattleState.Barrier), Does.Contain(BattleState.Sleep));
            Assert.That(rules.ReplacedBy(BattleState.Barrier), Has.No.Member(BattleState.PoisonWater));
            Assert.That(rules.CanAdd(BattleState.Seal, new[] { BattleState.Invisible }, Array.Empty<BattleState>()), Is.Not.Null);
        }

        [TestCase(BattleState.Evade)]
        [TestCase(BattleState.Reflection)]
        public void EvadeAndReflectionAffectPhysicalAttacksButDoNotBlockMagic(BattleState state)
        {
            BattleScenario scenario = Scenario();
            Learn(scenario.Participants[0], 301);
            var enemy = new BattleActorData { InstanceId = "E", InitialStates = new[] { state } };
            BattleSession physical = Session(scenario, new[] { Magic() }, new[] { enemy });
            physical.Submit("A", BattleAction.Attack, "E");
            Steps(physical, 2);
            Assert.That(Actor(physical, "E").Hp, Is.EqualTo(500));
            Assert.That(Actor(physical, "A").Hp, state == BattleState.Reflection ? Is.LessThan(500) : Is.EqualTo(500));

            BattleSession magic = Session(scenario, new[] { Magic() }, new[] { enemy });
            magic.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301));
            Steps(magic, 2);
            Assert.That(Actor(magic, "E").Hp, Is.LessThan(500));
            Assert.That(Actor(magic, "A").Hp, Is.EqualTo(500));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ManualRevivalCanSelectLivingAndDefeatedAllies(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants.Add(Participant("B", BattleSide.Ally, BattleSlot.Fire, 0));
            var group = Magic(range: BattleTargetRange.OwnAll);
            group.States = new[] { new BattleStatusEffect(BattleState.Death, true) };
            group.Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, 20) };
            Learn(scenario.Participants[0], 301);
            BattleSession session = Session(scenario, new[] { group, Item(6201), Item(6211, revive: true) });
            var command = new BattleCommand("A", BattleAction.Skill, abilityId: 301);
            Assert.That(session.GetTargetCandidates(command).Select(target => target.Id), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(session.GetTargetCandidates(new BattleCommand("A", BattleAction.Item, abilityId: 6201)).Select(target => target.Id), Is.EqualTo(new[] { "A" }));
            Assert.That(session.GetTargetCandidates(new BattleCommand("A", BattleAction.Item, abilityId: 6211)).Select(target => target.Id), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(session.Submit(command), Is.True);
            Steps(session, 2);
            Assert.That(Actor(session, "B").Hp, Is.Positive);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ManualAttackRespectsTheCenterUntilItFallsAndKeepsPiercingRanges(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[1].Slot = BattleSlot.Fire;
            scenario.Participants.Add(Participant("Center", BattleSide.Enemy, BattleSlot.Center));
            Learn(scenario.Participants[0], 301);
            var command = new BattleCommand("A", BattleAction.Attack, "E");
            BattleSession WithRange(BattleTargetRange range) => Session(scenario, new[] { Magic() },
                new[] { new BattleActorData { InstanceId = "A", Identity = 0, NormalAttackRange = range } });

            foreach (BattleTargetRange range in new[] { BattleTargetRange.OpponentSingle, BattleTargetRange.OpponentRow })
            {
                BattleSession session = WithRange(range);
                Assert.That(session.GetTargetCandidates(command).Select(actor => actor.Id), Is.EqualTo(new[] { "Center" }));
                Assert.That(session.Submit(command), Is.False);
                Assert.That(session.CanSubmit(new BattleCommand("A", BattleAction.Skill, "E", 301)), Is.Null);
            }
            foreach (BattleTargetRange range in new[] { BattleTargetRange.OpponentColumn, BattleTargetRange.OpponentAll })
            {
                BattleSession session = WithRange(range);
                Assert.That(session.GetPotentialAffectedTargets(command).Select(actor => actor.Id), Is.EquivalentTo(new[] { "E", "Center" }));
                Assert.That(session.Submit(command), Is.True);
                Steps(session, 2);
                Assert.That(Actor(session, "E").Hp, Is.LessThan(500));
                Assert.That(Actor(session, "Center").Hp, Is.LessThan(500));
            }
            scenario.Participants[2].Vitals[0] = 0;
            Assert.That(WithRange(BattleTargetRange.OpponentSingle).Submit(command), Is.True);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void RepeatKeepsTheSelectedRowAfterItsOriginalAnchorDies(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[1].Vitals[0] = 1;
            scenario.Participants.Add(Participant("OtherRow", BattleSide.Enemy, BattleSlot.Fire, 1));
            scenario.Participants.Add(Participant("SameRow", BattleSide.Enemy, BattleSlot.Wind));
            BattleSession session = Session(scenario, Array.Empty<BattleAbilityData>(),
                new[] { new BattleActorData { InstanceId = "A", Identity = 0, NormalAttackRange = BattleTargetRange.OpponentRow } });
            session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            UntilReady(session, "A");
            Assert.That(Actor(session, "E").Alive, Is.False);
            int hp = Actor(session, "SameRow").Hp;
            Assert.That(session.RequestAiRepeat("A"), Is.True);
            Steps(session, 2);
            Assert.That(Actor(session, "SameRow").Hp, Is.LessThan(hp));
            Assert.That(Actor(session, "OtherRow").Hp, Is.EqualTo(1));
            BattleSession replay = BattleReplay.Play(new BattleSetup(scenario), new Catalog(), session.Inputs, session.Tick,
                new ReadyRules(), session.Definitions);
            Assert.That(Trace(replay), Is.EqualTo(Trace(session)));
        }

        [TestCase(BattleTargetRange.OpponentRow, new[] { "E0", "E2", "E5" })]
        [TestCase(BattleTargetRange.OpponentColumn, new[] { "E0", "E3" })]
        public void RowAndColumnExpandFromTheSelectedEnemyFormationSlot(BattleTargetRange range, string[] expected)
        {
            BattleScenario scenario = Scenario();
            scenario.Participants.RemoveAt(1);
            for (int slot = 0; slot < 6; slot++)
                scenario.Participants.Add(Participant("E" + slot, BattleSide.Enemy, (BattleSlot)slot));
            Learn(scenario.Participants[0], 301);
            BattleSession session = Session(scenario, new[] { Magic(range: range) });
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E0", 301));
            Steps(session, 2);
            Assert.That(session.Actors.Where(actor => actor.Side == BattleSide.Enemy && actor.Hp < 500).Select(actor => actor.Id), Is.EqualTo(expected));
            Assert.That(Actor(session, "A").Skills.Single().Uses, Is.EqualTo(1));
        }

        [TestCase(BattleTargetRange.OpponentRow, new[] { "E0", "E2", "E5" })]
        [TestCase(BattleTargetRange.OpponentColumn, new[] { "E0", "E3" })]
        [TestCase(BattleTargetRange.OpponentAll, new[] { "E0", "E1", "E2", "E3", "E4", "E5" })]
        public void PhysicalWeaponRangeUsesCapturedActorDefinition(BattleTargetRange range, string[] expected)
        {
            BattleScenario scenario = Scenario();
            scenario.Participants.RemoveAt(1);
            for (int slot = 0; slot < 6; slot++)
                scenario.Participants.Add(Participant("E" + slot, BattleSide.Enemy, (BattleSlot)slot));
            BattleSession session = Session(scenario, Array.Empty<BattleAbilityData>(),
                new[] { new BattleActorData { InstanceId = "A", Identity = 0, NormalAttackRange = range } });
            Assert.That(session.Submit("A", BattleAction.Attack, "E0"), Is.True);
            Steps(session, 2);
            Assert.That(session.Actors.Where(actor => actor.Side == BattleSide.Enemy && actor.Hp < 500).Select(actor => actor.Id), Is.EqualTo(expected));
        }

        [Test]
        public void SpeedStatusReplacesAnExistingCastMultiplierWhileAttackStatusDoesNot()
        {
            foreach (BattleState state in new[] { BattleState.AttackIncrease, BattleState.SpeedIncrease })
            {
                BattleScenario scenario = Scenario();
                Learn(scenario.Participants[0], 301, 2);
                Learn(scenario.Participants[1], 4100);
                var boost = new BattleAbilityData
                {
                    Id = 4100,
                    Range = BattleTargetRange.OpponentSingle,
                    States = new[] { new BattleStatusEffect(state, false) }
                };
                BattleSession session = Session(scenario, new[] { Magic(), boost }, bothSidesReady: true);
                session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301));
                session.Submit(new BattleCommand("E", BattleAction.Skill, "A", 4100));
                Steps(session, 2);
                int before = Actor(session, "A").CastGauge;
                Steps(session, 1);
                Assert.That(Actor(session, "A").CastGauge - before, Is.EqualTo(state == BattleState.SpeedIncrease ? 40 : 140));
            }
        }

        [Test]
        public void IdenticalMonsterResourceIdsKeepIndependentHpAndStatusInstances()
        {
            BattleScenario scenario = Scenario();
            scenario.Participants.Add(Participant("E2", BattleSide.Enemy, BattleSlot.Fire));
            Learn(scenario.Participants[0], 301);
            BattleAbilityData magic = Magic();
            magic.States = new[] { new BattleStatusEffect(BattleState.PoisonWater, false) };
            BattleSession session = Session(scenario, new[] { magic });
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301));
            Steps(session, 2);
            Assert.That(Actor(session, "E").Hp, Is.LessThan(500));
            Assert.That(Actor(session, "E").States.Select(state => state.State), Does.Contain(BattleState.PoisonWater));
            Assert.That(Actor(session, "E2").Hp, Is.EqualTo(500));
            Assert.That(Actor(session, "E2").States, Is.Empty);
            Assert.That(scenario.Participants.Where(actor => actor.Side == BattleSide.Enemy).Select(actor => actor.Vitals[0]), Is.EqualTo(new[] { 500, 500 }));
        }

        [TestCase("PAL3", 98, 99)]
        [TestCase("PAL3", 99, 99)]
        [TestCase("PAL3", 100, 100)]
        [TestCase("PAL3A", 98, 99)]
        [TestCase("PAL3A", 99, 99)]
        [TestCase("PAL3A", 100, 100)]
        public void SettlementLootReportsOnlyItemsAddedToTheInventory(string variant, int initialCount, int expectedCount)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Items.Add(new BattleItem { Id = 6201, Count = initialCount });
            scenario.Participants[1].Vitals[0] = 0;
            BattleSession session = Session(scenario, Array.Empty<BattleAbilityData>(),
                new[] { new BattleActorData { InstanceId = "E", LootItem = 6201, LootCount = 2 } });

            int added = expectedCount - initialCount;
            Assert.That(session.Result.Loot.Select(item => (item.Id, item.Count)),
                Is.EqualTo(added > 0 ? new[] { (6201, added) } : Array.Empty<(int, int)>()));
            Assert.That(session.Result.Items.Single().Count, Is.EqualTo(expectedCount));
            BattleEvent change = session.Events.Single(entry => entry.Kind == BattleEventKind.ItemChanged);
            Assert.That((change.RequestedDelta, change.ActualDelta), Is.EqualTo((2, added)));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void SettlementRecoveryCannotPutHpAboveTheAuthoredMaximum(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[1].Vitals[0] = 0;
            BattleParticipant ally = Participant("B", BattleSide.Ally, BattleSlot.Fire, hp: 0);
            ally.Attributes[0] = 0;
            scenario.Participants.Add(ally);
            BattleSession session = Session(scenario, Array.Empty<BattleAbilityData>());

            Assert.That(session.Result.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(session.Result.Winner, Is.EqualTo(BattleSide.Ally));
            BattleActorSnapshot restored = session.Result.Actors.Single(actor => actor.Id == "B");
            Assert.That(restored.Hp, Is.EqualTo(0));
            Assert.That(restored.MaxHp, Is.EqualTo(0));
        }

        [TestCase("PAL3", false, false, 202, 353, 202)]
        [TestCase("PAL3A", false, false, 202, 353, 202)]
        [TestCase("PAL3", true, false, 303, 454, 0)]
        [TestCase("PAL3A", true, false, 303, 454, 0)]
        [TestCase("PAL3", false, true, 232, 353, 202)]
        [TestCase("PAL3A", false, true, 232, 353, 202)]
        public void NativeResultVectorsSplitOnlyAmongSurvivorsAndAwardEverySameTickFinalKill(string variant,
            bool thirdDead, bool experienceBonus, int first, int second, int third)
        {
            // native-data-vectors.json: EXP [101,202,303], deaths [1,2,2], killers [0,1,1].
            BattleScenario scenario = Scenario(variant);
            scenario.Money = 7;
            scenario.Participants.RemoveAt(1);
            scenario.Participants.Add(Participant("B", BattleSide.Ally, BattleSlot.Fire));
            scenario.Participants.Add(Participant("C", BattleSide.Ally, BattleSlot.Wind, thirdDead ? 0 : 500));
            for (int slot = 0; slot < 3; slot++)
                scenario.Participants.Add(Participant("E" + slot, BattleSide.Enemy, (BattleSlot)slot, 10));
            Learn(scenario.Participants[0], 301);
            Learn(scenario.Participants[1], 302);
            var definitions = new List<BattleActorData>();
            for (int slot = 0; slot < 3; slot++)
                definitions.Add(new BattleActorData
                {
                    InstanceId = "E" + slot,
                    ExperienceReward = new[] { 101, 202, 303 }[slot],
                    MoneyReward = new[] { 11, 22, 33 }[slot],
                    LootItem = 6201,
                    LootCount = slot + 1
                });
            if (experienceBonus)
                definitions.Add(new BattleActorData
                {
                    InstanceId = "A",
                    Identity = 0,
                    Equipment = new[]
                    { new BattleEquipmentDefinition(new BattleEquipmentData { Id = 80001, Slot = 1, SpecialEffect = 21 }) }
                });
            var catalog = new Catalog();
            catalog.Timings[301] = new AttackTiming(0, 5, new[] { 1 });
            BattleSession session = Session(scenario, new[] { Magic(301, -1000), Magic(302, -1000, BattleTargetRange.OpponentAll) }, definitions, catalog);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E0", 301)), Is.True);
            Assert.That(session.Submit(new BattleCommand("B", BattleAction.Skill, abilityId: 302)), Is.True);
            Steps(session, 2);
            Complete(session);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(session.Result.Rewards.OrderBy(reward => reward.Actor).Select(reward => reward.ExperienceGained), Is.EqualTo(new[] { first, second, third }));
            Assert.That(session.Result.MoneyAfter, Is.EqualTo(experienceBonus ? 76 : 73));
            Assert.That(session.Result.Loot.Single().Count, Is.EqualTo(6));
            Assert.That(session.Result.Items.Single().Count, Is.EqualTo(6));
            Assert.That(Actor(session, "C").Hp, Is.EqualTo(thirdDead ? 1 : 500));
            Assert.That(Actor(session, "A").Mp, Is.EqualTo(90));
            Assert.That(Actor(session, "B").Mp, Is.EqualTo(90));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void SkillUsesAccumulateDuringBattleAndOnlySettlementAdvancesTiersAndUnlocks(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[0].Level = 35;
            Learn(scenario.Participants[0], 301, 1, 5);
            Learn(scenario.Participants[0], 319, 2);
            Learn(scenario.Participants[0], 399);
            BattleAbilityData first = Magic(301, -1);
            first.UsesBeforeLevelUp = new[] { 0, 2, 4, 6 };
            first.NextSkillId = 302;
            first.NextSkillRequiredProficiency = 2;
            first.LearningLevel = 4;
            BattleAbilityData second = Magic(302);
            second.Unlocks = new[] { new BattleSkillUnlock(326, 319, 2, 1, 35) };
            BattleSession session = Session(scenario, new[] { first, second, Magic(319), Magic(326), Magic(399, -1000) });
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301));
            Steps(session, 170);
            Assert.That(Actor(session, "A").Skills.Single(skill => skill.Id == 301).Uses, Is.EqualTo(6));
            Assert.That(Actor(session, "A").Skills.Single(skill => skill.Id == 301).Level, Is.EqualTo(1));
            Assert.That(Actor(session, "A").Skills.Any(skill => skill.Id is 302 or 326), Is.False);
            UntilReady(session, "A");
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 399));
            Steps(session, 2);
            Complete(session);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(Actor(session, "A").Skills.Single(skill => skill.Id == 301).Level, Is.EqualTo(4));
            Assert.That(Actor(session, "A").Skills.Single(skill => skill.Id == 301).Uses, Is.EqualTo(6));
            Assert.That(Actor(session, "A").Skills.Where(skill => skill.Id is 302 or 326).Select(skill => (skill.Level, skill.Uses, skill.Mastered)),
                Is.EqualTo(new[] { (1, 0, true), (1, 0, true) }));
        }

        [TestCase(34, 2, true)]
        [TestCase(35, 1, true)]
        [TestCase(35, 2, false)]
        public void CompositeUnlockRequiresActorLevelOtherTierAndTheOtherSkillsLegalIdentity(int actorLevel, int otherTier, bool legalOther)
        {
            BattleScenario scenario = Scenario();
            scenario.Participants[0].Level = actorLevel;
            Learn(scenario.Participants[0], 302, 3);
            Learn(scenario.Participants[0], 319, otherTier);
            Learn(scenario.Participants[0], 399);
            BattleAbilityData prerequisite = Magic(302);
            prerequisite.Unlocks = new[] { new BattleSkillUnlock(326, 319, 2, 3, 35) };
            BattleAbilityData other = Magic(319);
            other.ApplicableIdentities = new[] { legalOther ? 0 : 1 };
            BattleSession session = Session(scenario, new[] { prerequisite, other, Magic(326), Magic(399, -1000) });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 399)), Is.True);
            Steps(session, 2);
            Complete(session);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(Actor(session, "A").Skills.Any(skill => skill.Id == 326 && skill.Mastered), Is.False);
        }

        [TestCase("PAL3", 50)]
        [TestCase("PAL3A", 0)]
        public void EscapeUsesTheVariantsNativeExperiencePassAndKeepsDeadEnemyLoot(string variant, int experience)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[1].InstanceId = "E0";
            scenario.Participants[1].Vitals[0] = 10;
            scenario.Participants.Add(Participant("E1", BattleSide.Enemy, BattleSlot.Fire));
            BattleParticipant escaping = Participant("B", BattleSide.Ally, BattleSlot.Fire);
            escaping.Level = 10;
            escaping.Attributes[6] = 10;
            scenario.Participants.Add(escaping);
            Learn(scenario.Participants[0], 399);
            var enemy = new BattleActorData { InstanceId = "E0", ExperienceReward = 101, MoneyReward = 11, LootItem = 6201, LootCount = 2 };
            var catalog = new Catalog();
            catalog.Timings[399] = new AttackTiming(0, 5, new[] { 1 });
            BattleSession session = Session(scenario, new[] { Magic(399, -1000) }, new[] { enemy }, catalog);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E0", 399)), Is.True);
            Assert.That(session.Submit("B", BattleAction.Flee), Is.True);
            Steps(session, 261);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running));
            Assert.That(Actor(session, "B").CastGauge, Is.EqualTo(11484));
            Steps(session, 1);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running));
            Assert.That(Actor(session, "B").CastGauge, Is.EqualTo(11500));
            Steps(session, 1);
            Complete(session);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(session.Result.Escaped, Is.True);
            Assert.That(session.Result.Winner, Is.Null);
            Assert.That(session.Result.Rewards.Select(reward => reward.ExperienceGained), Is.EqualTo(new[] { experience, experience }));
            Assert.That(Actor(session, "A").Skills.Single(skill => skill.Id == 399).Uses, Is.EqualTo(1));
            Assert.That(session.Result.MoneyAfter, Is.EqualTo(11));
            Assert.That(session.Result.Loot.Single().Count, Is.EqualTo(2));
        }

        [TestCase(3, 44, 262)]
        [TestCase(15, 140, 83)]
        [TestCase(20, 180, 64)]
        [TestCase(50, 164, 71)]
        public void EscapeWaitsForTheSecondSlotAndUsesByteWrappedSpeedInsteadOfASaturatedMultiplier(int speed, int castSpeed, int ticks)
        {
            // Published EXEs: PAL3 0x515286..529f / PAL3A 0x509853..986a
            // store BYTE(20 + 8 * speed). The available C++ source's x4 differs.
            BattleScenario scenario = Scenario();
            scenario.Participants[0].Level = 10;
            scenario.Participants[0].Attributes[5] = speed;
            scenario.Participants[0].Attributes[6] = 10;
            BattleSession session = Session(scenario, Array.Empty<BattleAbilityData>());
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            Steps(session, 1);
            Assert.That(Actor(session, "A").CastGauge, Is.EqualTo(castSpeed));
            Assert.That(Actor(session, "A").Pose, Is.EqualTo(BattlePose.Fleeing));
            Steps(session, ticks - 2);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running));
            Assert.That(Actor(session, "A").CastGauge, Is.LessThan(11500));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Escaped), Is.False);
            Steps(session, 1);
            Assert.That(Actor(session, "A").CastGauge, Is.EqualTo(11500));
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running));
            Steps(session, 1);
            Complete(session);
            Assert.That(session.Result.Escaped, Is.True);
        }

        [Test]
        public void AnotherManualInputPausesTheEscapeSecondSlot()
        {
            BattleScenario scenario = Scenario();
            scenario.Participants.Add(Participant("B", BattleSide.Ally, BattleSlot.Fire));
            BattleSession session = Session(scenario, Array.Empty<BattleAbilityData>());
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            Steps(session, 20);
            Assert.That(session.WaitingActor, Is.EqualTo("B"));
            Assert.That(Actor(session, "A").CastGauge, Is.Zero);
            Assert.That(session.Submit("B", BattleAction.Defend), Is.True);
            Steps(session, 1);
            Assert.That(Actor(session, "A").CastGauge, Is.EqualTo(44));
        }

        [TestCase(BattleState.Seal, true, false, true)]
        [TestCase(BattleState.Forbidden, false, true, false)]
        public void SealAndForbiddenHaveSeparateEscapeMagicAndItemAvailability(BattleState state, bool escapeAllowed, bool magicAllowed, bool itemAllowed)
        {
            BattleScenario scenario = Scenario();
            Learn(scenario.Participants[0], 301, 1);
            scenario.Items.Add(new BattleItem { Id = 6201, Count = 1 });
            BattleSession session = Session(scenario, new[] { Magic(), Item() },
                new[] { new BattleActorData { InstanceId = "A", Identity = 0, InitialStates = new[] { state } } });
            Assert.That(session.CanSubmit(new BattleCommand("A", BattleAction.Flee)) == null, Is.EqualTo(escapeAllowed));
            Assert.That(session.CanSubmit(new BattleCommand("A", BattleAction.Skill, "E", 301)) == null, Is.EqualTo(magicAllowed));
            Assert.That(session.CanSubmit(new BattleCommand("A", BattleAction.Item, "A", 6201)) == null, Is.EqualTo(itemAllowed));
            Assert.That(session.Inputs, Is.Empty);
            Assert.That(session.Inventory.Single().Count, Is.EqualTo(1));
        }

        [TestCase(true, BattleState.Forbidden, true)]
        [TestCase(true, BattleState.Seal, false)]
        [TestCase(false, BattleState.Forbidden, false)]
        [TestCase(false, BattleState.Seal, true)]
        public void ForbiddenCancelsOnlyEscapeWaitingAndSealCancelsOnlyMagicWaiting(bool fleeing, BattleState state, bool cancelled)
        {
            BattleScenario scenario = Scenario();
            Learn(scenario.Participants[0], 301, 1);
            Learn(scenario.Participants[1], 4100);
            var interrupt = new BattleAbilityData
            {
                Id = 4100,
                Range = BattleTargetRange.OpponentSingle,
                States = new[] { new BattleStatusEffect(state, false) }
            };
            BattleSession session = Session(scenario, new[] { Magic(), interrupt }, bothSidesReady: true);
            Assert.That(session.Submit(fleeing ? new BattleCommand("A", BattleAction.Flee) :
                new BattleCommand("A", BattleAction.Skill, "E", 301)), Is.True);
            long action = Actor(session, "A").ActionId;
            Assert.That(session.Submit(new BattleCommand("E", BattleAction.Skill, "A", 4100)), Is.True);
            Steps(session, 2);
            if (cancelled && fleeing)
                Steps(session, 3); // Native escape waiting releases its body before the failure dialog thread ends.
            Assert.That(Actor(session, "A").ActionId, Is.EqualTo(cancelled ? 0 : action));
            if (cancelled)
            {
                Steps(session, 200);
                Assert.That(Actor(session, "A").Mp, Is.EqualTo(100));
                Assert.That(Actor(session, "A").Skills.Single().Uses, Is.Zero);
                Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Escaped), Is.False);
                Assert.That(Actor(session, "E").Hp, Is.EqualTo(500));
            }
            else
                Assert.That(Actor(session, "A").CastGauge, Is.EqualTo(fleeing ? 88 : 140));
        }

        [Test]
        public void DefeatDoesNotPayExperienceMoneyOrLootAndDoesNotReviveTheParty()
        {
            BattleScenario scenario = Scenario();
            Learn(scenario.Participants[1], 399);
            var enemy = new BattleActorData { InstanceId = "E", ExperienceReward = 101, MoneyReward = 11, LootItem = 6201, LootCount = 2 };
            BattleSession session = Session(scenario, new[] { Magic(399, -10000) }, new[] { enemy }, bothSidesReady: true);
            Assert.That(session.Submit("A", BattleAction.Defend), Is.True);
            Assert.That(session.Submit(new BattleCommand("E", BattleAction.Skill, "A", 399)), Is.True);
            Steps(session, 2);
            Complete(session);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(session.Result.Winner, Is.EqualTo(BattleSide.Enemy));
            Assert.That(session.Result.Rewards, Is.Empty);
            Assert.That(session.Result.Loot, Is.Empty);
            Assert.That(session.Result.MoneyAfter, Is.Zero);
            Assert.That(Actor(session, "A").Hp, Is.Zero);
        }

        [TestCase("PAL3", 18, 1)]
        [TestCase("PAL3", 19, 2)]
        [TestCase("PAL3", 47, 3)]
        [TestCase("PAL3A", 9, 1)]
        [TestCase("PAL3A", 10, 2)]
        [TestCase("PAL3A", 47, 3)]
        [TestCase("PAL3", int.MaxValue, 100)]
        [TestCase("PAL3A", int.MaxValue, 100)]
        public void NativeCumulativeExperienceBoundariesPreservePermanentOffsetsAndFillNewVitals(string variant, int experience, int expectedLevel)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[0].Experience = experience;
            scenario.Participants[0].Vitals = new[] { 7, 8, 9 };
            Learn(scenario.Participants[0], 399);
            int[] thresholds = Enumerable.Repeat(int.MaxValue, 101).ToArray();
            thresholds[1] = 0;
            thresholds[2] = variant == "PAL3" ? 19 : 10;
            thresholds[3] = 47;
            for (int level = 4; level < 100; level++)
                thresholds[level] = 100 + level;
            var growth = Enumerable.Range(0, 101).Select(level => new[] { level * 8 + 80, level + 16, level * 4 + 25, level * 5 + 25, level * 4 + 16, 3, 0, 0, 0, 0, 0, 0 }).ToArray();
            var actor = new BattleActorData { InstanceId = "A", Identity = 0, ExperienceThresholds = thresholds, AttributesByLevel = growth };
            BattleAbilityData finishing = Magic(399, -1000);
            finishing.Costs = Array.Empty<BattleCost>();
            BattleSession session = Session(scenario, new[] { finishing }, new[] { actor });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 399)), Is.True);
            Steps(session, 2);
            Complete(session);
            Assert.That(Actor(session, "A").Level, Is.EqualTo(expectedLevel));
            Assert.That(Actor(session, "A").Experience, Is.EqualTo(experience));
            if (expectedLevel > 1)
            {
                Assert.That(Actor(session, "A").MaxHp, Is.EqualTo(500 + (expectedLevel - 1) * 8));
                Assert.That(Actor(session, "A").Hp, Is.EqualTo(Actor(session, "A").MaxHp));
                Assert.That(Actor(session, "A").Sp, Is.EqualTo(Actor(session, "A").MaxSp));
                Assert.That(Actor(session, "A").Mp, Is.EqualTo(Actor(session, "A").MaxMp));
            }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void AbilityReplayAndFrameRatePreserveCostsInventoryStatusesAndRewards(int frameRate)
        {
            BattleScenario scenario = Scenario();
            scenario.Participants[0].Vitals[0] = 100;
            Learn(scenario.Participants[0], 301, 1);
            scenario.Participants.Add(Participant("B", BattleSide.Ally, BattleSlot.Fire));
            scenario.Items.Add(new BattleItem { Id = 6201, Count = 2 });
            BattleSession expected = Session(scenario, new[] { Magic(301, -1000), Item() });
            expected.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301));
            expected.Submit(new BattleCommand("B", BattleAction.Item, "A", 6201));
            Steps(expected, 167);
            Complete(expected);
            var setup = new BattleSetup(scenario);
            BattleSession replay = BattleReplay.Play(setup, new Catalog(), expected.Inputs, expected.Tick, new ReadyRules(), expected.Definitions);
            Assert.That(Trace(replay), Is.EqualTo(Trace(expected)));
            BattleSession actual = Session(scenario, new[] { Magic(301, -1000), Item() });
            actual.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301));
            actual.Submit(new BattleCommand("B", BattleAction.Item, "A", 6201));
            for (int index = 0; index < frameRate * 8 && actual.Status == BattleSessionStatus.Running; index++)
                actual.Advance(1.0 / frameRate);
            Assert.That(Trace(actual), Is.EqualTo(Trace(expected)));
            Assert.That(actual.Result.Items.Single().Count, Is.EqualTo(1));
            Assert.That(actual.Result.Actors.Single(actor => actor.Id == "A").Mp, Is.EqualTo(90));
        }
    }
}
