// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using Core.Contract.Constants;
    using Core.DataReader.Dat;
    using Game.Effect.Runtime;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using ResourceBrowser;
    using UnityEngine;
    using UnityEngine.TestTools;

    [Category("LocalGameData")]
    public sealed class BattleAbilityResourceTests
    {
        private BattleData _data;
        private readonly List<object> _coverage = new();

        [SetUp]
        public void LoadOriginalData()
        {
            string configured = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            string root = configured ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root))
            {
                if (configured != null)
                    Assert.Fail("Configured original-game data is missing: " + root);
                Assert.Ignore("Local original-game data is not available.");
            }
            _data = new BattleData(root, 936);
            _coverage.Clear();
        }

        [TearDown]
        public void Dispose()
        {
            string output = Environment.GetEnvironmentVariable("PAL3_BATTLE_REPORT_DIR");
            if (output != null)
            {
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, BattleData.Variant + "-" + TestContext.CurrentContext.Test.Name + ".json"),
                    JsonConvert.SerializeObject(_coverage, Formatting.Indented));
            }
            _data?.Dispose();
        }

        private BattleScenario Scenario(int source = 101)
        {
            var scenario = new BattleScenario { Variant = BattleData.Variant, Scene = _data.Scenes.First(), DataFingerprint = _data.Fingerprint };
            bool main = ActorConstants.MainActorCombatActorIdMap.ContainsValue(source);
            scenario.Participants.Add(_data.CreateParticipant(main ? source : 101, BattleSide.Ally, BattleSlot.Water));
            int enemy = main ? _data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(_data, id)) : source;
            scenario.Participants.Add(_data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            // These tests compile each requested record explicitly, so initial actor skills
            // do not cause unrelated numeric-handler diagnostics during catalog construction.
            foreach (BattleParticipant actor in scenario.Participants)
                actor.Skills.Clear();
            return scenario;
        }

        [Test]
        public void AllReachableOrdinaryRecordsCompileTheirOriginalBodyAndBothLinkers()
        {
            BattleScenario basic = Scenario();
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, new BattleSetup(basic));
            var errors = new List<string>();
            int compiled = 0;
            foreach (BattleAbilityDefinition ability in definitions.Skills.Values.Concat(definitions.Items.Values)
                .Where(ability => ability.BattleUsable && (ability.IsItem || ability.SkillTypeId != 4)).OrderBy(ability => ability.Id))
            {
                if (!ability.Supported)
                {
                    _coverage.Add(new
                    {
                        ability.Id,
                        ability.IsItem,
                        status = "numeric-dependency",
                        reason = ability.UnsupportedReason
                    });
                    continue;
                }
                int[] sources = ability.IsItem ? new[] { 101 } : ability.ApplicableIdentities
                    .Where(identity => ActorConstants.MainActorCombatActorIdMap.ContainsKey((Core.Contract.Enums.PlayerActorId)identity))
                    .Select(identity => ActorConstants.MainActorCombatActorIdMap[(Core.Contract.Enums.PlayerActorId)identity]).Distinct().ToArray();
                if (sources.Length == 0)
                    sources = _data.Database.CombatActorInfos.Values.Where(actor => actor.SkillIds.Contains((uint)ability.Id)).Select(actor => (int)actor.Id).ToArray();
                if (sources.Length == 0)
                {
                    _coverage.Add(new
                    {
                        ability.Id,
                        ability.IsItem,
                        status = "unreferenced-record"
                    });
                    continue;
                }
                foreach (int source in sources)
                {
                    try
                    {
                        BattleScenario scenario = Scenario(source);
                        var setup = new BattleSetup(scenario);
                        var catalog = new BattleAnimationCatalog(_data, setup);
                        string actor = scenario.Participants[ActorConstants.MainActorCombatActorIdMap.ContainsValue(source) ? 0 : 1].InstanceId;
                        BattleAbilityStageProfile profile = catalog.AbilityStage(actor, ability);
                        AttackTiming timing = catalog.Ability(actor, ability);
                        Assert.That(timing.NativeWorkTimeSeconds, Is.EqualTo(profile.WorkSeconds));
                        Assert.That(profile.BodyAnimationTicks, Is.GreaterThanOrEqualTo(0));
                        Assert.That(profile.First.GroupId, Is.GreaterThanOrEqualTo(-1));
                        Assert.That(profile.Second.GroupId, Is.GreaterThanOrEqualTo(-1));
                        _coverage.Add(new
                        {
                            ability.Id,
                            ability.IsItem,
                            source,
                            status = "compiled",
                            profile.Kind,
                            profile.BodyAnimationTicks,
                            profile.WorkSeconds,
                            profile.ApproachKind,
                            profile.ReturnKind,
                            first = profile.First.GroupId,
                            second = profile.Second.GroupId
                        });
                        compiled++;
                    }
                    catch (Exception error)
                    {
                        errors.Add($"{ability.Id}, actor {source}: {error.Message}");
                        _coverage.Add(new
                        {
                            ability.Id,
                            ability.IsItem,
                            source,
                            status = "resource-error",
                            reason = error.Message
                        });
                    }
                }
            }
            Assert.That(compiled, Is.GreaterThan(100));
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void ApproachSkillUsesCurrentBodyRadiusWhenAnEnemySlotIsPopulatedAndReplaced()
        {
            BattleScenario scenario = Scenario();
            scenario.SceneElement = 2;
            var setup = new BattleSetup(scenario);
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, setup);
            var catalog = new BattleAnimationCatalog(_data, setup, definitions);
            var session = new BattleSession(setup, catalog, definitions: definitions);
            BattleActorSnapshot source = session.Actors.Single(actor => actor.Side == BattleSide.Ally);
            MethodInfo create = typeof(BattleSession).GetMethod("CreateOriginalMonster", BindingFlags.Instance | BindingFlags.NonPublic);
            int[] monsters = _data.Database.CombatActorInfos.Keys.Where(id => id != 706 && BattleAnimationCatalog.IsBasicEnemy(_data, id)).Take(2).ToArray();
            Assert.That(monsters, Has.Length.EqualTo(2));
            foreach (int monster in monsters)
            {
                create.Invoke(session, new object[] { 6, monster, 0L });
                BattleActorSnapshot target = session.Actors.Single(actor => actor.NativeIndex == 6);
                Assert.That(target.ActorId, Is.EqualTo(monster));
                BattleMovementPlan plan = catalog.AbilityMovement(source, target, definitions.Skills[4101], definitions, false).Single();
                BattleParticipant participant = session.CaptureParticipant(target.Id);
                BattleAnimationMetadata idle = _data.GetAnimationMetadata(_data.ActionPath(participant, _data.IdleAction(participant)));
                BattleAttackPlan attack = catalog.PlanAttack(source.Id, target.Id, catalog.InitialHome(source.Id), catalog.InitialHome(target.Id));
                Assert.That(catalog.Approach(source.Id, target.Id), Is.EqualTo(BattleAnimationCatalog.UnityPosition(attack.Approach)));
                float distance = catalog.Physical(source.Id).Radius(BattlePose.Idle) + (idle.MaximumZ - idle.MinimumZ) * .5f;
#if PAL3A
                if (catalog.Physical(source.Id).WeaponType == 1)
                    distance += 40f;
#endif
                Assert.That(BattleMovement.Distance(plan.To, target.Home), Is.EqualTo(distance).Within(.001));
            }
        }

        [Test]
        public void NpcItemsUseEachPublishedGamesDedicatedItemBodyPath()
        {
            BattleScenario scenario = Scenario();
            var setup = new BattleSetup(scenario);
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, setup);
            var catalog = new BattleAnimationCatalog(_data, setup);
            string actor = scenario.Participants[1].InstanceId;
            int count = 0;
            foreach (BattleAbilityDefinition item in definitions.Items.Values.Where(item => item.BattleUsable))
            {
                BattleAbilityStageProfile profile = catalog.AbilityStage(actor, item);
                int? nativeAction = catalog.AbilityNativeAction(actor, item);
                Assert.That(profile.Kind, Is.EqualTo(BattleAbilityStageKind.Item));
                Assert.That(profile.NpcItemIdle, Is.EqualTo(BattleData.Variant == "PAL3A"));
                if (BattleData.Variant == "PAL3A" || item.IsTreasure)
                    Assert.That(nativeAction, Is.Null);
                else
                    Assert.That(nativeAction, Is.EqualTo(38).Or.EqualTo(39), "NPC items do not use the NPC magic slots 55/56/57.");
                catalog.Ability(actor, item);
                catalog.AbilityBody(actor, item);
                _coverage.Add(new
                {
                    item.Id,
                    source = scenario.Participants[1].ActorId,
                    profile.NpcItemIdle,
                    nativeAction,
                    profile.RetainsBody
                });
                count++;
            }
            Assert.That(count, Is.GreaterThan(50));
        }

#if PAL3
        [UnityTest]
        public IEnumerator OriginalTreasureMovieDecodesAndPresentsItsActualFrames()
        {
            using var preview = new ResourceModelPreview(_data.Resources, false);
            using var movies = new BattleMoviePlayer(_data, preview) { Muted = true };
            preview.Rendering += movies.Render;
            using IEffectMovie film = movies.Play("2DEFFECT\\t102.bik");
            Assert.That(movies.MissingMovies, Is.Empty, "The original local movie archive must contain t102.BIK.");
            bool decoded = false;
            int frames = 0;
            var timeout = Stopwatch.StartNew();
            while (!film.Finished && timeout.Elapsed.TotalSeconds < 60)
            {
                movies.Advance((float)BattleSession.StepSeconds);
                decoded |= movies.HasDecodedFrame;
                if (movies.HasDecodedFrame)
                {
                    RenderTexture rendered = preview.Render(800, 600);
                    if (++frames == 60)
                        VerifyMovieImage(movies.CurrentTexture as Texture2D, rendered);
                }
                yield return null;
            }
            Assert.That(decoded, Is.True, "The actual Bink-derived frames were not decoded by the editor video player.");
            Assert.That(film.Finished, Is.True, "The modal movie did not finish on its original frame clock.");
            Assert.That(movies.BlocksBattleTime, Is.False);
        }

        private static void VerifyMovieImage(Texture2D source, RenderTexture rendered)
        {
            Assert.That(source, Is.Not.Null);
            var image = new Texture2D(rendered.width, rendered.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = rendered;
                image.ReadPixels(new Rect(0, 0, rendered.width, rendered.height), 0, 0);
                image.Apply();
                float error = 0;
                for (int y = 50; y < 600; y += 100)
                    for (int x = 50; x < 800; x += 100)
                    {
                        Color expected = source.GetPixel(x, 599 - y);
                        Color actual = image.GetPixel(x, y);
                        error = Mathf.Max(error, Mathf.Abs(expected.r - actual.r), Mathf.Abs(expected.g - actual.g), Mathf.Abs(expected.b - actual.b));
                    }
                string output = Environment.GetEnvironmentVariable("PAL3_BATTLE_REPORT_DIR");
                if (output != null)
                    File.WriteAllBytes(Path.Combine(output, "PAL3-t102-editor-frame.png"), image.EncodeToPNG());
                Assert.That(error, Is.LessThan(0.025f), "The rendered overlay must preserve the decoded original image and its vertical orientation.");
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
#endif

        private sealed class CameraPort : IEffectCamera
        {
            public void StopList()
            {
            }

            public bool StartList(int index) => true;

            public void SetQuake(float amplitude, float frequency, float decay)
            {
            }
        }

        [UnityTest]
        public IEnumerator EveryOrdinaryEffectGroupRunsToCompletionOnTheScaledClock()
        {
            BattleScenario scenario = Scenario();
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, new BattleSetup(scenario));
            EffectLinkerFile linkers = _data.Read("basedata.cpk\\cbdata\\efflinker.dat", new EffectLinkerFileReader());
            int[] groups = definitions.Skills.Values.Concat(definitions.Items.Values)
                .Where(ability => ability.BattleUsable && (ability.IsItem || ability.SkillTypeId != 4))
                .SelectMany(ability => linkers.SkillIdToEffectGroupMap.TryGetValue(ability.Id, out EffectGroupInfo[] found) ? found : Array.Empty<EffectGroupInfo>())
                .Select(group => group.EffGroupId).Where(group => group >= 0).Distinct().OrderBy(group => group).ToArray();
            using var preview = new ResourceModelPreview(_data.Resources, false);
            using var director = new BattleEffectDirector(_data, preview);
            var catalog = new BattleAnimationCatalog(_data, new BattleSetup(scenario), definitions);
            var session = new BattleSession(new BattleSetup(scenario), catalog, definitions: definitions, effects: director);
            director.Bind(session, new CameraPort());
            long frame = 0;
            var errors = new List<string>();
            foreach (int group in groups)
            {
                long handle = 0;
                Exception failure = null;
                try
                {
                    director.Preload(group);
                    handle = director.Start(new BattleEffectRequest(group, "A", new[] { "E" }, default, 1));
                }
                catch (Exception error) { failure = error; }
                int steps = 0;
                var timeout = Stopwatch.StartNew();
                while (failure == null && director.IsRunning(handle) && steps++ < 1000000 && timeout.Elapsed.TotalSeconds < 120)
                {
                    try
                    {
                        director.Advance(++frame, director.BlocksBattleTime ? 0 : (float)BattleSession.StepSeconds, (float)BattleSession.StepSeconds);
                    }
                    catch (Exception error) { failure = error; }
                    if (director.BlocksBattleTime)
                        yield return null;
                }
                if (failure == null && director.IsRunning(handle))
                    failure = new InvalidDataException("Original group did not complete within 120 seconds.");
                if (failure == null)
                    _coverage.Add(new
                    {
                        group,
                        status = "completed",
                        steps
                    });
                else
                {
                    errors.Add($"Group {group}: {failure.Message}");
                    _coverage.Add(new
                    {
                        group,
                        status = "resource-error",
                        reason = failure.Message
                    });
                }
                if (handle != 0)
                    director.Stop(handle);
            }
            foreach (string missing in director.MissingMovies.Distinct())
                _coverage.Add(new
                {
                    movie = missing,
                    status = "original-open-failed",
                    reason = "The supplied game data and local original installation contain no matching movie."
                });
            foreach (string missing in director.MissingBattleTextures)
                _coverage.Add(new
                {
                    texture = missing,
                    status = "original-texture-load-failed",
                    reason = "The native effect ignores the failed texture load and draws its vertex color with the original blend operation."
                });
            Assert.That(groups.Length, Is.GreaterThan(100));
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }
    }
}
