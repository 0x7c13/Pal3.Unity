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

    public sealed class BattleSpecialMechanicsTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings
        {
            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public AttackTiming Ability(string source, BattleAbilityDefinition ability) => new(0, 5, new[] { 2 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => 0;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class ReadyRules : IBattleRules
        {
            private readonly BattleBasicRules _original = new();

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => BattleSession.FullGauge;

            public int GaugePerStep(BattleParticipant actor) => _original.GaugePerStep(actor);

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1)
                => _original.Hit(source, target, guarding, random, targetAtHome, damageDivisor);
        }

        private static BattleParticipant Participant(string id, BattleSide side, BattleSlot slot = BattleSlot.Water, int hp = 500) => new()
        {
            InstanceId = id,
            ActorId = side == BattleSide.Ally ? 101 : 701,
            Side = side,
            Slot = slot,
            Attributes = new[] { 500, 100, 100, 100, 0, 3, 0, 0, 0, 0, 0, 0 },
            Vitals = new[] { hp, 100, 100 }
        };

        private static BattleScenario Scenario(string variant = "PAL3")
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "fixture", DataFingerprint = "special", Seed = 1 };
            scenario.Participants.Add(Participant("A", BattleSide.Ally));
            scenario.Participants.Add(Participant("E", BattleSide.Enemy));
            return scenario;
        }

        private static BattleAbilityData Technique(string variant, BattleSpecialHandler handler, int attribute = 0, int value = -20)
        {
            int id = handler switch
            {
                BattleSpecialHandler.HalfActualHpAbsorb => variant == "PAL3" ? 4201 : 4301,
                BattleSpecialHandler.ReturnedMpAbsorb => variant == "PAL3" ? 4304 : 4504,
                BattleSpecialHandler.Steal => 4101,
                BattleSpecialHandler.MoneyRecovery => variant == "PAL3" ? 4102 : 4106,
                _ => variant == "PAL3" ? 4305 : 4206
            };
            return new BattleAbilityData
            {
                Id = id,
                Handler = handler,
                SkillTypeId = 0,
                Range = BattleTargetRange.OpponentSingle,
                Attributes = value == 0 ? Array.Empty<BattleAttributeEffect>() : new[] { new BattleAttributeEffect(attribute, BattleValueMode.Absolute, value) }
            };
        }

        private static void Learn(BattleParticipant actor, int id, int level = 4) => actor.Skills.Add(new BattleSkill { Id = id, Level = level, Mastered = true });

        private static BattleDefinitions Definitions(BattleScenario scenario, IEnumerable<BattleAbilityData> abilities, params BattleActorData[] actors)
        {
            BattleAbilityDefinition[] all = abilities.Select(data => new BattleAbilityDefinition(data)).ToArray();
            var overrides = actors.ToDictionary(actor => actor.InstanceId);
            return new BattleDefinitions(scenario.Variant, scenario.DataFingerprint, all.Where(ability => !ability.IsItem), all.Where(ability => ability.IsItem),
                scenario.Participants.Select(actor => new BattleActorDefinition(overrides.GetValueOrDefault(actor.InstanceId) ??
                    new BattleActorData { InstanceId = actor.InstanceId, Identity = actor.Side == BattleSide.Ally ? 0 : -1 })));
        }

        private static BattleSession Session(BattleScenario scenario, IEnumerable<BattleAbilityData> abilities, params BattleActorData[] actors)
        {
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), new ReadyRules(), Definitions(scenario, abilities, actors));
            session.SetAutomatic(BattleSide.Enemy, false);
            return session;
        }

        private static BattleActorSnapshot Actor(BattleSession session, string id) => session.Actors.Single(actor => actor.Id == id);

        private static void Steps(BattleSession session, int count = 2)
        {
            for (int index = 0; index < count; index++)
                session.Step();
            Assert.That(session.Status, Is.Not.EqualTo(BattleSessionStatus.Faulted), session.Result?.Reason);
        }

        [TestCase("PAL3", 98, 7, 1, 99, 7)]
        [TestCase("PAL3A", 98, 7, 1, 99, 7)]
        [TestCase("PAL3", 99, 7, 1, 99, 7)]
        [TestCase("PAL3A", 99, 7, 1, 99, 7)]
        [TestCase("PAL3", 90, 2, 1, 92, 2)]
        [TestCase("PAL3A", 90, 2, 1, 92, 2)]
        [TestCase("PAL3", int.MaxValue, 1, 1, int.MaxValue, 1)]
        [TestCase("PAL3A", int.MaxValue, 1, 1, int.MaxValue, 1)]
        [TestCase("PAL3", 0, 150, 20, 104, 104)]
        [TestCase("PAL3A", 0, 150, 20, 104, 104)]
        public void InventoryCapsGainsWithoutRemovingExistingStockOrReducingNewEntries(string variant, int before, int pool, int seed, int expected, int requested)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Seed = seed;
            if (before > 0)
                scenario.Items.Add(new BattleItem { Id = 6201, Count = before });
            BattleAbilityData ability = Technique(variant, BattleSpecialHandler.Steal, value: 0);
            Learn(scenario.Participants[0], ability.Id);
            BattleSession session = Session(scenario, new[] { ability },
                new BattleActorData { InstanceId = "E", StealableItemId = 6201, StealableItemCount = pool });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", ability.Id)), Is.True);
            Steps(session);
            Assert.That(session.Inventory.Single().Count, Is.EqualTo(expected));
            Assert.That(session.StealPools.Single(value => value.Actor == "E").ItemCount, Is.EqualTo(pool - requested));
            BattleEvent gain = session.Events.Single(entry => entry.Kind == BattleEventKind.ItemChanged);
            Assert.That((gain.RequestedDelta, gain.ActualDelta), Is.EqualTo((requested, expected - before)));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void HalfHpAbsorptionUsesActualOverkillLossAndRunsBeforeLethalSettlement(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[0].Vitals[0] = 400;
            scenario.Participants[1].Vitals[0] = 5;
            BattleAbilityData ability = Technique(variant, BattleSpecialHandler.HalfActualHpAbsorb);
            ability.Costs = new[] { new BattleCost(1, BattleValueMode.Absolute, 10) };
            Learn(scenario.Participants[0], ability.Id);
            BattleSession session = Session(scenario, new[] { ability });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", ability.Id)), Is.True);
            Steps(session);
            Assert.That(Actor(session, "A").Hp, Is.EqualTo(402));
            Assert.That(Actor(session, "A").Sp, Is.EqualTo(90));
            for (int index = 0; index < 1000 && session.Status == BattleSessionStatus.Running; index++)
                session.Step();
            Assert.That(session.Result.Winner, Is.EqualTo(BattleSide.Ally));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void MpAbsorptionUsesTheVariantsDisplayedResourceReturn(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants.ForEach(actor => actor.Vitals[2] = 0);
            BattleAbilityData ability = Technique(variant, BattleSpecialHandler.ReturnedMpAbsorb, 2, -150);
            Learn(scenario.Participants[0], ability.Id);
            BattleSession session = Session(scenario, new[] { ability });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", ability.Id)), Is.True);
            Steps(session);
            Assert.That(Actor(session, "A").Mp, Is.EqualTo(variant == "PAL3" ? 100 : 0));
            Assert.That(Actor(session, "E").Mp, Is.Zero);
            // Original full call chain: PAL3A showNum clamps the returned loss to zero;
            // PAL3 does not display target MP and returns the warped request.
            Assert.That(session.Events.Single(entry => entry.Detail == "native returned MP absorption").RequestedDelta,
                Is.EqualTo(variant == "PAL3" ? 161 : 0));
        }

        [TestCase("PAL3", 0, 60)] // 洒金笺 charges for the last selected ally when the higher slot is dead.
        [TestCase("PAL3", 490, 110)]
        [TestCase("PAL3A", 490, 110)]
        [TestCase("PAL3", 500, 120)]
        [TestCase("PAL3A", 500, 120)]
        public void MoneyRecoveryChecksTheWholePartyButOnlyChargesTheLastReturnedRecovery(string variant, int lastHp, int moneyAfter)
        {
            BattleScenario scenario = Scenario(variant);
            int required = lastHp == 0 ? 80 : 120;
            scenario.Money = required - 1;
            scenario.Participants[0].Level = 10;
            scenario.Participants[0].Vitals[0] = 400;
            scenario.Participants.Add(Participant("B", BattleSide.Ally, BattleSlot.Fire, lastHp));
            BattleAbilityData ability = Technique(variant, BattleSpecialHandler.MoneyRecovery, 0, 80);
            ability.Range = BattleTargetRange.OwnAll;
            ability.Costs = new[] { new BattleCost(10, BattleValueMode.Absolute, 80) };
            Learn(scenario.Participants[0], ability.Id);
            var actors = new[] { new BattleActorData { InstanceId = "A", Identity = 0 }, new BattleActorData { InstanceId = "B", Identity = 1 } };
            BattleSession denied = Session(scenario, new[] { ability }, actors);
            Assert.That(denied.CanSubmit(new BattleCommand("A", BattleAction.Skill, null, ability.Id)), Does.Contain(required.ToString()));
            scenario.Money = 120;
            BattleSession session = Session(scenario, new[] { ability }, actors);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, null, ability.Id)), Is.True);
            Steps(session);
            Assert.That(Actor(session, "A").Hp, Is.EqualTo(460));
            Assert.That(session.Money, Is.EqualTo(moneyAfter));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void MoneyThrowRequiresAndPaysRawThreeThousandInsteadOfTheRedundantOneThousandCheck(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            BattleAbilityData ability = Technique(variant, BattleSpecialHandler.MoneyThrow);
            ability.Costs = new[] { new BattleCost(10, BattleValueMode.Absolute, 3000) };
            Learn(scenario.Participants[0], ability.Id);
            scenario.Money = 2999;
            Assert.That(Session(scenario, new[] { ability }).CanSubmit(new BattleCommand("A", BattleAction.Skill, "E", ability.Id)), Does.Contain("3000"));
            scenario.Money = 3000;
            BattleSession session = Session(scenario, new[] { ability });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", ability.Id)), Is.True);
            Steps(session);
            Assert.That(session.Money, Is.Zero);
        }

        [TestCase("PAL3", 1000)]
        [TestCase("PAL3A", 3000)]
        public void ModifiedMoneyThrowCostKeepsTheReleasedVariantSpecificMinimum(string variant, int minimum)
        {
            BattleScenario scenario = Scenario(variant);
            BattleAbilityData ability = Technique(variant, BattleSpecialHandler.MoneyThrow);
            ability.Costs = new[] { new BattleCost(10, BattleValueMode.Absolute, 500) };
            Learn(scenario.Participants[0], ability.Id);
            scenario.Money = minimum - 1;
            Assert.That(Session(scenario, new[] { ability }).Submit(new BattleCommand("A", BattleAction.Skill, "E", ability.Id)), Is.False);
            scenario.Money = minimum;
            BattleSession session = Session(scenario, new[] { ability });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", ability.Id)), Is.True);
            Steps(session);
            Assert.That(session.Money, Is.EqualTo(minimum - 500));
        }

        [TestCase("PAL3", false, 0)]
        [TestCase("PAL3A", false, 0)]
        [TestCase("PAL3", true, 100)]
        [TestCase("PAL3A", true, 100)]
        public void PoisonDragonGallKillsOnlyTheUnpoisonedAndOtherwiseRemovesPoison(string variant, bool poisoned, int hpAfter)
        {
            BattleScenario scenario = Scenario(variant);
            var target = Participant("B", BattleSide.Ally, BattleSlot.Fire, 100);
            if (poisoned)
                target.Statuses.Add(new BattleStatus { Id = (int)BattleState.PoisonWater, Rounds = 5 });
            scenario.Participants.Add(target);
            scenario.Items.Add(new BattleItem { Id = 6230, Count = 1 });
            var item = new BattleAbilityData
            {
                Id = 6230,
                IsItem = true,
                Handler = BattleSpecialHandler.PoisonDragonGall,
                ItemType = 1,
                Range = BattleTargetRange.OwnSingle,
                States = Enumerable.Range(0, 5).Select(state => new BattleStatusEffect((BattleState)state, true)).ToArray()
            };
            BattleSession session = Session(scenario, new[] { item });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "B", 6230)), Is.True);
            Steps(session);
            Assert.That(Actor(session, "B").Hp, Is.EqualTo(hpAfter));
            Assert.That(Actor(session, "B").States, Is.Empty);
            Assert.That(session.Inventory, Is.Empty);
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        [TestCase("PAL3", true)]
        [TestCase("PAL3A", true)]
        public void RandomPoisonConsumesOneRollOnlyWhenNoPoisonOrResistanceBlocksIt(string variant, bool alreadyPoisoned)
        {
            BattleScenario scenario = Scenario(variant);
            if (alreadyPoisoned)
                scenario.Participants[1].Statuses.Add(new BattleStatus { Id = (int)BattleState.PoisonWater, Rounds = 5 });
            var ability = new BattleAbilityData { Id = 254, IsMagic = true, Handler = BattleSpecialHandler.RandomPoison, Range = BattleTargetRange.OpponentSingle };
            Learn(scenario.Participants[0], ability.Id);
            BattleSession session = Session(scenario, new[] { ability });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 254)), Is.True);
            Steps(session);
            Assert.That(Actor(session, "E").States.Single().State, Is.EqualTo(alreadyPoisoned ? BattleState.PoisonWater : BattleState.PoisonThunder));
            Assert.That(session.Events.Last().RandomState, Is.EqualTo(alreadyPoisoned ? 1u : 2745024u));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void TreasureConsumesItsIndependentUseAtStartWithoutDeletingInventoryAndCancellationDoesNotRefund(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Items.Add(new BattleItem { Id = 6119, Count = 1 });
            scenario.TreasureUses.Add(new BattleItem { Id = 6119, Count = 1 });
            var item = new BattleAbilityData { Id = 6119, IsItem = true, IsTreasure = true, ItemType = 2, Range = BattleTargetRange.OpponentSingle };
            BattleDefinitions definitions = Definitions(scenario, new[] { item });
            BattleSession session = new(new BattleSetup(scenario), new Catalog(), new ReadyRules(), definitions);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "E", 6119)), Is.True);
            Assert.That(session.Inventory.Single().Count, Is.EqualTo(1));
            Assert.That(session.TreasureUses.Single().Count, Is.EqualTo(1));
            Steps(session, 1);
            Assert.That(session.TreasureUses.Single().Count, Is.Zero);
            Assert.That(session.CancelAction("A"), Is.True);
            Steps(session);
            var replay = BattleReplay.Play(new BattleSetup(scenario), new Catalog(), session.Inputs, session.Tick, new ReadyRules(), definitions);
            Assert.That(replay.TreasureUses.Single().Count, Is.Zero);
            Assert.That(replay.Inventory.Single().Count, Is.EqualTo(1));
            Assert.That(scenario.TreasureUses.Single().Count, Is.EqualTo(1));
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.TreasureUseChanged), Is.EqualTo(1));
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        [TestCase("PAL3", true)]
        [TestCase("PAL3A", true)]
        public void CancellingAnItemBeforeTheFirstSystemPassRetainsItsStockAndUseCounter(string variant, bool treasure)
        {
            BattleScenario scenario = Scenario(variant);
            int id = treasure ? 6119 : 6201;
            scenario.Items.Add(new BattleItem { Id = id, Count = 1 });
            if (treasure)
                scenario.TreasureUses.Add(new BattleItem { Id = id, Count = 1 });
            var item = new BattleAbilityData { Id = id, IsItem = true, IsTreasure = treasure, ItemType = 2, Range = BattleTargetRange.OpponentSingle };
            BattleSession session = Session(scenario, new[] { item });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "E", id)), Is.True);
            Assert.That(session.CancelAction("A"), Is.True);
            Steps(session, 10);
            Assert.That(session.Inventory.Single().Count, Is.EqualTo(1));
            if (treasure)
                Assert.That(session.TreasureUses.Single().Count, Is.EqualTo(1));
            Assert.That(session.Events.Any(entry => entry.Kind is BattleEventKind.ItemChanged or BattleEventKind.TreasureUseChanged), Is.False);
        }

        [Test]
        public void Pal3aHasNoPoisonPearlResourceAndTreasureSnapshotsRejectDuplicateOrOutOfByteRangeCounters()
        {
            BattleScenario scenario = Scenario("PAL3A");
            scenario.TreasureUses.Add(new BattleItem { Id = 6125, Count = 1 });
            Assert.That(scenario.Validate(), Is.Not.Empty);
            scenario.TreasureUses[0] = new BattleItem { Id = 6119, Count = 256 };
            Assert.That(scenario.Validate(), Is.Not.Empty);
            scenario.TreasureUses[0].Count = 1;
            scenario.TreasureUses.Add(scenario.TreasureUses[0].Copy());
            Assert.That(scenario.Validate(), Is.Not.Empty);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void StealUsesIndependentInstancePoolsMoneyThenItemsAndReplaysTheNativeTwoRollVector(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants.Add(Participant("E2", BattleSide.Enemy, BattleSlot.Fire));
            BattleAbilityData ability = Technique(variant, BattleSpecialHandler.Steal, value: 0);
            Learn(scenario.Participants[0], ability.Id);
            var actors = new[] { "E", "E2" }.Select(id => new BattleActorData { InstanceId = id, StealableMoney = 100, StealableItemId = 6201, StealableItemCount = 7 }).ToArray();
            BattleDefinitions definitions = Definitions(scenario, new[] { ability }, actors);
            BattleSession session = new(new BattleSetup(scenario), new Catalog(), new ReadyRules(), definitions);
            session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4101)), Is.True);
            Steps(session);
            Assert.That(session.Money, Is.EqualTo(42));
            Assert.That(session.Inventory.Single().Count, Is.EqualTo(2));
            Assert.That(session.StealPools.Single(pool => pool.Actor == "E").Money, Is.EqualTo(58));
            Assert.That(session.StealPools.Single(pool => pool.Actor == "E").ItemCount, Is.EqualTo(5));
            Assert.That(session.StealPools.Single(pool => pool.Actor == "E2").Money, Is.EqualTo(100));
            Assert.That(session.Events.Last().RandomState, Is.EqualTo(3357800067u));
            BattleSession replay = BattleReplay.Play(new BattleSetup(scenario), new Catalog(), session.Inputs, session.Tick, new ReadyRules(), definitions);
            Assert.That(replay.Money, Is.EqualTo(session.Money));
            Assert.That(replay.StealPools.Single(pool => pool.Actor == "E").ItemCount, Is.EqualTo(5));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AttackStealPassiveUsesThePostHitProbabilityThenMoneyAndItemRolls(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Seed = 494;
            var glove = new BattleEquipmentDefinition(new BattleEquipmentData { Id = 8130, Slot = 4, AttackStealPercent = 5 });
            BattleSession session = Session(scenario, Array.Empty<BattleAbilityData>(),
                new BattleActorData { InstanceId = "A", Identity = 0, Equipment = new[] { glove } },
                new BattleActorData { InstanceId = "E", StealableMoney = 100, StealableItemId = 6201, StealableItemCount = 7 });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Attack, "E")), Is.True);
            Steps(session);
            Assert.That(session.Money, Is.EqualTo(16));
            Assert.That(session.Inventory.Single().Count, Is.EqualTo(6));
            Assert.That(session.Events.Last().RandomState, Is.EqualTo(3346129948u));
        }

        [TestCase("PAL3", 0)]
        [TestCase("PAL3A", 6)]
        public void EnemyItemTheftUsesTheOrderedRecoveryEntryPoolAndVariantSpecificRemovalQuantity(string variant, int remaining)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Seed = 494;
            scenario.Items.AddRange(new[] { new BattleItem { Id = 6202, Count = 3 }, new BattleItem { Id = 6113, Count = 5 }, new BattleItem { Id = 6201, Count = 7 } });
            BattleAbilityData[] items = new[] { 6201, 6202, 6113 }.Select(id => new BattleAbilityData
            {
                Id = id,
                IsItem = true,
                ItemType = id == 6113 ? 2 : 1,
                Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, 1) }
            }).ToArray();
            BattleSession session = Session(scenario, items, new BattleActorData { InstanceId = "E", NormalAttackMode = 914 });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Defend)), Is.True);
            Assert.That(session.Submit(new BattleCommand("E", BattleAction.Attack, "A")), Is.True);
            Steps(session);
            Assert.That(session.Inventory.FirstOrDefault(item => item.Id == 6201)?.Count ?? 0, Is.EqualTo(remaining));
            Assert.That(session.Inventory.Single(item => item.Id == 6202).Count, Is.EqualTo(3));
            Assert.That(session.Inventory.Single(item => item.Id == 6113).Count, Is.EqualTo(5));
            Assert.That(session.InventoryEntries.First().Id, Is.EqualTo(6202));
            Assert.That(session.Events.Last().RandomState, Is.EqualTo(1652494221u));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void TreasureWithoutAnExplicitUseSnapshotIsRejectedWithoutConsumingInventory(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Items.Add(new BattleItem { Id = 6119, Count = 1 });
            var item = new BattleAbilityData { Id = 6119, IsItem = true, IsTreasure = true, ItemType = 2, Range = BattleTargetRange.OpponentSingle };
            BattleSession session = Session(scenario, new[] { item });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "E", 6119)), Is.False);
            Assert.That(session.Inventory.Single().Count, Is.EqualTo(1));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.TreasureUseChanged), Is.False);
        }

        [TestCase(false, 0, -100, 0)]
        [TestCase(false, 5, -95, 5)]
        [TestCase(false, 100, 0, 100)]
        [TestCase(true, 5, -45, 5)]
        [TestCase(true, 100, 0, 50)]
        public void NativeMoneyShieldVectorsApplyAfterGuardAndUseMoneyBeforeHp(bool guard, int money, int remaining, int paid)
        {
            int request = guard ? -50 : -100;
            Assert.That(BattleSpecialRules.ShieldedHpRequest(request, money, out int payment), Is.EqualTo(remaining));
            Assert.That(payment, Is.EqualTo(paid));
        }

        [Test]
        public void MoneyShieldPreventsHpAndSleepChangesButThePhysicalSecondSlotInterruptPrecedesPayment()
        {
            BattleScenario scenario = Scenario();
            scenario.Money = 1000;
            var ability = new BattleAbilityData { Id = 301, IsMagic = true, Range = BattleTargetRange.OpponentSingle };
            Learn(scenario.Participants[0], 301, 1);
            BattleSession session = Session(scenario, new[] { ability }, new BattleActorData { InstanceId = "A", Identity = 0, MoneyShield = true });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301)), Is.True);
            Assert.That(session.Submit(new BattleCommand("E", BattleAction.Attack, "A")), Is.True);
            Steps(session);
            Assert.That(Actor(session, "A").Hp, Is.EqualTo(500));
            Assert.That(Actor(session, "A").CurrentAction, Is.Null);
            Assert.That(Actor(session, "A").Gauge, Is.GreaterThanOrEqualTo(5750));
            Assert.That(session.Money, Is.LessThan(1000));
        }

        [TestCase("PAL3", 415139642u)]
        [TestCase("PAL3A", 3357800067u)]
        public void StartupInvisibilityRollFollowsEveryInitialGaugeAndDuplicateCharmsStillRollOnce(string variant, uint seedAfter)
        {
            BattleScenario scenario = Scenario(variant);
            var charm = new BattleEquipmentDefinition(new BattleEquipmentData { Id = 8114, Slot = 4, StartupInvisibility = true });
            BattleDefinitions definitions = Definitions(scenario, Array.Empty<BattleAbilityData>(), new BattleActorData { InstanceId = "A", Identity = 0, Equipment = new[] { charm, charm } });
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), new BattleBasicRules(), definitions);
            Assert.That(Actor(session, "A").Gauge, Is.EqualTo(1955));
            Assert.That(Actor(session, "E").Gauge, Is.EqualTo(2517));
            Assert.That(session.Events.Last().RandomState, Is.EqualTo(seedAfter));
            Assert.That(Actor(session, "A").States, Is.Empty);
        }
    }
}
