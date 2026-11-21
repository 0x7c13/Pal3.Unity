// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Linq;
    using Battle;
    using Newtonsoft.Json;
    using NUnit.Framework;

    public sealed class BattleNativeArithmeticBoundaryTests
    {
        [TestCase("PAL3", int.MinValue, -30)]
        [TestCase("PAL3", int.MaxValue, 27)]
        [TestCase("PAL3A", int.MinValue, -30)]
        [TestCase("PAL3A", int.MaxValue, 27)]
        public void PublishedMagicFormulaKeepsTheLowWordOfItsTruncatingCast(string variant, int raw, int expected)
        {
            var caster = new BattleParticipant { Level = 20, Slot = BattleSlot.Center, Side = BattleSide.Ally };
            var target = new BattleParticipant { Level = 20, Slot = BattleSlot.Center, Side = BattleSide.Enemy };
            var random = new BattleRandom(1, variant);
            Assert.That(new BattleEffectRules(variant).MagicPower(raw, caster, target, 0, random), Is.EqualTo(expected));
            Assert.That(random.State, Is.EqualTo(3357800067u));
        }

        [Test]
        public void EventTextUsesAStableDecimalSeparator()
        {
            var previous = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
                Assert.That(new BattleEvent().ToString(), Does.StartWith("0.00s"));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }
        }

        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings
        {
            public AttackTiming Attack(string source, string target) => new(0, 2, new[] { 1 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => new(0, 2, new[] { 1 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 1;

            public int SpImpact(string actor) => 1;

            public int Priority(string actor) => actor == "Ally" ? 0 : 5;

            public int DamageDivisor(string actor) => 1;
        }

        private static BattleScenario Scenario(string variant, int enemyCount = 1)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "native-boundary", DataFingerprint = "native-arithmetic-boundary", Encounter = BattleEncounter.AllyAmbush };
            for (int index = 0; index <= enemyCount; index++)
                scenario.Participants.Add(new BattleParticipant
                {
                    InstanceId = index == 0 ? "Ally" : "Enemy" + index,
                    ActorId = index == 0 ? 101 : 701,
                    Side = index == 0 ? BattleSide.Ally : BattleSide.Enemy,
                    Slot = (BattleSlot)Math.Max(0, index - 1),
                    Attributes = new[] { 100, 100, 100, 10, 10, 10, 10, 0, 0, 0, 0, 0 },
                    Vitals = new[] { index == 0 ? 100 : 10, 100, 100 }
                });
            return scenario;
        }

        private static BattleDefinitions Definitions(BattleScenario scenario, int reward = 0, int[] thresholds = null, int[][] growth = null)
        {
            return new BattleDefinitions(scenario.Variant, scenario.DataFingerprint,
                new[] { 301, 302 }.Select(id => new BattleAbilityDefinition(new BattleAbilityData
                {
                    Id = id,
                    IsMagic = true,
                    SkillTypeId = 2,
                    ElementMask = 1,
                    Range = BattleTargetRange.OpponentAll,
                    ApplicableIdentities = new[] { 0 },
                    Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -1000) }
                })), Array.Empty<BattleAbilityDefinition>(), scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Ally ? 0 : -1,
                    ExperienceReward = actor.Side == BattleSide.Enemy ? reward : 0,
                    ExperienceThresholds = actor.Side == BattleSide.Ally ? thresholds : null,
                    AttributesByLevel = actor.Side == BattleSide.Ally ? growth : null
                })));
        }

        [TestCase("PAL3", 256)]
        [TestCase("PAL3", int.MaxValue)]
        [TestCase("PAL3A", 65536)]
        [TestCase("PAL3A", int.MaxValue)]
        public void InitialSkillUsesOutsideThePublishedCounterCannotEnterALiveSession(string variant, int uses)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = 301, Uses = uses, Mastered = true });
            Assert.That(scenario.Validate().Any(error => error.Contains("skill use", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.Throws<ArgumentException>(() => new BattleSetup(scenario));
        }

        [TestCase("PAL3", 255)]
        [TestCase("PAL3A", 65535)]
        public void MaximumInitialCountersRemainSaturatedAndTheirResultStatisticsAndReplayAreConsumable(string variant, int maximum)
        {
            BattleScenario scenario = Scenario(variant);
            foreach (int id in new[] { 301, 302 })
                scenario.Participants[0].Skills.Add(new BattleSkill { Id = id, Level = 4, Uses = maximum, Mastered = true });
            var setup = new BattleSetup(scenario);
            BattleDefinitions definitions = Definitions(scenario);
            var catalog = new Catalog();
            var session = new BattleSession(setup, catalog, definitions: definitions);
            Assert.That(session.Submit(new BattleCommand("Ally", BattleAction.Skill, abilityId: 301)), Is.True);
            Complete(session);
            Assert.That(session.Result.Actors.Single(actor => actor.Id == "Ally").Skills.Select(skill => skill.Uses), Is.All.EqualTo(maximum));
            Assert.That(BattleResultStatistics.MagicUseCounts(variant, definitions, 0, session.Result.PreSettlementActors.Single(actor => actor.Id == "Ally").Skills),
                Is.EqualTo(new[] { maximum * 2, 0, 0, 0, 0, 0 }));
            AssertReplay(setup, catalog, definitions, session);
        }

        // Executed released PAL3 0x4eef20..0x4ef20f, including its CRT _ftol.
        // The caller retains EAX after int64 truncation, independently of f2i32.
        [TestCase(-100, int.MaxValue, -50)]
        [TestCase(-101, int.MaxValue, -1073741874)]
        [TestCase(short.MinValue, int.MaxValue, -16384)]
        [TestCase(short.MaxValue, int.MaxValue, -1073725441)]
        [TestCase(-101, 3, -252)]
        public void ResidualSwordRetainsThePublishedCrtTruncationAndLowWord(short raw, int count, int expected)
        {
            var snapshot = new BattleMagicSword { PurificationCount = count }.Capture();
            Assert.That(BattleMagicSwordRules.SkillPower(snapshot, 2224, raw, BattleSide.Enemy, out BattleValueMode mode), Is.EqualTo(expected));
            Assert.That(mode, Is.EqualTo(BattleValueMode.Absolute));
        }

        [TestCase("PAL3", int.MaxValue, 1, 1, int.MinValue)]
        [TestCase("PAL3A", int.MaxValue, 1, 1, int.MinValue)]
        [TestCase("PAL3", int.MaxValue - 1, 3, 1, int.MinValue + 1)]
        [TestCase("PAL3A", int.MaxValue - 1, 3, 1, int.MinValue + 1)]
        [TestCase("PAL3", 0, int.MaxValue, 2, -2)]
        [TestCase("PAL3A", 0, int.MaxValue, 2, -2)]
        [TestCase("PAL3", 123, 1, 1, 124)]
        [TestCase("PAL3A", 123, 1, 1, 124)]
        public void SettlementCommitsNativeSignedExperienceAndReplayRetainsTheResult(string variant, int before, int reward, int enemies, int expected)
        {
            BattleScenario scenario = Scenario(variant, enemies);
            scenario.Participants[0].Level = 100;
            scenario.Participants[0].Experience = before;
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = 301, Level = 4, Mastered = true });
            var setup = new BattleSetup(scenario);
            BattleDefinitions definitions = Definitions(scenario, reward);
            var catalog = new Catalog();
            var session = new BattleSession(setup, catalog, definitions: definitions);
            Assert.That(session.Submit(new BattleCommand("Ally", BattleAction.Skill, abilityId: 301)), Is.True);
            Complete(session);
            BattleActorReward result = session.Result.Rewards.Single();
            Assert.That(result.ExperienceBefore, Is.EqualTo(before));
            Assert.That(result.ExperienceAfter, Is.EqualTo(expected));
            Assert.That(session.Result.Actors.Single(actor => actor.Id == "Ally").Experience, Is.EqualTo(expected));
            Assert.That(result.LevelAfter, Is.EqualTo(100));
            Assert.DoesNotThrow(() => new BattleResultPresentation(variant, session.Result, session.Events));
            Assert.That(BattleResultStatistics.Experience(result.ExperienceGained, 2), Is.EqualTo(result.ExperienceGained));
            Assert.That(scenario.Participants[0].Experience, Is.EqualTo(before));
            AssertReplay(setup, catalog, definitions, session);
        }

        [TestCase("PAL3", 1, false)]
        [TestCase("PAL3A", 1, false)]
        [TestCase("PAL3", 50, false)]
        [TestCase("PAL3A", 50, false)]
        [TestCase("PAL3", 1, true)]
        [TestCase("PAL3A", 1, true)]
        [TestCase("PAL3", 50, true)]
        [TestCase("PAL3A", 50, true)]
        public void WrappedExperienceUsesUnsignedNativeLevelsAndCommitsGrowthThroughResultsAndReplay(string variant, int oldLevel, bool negativeGain)
        {
            BattleScenario scenario = Scenario(variant, negativeGain ? 2 : 1);
            BattleParticipant initial = scenario.Participants[0];
            initial.Level = oldLevel;
            initial.Experience = negativeGain ? 0 : int.MaxValue;
            initial.Attributes[3] = 100;
            initial.Attributes[4] = 100;
            initial.Vitals = new[] { 7, 8, 9 };
            initial.Skills.Add(new BattleSkill { Id = 301, Level = 4, Mastered = true });
            var setup = new BattleSetup(scenario);
            int[] thresholds = Enumerable.Range(0, 101).Select(level => Math.Max(0, level - 1) * 100).ToArray();
            thresholds[100] = int.MaxValue;
            int[][] growth = Enumerable.Range(0, 101).Select(_ => new int[12]).ToArray();
            // Published growth functions for identity zero at levels 1/50/100.
            // Full native CalCombatResult, CalLevel and growth were executed together.
            growth[1] = variant == "PAL3" ? new[] { 88, 17, 29, 30, 20, 0, 0, 0, 0, 0, 0, 0 } : new[] { 78, 9, 24, 25, 22, 0, 0, 0, 0, 0, 0, 0 };
            growth[50] = variant == "PAL3" ? new[] { 480, 72, 225, 251, 182, 0, 0, 0, 0, 0, 0, 0 } : new[] { 470, 58, 220, 270, 218, 0, 0, 0, 0, 0, 0, 0 };
            growth[100] = variant == "PAL3" ? new[] { 880, 98, 425, 476, 347, 0, 0, 0, 0, 0, 0, 0 } : new[] { 870, 108, 420, 520, 418, 0, 0, 0, 0, 0, 0, 0 };
            int[] expected = (variant, oldLevel) switch
            {
                ("PAL3", 1) => new[] { 892, 181, 496, 546, 427, 10, 10 },
                ("PAL3", 50) => new[] { 500, 126, 300, 325, 265, 10, 10 },
                ("PAL3A", 1) => new[] { 892, 199, 496, 595, 496, 10, 10 },
                _ => new[] { 500, 150, 300, 350, 300, 10, 10 }
            };
            BattleDefinitions definitions = Definitions(scenario, negativeGain ? int.MaxValue : 1, thresholds, growth);
            var catalog = new Catalog();
            var session = new BattleSession(setup, catalog, definitions: definitions);
            Assert.That(session.Submit(new BattleCommand("Ally", BattleAction.Skill, abilityId: 301)), Is.True);
            Complete(session);
            BattleActorReward reward = session.Result.Rewards.Single();
            BattleActorSnapshot actor = session.Result.Actors.Single(candidate => candidate.Id == "Ally");
            BattleActorSnapshot before = session.Result.PreSettlementActors.Single(candidate => candidate.Id == "Ally");
            Assert.That(reward.ExperienceAfter, Is.EqualTo(negativeGain ? -2 : int.MinValue));
            Assert.That(reward.LevelBefore, Is.EqualTo(oldLevel));
            Assert.That(reward.LevelAfter, Is.EqualTo(100));
            Assert.That(reward.AttributesBefore, Is.EqualTo(initial.Attributes));
            Assert.That(reward.AttributesAfter.Take(7), Is.EqualTo(expected));
            Assert.That(actor.Level, Is.EqualTo(100));
            Assert.That(actor.Experience, Is.EqualTo(reward.ExperienceAfter));
            Assert.That(actor.Attributes.Take(7), Is.EqualTo(expected));
            Assert.That(new[] { actor.Hp, actor.Sp, actor.Mp }, Is.EqualTo(expected.Take(3)));
            Assert.That(new[] { before.Hp, before.Sp, before.Mp }, Is.EqualTo(new[] { 7, 8, 9 }));
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.LevelUp && entry.Source == "Ally"), Is.EqualTo(1));
            var presentation = new BattleResultPresentation(variant, session.Result, session.Events);
            Assert.That(presentation.Sequence.Single(step => step.Kind == BattleResultStepKind.LevelUp).Value, Is.EqualTo(100));
            Assert.That(presentation.Sequence.Count(step => step.Kind == BattleResultStepKind.LevelUpBody), Is.EqualTo(variant == "PAL3A" ? 1 : 0));
            for (int frame = 0; frame < 3000 && !presentation.Completed; frame++)
                presentation.Step();
            Assert.That(presentation.Completed, Is.True);
            Assert.That(initial.Level, Is.EqualTo(oldLevel));
            Assert.That(initial.Experience, Is.EqualTo(negativeGain ? 0 : int.MaxValue));
            Assert.That(initial.Vitals, Is.EqualTo(new[] { 7, 8, 9 }));
            AssertReplay(setup, catalog, definitions, session);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void NegativeInitialExperienceIsStillRejectedBeforeRuntimeSettlement(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[0].Experience = -1;
            Assert.That(scenario.Validate(), Is.Not.Empty);
            Assert.Throws<ArgumentException>(() => new BattleSetup(scenario));
        }

        private static void Complete(BattleSession session)
        {
            for (int step = 0; step < 5000 && session.Status == BattleSessionStatus.Running; step++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed), session.Result?.Reason);
        }

        private static void AssertReplay(BattleSetup setup, Catalog catalog, BattleDefinitions definitions, BattleSession session)
        {
            BattleSession replay = BattleReplay.Play(setup, catalog, session.Inputs, session.Tick, definitions: definitions);
            Assert.That(JsonConvert.SerializeObject(replay.Result), Is.EqualTo(JsonConvert.SerializeObject(session.Result)));
            Assert.That(JsonConvert.SerializeObject(replay.Events), Is.EqualTo(JsonConvert.SerializeObject(session.Events)));
        }
    }
}
