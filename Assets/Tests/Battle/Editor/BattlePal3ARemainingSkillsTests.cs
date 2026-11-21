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

    public sealed class BattlePal3ARemainingSkillsTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings
        {
            public AttackTiming Attack(string source, string target) => new(0, 3, new[] { 1 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => new(0, 3, new[] { 1 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 1000;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor == "S" ? 4 : actor == "T" ? 0 : 6;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class ReadyRules : IBattleRules
        {
            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => actor.InstanceId == "S" ? BattleSession.FullGauge : 0;

            public int GaugePerStep(BattleParticipant actor) => 0;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1) => new(-1, false, false);
        }

        private static BattleAbilityData Ability(int id) => id switch
        {
            205 or 273 => new BattleAbilityData
            {
                Id = id,
                IsMagic = true,
                SkillTypeId = 1,
                Handler = BattleSpecialHandler.GenericOnly,
                SpecialEffect = id == 273 ? 35 : 0,
                Range = id == 205 ? BattleTargetRange.OpponentSingle : BattleTargetRange.OpponentAll,
                Costs = new[] { new BattleCost(2, BattleValueMode.Absolute, id == 205 ? 3 : 30) }
            },
            4503 => new BattleAbilityData
            {
                Id = id,
                SuccessRate = 50,
                Range = BattleTargetRange.OwnSingle,
                States = new[] { new BattleStatusEffect(BattleState.Death, true) },
                Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Percentage, 100), new BattleAttributeEffect(2, BattleValueMode.Percentage, 100) },
                Costs = new[] { new BattleCost(1, BattleValueMode.Absolute, 30) }
            },
            4505 => new BattleAbilityData
            {
                Id = id,
                SuccessRate = 50,
                Range = BattleTargetRange.OpponentSingle,
                SpecialEffect = 37,
                Handler = BattleSpecialHandler.GenericOnly,
                States = new[] { new BattleStatusEffect(BattleState.Death, false) },
                Costs = new[] { new BattleCost(1, BattleValueMode.Absolute, 25) }
            },
            4507 => new BattleAbilityData
            {
                Id = id,
                SuccessRate = 100,
                Range = BattleTargetRange.OpponentAll,
                Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -1000) },
                Costs = new[] { new BattleCost(1, BattleValueMode.Absolute, 60), new BattleCost(0, BattleValueMode.Percentage, 50) }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(id))
        };

        private static BattleParticipant Participant(string id, BattleSide side, int hp) => new()
        {
            InstanceId = id,
            ActorId = id == "S" ? 107 : side == BattleSide.Ally ? 101 : 701,
            Side = side,
            Slot = id == "S" ? BattleSlot.Water : id == "T" ? BattleSlot.Fire : BattleSlot.Earth,
            Level = 10,
            Attributes = new[] { 5000, 100, 200, 10, 10, 3, id == "S" ? 9999 : 0, 0, 0, 0, 0, 0 },
            Vitals = new[] { hp, 100, id == "T" ? 0 : 200 }
        };

        private static BattleSession Session(int id, int seed = 1, bool boss = false, bool targetAlly = false, int sourceHp = 1000, int targetHp = 5000)
        {
            var scenario = new BattleScenario { Variant = "PAL3A", Scene = "portable", DataFingerprint = "remaining-skills", Seed = seed };
            BattleParticipant source = Participant("S", BattleSide.Ally, sourceHp);
            source.Skills.Add(new BattleSkill { Id = id, Level = 4, Mastered = true });
            scenario.Participants.Add(source);
            scenario.Participants.Add(Participant("T", targetAlly ? BattleSide.Ally : BattleSide.Enemy, targetHp));
            scenario.Participants.Add(Participant("E", BattleSide.Enemy, 5000));
            var definitions = new BattleDefinitions("PAL3A", "remaining-skills", new[] { new BattleAbilityDefinition(Ability(id)) }, Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.InstanceId == "S" ? 4 : actor.Side == BattleSide.Ally ? 0 : -1,
                    Boss = boss && actor.InstanceId == "T",
                    Equipment = actor.InstanceId == "S" ? new[] { new BattleEquipmentDefinition(new BattleEquipmentData { Id = 1, Slot = 4, TechniqueSuccessBonus = 500 }) } : Array.Empty<BattleEquipmentDefinition>()
                })));
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), new ReadyRules(), definitions);
            session.SetAutomatic(BattleSide.Enemy, false);
            session.SetAutomatic(BattleSide.Ally, false);
            return session;
        }

        private static BattleActorSnapshot Actor(BattleSession session, string id) => session.Actors.Single(actor => actor.Id == id);

        private static object MutableActor(BattleSession session, string id) => ((IDictionary)typeof(BattleSession).GetField("_byId", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(session))[id];

        private static void Resolve(BattleSession session, int id)
        {
            Assert.That(session.Submit(new BattleCommand("S", BattleAction.Skill, "T", id)), Is.True);
            int limit = 400;
            while (session.Status == BattleSessionStatus.Running && Actor(session, "S").CurrentAction != null && limit-- > 0)
                session.Step();
            Assert.That(session.Status, Is.Not.EqualTo(BattleSessionStatus.Faulted), session.Result?.Reason);
        }

        [TestCase(4503, 1, true)]
        [TestCase(4503, 200, false)]
        [TestCase(4505, 1, true)]
        [TestCase(4505, 200, false)]
        public void LiteralFiftyPercentIgnoresLuckAndEquipmentAndConsumesOneDraw(int id, int seed, bool succeeds)
        {
            BattleSession session = Session(id, seed, targetAlly: id == 4503, targetHp: id == 4503 ? 0 : 5000);
            Resolve(session, id);
            uint expectedSeed = unchecked((uint)seed * 214013 + 2531011);
            BattleEvent payment = session.Events.Single(entry => entry.Source == "S" && entry.Detail == "skill cost " + id && entry.Resource == 1);
            Assert.That(payment.RandomState, Is.EqualTo(expectedSeed));
            Assert.That(Actor(session, "S").Sp, Is.EqualTo(id == 4503 ? 70 : 75));
            Assert.That(Actor(session, "T").Hp, Is.EqualTo(id == 4503 ? succeeds ? 5000 : 0 : succeeds ? 0 : 5000));
            if (id == 4503)
                Assert.That(Actor(session, "T").Mp, Is.EqualTo(succeeds ? 200 : 0));
        }

        [TestCase(1)]
        [TestCase(200)]
        public void DeathSkillRejectsBossBeforeDrawingOrApplyingDeathButStillPaysSp(int seed)
        {
            BattleSession session = Session(4505, seed, boss: true);
            Resolve(session, 4505);
            Assert.That(Actor(session, "T").Hp, Is.EqualTo(5000));
            Assert.That(Actor(session, "S").Sp, Is.EqualTo(75));
            Assert.That(session.Events.Single(entry => entry.Detail.StartsWith("original boss restriction", StringComparison.Ordinal)).RandomState, Is.EqualTo(unchecked((uint)seed)));
            Assert.That(session.Events.Single(entry => entry.Source == "S" && entry.Detail == "skill cost 4505").RandomState, Is.EqualTo(unchecked((uint)seed)));
        }

        [TestCase(49, false)]
        [TestCase(50, true)]
        [TestCase(51, true)]
        [TestCase(1000, true)]
        public void HpCostEligibilityAndPaymentUseFiftyAbsoluteHpInsteadOfHalfCurrentHp(int hp, bool allowed)
        {
            BattleSession session = Session(4507, sourceHp: hp);
            var command = new BattleCommand("S", BattleAction.Skill, "T", 4507);
            BattleCostEstimate cost = session.GetCostEstimate(command).Single(value => value.Resource == 0);
            Assert.That(cost.Required, Is.EqualTo(50));
            Assert.That(cost.Payable, Is.EqualTo(50));
            Assert.That(session.CanSubmit(command) == null, Is.EqualTo(allowed));
            if (!allowed)
                return;
            Resolve(session, 4507);
            Assert.That(Actor(session, "S").Hp, Is.EqualTo(Math.Max(0, hp - 50)));
            Assert.That(Actor(session, "S").Sp, Is.EqualTo(40));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Interrupted && entry.Source == "S"), Is.False);
            Assert.That(Actor(session, "S").Pose, Is.Not.EqualTo(BattlePose.Hurt));
            Assert.That(session.Events.Count(entry => entry.Detail == "skill cost 4507" && entry.Resource == 0), Is.EqualTo(1));
        }

        [TestCase(205, 197)]
        [TestCase(273, 170)]
        public void EmptyMagicRecordsOnlyPayTheirOriginalCostAndImproveProficiency(int id, int expectedMp)
        {
            BattleSession session = Session(id);
            Resolve(session, id);
            Assert.That(Actor(session, "T").Hp, Is.EqualTo(5000));
            Assert.That(Actor(session, "E").Hp, Is.EqualTo(5000));
            Assert.That(Actor(session, "S").Mp, Is.EqualTo(expectedMp));
            Assert.That(Actor(session, "S").Skills.Single().Uses, Is.EqualTo(1));
            Assert.That(session.Events.Any(entry => entry.Target is "T" or "E" && entry.Kind is BattleEventKind.Hit or BattleEventKind.StatusAdded or BattleEventKind.StatusRemoved), Is.False);
        }

        [TestCase("PAL3A", 205, true, 0, BattleSpecialHandler.GenericOnly)]
        [TestCase("PAL3A", 273, true, 35, BattleSpecialHandler.GenericOnly)]
        [TestCase("PAL3A", 4505, false, 37, BattleSpecialHandler.GenericOnly)]
        [TestCase("PAL3", 205, true, 0, BattleSpecialHandler.None)]
        [TestCase("PAL3A", 274, true, 35, BattleSpecialHandler.None)]
        public void VerifiedEmptyAndMarkerRecordsDoNotInventSpecialEffects(string variant, int id, bool magic, int special, BattleSpecialHandler expected)
        {
            Assert.That(BattleSpecialRules.AbilityHandler(variant, id, false, magic, special), Is.EqualTo(expected));
        }
    }
}
