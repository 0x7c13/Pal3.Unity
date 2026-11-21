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

    public sealed class BattleResultPresentationTests
    {
        private static BattleSession Finished(string variant, bool win = true)
        {
            BattleScenario scenario = BattleSessionTests.Scenario();
            scenario.Variant = variant;
            scenario.DataFingerprint = "fixture";
            scenario.Money = 100;
            scenario.Participants[win ? 1 : 0].Vitals[0] = 0;
            BattleParticipant secondEnemy = scenario.Participants[1].Copy();
            secondEnemy.InstanceId = "Enemy2";
            secondEnemy.Slot = BattleSlot.Fire;
            scenario.Participants.Add(secondEnemy);
            var definitions = new BattleDefinitions(variant, "fixture", Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(), new[]
            {
                new BattleActorDefinition(new BattleActorData { InstanceId = "Ally", Identity = 0 }),
                new BattleActorDefinition(new BattleActorData { InstanceId = "Enemy", MoneyReward = 9, LootItem = 9001, LootCount = 2 }),
                new BattleActorDefinition(new BattleActorData { InstanceId = "Enemy2", LootItem = 9001, LootCount = 1 })
            });
            var session = new BattleSession(new BattleSetup(scenario), new BattleSessionTests.Catalog(), definitions: definitions);
            for (int tick = 0; tick < 100 && session.Status == BattleSessionStatus.Running; tick++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed));
            return session;
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void WinSequenceMatchesExecutedNativeRegistrationAndKeepsSeparateLootPrompts(string variant)
        {
            BattleSession combat = Finished(variant);
            var result = new BattleResultPresentation(variant, combat.Result, combat.Events);
            Assert.That(result.CameraList, Is.EqualTo(3));
            Assert.That(result.Sequence.Select(step => (step.Kind, step.Duration)), Is.EqualTo(new[]
            {
                (BattleResultStepKind.MusicFadeOut, .5f), (BattleResultStepKind.WinBanner, 3f),
                (BattleResultStepKind.Statistics, 2f), (BattleResultStepKind.Money, 5f),
                (BattleResultStepKind.Item, 4f), (BattleResultStepKind.Item, 4f),
                (BattleResultStepKind.Wait, 5f), (BattleResultStepKind.MusicFadeIn, .2f),
                (BattleResultStepKind.ScreenFadeOut, 1f)
            }));
            Assert.That(result.Sequence.Where(step => step.Kind == BattleResultStepKind.Item).Select(step => (step.Id, step.Value)),
                Is.EqualTo(new[] { (9001, 2), (9001, 1) }));
            Assert.That(combat.Result.Loot.Single().Count, Is.EqualTo(3));
            Assert.That(combat.Result.PreSettlementActors.Single(actor => actor.Id == "Enemy").AliveFlag, Is.False);
            long tick = combat.Tick;
            int events = combat.Events.Count;
            for (int frame = 0; frame < 5000 && !result.Completed; frame++)
            {
                result.Step();
                result.SkipCurrent();
            }
            Assert.That(result.Completed, Is.True);
            Assert.That(result.ScreenOpacity, Is.Zero);
            Assert.That(combat.Tick, Is.EqualTo(tick));
            Assert.That(combat.Events.Count, Is.EqualTo(events));
            Assert.That(combat.Result.Items.Single().Count, Is.EqualTo(3));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void LoseSequenceHasNoRewardStatisticsAndEndsWithMusicAfterScreenFade(string variant)
        {
            BattleSession combat = Finished(variant, false);
            var result = new BattleResultPresentation(variant, combat.Result, combat.Events);
            Assert.That(result.CameraList, Is.EqualTo(10));
            Assert.That(result.Sequence.Select(step => (step.Kind, step.Duration)), Is.EqualTo(new[]
            {
                (BattleResultStepKind.MusicFadeOut, .5f), (BattleResultStepKind.LoseBanner, 5f),
                (BattleResultStepKind.ScreenFadeOut, 1f), (BattleResultStepKind.MusicFadeIn, .2f)
            }));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void BannerFirstExecutionRejectsSkipAndUsesStrictNativeBoundary(string variant)
        {
            BattleSession combat = Finished(variant);
            var result = new BattleResultPresentation(variant, combat.Result, combat.Events);
            while (result.Current.Kind != BattleResultStepKind.WinBanner)
                result.Step();
            Assert.That(result.SkipCurrent(), Is.False);
            result.Step();
            Assert.That(result.HasExecuted, Is.True);
            for (int frame = 1; frame < 150; frame++)
                result.Step();
            Assert.That(result.Current.Kind, Is.EqualTo(BattleResultStepKind.WinBanner));
            Assert.That(result.Elapsed, Is.EqualTo(3).Within(.000001));
            result.Step();
            Assert.That(result.Current.Kind, Is.EqualTo(BattleResultStepKind.Statistics));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void NativeReverseMusicRampAndStopAreIndependentOfSkip(string variant)
        {
            BattleSession combat = Finished(variant);
            var result = new BattleResultPresentation(variant, combat.Result, combat.Events);
            Assert.That(result.SkipCurrent(), Is.False);
            result.Advance(.5);
            Assert.That(result.MusicGain, Is.Zero.Within(.000001));
            Assert.That(result.MusicStopCount, Is.Zero);
            result.Step();
            Assert.That(result.MusicStopCount, Is.EqualTo(1));
            while (result.Current.Kind != BattleResultStepKind.MusicFadeIn)
            {
                result.Step();
                result.SkipCurrent();
            }
            result.Step();
            float first = result.MusicGain;
            result.Step();
            Assert.That(result.MusicGain, Is.LessThan(first));
            while (result.Current.Kind == BattleResultStepKind.MusicFadeIn)
                result.Step();
            Assert.That(result.MusicGain, Is.EqualTo(1));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void VictoryBodyWaitsForOneMessagePerPassAndFailedGotoRetainsPreviousClip(string variant)
        {
            var old = new BattlePhysicalAnimation("idle", 20, 1, gameBoxDuration: 480u);
            var initial = new BattleBodyAnimationBinding(old);
            initial.Advance();
            uint previous = initial.GameBoxTime;
            var body = new BattleVictoryBody(variant, initial, null, old);
            body.Step();
            Assert.That(body.Binding.CurrentAnimation, Is.SameAs(old));
            Assert.That(body.Binding.GameBoxTime, Is.GreaterThan(previous));
            Assert.That(initial.GameBoxTime, Is.EqualTo(previous));
            Assert.That(body.Binding.RequestedNativeActionIndex, Is.EqualTo(49));
            Assert.That(body.BladeSeconds, Is.EqualTo(.25f));
            for (int frame = 0; frame < 30 && !body.Completed; frame++)
                body.Step();
            Assert.That(body.Completed, Is.True);
            Assert.That(body.Binding.RequestedNativeActionIndex, Is.EqualTo(49));
            Assert.That(body.BladeSeconds, Is.EqualTo(.01f));
            body.Step();
            Assert.That(body.Binding.RequestedNativeActionIndex, Is.EqualTo(32));
        }

        [TestCase("PAL3", new[] { 4 })]
        [TestCase("PAL3A", new[] { 4 })]
        public void CommittedLevelLearningAndMagicGrowthEventsBecomeOrderedFiveSecondPrompts(string variant, int[] tiers)
        {
            BattleScenario scenario = BattleSessionTests.Scenario();
            scenario.Variant = variant;
            scenario.DataFingerprint = "fixture";
            scenario.Participants[1].Vitals[0] = 0;
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = 301, Mastered = true, Level = 1, Uses = 5 });
            int[] thresholds = Enumerable.Repeat(int.MaxValue, 101).ToArray();
            thresholds[0] = thresholds[1] = 0;
            thresholds[2] = 10;
            var definitions = new BattleDefinitions(variant, "fixture", new[]
            {
                new BattleAbilityDefinition(new BattleAbilityData { Id = 401, Name = "learned technique", SkillTypeId = 0,
                    ApplicableIdentities = new[] { 0 }, LearningLevel = 2 }),
                new BattleAbilityDefinition(new BattleAbilityData { Id = 301, Name = "growing magic", IsMagic = true,
                    ApplicableIdentities = new[] { 0 }, UsesBeforeLevelUp = new[] { 0, 1, 3, 5 } })
            }, Array.Empty<BattleAbilityDefinition>(), new[]
            {
                new BattleActorDefinition(new BattleActorData { InstanceId = "Ally", Identity = 0, ExperienceThresholds = thresholds,
                    AttributesByLevel = Enumerable.Range(0, 101).Select(_ => new int[12]).ToArray() }),
                new BattleActorDefinition(new BattleActorData { InstanceId = "Enemy", ExperienceReward = 20 })
            });
            var combat = new BattleSession(new BattleSetup(scenario), new BattleSessionTests.Catalog(), definitions: definitions);
            combat.Step();
            Assert.That(combat.Status, Is.EqualTo(BattleSessionStatus.Completed));
            var presentation = new BattleResultPresentation(variant, combat.Result, combat.Events);
            BattleResultStep[] prompts = presentation.Sequence.Where(step => step.Kind is BattleResultStepKind.LevelUp or
                BattleResultStepKind.SkillLearned or BattleResultStepKind.SkillImproved).ToArray();
            Assert.That(prompts[0].Kind, Is.EqualTo(BattleResultStepKind.LevelUp));
            Assert.That(prompts[0].Value, Is.EqualTo(2));
            Assert.That(prompts[1].Kind, Is.EqualTo(BattleResultStepKind.SkillLearned));
            Assert.That(prompts[1].Id, Is.EqualTo(401));
            Assert.That(prompts.Skip(2).Select(step => step.Value), Is.EqualTo(tiers));
            Assert.That(prompts.All(step => step.Duration == 5 && step.Actor == "Ally"), Is.True);
        }

        [TestCase(0, 0, -256)]
        [TestCase(0, .08, -246)]
        [TestCase(0, .1, -240)]
        [TestCase(0, .2, -195)]
        [TestCase(0, .4, -15)]
        [TestCase(0, .5, 0)]
        [TestCase(1, .08, -256)]
        [TestCase(1, .1, -255)]
        [TestCase(1, .2, -234)]
        [TestCase(1, .4, -102)]
        [TestCase(1, .5, 0)]
        [TestCase(2, .2, -253)]
        [TestCase(2, .4, -169)]
        [TestCase(2, .5, -82)]
        [TestCase(2, .7, 0)]
        [TestCase(3, .4, -217)]
        [TestCase(3, .5, -154)]
        [TestCase(3, .7, 0)]
        public void StatisticsCardsMatchBothNativeGravityCallbacks(int column, double seconds, int y)
        {
            Assert.That(BattleResultStatistics.CardY(column, seconds), Is.EqualTo(y));
        }

        private sealed class ReadyRules : IBattleRules
        {
            private readonly BattleBasicRules _rules = new();

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => BattleSession.FullGauge;

            public int GaugePerStep(BattleParticipant actor) => _rules.GaugePerStep(actor);

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1) =>
                _rules.Hit(source, target, guarding, random, targetAtHome, damageDivisor);
        }

        [TestCase("PAL3", false, 1, -2)]
        [TestCase("PAL3A", false, 1, -2)]
        [TestCase("PAL3", true, 0, 0)]
        [TestCase("PAL3A", true, 1, -2)]
        public void ResultRecoveryKeepsNativeAliveModelAndStateFlags(string variant, bool flee, int hp, int loveChange)
        {
            BattleScenario scenario = BattleSessionTests.Scenario();
            scenario.Variant = variant;
            scenario.DataFingerprint = "fixture";
            scenario.Participants[0].Level = 100;
            scenario.Participants[0].Attributes[5] = 20;
            scenario.Participants[0].Attributes[6] = 100;
            scenario.Participants[1].Vitals[0] = flee ? 100 : 0;
            BattleParticipant dead = scenario.Participants[0].Copy();
            dead.InstanceId = "Dead";
            dead.Slot = BattleSlot.Fire;
            dead.Vitals[0] = 0;
            dead.Love = 50;
            scenario.Participants.Add(dead);
            var definitions = new BattleDefinitions(variant, "fixture", Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(), new[]
            {
                new BattleActorDefinition(new BattleActorData { InstanceId = "Ally", Identity = 0 }),
                new BattleActorDefinition(new BattleActorData { InstanceId = "Dead", Identity = 1,
                    InitialStates = new[] { BattleState.PoisonFire, BattleState.Sleep, BattleState.Paralysis, BattleState.Invisible } }),
                new BattleActorDefinition(new BattleActorData { InstanceId = "Enemy" })
            });
            var combat = new BattleSession(new BattleSetup(scenario), new BattleSessionTests.Catalog(), new ReadyRules(), definitions);
            if (flee)
                Assert.That(combat.Submit("Ally", BattleAction.Flee), Is.True);
            for (int tick = 0; tick < 5000 && combat.Status == BattleSessionStatus.Running; tick++)
                combat.Step();
            Assert.That(combat.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(combat.Result.Escaped, Is.EqualTo(flee));
            if (flee)
            {
                var presentation = new BattleResultPresentation(variant, combat.Result, combat.Events);
                Assert.That(presentation.CameraList, Is.EqualTo(8));
                Assert.That(presentation.Sequence.Select(step => (step.Kind, step.Duration)), Is.EqualTo(new[]
                {
                    (BattleResultStepKind.Statistics, 2f), (BattleResultStepKind.Wait, variant == "PAL3" ? 15f : 3f)
                }));
            }
            BattleActorSnapshot final = combat.Result.Actors.Single(actor => actor.Id == "Dead");
            BattleActorSnapshot before = combat.Result.PreSettlementActors.Single(actor => actor.Id == "Dead");
            Assert.That(before.Hp, Is.Zero);
            Assert.That(final.Hp, Is.EqualTo(hp));
            Assert.That(final.AliveFlag, Is.False);
            Assert.That(final.ModelAliveFlag, Is.False);
            Assert.That(final.Love - before.Love, Is.EqualTo(loveChange));
            if (flee)
                Assert.That(before.States.Select(state => state.State), Does.Contain(BattleState.Death));
            Assert.That(final.States.Select(state => state.State), Is.EquivalentTo(before.States.Select(state => state.State)));
            Assert.That(combat.Events.Skip(combat.Result.SettlementEventStart).Where(entry => entry.Kind == BattleEventKind.StatusRemoved).Any(entry => entry.Detail.Contains("post-result")), Is.False);
        }

        [Test]
        public void ClonedAnimationFifoAndClockRemainIndependent()
        {
            var animation = new BattlePhysicalAnimation("fixture", 10, 1, new[]
            {
                new BattlePhysicalMarker(1, BattlePhysicalMarkerKind.Other, 1, "sound"),
                new BattlePhysicalMarker(1, BattlePhysicalMarkerKind.Over, 1, "over")
            });
            var original = new BattleBodyAnimationBinding(animation);
            original.Advance();
            BattleBodyAnimationBinding clone = original.Clone();
            clone.PopMessage();
            clone.ClearMessages();
            clone.Advance();
            Assert.That(original.PendingMessageCount, Is.EqualTo(2));
            Assert.That(original.GameBoxTime, Is.EqualTo(96));
            Assert.That(clone.GameBoxTime, Is.EqualTo(192));
        }
    }
}
