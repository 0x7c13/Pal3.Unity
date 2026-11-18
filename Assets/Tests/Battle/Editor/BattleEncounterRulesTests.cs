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

    public sealed class BattleEncounterRulesTests
    {
        [Test]
        public void OnlyNativeCenterCountsTheEncounterLimitAndItStopsLaterRoundEffects()
        {
            int processed = 0;
            int boss = 0;
            Assert.That(BattleEncounterRules.BeginRound(5, 4, 2, ref processed, ref boss), Is.True);
            Assert.That((processed, boss), Is.EqualTo((1, 0)));
            processed = 0;
            Assert.That(BattleEncounterRules.BeginRound(10, 4, 2, ref processed, ref boss), Is.True);
            Assert.That((processed, boss), Is.EqualTo((1, 1)));
            Assert.That(BattleEncounterRules.BeginRound(10, 4, 2, ref processed, ref boss), Is.True);
            Assert.That((processed, boss), Is.EqualTo((2, 2)));
            int other = 0;
            Assert.That(BattleEncounterRules.BeginRound(0, 4, 2, ref other, ref boss), Is.False);
            Assert.That((other, boss), Is.EqualTo((0, 2)));
            Assert.That(BattleEncounterRules.RoundLimitReached(0, -1), Is.False);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void DefeatWinsTheSimultaneousDeathDecisionAndMustFailDoesNotForbidAnOrdinaryWin(string variant)
        {
            var rules = new BattleEncounterRules(variant);
            Assert.That(rules.CheckOutcome(false, false, 0, -1, false, Array.Empty<int>()), Is.EqualTo(BattleEncounterOutcome.Lose));
            Assert.That(rules.CheckOutcome(true, true, 3, 3, true, Array.Empty<int>()), Is.EqualTo(BattleEncounterOutcome.None));
            Assert.That(rules.CheckOutcome(true, false, 3, 3, true, Array.Empty<int>()), Is.EqualTo(BattleEncounterOutcome.Win));
            Assert.That(rules.CheckOutcome(true, true, 3, 3, false, Array.Empty<int>()), Is.EqualTo(BattleEncounterOutcome.Win));
        }

        [TestCase("PAL3", 12, BattleEncounterOutcome.None, BattleEncounterOutcome.None)]
        [TestCase("PAL3", 11, BattleEncounterOutcome.Lose, BattleEncounterOutcome.Win)]
        [TestCase("PAL3A", 13, BattleEncounterOutcome.None, BattleEncounterOutcome.None)]
        [TestCase("PAL3A", 17, BattleEncounterOutcome.None, BattleEncounterOutcome.None)]
        [TestCase("PAL3A", 11, BattleEncounterOutcome.None, BattleEncounterOutcome.Win)]
        [TestCase("PAL3A", 31, BattleEncounterOutcome.Lose, BattleEncounterOutcome.None)]
        public void OutcomeWaitsForTheReleasedVariantsDistinctStageTasks(string variant, int task, BattleEncounterOutcome defeat, BattleEncounterOutcome victory)
        {
            var rules = new BattleEncounterRules(variant);
            Assert.That(rules.CheckOutcome(false, true, 0, -1, false, new[] { task }), Is.EqualTo(defeat));
            Assert.That(rules.CheckOutcome(true, false, 0, -1, false, new[] { task }), Is.EqualTo(victory));
        }

        [Test]
        public void PreparedMonsterCapturesItsParticipantBeforeTheLiveSlotChanges()
        {
            var participant = new BattleParticipant { InstanceId = "slot10", ActorId = 739, Side = BattleSide.Enemy, Slot = BattleSlot.Center };
            participant.Attributes[0] = participant.Vitals[0] = 500;
            var definition = new BattleActorDefinition(new BattleActorData { InstanceId = "slot10" });
            var creation = new BattleMonsterCreation(10, participant, definition);
            participant.ActorId = 701;
            participant.Vitals[0] = 0;
            Assert.That(creation.Participant.ActorId, Is.EqualTo(739));
            Assert.That(creation.Participant.Vitals[0], Is.EqualTo(500));
            BattleParticipant exposed = creation.Participant;
            exposed.Vitals[0] = 1;
            Assert.That(creation.Participant.Vitals[0], Is.EqualTo(500));
        }

        [Test]
        public void MustFailRestoresOnlyNativeCenterToItsOwnMaximum()
        {
            Assert.That(BattleEncounterRules.HpAfterMustFail(true, 10, -100, 1000), Is.EqualTo(1000));
            Assert.That(BattleEncounterRules.HpAfterMustFail(true, 5, -100, 1000), Is.EqualTo(-100));
            Assert.That(BattleEncounterRules.HpAfterMustFail(false, 10, -100, 1000), Is.EqualTo(-100));
        }

        [Test]
        public void SwordBossThirdRoundUsesTheActualVariantsStateOrder()
        {
            Assert.That(new BattleBossRules("PAL3").RoundStates(7372, 3), Is.EqualTo(new[] { BattleState.AttackIncrease }));
            Assert.That(new BattleBossRules("PAL3A").RoundStates(7372, 3), Is.EqualTo(new[]
            {
                BattleState.AttackIncrease, BattleState.SpeedIncrease, BattleState.DefenseIncrease
            }));
            Assert.That(new BattleBossRules("PAL3A").RoundStates(7372, 4), Is.Empty);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void SwampSplitCanReuseDeadSlotsWhereBloodCloudNeedsAnInvalidSlot(string variant)
        {
            var rules = new BattleBossRules(variant);
            BattleNativeEnemySlot[] slots = Enumerable.Range(0, 6).Select(index => new BattleNativeEnemySlot(true, index != 0)).ToArray();
            var swampRandom = new BattleRandom(1);
            var cloudRandom = new BattleRandom(1);
            Assert.That(rules.SplitSlot(7378, 3, slots, swampRandom), Is.EqualTo(5));
            Assert.That(rules.SplitSlot(7395, 2, slots, cloudRandom), Is.EqualTo(-1));
            Assert.That(swampRandom.State, Is.EqualTo(2745024u));
            Assert.That(cloudRandom.State, Is.EqualTo(swampRandom.State));
            slots[1] = default;
            Assert.That(rules.SplitSlot(7395, 2, slots, new BattleRandom(1)), Is.EqualTo(6));
            var early = new BattleRandom(1);
            Assert.That(rules.SplitSlot(7378, 2, slots, early), Is.EqualTo(-1));
            Assert.That(early.State, Is.EqualTo(1u));
        }

        [TestCase("PAL3", 5, 7370, 7, 7369, 1423890337u)]
        [TestCase("PAL3A", 7, 7364, 8, 7365, 3357800067u)]
        public void SummonUsesTheReleasedVariantsIdsSlotsAndRandomConsumption(string variant, int firstSlot, int firstActor, int secondSlot, int secondActor, uint seedAfter)
        {
            var random = new BattleRandom(1);
            var summons = new BattleBossRules(variant).Summon(10, 1, new BattleNativeEnemySlot[6], random);
            Assert.That(summons.Select(request => (request.NativeIndex, request.ActorId)),
                Is.EqualTo(new[] { (firstSlot, firstActor), (secondSlot, secondActor) }));
            Assert.That(random.State, Is.EqualTo(seedAfter));
        }

        [Test]
        public void OriginalSummonRejectsALivingUnescapedFixedSlotBeforeDrawingRandom()
        {
            var slots = new BattleNativeEnemySlot[6];
            slots[0] = new BattleNativeEnemySlot(false, true);
            var random = new BattleRandom(1);
            var rules = new BattleBossRules("PAL3");
            Assert.That(rules.Summon(10, 1, slots, random), Is.Empty);
            Assert.That(random.State, Is.EqualTo(1u));
            slots[0] = new BattleNativeEnemySlot(false, true, true);
            Assert.That(rules.Summon(10, 1, slots, random), Has.Count.EqualTo(2));
        }

        [TestCase("PAL3", 210)]
        [TestCase("PAL3A", 308)]
        public void FireballSelfDestructionRequiresTheExactActorAndMagic(string variant, int magic)
        {
            var rules = new BattleBossRules(variant);
            Assert.That(rules.SelfDestructsAfterMagic(7312, magic), Is.True);
            Assert.That(rules.SelfDestructsAfterMagic(701, magic), Is.False);
            Assert.That(rules.SelfDestructsAfterMagic(7312, magic + 1), Is.False);
            Assert.That(rules.DeathReplacement(738), Is.EqualTo(variant == "PAL3" ? 739 : 0));
        }
    }
}
