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
    using NUnit.Framework;
    using ResourceBrowser;
    using UnityEngine;

    [Category("LocalGameData")]
    public sealed class BattleMagicSwordResourceTests
    {
        private BattleData _data;

        [SetUp]
        public void LoadOriginalData()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root))
                Assert.Ignore("Local original-game data is not available.");
            _data = new BattleData(root, 936);
        }

        [TearDown]
        public void Dispose() => _data?.Dispose();

        private BattleScenario Scenario()
        {
            var scenario = new BattleScenario { Variant = BattleData.Variant, Scene = _data.Scenes.First(), DataFingerprint = _data.Fingerprint };
            scenario.Participants.Add(_data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = _data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(_data, id));
            scenario.Participants.Add(_data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            return scenario;
        }

        [Test]
        public void AllOriginalDualElementSpellsAreCapturedAsSupportedOrdinaryMagic()
        {
            BattleScenario scenario = Scenario();
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, new BattleSetup(scenario));
            int[] ids = Enumerable.Range(326, 8).Concat(BattleData.Variant == "PAL3" ? Enumerable.Range(241, 7) : Array.Empty<int>()).ToArray();
            foreach (int id in ids)
            {
                BattleAbilityDefinition skill = definitions.Skills[id];
                Assert.That(skill.Supported, Is.True, id + ": " + skill.UnsupportedReason);
                Assert.That(skill.IsMagic, Is.True, id.ToString());
                Assert.That(skill.SkillTypeId, Is.InRange(1, 3));
                Assert.That(skill.Handler, Is.EqualTo(BattleSpecialHandler.None));
                Assert.That((skill.ElementMask & (skill.ElementMask - 1)) != 0, Is.True, id.ToString());
            }
        }

#if PAL3
        [Test]
        public void AllTwentyFourWeaponSkillsUseTheirActualLinkersAndWeaponAnimation()
        {
            BattleScenario scenario = Scenario();
            BattleParticipant source = scenario.Participants[0];
            source.Equipment[0] = BattleMagicSwordRules.WeaponId;
            source.MagicSword = new BattleMagicSword { Energy = 200 };
            foreach (int id in Enumerable.Range(2201, 24))
            {
                source.MagicSword.SkillId = id;
                var setup = new BattleSetup(scenario);
                BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, setup);
                var catalog = new BattleAnimationCatalog(_data, setup, definitions);
                BattleAbilityDefinition skill = definitions.Skills[id];
                Assert.That(skill.Supported, Is.True, id + ": " + skill.UnsupportedReason);
                BattleMagicSwordStageProfile profile = catalog.MagicSword(source.InstanceId, skill);
                Assert.That((profile.FirstGroupId, profile.SecondGroupId, profile.FirstWaitSeconds, profile.SecondWaitSeconds),
                    Is.EqualTo((415 + id - 2201, id == 2215 ? 7 : -1, 0f, 0f)), id.ToString());
                Assert.That(catalog.Ability(source.InstanceId, skill).NativeWorkTimeSeconds, Is.Null);
                Assert.That(profile.BodyAnimationTicks, Is.GreaterThan(0));
            }
        }

        [Test]
        public void ActualWeaponGroupsAndTheirCallbacksFinishWithoutRenderingOrCombatTime()
        {
            using var preview = new ResourceModelPreview(_data.Resources, false);
            using var director = new BattleEffectDirector(_data, preview);
            long frame = 0;
            foreach (int group in Enumerable.Range(415, 24).Append(7))
            {
                long handle = director.Start(new BattleEffectRequest(group, "A", new[] { "E" }, default, 1, absoluteTime: true));
                int steps = 0;
                while (director.IsRunning(handle) && steps++ < 3000)
                    director.Advance(++frame, 0, (float)BattleSession.StepSeconds);
                Assert.That(director.IsRunning(handle), Is.False, "Native effect group " + group + " did not finish on the absolute clock.");
            }
        }
#endif
    }
}
