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
    using Core.Contract.Constants;
    using NUnit.Framework;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Category("LocalGameData")]
    public sealed class BattleIntegrationTests
    {
        private BattleEditorSession _session;

        [SetUp]
        public void Load()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root))
                Assert.Ignore("Local original data unavailable.");
            _session = new BattleEditorSession();
            _session.Load(root, 936);
        }

        [TearDown]
        public void Dispose() => _session?.Dispose();

        [TestCase(0, new[] { 0, 1, 3, 2, 4 })]
        [TestCase(1, new[] { 0, 1, 3, 2, 4 })]
        [TestCase(2, new[] { 4, 0, 2, 1, 3 })]
        [TestCase(3, new[] { 2, 3, 0, 4, 1 })]
        [TestCase(4, new[] { 3, 4, 1, 0, 2 })]
        [TestCase(5, new[] { 1, 2, 4, 3, 0 })]
        public void FormationHomesUseTheNativeRoleCoordinates(int sceneElement, int[] nativeOrder)
        {
            BattleScenario scenario = Create(false);
            scenario.SceneElement = sceneElement;
            BattleParticipant ally = scenario.Participants[0], enemy = scenario.Participants[1];
            ally.Slot = BattleSlot.Wind;
            enemy.Slot = BattleSlot.Center;
            var catalog = new BattleAnimationCatalog(_session.Data, new BattleSetup(scenario));
            var positions = _session.Data.Layout.ActorGameBoxPositions;
            BattleMovementPoint Point(int index) => new(positions[index].X, positions[index].Y, positions[index].Z);
            for (int slot = 0; slot < nativeOrder.Length; slot++)
            {
                BattleMovementPoint expected = Point(nativeOrder[slot]);
                BattleMovementPoint actual = catalog.FormationHome(ally.InstanceId, (BattleSlot)slot);
                Assert.That(actual.X, Is.EqualTo(expected.X).Within(.001f));
                Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(.001f));
                Assert.That(actual.Z, Is.EqualTo(expected.Z).Within(.001f));
            }
            Assert.That(catalog.InitialHome(ally.InstanceId), Is.EqualTo(catalog.FormationHome(ally.InstanceId, ally.Slot)));
            Assert.That(catalog.CoAttackHome(ally.InstanceId, BattleSlot.Wind).Z, Is.EqualTo(positions[3].Z).Within(.001f));
            Assert.That(catalog.InitialHome(enemy.InstanceId).Z, Is.EqualTo(151.262329f).Within(.001f));
        }

        private BattleScenario Create(bool multiple)
        {
            BattleData data = _session.Data;
            var scenario = new BattleScenario { Variant = BattleData.Variant, Scene = data.Scenes.First(), DataFingerprint = data.Fingerprint, Encounter = BattleEncounter.AllyAmbush };
            scenario.Participants.Add(data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = data.Database.CombatActorInfos.Keys.OrderBy(id => id).First(id => BattleAnimationCatalog.IsBasicEnemy(data, id) &&
                data.Resources.Find(FileConstants.GetActorFolderVirtualPath(data.Database.CombatActorInfos[id].ModelId) + data.Database.CombatActorInfos[id].ModelId + ".ini") != null);
            scenario.Participants.Add(data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            if (multiple)
            {
                scenario.Participants.Add(data.CreateParticipant(104, BattleSide.Ally, BattleSlot.Fire));
                scenario.Participants.Add(data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Fire));
            }
            foreach (BattleParticipant actor in scenario.Participants)
            {
                actor.Attributes = new[] { 80, 50, 30, actor.Side == BattleSide.Ally ? 70 : 25, 10, 3, 0, 0, 0, 0, 0, 0 };
                actor.Vitals = new[] { 80, 0, 30 };
                actor.Skills.Clear();
#if PAL3
                if (actor.ActorId == 101)
                    actor.Equipment[0] = 6601; // Ordinary sword; magic-sword cultivation is a separate scenario.
#endif
            }
            return scenario;
        }

        [Test]
        public void QueuedReplayKeepsTheOriginalUntilCompletionAndCanBeCanceled()
        {
            _session.StartBattle(Create(false));
            BattleSession original = _session.Battle;
            for (int tick = 0; tick < 200; tick++)
                original.Step();
            _session.SyncBattle();

            void Call(string name) =>
                typeof(BattleEditorSession).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_session, null);

            Call("BeginReplay");
            Assert.That(_session.Battle, Is.SameAs(original));
            Call("CancelReplay");
            Call("TickReplay");
            Assert.That(_session.Battle, Is.SameAs(original));

            Call("BeginReplay");
            for (int slice = 0; slice < 200 && _session.Battle == original; slice++)
                Call("TickReplay");
            Assert.That(_session.Battle, Is.Not.SameAs(original));
            Assert.That(_session.Battle.Tick, Is.EqualTo(200));
            Assert.That(_session.Battle.Status, Is.EqualTo(BattleSessionStatus.Running));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void RealResourcesReachNaturalResultAndReplayWithoutMutatingDraft(bool multiple, bool toon)
        {
            BattleScenario scenario = Create(multiple);
            scenario.ToonRendering = toon;
            string original = JsonUtility.ToJson(scenario);
            int sceneCount = EditorSceneManager.previewSceneCount;
            _session.StartBattle(scenario, true, true);
            BattleSession combat = _session.Battle;
            int firstHit = 0;
            for (int i = 0; i < 15000 && combat.Status == BattleSessionStatus.Running; i++)
            {
                combat.Step();
                if (firstHit == 0 && combat.Events.Any(entry => entry.Kind == BattleEventKind.Hit))
                {
                    firstHit = i + 1;
                    _session.SyncBattle();
                    Capture(multiple, toon);
                }
            }
            _session.SyncBattle();
            Assert.That(firstHit, Is.GreaterThan(0));
            Assert.That(combat.Status, Is.EqualTo(BattleSessionStatus.Completed), string.Join("\n", combat.Events));
            Assert.That(combat.Result.Winner, Is.EqualTo(BattleSide.Ally));
            string events = Newtonsoft.Json.JsonConvert.SerializeObject(combat.Events);
            _session.ReplayBattle();
            Assert.That(Newtonsoft.Json.JsonConvert.SerializeObject(_session.Battle.Events), Is.EqualTo(events));
            Assert.That(JsonUtility.ToJson(scenario), Is.EqualTo(original));
            Assert.That(_session.Preview.Diagnostics, Is.Empty);
            for (int i = 0; i < 5; i++)
            {
                _session.Restart();
                Assert.That(_session.Battle.Status, Is.EqualTo(BattleSessionStatus.Running));
                _session.StepBattle();
                Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(sceneCount + 1));
            }
            _session.Stop();
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(sceneCount));
        }

        [Test]
        public void FailedCombatStartKeepsExistingBattleAndPresentation()
        {
            BattleScenario scenario = Create(false);
            _session.StartBattle(scenario);
            BattleSession previous = _session.Battle;
            BattlePreview view = _session.Preview;
            scenario.Participants[0].Statuses.Add(new BattleStatus { Id = int.MaxValue });
            Assert.Throws<ArgumentException>(() => _session.StartBattle(scenario));
            Assert.That(_session.Battle, Is.SameAs(previous));
            Assert.That(_session.Preview, Is.SameAs(view));
            scenario.Participants[0].Statuses.Clear();
            scenario.Participants[0].Model = "missingfixture";
            Assert.Throws<ArgumentException>(() => _session.StartBattle(scenario));
            Assert.That(_session.Battle, Is.SameAs(previous));
            Assert.That(_session.Preview, Is.SameAs(view));
        }

        [Test]
        public void BasicWeaponMetadataUsesTheNativeDivisorAndExactWorkMarkers()
        {
            BattleScenario scenario = Create(false);
            var catalog = new BattleAnimationCatalog(_session.Data, new BattleSetup(scenario));
            BattleParticipant source = scenario.Participants[0], target = scenario.Participants[1];
#if PAL3A
            Assert.That(catalog.DamageDivisor(source.InstanceId), Is.EqualTo(2));
            Assert.That(catalog.Attack(source.InstanceId, target.InstanceId).HitTicks, Is.EqualTo(new[] { 14, 30 }));
#else
            Assert.That(catalog.DamageDivisor(source.InstanceId), Is.EqualTo(1));
            Assert.That(catalog.Attack(source.InstanceId, target.InstanceId).HitTicks, Is.EqualTo(new[] { 25 }));
#endif
        }

        [Test]
        public void NextHitAdvancesRunningActionsWhileAnotherActorWaitsForInput()
        {
            BattleScenario scenario = Create(true);
            _session.StartBattle(scenario);
            string attacker = _session.Battle.WaitingActor;
            string target = _session.Battle.Actors.First(actor => actor.Side == BattleSide.Enemy).Id;
            Assert.That(_session.Battle.Submit(attacker, BattleAction.Attack, target), Is.True);
            Assert.That(_session.Battle.WaitingActor, Is.Not.Null);
            _session.AdvanceTo(BattleEventKind.Hit);
            Assert.That(_session.Battle.Events.Any(entry => entry.Kind == BattleEventKind.Hit), Is.True);
            Assert.That(_session.Battle.Tick, Is.GreaterThan(1));
        }

#if PAL3A
        [TestCase(103)]
        [TestCase(105)]
        public void FluteAndCrossbowUseStationaryTimelines(int actorId)
        {
            BattleScenario scenario = Create(false);
            scenario.Participants[0] = _session.Data.CreateParticipant(actorId, BattleSide.Ally, BattleSlot.Water);
            if (actorId == 103)
                scenario.Participants[0].Equipment[0] = 82301;
            var catalog = new BattleAnimationCatalog(_session.Data, new BattleSetup(scenario));
            BattleParticipant source = scenario.Participants[0], target = scenario.Participants[1];
            Assert.That(catalog.Attack(source.InstanceId, target.InstanceId).TravelTicks, Is.Zero);
            Assert.That(catalog.Approach(source.InstanceId, target.InstanceId), Is.EqualTo(_session.Data.Position(source, scenario.SceneElement)));
        }

        [Test]
        public void AlternateWeaponUsesItsCapturedSecondAttackClip()
        {
            BattleScenario scenario = Create(false);
            scenario.Participants[0] = _session.Data.CreateParticipant(102, BattleSide.Ally, BattleSlot.Water);
            scenario.Participants[0].Equipment[0] = 82209; // Type 4 spear uses z02, not z01.
            _session.StartBattle(scenario);
            string source = scenario.Participants[0].InstanceId;
            Assert.That(_session.Battle.WaitingActor, Is.EqualTo(source));
            Assert.That(_session.Battle.Submit(source, BattleAction.Attack, scenario.Participants[1].InstanceId), Is.True);
            bool seen = false;
            for (int tick = 0; tick < 400 && _session.Battle.Status == BattleSessionStatus.Running; tick++)
            {
                _session.StepBattle();
                if (_session.Battle.Actors.Single(actor => actor.Id == source).NativeAnimationClip == "z02")
                {
                    seen = true;
                    break;
                }
            }
            Assert.That(seen, Is.True, "The type 4 spear must bind its actual second attack clip.");
        }
#else
        [Test]
        public void EmptyWeaponUsesThePublishedLongkuiDefaultWithoutGrantingJingtiansSwordAbility()
        {
            BattleScenario scenario = Create(false);
            scenario.Participants[0] = _session.Data.CreateParticipant(106, BattleSide.Ally, BattleSlot.Water);
            scenario.Participants[0].Equipment[0] = 0;
            _session.StartBattle(scenario);
            BattleParticipant captured = _session.CapturedScenario.Participants[0];
            Assert.That(captured.Equipment[0], Is.EqualTo(6603));
            Assert.That(captured.UsesNativeDefaultWeapon, Is.True);
            Assert.That(_session.Battle.MagicSwords, Is.Empty);
            Assert.That(scenario.Participants[0].Equipment[0], Is.Zero);
        }
#endif

        private void Capture(bool multiple, bool toon)
        {
            RenderTexture target = _session.Preview.View.Render(800, 600);
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(800, 600, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
                image.Apply();
                Assert.That(image.GetPixels32().Select(pixel => (pixel.r, pixel.g, pixel.b)).Distinct().Take(100).Count(), Is.GreaterThan(20));
                string directory = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT");
                if (string.IsNullOrEmpty(directory))
                    return;
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, $"{BattleData.Variant}-{(multiple ? "party" : "duel")}-{(toon ? "toon" : "original")}.png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }
    }
}
