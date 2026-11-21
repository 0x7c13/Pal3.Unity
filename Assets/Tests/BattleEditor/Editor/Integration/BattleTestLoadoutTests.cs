// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using Battle;
    using Battle.Presentation;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using NUnit.Framework;

    [Category("LocalGameData")]
    public sealed class BattleTestLoadoutTests
    {
        private BattleData _data;

        [OneTimeSetUp]
        public void Load()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
                Assert.Ignore("Original game data is required.");
            _data = new BattleData(root, 936);
        }

        [OneTimeTearDown]
        public void Dispose() => _data?.Dispose();

        [Test]
        public void FreshTestInventoryContainsEverySupportedBattleItemAndIsCapturedWithoutChangingOtherScenarios()
        {
            BattleScenario scenario = BattleTestLoadoutBuilder.CreateScenario(_data);
            BattleScenario other = BattleTestLoadoutBuilder.CreateScenario(_data);
            scenario.Scene = _data.Scenes.First();
            scenario.Encounter = BattleEncounter.AllyAmbush;
            scenario.Participants = Scenario(_data.CreateTestParticipant(101, BattleSide.Ally, BattleSlot.Water)).Participants;
            BattleSetup setup = _data.Capture(scenario);
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, setup);
            int[] expected = definitions.Items.Values.Where(item => item.BattleUsable && item.Supported).Select(item => item.Id).OrderBy(id => id).ToArray();
            Assert.That(expected.Length, Is.GreaterThan(50));
            Assert.That(scenario.Items.Select(item => item.Id), Is.EqualTo(expected));
            var catalog = new BattleAnimationCatalog(_data, setup, definitions);
            using var preview = new BattlePreview(_data, setup);
            var battle = new BattleSession(setup, catalog, definitions: definitions, effects: preview.Effects);
            preview.BindBattle(battle, catalog);
            Assert.That(battle.Inventory.Select(item => item.Id), Is.EqualTo(expected));
            Assert.That(battle.Inventory.Select(item => item.Count), Is.All.EqualTo(99));
            int[] treasureIds = Enumerable.Range(6119, BattleData.Variant == "PAL3" ? 7 : 6).ToArray();
            Assert.That(battle.TreasureUses.Select(item => item.Id), Is.EqualTo(treasureIds));
            Assert.That(battle.TreasureUses.Select(item => item.Count), Is.All.EqualTo(3));
            var hud = new BattleHudPresenter();
            hud.Synchronize(battle);
            hud.Open(BattleHudPage.Items);
            hud.SetCategory(definitions.Items[6119].ItemType - 1);
            Assert.That(hud.Abilities().Single(item => item.Definition.Id == 6119).Denied, Is.Null);
            scenario.TreasureUses[0].Count = 0;
            Assert.That(other.TreasureUses[0].Count, Is.EqualTo(3));
            Assert.That(battle.TreasureUses[0].Count, Is.EqualTo(3));
            scenario.Items[0].Count = 1;
            Assert.That(other.Items[0].Count, Is.EqualTo(99));
            Assert.That(battle.Inventory[0].Count, Is.EqualTo(99));
            Assert.That(new BattleScenario().Items, Is.Empty, "Runtime or loaded scenarios must not receive editor test defaults.");
        }

        private BattleScenario Scenario(BattleParticipant ally)
        {
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = _data.Scenes.First(),
                DataFingerprint = _data.Fingerprint,
                Encounter = BattleEncounter.AllyAmbush
            };
            scenario.Participants.Add(ally);
            int enemyId = _data.Database.CombatActorInfos.Keys.OrderBy(id => id)
                .First(id => id > 0 && BattleAnimationCatalog.IsBasicEnemy(_data, id));
            scenario.Participants.Add(_data.CreateParticipant(enemyId, BattleSide.Enemy, BattleSlot.Water));
            return scenario;
        }

#if PAL3
        [TestCase(0, 34, 6)]
        [TestCase(1, 32, 5)]
        [TestCase(2, 30, 5)]
        [TestCase(3, 34, 5)]
        [TestCase(4, 33, 5)]
#else
        [TestCase(0, 32, 8)]
        [TestCase(1, 35, 7)]
        [TestCase(2, 33, 7)]
        [TestCase(3, 33, 7)]
        [TestCase(4, 31, 7)]
#endif
        public void EveryLegalPersonalAbilityAppearsAtMaximumProficiencyInItsNativeMenu(int identity, int total, int techniques)
        {
            int actorId = ActorConstants.MainActorCombatActorIdMap[(PlayerActorId)identity];
            BattleParticipant actor = _data.CreateTestParticipant(actorId, BattleSide.Ally, BattleSlot.Water);
            Assert.That(actor.Skills, Has.Count.EqualTo(total));
            Assert.That(actor.Skills.All(skill => skill.Mastered && skill.Level == 4 && skill.Uses == 0), Is.True);
            Assert.That(actor.Skills.Select(skill => skill.Id).Distinct().Count(), Is.EqualTo(total));
            foreach (BattleSkill skill in actor.Skills)
            {
                var original = _data.Database.SkillInfos[skill.Id];
                Assert.That(original.ApplicableActors, Does.Contain((PlayerActorId)identity));
                Assert.That((int)original.Type, Is.InRange(0, 3));
            }
            BattleSetup setup = _data.Capture(Scenario(actor));
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, setup);
            foreach (BattleSkill skill in actor.Skills)
                Assert.That(definitions.Skills[skill.Id].Supported, Is.True, definitions.Skills[skill.Id].UnsupportedReason);
            var slots = BattleMagicSlots.Capture(BattleData.Variant, definitions, identity, actor.Skills);
            Assert.That(slots, Has.Count.EqualTo(total - techniques));
            Assert.That(slots.All(slot => slot.Group < 6 && slot.Slot < (BattleData.Variant == "PAL3A" ? 9 : 8)), Is.True);
            var battle = new BattleSession(setup, new BattleAnimationCatalog(_data, setup, definitions), definitions: definitions);
            battle.SetAutomatic(BattleSide.Enemy, false);
            var hud = new BattleHudPresenter();
            hud.Synchronize(battle);
            Assert.That(hud.Controller, Is.EqualTo(actor.InstanceId));
            hud.Open(BattleHudPage.Skills);
            Assert.That(hud.Abilities().Count, Is.EqualTo(techniques));
            var displayed = hud.Abilities().Select(ability => ability.Definition.Id).ToList();
            hud.Open(BattleHudPage.Magic);
            for (int group = 0; group < 6; group++)
            {
                hud.SetCategory(group);
                displayed.AddRange(hud.Abilities().Select(ability => ability.Definition.Id));
            }
            Assert.That(displayed, Is.EquivalentTo(actor.Skills.Select(skill => skill.Id)));
        }

        [Test]
        public void RandomExamplesUnlockAllPersonalSkillsEvenAtLowLevels()
        {
            var builder = new BattleRandomExampleBuilder(_data);
            bool lowLevel = false;
            for (int seed = 0; seed < 32; seed++)
            {
                BattleScenario scenario = builder.Create(new Random(seed));
                foreach (BattleParticipant actor in scenario.Participants.Where(actor => actor.Side == BattleSide.Ally))
                {
                    BattleParticipant expected = _data.CreateTestParticipant(actor.ActorId, actor.Side, actor.Slot);
                    Assert.That(actor.Skills.Select(skill => (skill.Id, skill.Level, skill.Mastered)),
                        Is.EqualTo(expected.Skills.Select(skill => (skill.Id, skill.Level, skill.Mastered))));
                    lowLevel |= actor.Level < 10;
                }
            }
            Assert.That(lowLevel, Is.True);
        }

        [Test]
        public void OriginalFactoryEnemyLoadoutsAndDatabaseRemainUnchanged()
        {
            BattleParticipant original = _data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water);
            BattleParticipant test = _data.CreateTestParticipant(101, BattleSide.Ally, BattleSlot.Water);
            Assert.That(test.Equipment, Is.EqualTo(original.Equipment));
            Assert.That(test.Attributes, Is.EqualTo(original.Attributes));
            Assert.That(test.Vitals, Is.EqualTo(original.Vitals));
            Assert.That(test.MagicSword, Is.Null);
            Assert.That((test.Level, test.Experience), Is.EqualTo((original.Level, original.Experience)));
            test.Skills[0].Mastered = false;
            test.Skills[0].Level = 1;
            Assert.That(_data.CreateTestParticipant(101, BattleSide.Ally, BattleSlot.Water).Skills[0].Level, Is.EqualTo(4));
            Assert.That(_data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water).Skills.Select(skill => (skill.Id, skill.Level, skill.Mastered)),
                Is.EqualTo(original.Skills.Select(skill => (skill.Id, skill.Level, skill.Mastered))));
            int enemyId = _data.Database.CombatActorInfos.Keys.First(id => id > 0 && _data.Database.CombatActorInfos[id].Type != CombatActorType.MainActor);
            foreach (BattleSide side in new[] { BattleSide.Ally, BattleSide.Enemy })
                Assert.That(_data.CreateTestParticipant(enemyId, side, BattleSlot.Water).Skills.Select(skill => (skill.Id, skill.Level, skill.Mastered)),
                    Is.EqualTo(_data.CreateParticipant(enemyId, side, BattleSlot.Water).Skills.Select(skill => (skill.Id, skill.Level, skill.Mastered))));
        }

        [Test]
        public void MasteryDoesNotBypassMpOrDedicatedWeaponWheelAndTransformationGates()
        {
            BattleParticipant actor = _data.CreateTestParticipant(101, BattleSide.Ally, BattleSlot.Water);
            actor.Vitals[2] = 0;
            BattleSetup setup = _data.Capture(Scenario(actor));
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, setup);
            var battle = new BattleSession(setup, new BattleAnimationCatalog(_data, setup, definitions), definitions: definitions);
            var query = new BattleCommand(actor.InstanceId, BattleAction.Skill, abilityId: 301);
            string target = battle.GetTargetCandidates(query).First().Id;
            Assert.That(battle.CanSubmit(new BattleCommand(actor.InstanceId, BattleAction.Skill, target, 301)), Does.Contain("Resource 2"));
            Assert.That(battle.MagicSwordAbilityId(actor.InstanceId), Is.Zero);
#if PAL3A
            Assert.That(actor.Skills.Any(skill => skill.Id is >= 264 and <= 268), Is.False);
            Assert.That(battle.CanSubmitElementWheel(actor.InstanceId, 264), Does.Contain("nine charges"));
            Assert.That(battle.CanSubmit(new BattleCommand(actor.InstanceId, BattleAction.Transform)), Does.Contain("reach three"));
            Assert.That(actor.Skills.Any(skill => skill.Id == BattlePal3ARules.SpiritCaptureSkill), Is.True);
            Assert.That(actor.Skills.Any(skill => skill.Id == BattlePal3ARules.PengxuTransformSkill), Is.False);
            Assert.That(_data.CreateTestParticipant(103, BattleSide.Ally, BattleSlot.Water).Skills
                .Any(skill => skill.Id == BattlePal3ARules.PengxuTransformSkill && skill.Mastered), Is.True);
#else
            Assert.That(actor.Skills.Any(skill => definitions.Skills[skill.Id].SkillTypeId == 4), Is.False);
#endif
        }
    }
}
