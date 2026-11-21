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

    public sealed class BattleResultRepairTests
    {
        private static BattlePhysicalAnimation Clip(string name, uint duration = 480) => new(name, 100, 1, gameBoxDuration: duration);

        [TestCase("PAL3", 49)]
        [TestCase("PAL3A", 49)]
        [TestCase("PAL3A", 59)]
        public void NativeOverPassRetainsActionClipAndRateUntilTheFollowingFreePass(string variant, int action)
        {
            var clips = new Dictionary<int, BattlePhysicalAnimation> { [32] = Clip("idle"), [46] = Clip("dying"), [49] = Clip("win"), [59] = Clip("level") };
            var role = new BattleResultRoleBody(variant, new BattleBodyAnimationBinding(clips[32]), clips,
                new BattleResultBodyState(1, 100), action == 49);
            if (action == 59)
                Assert.That(role.TryStartLevelUp(), Is.True);
            role.Step();
            Assert.That(role.Binding.RequestedNativeActionIndex, Is.EqualTo(action));
            for (int frame = 0; frame < 30 && role.HasTask; frame++)
                role.Step();
            Assert.That(role.HasTask, Is.False);
            Assert.That(role.Binding.RequestedNativeActionIndex, Is.EqualTo(action));
            Assert.That(role.Binding.CurrentAnimation.Clip, Is.EqualTo(action == 49 ? "win" : "level"));
            Assert.That(role.PlaybackRate, Is.EqualTo(variant == "PAL3" ? 1.3f : 1));
            Assert.That(role.BladeSeconds, Is.EqualTo(action == 49 ? .01f : .25f));
            role.Step();
            Assert.That(role.Binding.RequestedNativeActionIndex, Is.EqualTo(46));
            Assert.That(role.Binding.CurrentAnimation.Clip, Is.EqualTo("dying"));
            Assert.That(role.BladeSeconds, Is.EqualTo(.01f));
        }

        [TestCase(0, false, false, false, 47)]
        [TestCase(0, true, false, false, 47)]
        [TestCase(1, false, false, false, 46)]
        [TestCase(24, false, false, false, 46)]
        [TestCase(25, false, false, false, 32)]
        [TestCase(100, false, false, false, 32)]
        [TestCase(1, true, false, false, 42)]
        [TestCase(100, true, true, true, 42)]
        [TestCase(1, false, true, false, 45)]
        [TestCase(1, false, false, true, 45)]
        public void ResultProperActionUsesNativeHpGuardAndSleepPrecedence(int hp, bool guard, bool sleep, bool stable, int action)
        {
            Assert.That(new BattleResultBodyState(hp, 100, guard, sleep, stable).ProperAction, Is.EqualTo(action));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void NativeIntegerHpThresholdKeepsTinyMaximumHpZeroInTheOrdinaryAction(int maximum)
        {
            Assert.That(new BattleResultBodyState(0, maximum).ProperAction, Is.EqualTo(32));
            Assert.That(new BattleResultBodyState(0, maximum, true).ProperAction, Is.EqualTo(42));
        }

        [Test]
        public void LevelUpWaitsForNormalWinReleaseButDoesNotWaitForItsOwnOver()
        {
            var clips = new Dictionary<int, BattlePhysicalAnimation> { [32] = Clip("idle"), [49] = Clip("win"), [59] = Clip("level", 48000) };
            var role = new BattleResultRoleBody("PAL3A", new BattleBodyAnimationBinding(clips[32]), clips,
                new BattleResultBodyState(100, 100), true);
            Assert.That(role.NormalGrab, Is.True);
            Assert.That(role.TryStartLevelUp(), Is.False);
            role.Step();
            while (role.HasTask)
                role.Step();
            Assert.That(role.Owner, Is.EqualTo(BattleResultBodyOwner.Victory));
            Assert.That(role.TryStartLevelUp(), Is.False);
            role.PrepareStagePass();
            Assert.That(role.Owner, Is.EqualTo(BattleResultBodyOwner.None));
            Assert.That(role.TryStartLevelUp(), Is.True);
            Assert.That(role.Owner, Is.EqualTo(BattleResultBodyOwner.LevelUp));
            Assert.That(role.NormalGrab, Is.True);
            role.Step();
            Assert.That(role.Binding.RequestedNativeActionIndex, Is.EqualTo(59));
            Assert.That(role.HasTask, Is.True);
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void LevelUpCallbackForcesOnlyAnExistingNonNormalGrab(bool normal, bool acquired)
        {
            var role = new BattleResultRoleBody("PAL3A", new BattleBodyAnimationBinding(Clip("idle")),
                new Dictionary<int, BattlePhysicalAnimation>(), new BattleResultBodyState(100, 100));
            Assert.That(role.TryGrab(BattleResultBodyOwner.External, normal), Is.True);
            Assert.That(role.TryStartLevelUp(), Is.EqualTo(acquired));
            Assert.That(role.Owner, Is.EqualTo(acquired ? BattleResultBodyOwner.LevelUp : BattleResultBodyOwner.External));
        }

        private sealed class BodyPort : IBattleResultBodyPort
        {
            public BattleResultRoleBody Role;

            public bool TryStartLevelUp(string actor) => Role.TryStartLevelUp();
        }

        [Test]
        public void NativeLevelUpCallbackWaitsForWinReleaseAndStartsTextInItsRegistrationPass()
        {
            BattleScenario scenario = BattleSessionTests.Scenario();
            scenario.Variant = "PAL3A";
            scenario.DataFingerprint = "level";
            scenario.Participants[1].Vitals[0] = 0;
            int[] thresholds = Enumerable.Repeat(int.MaxValue, 101).ToArray();
            thresholds[0] = thresholds[1] = 0;
            thresholds[2] = 10;
            var definitions = new BattleDefinitions("PAL3A", "level", Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(), new[]
            {
                new BattleActorDefinition(new BattleActorData { InstanceId = "Ally", ExperienceThresholds = thresholds,
                    AttributesByLevel = Enumerable.Range(0, 101).Select(_ => new int[12]).ToArray() }),
                new BattleActorDefinition(new BattleActorData { InstanceId = "Enemy", ExperienceReward = 20 })
            });
            var combat = new BattleSession(new BattleSetup(scenario), new BattleSessionTests.Catalog(), definitions: definitions);
            combat.Step();
            var clips = new Dictionary<int, BattlePhysicalAnimation> { [32] = Clip("idle"), [49] = Clip("win"), [59] = Clip("level", 48000) };
            var port = new BodyPort { Role = new BattleResultRoleBody("PAL3A", new BattleBodyAnimationBinding(clips[32]), clips, new BattleResultBodyState(100, 100), true) };
            var presentation = new BattleResultPresentation("PAL3A", combat.Result, combat.Events, port);
            for (int frame = 0; frame < 100 && presentation.Current.Kind != BattleResultStepKind.LevelUpBody; frame++)
            {
                presentation.Step();
                presentation.SkipCurrent();
            }
            Assert.That(presentation.Current.Kind, Is.EqualTo(BattleResultStepKind.LevelUpBody));
            presentation.Step();
            Assert.That(presentation.HasExecuted, Is.False);
            Assert.That(presentation.CanSkip, Is.False);
            port.Role.Step();
            while (port.Role.HasTask)
                port.Role.Step();
            presentation.Step();
            Assert.That(presentation.Current.Kind, Is.EqualTo(BattleResultStepKind.LevelUpBody), "Win's normal grab is released by the next stage pass");
            port.Role.PrepareStagePass();
            presentation.Step();
            Assert.That(presentation.Current.Kind, Is.EqualTo(BattleResultStepKind.LevelUp));
            Assert.That(presentation.HasExecuted, Is.True);
            Assert.That(presentation.Elapsed, Is.Zero);
            port.Role.Step();
            Assert.That(port.Role.Binding.RequestedNativeActionIndex, Is.EqualTo(59));
            Assert.That(port.Role.HasTask, Is.True, "Five-second text begins without waiting for body59 Over");
        }

        private static BattleSession Growth(string variant, bool hasNext)
        {
            BattleScenario scenario = BattleSessionTests.Scenario();
            scenario.Variant = variant;
            scenario.DataFingerprint = "phases";
            scenario.Participants[1].Vitals[0] = 0;
            foreach ((int id, int group, int slot) in new[] { (316, 0, 0), (317, 0, 1), (319, 0, 2), (301, 3, 0), (302, 3, 1), (330, 5, 0), (331, 5, 1), (332, 5, 2) })
                scenario.Participants[0].Skills.Add(new BattleSkill { Id = id, NativeMagicGroup = group, NativeMagicSlot = slot, Mastered = id is 316 or 301, Uses = id is 316 or 301 ? 5 : 0 });
            BattleAbilityData Ability(int id, int group) => new()
            {
                Id = id,
                IsMagic = true,
                SkillTypeId = 1,
                ElementMask = group == 0 ? 1 : group == 3 ? 4 : 5,
                ApplicableIdentities = new[] { 0 },
                UsesBeforeLevelUp = new[] { 0, 1, 3, 5 },
                LearningLevel = 1,
                NextSkillRequiredProficiency = 1
            };
            var data = scenario.Participants[0].Skills.Select(skill => Ability(skill.Id, skill.NativeMagicGroup)).ToDictionary(skill => skill.Id);
            if (hasNext)
            {
                data[316].NextSkillId = 317;
                data[317].NextSkillId = 319;
                data[301].NextSkillId = 302;
                data[316].Unlocks = new[] { new BattleSkillUnlock(330, 301, 4, 4, 1) };
                data[330].Unlocks = new[] { new BattleSkillUnlock(331, 301, 4, 1, 1) };
                data[319].Unlocks = new[] { new BattleSkillUnlock(332, 301, 4, 1, 1) };
            }
            else
            {
                data[301].Unlocks = new[] { new BattleSkillUnlock(317, 316, 4, 4, 1) };
                data[317].Unlocks = new[] { new BattleSkillUnlock(332, 316, 1, 1, 1) };
            }
            var definitions = new BattleDefinitions(variant, "phases", data.Values.Select(skill => new BattleAbilityDefinition(skill)), Array.Empty<BattleAbilityDefinition>(),
                new[] { new BattleActorDefinition(new BattleActorData { InstanceId = "Ally", Identity = 0 }), new BattleActorDefinition(new BattleActorData { InstanceId = "Enemy" }) });
            var combat = new BattleSession(new BattleSetup(scenario), new BattleSessionTests.Catalog(), definitions: definitions);
            combat.Step();
            Assert.That(combat.Status, Is.EqualTo(BattleSessionStatus.Completed));
            return combat;
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void CompletePublishedMagicPhasesChooseHighestTiersAndRescanOnlyAfterNextLearning(string variant)
        {
            BattleSession combat = Growth(variant, true);
            BattleEvent[] prompts = combat.Events.Where(entry => entry.Kind is BattleEventKind.SkillImproved or BattleEventKind.SkillLearned).ToArray();
            Assert.That(prompts.Select(entry => entry.AbilityId), Is.EqualTo(new[] { 316, 301, 330, 331, 317, 319, 302, 332 }));
            Assert.That(prompts.Take(2).Select(entry => entry.ValueAfter), Is.EqualTo(new[] { 4, 4 }));
            Assert.That(prompts.Skip(2).All(entry => entry.Kind == BattleEventKind.SkillLearned), Is.True);
            BattleSession compositeOnly = Growth(variant, false);
            Assert.That(compositeOnly.Events.Where(entry => entry.Kind is BattleEventKind.SkillImproved or BattleEventKind.SkillLearned).Select(entry => entry.AbilityId),
                Is.EqualTo(new[] { 316, 301, 317 }));
            Assert.That(compositeOnly.Result.Actors.Single(actor => actor.Id == "Ally").Skills.Single(skill => skill.Id == 332).Mastered, Is.False);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void NativeMagicSlotsUseSixGroupsThenIdAndPreserveExplicitSaveSlots(string variant)
        {
            int[] ids = { 316, 301, 312, 330, 307, 345, 321 };
            int[] masks = { 1, 4, 1, 5, 8, 2, 16 };
            var definitions = new BattleDefinitions(variant, "layout", ids.Select((id, index) => new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = id,
                IsMagic = true,
                SkillTypeId = 1,
                ElementMask = masks[index],
                ApplicableIdentities = new[] { 0 }
            })), Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleActorDefinition>());
            var skills = new List<BattleSkill>();
            Assert.That(BattleMagicSlots.Capture(variant, definitions, 0, skills).Select(slot => slot.SkillId), Is.EqualTo(new[] { 312, 316, 345, 307, 301, 321, 330 }));
            skills.Add(new BattleSkill { Id = 316, NativeMagicGroup = 0, NativeMagicSlot = 0 });
            skills.Add(new BattleSkill { Id = 312, NativeMagicGroup = 0, NativeMagicSlot = 1 });
            Assert.That(BattleMagicSlots.Capture(variant, definitions, 0, skills).Take(2).Select(slot => slot.SkillId), Is.EqualTo(new[] { 316, 312 }));
        }

        [Test]
        public void CataloguePreservesUnlearnedSlotsAndExcludesCombatOnlyFiveSpiritMagic()
        {
            var definitions = new BattleDefinitions("PAL3A", "layout", new[]
            {
                new BattleAbilityDefinition(new BattleAbilityData { Id = 264, IsMagic = true, SkillTypeId = 0, ElementMask = 1, ApplicableIdentities = new[] { 0 } }),
                new BattleAbilityDefinition(new BattleAbilityData { Id = 312, IsMagic = true, SkillTypeId = 1, ElementMask = 1, ApplicableIdentities = new[] { 0 } }),
                new BattleAbilityDefinition(new BattleAbilityData { Id = 316, IsMagic = true, SkillTypeId = 3, ElementMask = 1, ApplicableIdentities = new[] { 0 } })
            }, Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleActorDefinition>());
            IReadOnlyList<BattleMagicSlot> slots = BattleMagicSlots.Capture("PAL3A", definitions, 0,
                new[] { new BattleSkill { Id = 316, Mastered = true } });
            Assert.That(slots.Select(slot => (slot.SkillId, slot.Slot)), Is.EqualTo(new[] { (312, 0), (316, 1) }));
        }

        [Test]
        public void ScenarioRejectsDuplicateNativeMagicSlots()
        {
            BattleScenario scenario = BattleSessionTests.Scenario();
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = 301, NativeMagicGroup = 0, NativeMagicSlot = 0 });
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = 302, NativeMagicGroup = 0, NativeMagicSlot = 0 });
            Assert.That(scenario.Validate().Any(error => error.Contains("multiple skills occupy")), Is.True);
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(6, 0)]
        [TestCase(0, 9)]
        public void ScenarioRejectsHalfConfiguredOrOutOfBoundsNativeMagicSlots(int group, int slot)
        {
            BattleScenario scenario = BattleSessionTests.Scenario();
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = 301, NativeMagicGroup = group, NativeMagicSlot = slot });
            Assert.That(scenario.Validate().Any(error => error.Contains("native magic group")), Is.True);
        }
    }
}
