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

    public sealed class BattleRemainingSpecialsTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings, IBattleStageGeometry
        {
            private readonly BattleScenario _scenario;

            internal Catalog(BattleScenario scenario) => _scenario = scenario;

            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => new(0, 5, new[] { 2 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor == "S" ? 0 : 1;

            public int DamageDivisor(string actor) => 1;

            public BattleMovementPoint InitialHome(string actor) => FormationHome(actor, _scenario.Participants.Single(value => value.InstanceId == actor).Slot);

            public BattleMovementPoint FormationHome(string actor, BattleSlot slot) => new(100 * ((int)slot + 1), 10 * ((int)slot + 1), 50 * ((int)slot + 1));

            public BattleAttackPlan PlanAttack(string source, string target, BattleMovementPoint sourceHome, BattleMovementPoint targetHome) =>
                new(Attack(source, target), sourceHome, targetHome, targetHome);

            public IReadOnlyList<BattleMovementPlan> PlanFormationMove(string actor, BattleMovementPoint currentHome, BattleMovementPoint destination, bool passThroughMidpoint) =>
                Array.AsReadOnly(new[] { new BattleMovementPlan(BattleMovementKind.Run, currentHome, destination) });
        }

        private sealed class Rules : IBattleRules
        {
            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => actor.InstanceId == "S" ? BattleSession.FullGauge : 0;

            public int GaugePerStep(BattleParticipant actor) => 0;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1) => new(-1, false, false);
        }

        private sealed class WaitingStage : IBattleStageJob
        {
            public bool Advance(BattleStageContext context) => false;
        }

        private static BattleParticipant Participant(string id, BattleSide side, BattleSlot slot) => new()
        {
            InstanceId = id,
            ActorId = side == BattleSide.Ally ? 101 : 701,
            Side = side,
            Slot = slot,
            Attributes = new[] { 500, 100, 100, 100, 0, 3, 0, 0, 0, 0, 0, 0 },
            Vitals = new[] { 500, 100, 100 }
        };

        private static BattleSession Session(string variant, BattleAbilityData ability, bool enemySource = false, bool secondAlly = false, bool extraEnemy = false)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "fixture", DataFingerprint = "remaining-specials", Seed = 1 };
            scenario.Participants.Add(Participant("S", enemySource ? BattleSide.Enemy : BattleSide.Ally, BattleSlot.Water));
            scenario.Participants.Add(Participant("T", enemySource ? BattleSide.Ally : BattleSide.Enemy, BattleSlot.Water));
            if (secondAlly)
            {
                scenario.Participants.Add(Participant("Z", BattleSide.Ally, BattleSlot.Earth));
                scenario.Participants[^1].Vitals[0] = 200;
            }
            if (extraEnemy)
                scenario.Participants.Add(Participant("W", BattleSide.Enemy, BattleSlot.Fire));
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = ability.Id, Level = 4, Mastered = true });
            var definitions = new BattleDefinitions(variant, scenario.DataFingerprint,
                new[] { new BattleAbilityDefinition(ability) }, Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Enemy ? -1 : actor.InstanceId == "Z" ? 3 : 0
                })));
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(scenario), new Rules(), definitions);
            session.SetAutomatic(BattleSide.Enemy, false);
            return session;
        }

        private static object MutableActor(BattleSession session, string id) =>
            ((IDictionary)typeof(BattleSession).GetField("_byId", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(session))[id];

        private static void SetField(BattleSession session, string id, string name, object value)
        {
            object actor = MutableActor(session, id);
            actor.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).SetValue(actor, value);
        }

        private static void Resolve(BattleSession session, int skill)
        {
            int ended = session.Events.Count(value => value.Kind == BattleEventKind.ActionEnded && value.Source == "S");
            Assert.That(session.Submit(new BattleCommand("S", BattleAction.Skill, "T", skill)), Is.True);
            for (int tick = 0; tick < 10 && session.Events.Count(value => value.Kind == BattleEventKind.ActionEnded && value.Source == "S") == ended; tick++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running), session.Result?.Reason);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void Magic258DoesNotUseTechniqueMpAbsorption(string variant)
        {
            BattleSpecialHandler handler = BattleSpecialRules.AbilityHandler(variant, 258, false, true, 18);
            Assert.That(handler, Is.EqualTo(BattleSpecialHandler.GenericOnly));
            BattleSession session = Session(variant, new BattleAbilityData
            {
                Id = 258,
                IsMagic = true,
                Handler = handler,
                SpecialEffect = 18,
                Range = BattleTargetRange.OpponentSingle,
                Attributes = new[] { new BattleAttributeEffect(2, BattleValueMode.Absolute, -20) }
            });
            SetField(session, "S", "Mp", 50);
            Resolve(session, 258);
            Assert.That(session.Actors.Single(value => value.Id == "S").Mp, Is.EqualTo(50));
            Assert.That(session.Actors.Single(value => value.Id == "T").Mp, Is.LessThan(100));
            Assert.That(BattleSpecialRules.AbilityHandler(variant, 999, false, true, 18), Is.EqualTo(BattleSpecialHandler.None));
        }

        [TestCase(0, 2219737664u, new[] { 0, 5, 4, 3, 1, 2 })]
        [TestCase(1, 120733505u, new[] { 1, 0, 4, 3, 5, 2 })]
        [TestCase(2, 2316696642u, new[] { 4, 5, 3, 2, 1, 0 })]
        [TestCase(123, 1740263867u, new[] { 2, 1, 4, 3, 0, 5 })]
        [TestCase(-1, 23774527u, new[] { 4, 0, 1, 3, 2, 5 })]
        public void RandomFormationMatchesBothNativeSixtyFourDraws(int seed, uint finalSeed, int[] expected)
        {
            var random = new BattleRandom(seed);
            Assert.That(BattleSpecialRules.RandomFormation(random).Select(slot => (int)slot), Is.EqualTo(expected));
            Assert.That(random.State, Is.EqualTo(finalSeed));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void RandomFormationChangesHomeAndPositionByNativeIdentityWithoutGaugeCost(string variant)
        {
            var ability = new BattleAbilityData { Id = 263, IsMagic = true, Handler = BattleSpecialHandler.RandomFormation, Range = BattleTargetRange.OpponentSingle };
            BattleSession session = Session(variant, ability, enemySource: true, secondAlly: true);
            Resolve(session, 263);
            BattleActorSnapshot target = session.Actors.Single(value => value.Id == "T");
            BattleActorSnapshot zixuan = session.Actors.Single(value => value.Id == "Z");
            Assert.That((target.Slot, zixuan.Slot), Is.EqualTo((BattleSlot.Fire, BattleSlot.Thunder)));
            Assert.That(target.Position.X, Is.EqualTo(200));
            Assert.That(target.Home.X, Is.EqualTo(200));
            Assert.That(zixuan.Position.X, Is.EqualTo(400));
            Assert.That(target.Gauge, Is.EqualTo(0));
            Assert.That(session.Events.Last(value => value.Kind == BattleEventKind.FormationChanged).RandomState, Is.EqualTo(120733505u));
            Assert.That(session.Events.Any(value => value.Kind == BattleEventKind.MovementStarted), Is.False);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void RandomFormationFailureDoesNotDrawOrMoveOtherAllies(string variant)
        {
            var ability = new BattleAbilityData { Id = 263, IsMagic = true, Handler = BattleSpecialHandler.RandomFormation, Range = BattleTargetRange.OpponentSingle };
            BattleSession session = Session(variant, ability, enemySource: true, secondAlly: true);
            // The magic target stays at home; another valid allied role is away.
            SetField(session, "Z", "Position", new BattleMovementPoint(9999, 0, 0));
            Resolve(session, 263);
            Assert.That(session.Events.Any(value => value.Kind == BattleEventKind.FormationChanged), Is.False);
            Assert.That(session.Events.Last().RandomState, Is.EqualTo(1u));
            Assert.That(session.Actors.Single(value => value.Id == "T").Slot, Is.EqualTo(BattleSlot.Water));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void RandomFormationRepeatsPerTargetAndRespectsAnotherAlliedStageTask(string variant)
        {
            var ability = new BattleAbilityData { Id = 263, IsMagic = true, Handler = BattleSpecialHandler.RandomFormation, Range = BattleTargetRange.OpponentAll };
            BattleSession session = Session(variant, ability, enemySource: true, secondAlly: true);
            Resolve(session, 263);
            Assert.That(session.Events.Count(value => value.Kind == BattleEventKind.FormationChanged), Is.EqualTo(4));
            Assert.That(session.Events.Last(value => value.Kind == BattleEventKind.FormationChanged).RandomState, Is.EqualTo(3168190081u));

            BattleSession busy = Session(variant, ability, enemySource: true, secondAlly: true);
            var stage = (BattleStageScheduler)typeof(BattleSession).GetField("_stage", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(busy);
            Assert.That(stage.TryAdd("Z", "native owned task", new WaitingStage(), 0, out _), Is.True);
            Resolve(busy, 263);
            Assert.That(busy.Events.Any(value => value.Kind == BattleEventKind.FormationChanged), Is.False);
            Assert.That(busy.Events.Last().RandomState, Is.EqualTo(1u));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void RandomFormationForcesFormationPanelClosedWithoutSpendingItsControllersGauge(string variant)
        {
            var ability = new BattleAbilityData { Id = 263, IsMagic = true, Handler = BattleSpecialHandler.RandomFormation, Range = BattleTargetRange.OpponentSingle };
            BattleSession session = Session(variant, ability, enemySource: true, secondAlly: true);
            Assert.That(session.Submit(new BattleCommand("S", BattleAction.Skill, "T", 263)), Is.True);
            SetField(session, "T", "Ready", true);
            SetField(session, "T", "Gauge", BattleSession.FullGauge);
            Assert.That(session.BeginFormation("T"), Is.True);
            for (int tick = 0; tick < 10 && session.ActiveFormationController != null; tick++)
                session.Step();
            Assert.That(session.ActiveFormationController, Is.Null);
            Assert.That(session.Actors.Single(value => value.Id == "T").Gauge, Is.EqualTo(BattleSession.FullGauge));
            Assert.That(session.Events.Single(value => value.Kind == BattleEventKind.FormationClosed).Detail, Does.Contain("no gauge cost"));
        }

        [TestCase("PAL3", 84006, 2, 32, true)]
        [TestCase("PAL3A", 8148, 4, 33, true)]
        [TestCase("PAL3A", 8151, 5, 34, true)]
        [TestCase("PAL3A", 84006, 2, 32, false)]
        [TestCase("PAL3", 8148, 4, 33, false)]
        [TestCase("PAL3A", 8151, 0, 34, false)]
        public void EquipmentDefaultDispatchDoesNotInventNamedPassives(string variant, int id, int slot, int special, bool supported) =>
            Assert.That(BattleSpecialRules.SupportsEquipmentSpecial(variant, id, slot, special), Is.EqualTo(supported));

        [TestCase(19, 1, 0)]
        [TestCase(20, 1, -1)]
        [TestCase(999, 2, -99)]
        [TestCase(65535, 3, -9830)]
        [TestCase(1000000, 3, -150000)]
        [TestCase(int.MaxValue, 3, -322122559)]
        [TestCase(100, 4, 0)]
        public void WormUsesPublishedFloatConstantBeforeIntegerTruncation(int maximumHp, int round, int expected) =>
            Assert.That(BattleSpecialRules.AbsorbHpWormRequest(maximumHp, round), Is.EqualTo(expected));

        [Test]
        public void WormRunsAfterStatesForThreeRoundsAndAlwaysRestoresZixuan()
        {
            BattleSpecialHandler handler = BattleSpecialRules.AbilityHandler("PAL3", 4402, false, false, 19);
            Assert.That(handler, Is.EqualTo(BattleSpecialHandler.AbsorbHpWorm));
            Assert.That(BattleSpecialRules.AbilityHandler("PAL3A", 4402, false, false, 19), Is.EqualTo(BattleSpecialHandler.None));
            BattleSession session = Session("PAL3", new BattleAbilityData { Id = 4402, Handler = handler, Range = BattleTargetRange.OpponentSingle }, secondAlly: true);
            Resolve(session, 4402);
            Assert.That(session.AbsorbHpWormRound("T"), Is.EqualTo(0));
            SetField(session, "T", "Guard", true);
            for (int round = 1; round <= 4; round++)
            {
                SetField(session, "T", "Rounds", round);
                session.Step();
                Assert.That(session.AbsorbHpWormRound("T"), Is.EqualTo(round == 4 ? (int?)null : round));
            }
            Assert.That(session.Actors.Single(value => value.Id == "T").Hp, Is.EqualTo(426));
            Assert.That(session.Actors.Single(value => value.Id == "Z").Hp, Is.EqualTo(350));
            Assert.That(session.Actors.Single(value => value.Id == "S").Hp, Is.EqualTo(500));
            Assert.That(session.Events.Where(value => value.Detail == "original absorb HP worm recovery").Select(value => value.RequestedDelta), Is.EqualTo(new[] { 25, 50, 75, 0 }));
        }

        [Test]
        public void WormReapplicationRestartsItsCounterAndInvisibleTargetStillFeedsZixuan()
        {
            BattleSession session = Session("PAL3", new BattleAbilityData { Id = 4402, Handler = BattleSpecialHandler.AbsorbHpWorm, Range = BattleTargetRange.OpponentSingle }, secondAlly: true);
            Resolve(session, 4402);
            SetField(session, "T", "Rounds", 1);
            session.Step();
            Assert.That(session.AbsorbHpWormRound("T"), Is.EqualTo(1));
            SetField(session, "S", "Ready", true);
            SetField(session, "S", "Gauge", BattleSession.FullGauge);
            Resolve(session, 4402);
            Assert.That(session.AbsorbHpWormRound("T"), Is.EqualTo(0));
            typeof(BattleSession).GetMethod("ApplyState", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(session,
                new[] { MutableActor(session, "T"), BattleState.Invisible, false, (object)100, false, null, 0L, true });
            SetField(session, "T", "Rounds", 2);
            session.Step();
            Assert.That(session.Actors.Single(value => value.Id == "T").Hp, Is.EqualTo(475));
            Assert.That(session.Actors.Single(value => value.Id == "Z").Hp, Is.EqualTo(250));
        }

        [Test]
        public void WormKeepsItsMarkerAndProcessesAPendingDeadRolesRound()
        {
            BattleSession session = Session("PAL3", new BattleAbilityData { Id = 4402, Handler = BattleSpecialHandler.AbsorbHpWorm, Range = BattleTargetRange.OpponentSingle }, secondAlly: true, extraEnemy: true);
            Resolve(session, 4402);
            SetField(session, "T", "Hp", 0);
            SetField(session, "T", "Alive", false);
            SetField(session, "T", "ModelAlive", false);
            SetField(session, "T", "Rounds", 1);
            session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running));
            Assert.That(session.AbsorbHpWormRound("T"), Is.EqualTo(1));
            Assert.That(session.Actors.Single(value => value.Id == "T").Hp, Is.EqualTo(0));
            Assert.That(session.Actors.Single(value => value.Id == "Z").Hp, Is.EqualTo(225));
        }
    }
}
