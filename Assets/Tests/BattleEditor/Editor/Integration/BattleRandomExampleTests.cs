// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using Core.Contract.Enums;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Random = System.Random;

    [Category("LocalGameData")]
    public sealed class BattleRandomExampleTests
    {
        private BattleData _data;
        private BattleRandomExampleBuilder _builder;

        [OneTimeSetUp]
        public void Load()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
                Assert.Ignore("Original game data is required.");
            _data = new BattleData(root, 936);
            _builder = new BattleRandomExampleBuilder(_data);
        }

        [OneTimeTearDown]
        public void Dispose() => _data?.Dispose();

        [Test]
        public void ManualRosterCandidatesMatchCombatSidesAndSearchOriginalNames()
        {
            MethodInfo candidates = typeof(BattleEditorWindow).GetMethod("RosterCandidates", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (BattleSide side in Enum.GetValues(typeof(BattleSide)))
            {
                int[] ids = (int[])candidates.Invoke(null, new object[] { _data, side, "", Array.Empty<BattleParticipant>() });
                int[] expected = _data.Database.CombatActorInfos.Where(pair => pair.Key > 0 &&
                    (side == BattleSide.Ally ? pair.Value.Type == CombatActorType.MainActor && BattleData.PlayerIdentity(pair.Key) >= 0 :
                        pair.Value.Type != CombatActorType.MainActor)).Select(pair => pair.Key).OrderBy(id => id).ToArray();
                Assert.That(ids, Is.EqualTo(expected));
                Assert.That(ids, Is.Not.Empty);
                string name = _data.Database.CombatActorInfos[ids[0]].Name;
                Assert.That((int[])candidates.Invoke(null, new object[] { _data, side, name, Array.Empty<BattleParticipant>() }), Does.Contain(ids[0]));
                BattleParticipant existing = _data.CreateParticipant(ids[0], side, BattleSlot.Water);
                int[] available = (int[])candidates.Invoke(null, new object[] { _data, side, name, new[] { existing } });
                Assert.That(available.Contains(ids[0]), Is.EqualTo(side == BattleSide.Enemy));
            }
        }

        [Test]
        public void ImportedOppositeSideRosterIsRejectedBeforeCreatingBattlePreview()
        {
            BattleScenario scenario = _builder.Create(new Random(13));
            BattleParticipant main = scenario.Participants.First(actor => actor.Side == BattleSide.Ally);
            BattleParticipant monster = scenario.Participants.First(actor => actor.Side == BattleSide.Enemy);
            (main.Side, monster.Side) = (monster.Side, main.Side);
            (main.Slot, monster.Slot) = (monster.Slot, main.Slot);
            Assert.That(_data.Validate(scenario), Is.Empty);
            using var session = new BattleEditorSession();
            session.Load(_data.Resources.Root, 936);
            ArgumentException error = Assert.Throws<ArgumentException>(() => session.StartBattle(scenario));
            Assert.That(error.Message, Does.Contain(main.ActorId.ToString()));
            Assert.That(error.Message, Does.Contain(monster.ActorId.ToString()));
            Assert.That(session.Preview, Is.Null);
            Assert.That(session.Battle, Is.Null);
        }

        [Test]
        public void ImportedDuplicateMainActorCanBePreviewedButCannotStartABattle()
        {
            BattleScenario scenario = _builder.Create(new Random(13));
            BattleParticipant main = scenario.Participants.First(actor => actor.Side == BattleSide.Ally);
            BattleParticipant other = scenario.Participants.First(actor => actor.Side == BattleSide.Ally && actor != main);
            BattleParticipant duplicate = main.Copy();
            duplicate.InstanceId = other.InstanceId;
            duplicate.Slot = other.Slot;
            scenario.Participants[scenario.Participants.IndexOf(other)] = duplicate;
            scenario = BattleScenarioJson.Deserialize(BattleScenarioJson.Serialize(scenario));

            Assert.That(_data.Validate(scenario), Is.Empty);
            using var session = new BattleEditorSession();
            session.Load(_data.Resources.Root, 936);
            ArgumentException error = Assert.Throws<ArgumentException>(() => session.StartBattle(scenario));
            Assert.That(error.Message, Does.Contain($"{main.ActorId}：同一主角只能参加一次战斗"));
            Assert.That(session.Preview, Is.Null);
            Assert.That(session.Battle, Is.Null);
        }

        [Test]
        public void RandomExamplesUseFourFullyEquippedAlliesSlightlyAboveTheEnemyLevel()
        {
            var scenes = new HashSet<string>();
            var allies = new HashSet<int>();
            var enemies = new HashSet<int>();
            var levels = new HashSet<int>();
            var weapons = new HashSet<int>();
            for (int seed = 0; seed < 128; seed++)
            {
                BattleScenario scenario = _builder.Create(new Random(seed));
                Assert.That(_data.Validate(scenario, forBattle: true), Is.Empty, "seed " + seed);
                scenes.Add(scenario.Scene);
                BattleParticipant[] team = scenario.Participants.Where(actor => actor.Side == BattleSide.Ally).ToArray();
                BattleParticipant[] opponents = scenario.Participants.Where(actor => actor.Side == BattleSide.Enemy).ToArray();
                Assert.That(team.Length, Is.EqualTo(4));
                Assert.That(opponents.Length, Is.InRange(1, 6));
                Assert.That(team.Select(actor => actor.ActorId).Distinct().Count(), Is.EqualTo(team.Length));
                Assert.That(opponents.Select(actor => actor.Level).Distinct().Count(), Is.EqualTo(1));
                Assert.That(team.Select(actor => actor.Level), Is.All.EqualTo(Math.Min(100, opponents[0].Level + 3)));
                Assert.That(scenario.ToonRendering, Is.True);
                levels.Add(team[0].Level);
                foreach (var actor in team)
                    allies.Add(actor.ActorId);
                foreach (var actor in opponents)
                    enemies.Add(actor.ActorId);
                BattleSetup setup = _data.Capture(scenario);
                BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, setup);
                Assert.That(scenario.Items.Select(item => item.Id), Is.EquivalentTo(definitions.Items.Values
                    .Where(item => item.BattleUsable && item.Supported).Select(item => item.Id)));
                Assert.That(scenario.Items.Select(item => item.Count), Is.All.EqualTo(99));
                foreach (BattleParticipant actor in team)
                {
                    weapons.Add(actor.Equipment[0]);
                    Assert.That(actor.Equipment, Has.Length.EqualTo(6));
                    Assert.That(actor.Equipment, Is.All.GreaterThan(0));
                    Assert.That(actor.Statuses, Is.Empty, "Only equipment effects may add statuses.");
                    BattleActorDefinition compiled = definitions.Actors[actor.InstanceId];
                    Assert.That(compiled.UnsupportedReason, Is.Empty);
                    Assert.That(compiled.Equipment.Select(item => item.UnsupportedReason), Is.All.Empty);
                    foreach (int id in actor.Equipment)
                        Assert.That(_data.Database.GameItemInfos[id].ApplicableActors.Select(identity => (int)identity),
                            Does.Contain(BattleData.PlayerIdentity(actor.ActorId)));
                    Assert.That(actor.Experience, Is.EqualTo(_data.Layout.LevelExperienceTable[actor.Level]));
                    Assert.That(actor.AttributeSource, Is.EqualTo(BattleAttributeSource.LevelGrowth));
                    Assert.That(actor.Attributes, Is.EqualTo(BattleAttributeCalculator.Calculate(actor, definitions.Actors[actor.InstanceId], scenario.Variant)));
                    Assert.That(actor.Vitals, Is.EqualTo(actor.Attributes.Take(3)));
                }
                var catalog = new BattleAnimationCatalog(_data, setup, definitions);
                var battle = new BattleSession(setup, catalog, definitions: definitions);
                Assert.That(battle.Status, Is.EqualTo(BattleSessionStatus.Running), "seed " + seed);
                battle.Step();
                Assert.That(battle.Status, Is.EqualTo(BattleSessionStatus.Running), "seed " + seed);
            }
            Assert.That(scenes.Count, Is.GreaterThan(8));
            Assert.That(allies.Count, Is.EqualTo(5));
            Assert.That(enemies.Count, Is.GreaterThan(20));
            Assert.That(levels.Count, Is.GreaterThan(10));
            Assert.That(weapons.Count, Is.GreaterThan(10));
        }

        [UnityTest]
        public IEnumerator AutomaticMusicSurvivesPreviewRefreshAndSharesTransportWithRealBattle()
        {
            BattleScenario scenario = _builder.Create(new Random(871));
#if PAL3
            scenario.Scene = "z39";
#else
            scenario.Scene = "q09";
#endif
            BattleSetup setup = _data.Capture(scenario);
            using var preview = new BattlePreview(_data, setup);
            const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
            var pending = (IDictionary)typeof(BattleAudioPreview).GetField("_pending", members).GetValue(preview.Audio);
            double until = EditorApplication.timeSinceStartup + 15;
            while (pending.Count > 0 && EditorApplication.timeSinceStartup < until)
                yield return null;
            Assert.That(pending.Count, Is.Zero);
            Assert.That(preview.Audio.Diagnostics, Is.Empty);
            var root = (GameObject)typeof(BattleAudioPreview).GetField("_root", members).GetValue(preview.Audio);
            AudioSource music = root.GetComponentsInChildren<AudioSource>().Single();
            Assert.That(music.clip.samples, Is.GreaterThan(48000));
            Assert.That(music.loop, Is.True);
            preview.Sample(2.5f);
            Assert.That(preview.Diagnostics.Any(message => message.Contains("Editor fallback")), Is.True);
            Assert.That(typeof(BattleAudioPreview).GetField("_time", members).GetValue(preview.Audio), Is.EqualTo(2.5d));
            preview.Audio.PlaybackEnabled = true;
            double started = EditorApplication.timeSinceStartup;
            while (EditorApplication.timeSinceStartup - started < .3)
            {
                preview.Sample(2.5f + (float)(EditorApplication.timeSinceStartup - started));
                yield return null;
            }
            Assert.That(music.time, Is.InRange(2.5f, 3.1f), "Animation preview must advance the same logical clock as music.");
            preview.Audio.PlaybackEnabled = false;
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, setup);
            var catalog = new BattleAnimationCatalog(_data, setup, definitions);
            var battle = new BattleSession(setup, catalog, definitions: definitions, effects: preview.Effects);
            preview.BindBattle(battle, catalog);
            preview.ApplyBattle(battle, catalog);
            Assert.That(root.GetComponentsInChildren<AudioSource>().Single(), Is.SameAs(music), "Binding a real battle must not restart the common music owner.");
            Assert.That(preview.Diagnostics.Any(message => message.Contains("Editor fallback")), Is.True);
            preview.Audio.Diagnostics.Add("missing explicit music fixture");
            preview.ApplyBattle(battle, catalog);
            Assert.That(preview.Diagnostics, Does.Contain("missing explicit music fixture"));
        }

        [Test]
        public void SameAuthoringSeedReproducesRosterAndSceneWithoutMutatingData()
        {
            BattleScenario first = _builder.Create(new Random(871));
            BattleScenario second = _builder.Create(new Random(871));
            Assert.That(second.Scene, Is.EqualTo(first.Scene));
            Assert.That(second.Seed, Is.EqualTo(first.Seed));
            Assert.That(second.Participants.Select(actor => (actor.ActorId, actor.Side, actor.Slot, actor.Level)),
                Is.EqualTo(first.Participants.Select(actor => (actor.ActorId, actor.Side, actor.Slot, actor.Level))));
            Assert.That(second.Participants.SelectMany(actor => actor.Equipment), Is.EqualTo(first.Participants.SelectMany(actor => actor.Equipment)));
            first.Participants[0].Attributes[0] = 987654;
            Assert.That(_builder.Create(new Random(871)).Participants[0].Attributes[0], Is.Not.EqualTo(987654));
        }

        [Test]
        public void MusicUsesVerifiedCityMappingAndDocumentsMissingContext()
        {
            var diagnostics = new List<string>();
            var scenario = new BattleScenario
            {
#if PAL3
                Scene = "z01"
#else
                Scene = "m01a"
#endif
            };
            Assert.That(BattleMusicResolver.Resolve(_data, scenario, diagnostics).ToUpperInvariant(),
#if PAL3
                Is.EqualTo("PI24"));
#else
                Is.EqualTo("P54"));
#endif
            Assert.That(diagnostics, Is.Empty);
#if PAL3
            scenario.Scene = "z08";
            Assert.That(BattleMusicResolver.Resolve(_data, scenario, diagnostics).ToUpperInvariant(), Is.EqualTo("PI24").Or.EqualTo("PI26").Or.EqualTo("PI26A"));
            Assert.That(diagnostics, Has.Count.EqualTo(1));
            diagnostics.Clear();
            scenario.Scene = "z39";
#else
            scenario.Scene = "q09";
#endif
            Assert.That(BattleMusicResolver.Resolve(_data, scenario, diagnostics), Is.Not.Empty);
            Assert.That(diagnostics, Has.Count.EqualTo(1));
            scenario.Music = "SpecialBoss.mp3";
            Assert.That(BattleMusicResolver.Resolve(_data, scenario, diagnostics), Is.EqualTo("SpecialBoss.mp3"));
            scenario.Music = "NONE";
            Assert.That(BattleMusicResolver.Resolve(_data, scenario, diagnostics), Is.EqualTo("NONE"));
        }
    }
}
