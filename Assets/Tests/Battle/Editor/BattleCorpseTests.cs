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

    public sealed class BattleCorpseTests
    {
        private sealed class Fixture : IBattleCatalog, IBattleAbilityTimings, IBattleRules
        {
            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => Attack(actor, actor);

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => 0;

            public int DamageDivisor(string actor) => 1;

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) =>
                actor.InstanceId == "S" ? BattleSession.FullGauge : 0;

            public int GaugePerStep(BattleParticipant actor) => 0;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) => new(-1000, false, false);
        }

        private static BattleSession Session(string variant, bool magic = true, bool item = false, int requiredSkill = 207,
            BattleSlot sourceSlot = BattleSlot.Water, bool deadCenter = false)
        {
            BattleParticipant Participant(string id, BattleSide side, BattleSlot slot, int hp) => new()
            {
                InstanceId = id,
                ActorId = side == BattleSide.Ally ? id == "S" ? 101 : variant == "PAL3" ? 102 : 104 : 701,
                Side = side,
                Slot = slot,
                Attributes = new[] { 100, 100, 100, 1000, 0, 3, 0, 0, 0, 0, 0, 0 },
                Vitals = new[] { hp, 100, 100 }
            };
            var source = Participant("S", BattleSide.Ally, sourceSlot, 100);
            source.Skills.Add(new BattleSkill { Id = 207, Mastered = true, Level = 4 });
            var scenario = new BattleScenario
            {
                Variant = variant,
                Scene = "corpse-fixture",
                DataFingerprint = "corpse-fixture",
                Seed = 123,
                Participants = new List<BattleParticipant> { source, Participant("T", BattleSide.Enemy, BattleSlot.Water, 1) }
            };
            if (deadCenter)
                scenario.Participants.Add(Participant("C", BattleSide.Ally, BattleSlot.Center, 0));
            if (item)
                scenario.Items.Add(new BattleItem { Id = 207, Count = 1 });
            var ability = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = 207,
                Name = "corpse-trigger fixture",
                IsMagic = magic && !item,
                IsItem = item,
                ItemType = 2,
                SuccessRate = 100,
                SkillTypeId = magic ? 1 : 0,
                Range = BattleTargetRange.OpponentSingle,
                Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -1000) },
                Costs = item ? Array.Empty<BattleCost>() : new[] { new BattleCost(2, BattleValueMode.Absolute, 10) }
            });
            var definitions = new BattleDefinitions(variant, scenario.DataFingerprint,
                item ? Array.Empty<BattleAbilityDefinition>() : new[] { ability }, item ? new[] { ability } : Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Enemy ? -1 : actor.InstanceId == "S" ? 0 : 1,
                    CorpseItem = actor.InstanceId == "T" ? 9001 : 0,
                    CorpseSkill = requiredSkill,
                    CorpsePercent = 100,
                    LootItem = actor.InstanceId == "T" ? 9001 : 0,
                    LootCount = 2
                })));
            var fixture = new Fixture();
            var session = new BattleSession(new BattleSetup(scenario), fixture, fixture, definitions);
            session.SetAutomatic(BattleSide.Enemy, false);
            var command = new BattleCommand("S", item ? BattleAction.Item : BattleAction.Skill, "T", 207);
            Assert.That(session.Submit(command), Is.True, session.CanSubmit(command));
            for (int tick = 0; tick < 2000 && session.Status == BattleSessionStatus.Running; tick++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed), session.Result?.Reason);
            return session;
        }

        [TestCase("PAL3", true)]
        [TestCase("PAL3A", true)]
        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        public void KillingMagicOrTechniqueRecordsTheCorpseBeforeCostAndCombinesItWithOrdinaryLoot(string variant, bool magic)
        {
            BattleSession session = Session(variant, magic);
            BattleEvent corpse = session.Events.Single(entry => entry.Kind == BattleEventKind.CorpseObtained);
            BattleEvent hit = session.Events.Last(entry => entry.Kind == BattleEventKind.Hit && entry.Target == "T");
            var random = new BattleRandom(unchecked((int)hit.RandomState), variant);
            Assert.That(random.Probability(100), Is.True);
            Assert.That(corpse.RandomState, Is.EqualTo(random.State));
            Assert.That(session.Events.First(entry => entry.Kind == BattleEventKind.ResourceChanged && entry.Resource == 2).RandomState,
                Is.EqualTo(corpse.RandomState));
            Assert.That(session.Result.Actors.Single(actor => actor.Id == "T").CorpseItem, Is.EqualTo(9001));
            Assert.That(session.Result.Loot.Single().Count, Is.EqualTo(3));
            Assert.That(session.Result.Items.Single(row => row.Id == 9001).Count, Is.EqualTo(3));
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        [TestCase("PAL3", true)]
        [TestCase("PAL3A", true)]
        public void EveryValidAllyAtCenterBlocksTheCorpseIncludingADeadAlly(string variant, bool deadCenter)
        {
            BattleSession session = Session(variant, sourceSlot: deadCenter ? BattleSlot.Water : BattleSlot.Center, deadCenter: deadCenter);
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.CorpseObtained), Is.False);
            Assert.That(session.Result.Loot.Single().Count, Is.EqualTo(2));
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        [TestCase("PAL3", true)]
        [TestCase("PAL3A", true)]
        public void WrongSkillAndLethalItemsCannotProduceTheCorpse(string variant, bool item)
        {
            BattleSession session = Session(variant, item: item, requiredSkill: item ? 207 : 208);
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.CorpseObtained), Is.False);
            Assert.That(session.Result.Loot.Single().Count, Is.EqualTo(2));
        }
    }
}
