// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using UnityEngine;

    [Category("LocalGameData")]
    public sealed class BattleSwordSparkTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;

        private static BattleScenario Scenario(BattleData data)
        {
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = data.Scenes.First(),
                DataFingerprint = data.Fingerprint,
                Music = "NONE",
                MotionBlur = false,
                Seed = 17
            };
            scenario.Participants.Add(data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(data, id));
            scenario.Participants.Add(data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            foreach (BattleParticipant actor in scenario.Participants)
            {
                actor.Skills.Clear();
                actor.AttributeSource = BattleAttributeSource.ExplicitEffective;
                actor.Attributes = new[] { 1000, 100, 100, 1, 10, 1, 0, 0, 0, 0, 0, 0 };
                actor.Vitals = new[] { 1000, 100, 100 };
            }
#if PAL3
            scenario.Participants[0].Equipment[0] = BattleMagicSwordRules.WeaponId;
            scenario.Participants[0].MagicSword = new BattleMagicSword { Energy = 200 };
#endif
            return scenario;
        }

        [Test]
        public void PausedPublicationPreservesStarsBothRandomStreamsAndBattleStateWhileRestartAndStopReleaseOwners()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
                Assert.Ignore("Original game data is required.");
            using var editor = new BattleEditorSession();
            editor.Load(root, 936);
            editor.StartBattle(Scenario(editor.Data));
            for (int step = 0; step < 50; step++)
                editor.StepBattle();
            BattlePreview previous = editor.Preview;
            if (editor.Battle.Definitions.Variant == "PAL3A")
            {
                Assert.That(previous.SwordSparkFrames, Is.Empty);
                return;
            }
            Assert.That(previous.SwordSparkFrames, Is.Not.Empty);
            BattleRandom gameplay = (BattleRandom)typeof(BattleSession).GetField("_random", Members).GetValue(editor.Battle);
            var effects = new BattleRandom(888);
            typeof(BattleEffectDirector).GetField("_effectRandom", Members).SetValue(previous.Effects, effects);
            string BattleState() => JsonConvert.SerializeObject(new
            {
                editor.Battle.Tick,
                editor.Battle.SimulationTick,
                editor.Battle.Actors,
                editor.Battle.Events,
                editor.Battle.Inputs,
                editor.Battle.Inventory,
                GameplayRandom = gameplay.State,
                EffectsRandom = effects.State
            });
            string state = BattleState();
            string stars = JsonConvert.SerializeObject(previous.SwordSparkFrames);
            editor.IsPlaying = false;
            for (int repaint = 0; repaint < 100; repaint++)
            {
                editor.Tick(1);
                editor.SyncBattle();
            }
            Assert.That(BattleState(), Is.EqualTo(state));
            Assert.That(JsonConvert.SerializeObject(previous.SwordSparkFrames), Is.EqualTo(stars));
            Material MaterialOwner(BattlePreview preview)
            {
                var material = new Material(Shader.Find("Pal3/Effect"));
                typeof(BattlePreview).GetField("_swordSparkMaterial", Members).SetValue(preview, material);
                return material;
            }
            Material oldMaterial = MaterialOwner(previous);
            editor.Restart();
            Assert.That(oldMaterial == null, Is.True);
            Assert.That(previous.SwordSparkFrames, Is.Empty);
            Assert.That(editor.Preview.SwordSparkFrames, Is.Empty);
            Material nextMaterial = MaterialOwner(editor.Preview);
            editor.Stop();
            Assert.That(nextMaterial == null, Is.True);
        }
    }
}
