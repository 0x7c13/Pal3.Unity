// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;
    using Battle;
    using Core.Contract.Enums;
    using Game.Rendering.Native;
    using Game.Rendering.Renderer;
    using NUnit.Framework;
    using UnityEngine;

    [Category("LocalGameData")]
    public sealed class BattleResultPresentationIntegrationTests
    {

        private static BattleEditorSession Start(bool flee, int enemies = 1, bool deadAlly = false, bool toon = false,
            BattleEncounter encounter = BattleEncounter.AllyAmbush)
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Original game data is required for result presentation verification.");
            var session = new BattleEditorSession();
            try
            {
                session.Load(root, 936);
                BattleData data = session.Data;
                BattleParticipant ally = data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water);
                ally.InstanceId = "A";
                ally.Level = 100;
                ally.Attributes[5] = 20;
                ally.Attributes[6] = 100;
                var scenario = new BattleScenario
                {
                    Variant = BattleData.Variant,
                    DataFingerprint = data.Fingerprint,
                    Scene = data.Scenes.First(),
                    Encounter = encounter,
                    ToonRendering = toon
                };
                scenario.Participants.Add(ally);
                if (deadAlly)
                {
                    BattleParticipant dead = data.CreateParticipant(102, BattleSide.Ally, BattleSlot.Fire);
                    dead.InstanceId = "D";
                    dead.Vitals[0] = 0;
                    scenario.Participants.Add(dead);
                }
                int enemyId = data.Database.CombatActorInfos.First(pair => pair.Value.Type != CombatActorType.MainActor && pair.Value.NormalLoot > 0 && pair.Value.NormalLootCount > 0).Key;
                for (int index = 0; index < enemies; index++)
                {
                    BattleParticipant enemy = data.CreateParticipant(enemyId, BattleSide.Enemy, index == 0 ? BattleSlot.Water : BattleSlot.Fire);
                    enemy.InstanceId = "E" + index;
                    if (!flee)
                        enemy.Vitals[0] = 0;
                    scenario.Participants.Add(enemy);
                }
                session.StartBattle(scenario);
                session.Preview.Audio.Muted = true;
                session.Preview.Effects.Muted = true;
                if (flee)
                {
                    Assert.That(session.Battle.Submit("A", BattleAction.Flee), Is.True);
                    for (int tick = 0; tick < 5000 && session.Battle.Status == BattleSessionStatus.Running; tick++)
                        session.Battle.Step();
                    Assert.That(session.Battle.Result.Escaped, Is.True);
                    session.SyncBattle();
                }
                return session;
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        private static IDictionary<string, AudioClip> Clips(BattleAudioPreview audio) =>
            (IDictionary<string, AudioClip>)typeof(BattleAudioPreview).GetField("_clips", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(audio);

        private static IDictionary Requests(BattleAudioPreview audio) =>
            (IDictionary)typeof(BattleAudioPreview).GetField("_desired", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(audio);

        private static BattleResultRoleBody ResultBody(BattlePreview preview, string actor) =>
            (BattleResultRoleBody)((IDictionary)typeof(BattlePreview).GetField("_resultBodies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview))[actor];

        private static object ActorView(BattlePreview preview, string actor) =>
            ((IDictionary)typeof(BattlePreview).GetField("_actors", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview))[actor];

        private static bool ActorVisible(BattlePreview preview, string actor)
        {
            object view = ActorView(preview, actor);
            return (bool)view.GetType().GetField("Visible").GetValue(view);
        }

        private static Mv3ModelRenderer ActorRenderer(BattlePreview preview, string actor)
        {
            object view = ActorView(preview, actor);
            return (Mv3ModelRenderer)view.GetType().GetField("Renderer").GetValue(view);
        }

        private static float ActorOpacity(BattlePreview preview, string actor) =>
            (float)typeof(Mv3ModelRenderer).GetField("_opacity", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ActorRenderer(preview, actor));

        [Test]
        public void VictoryAfterEnemyAmbushFacesThePartyFormation()
        {
            using BattleEditorSession session = Start(false, encounter: BattleEncounter.EnemyAmbush);
            Transform body = ActorRenderer(session.Preview, "A").transform;
            Quaternion formation = Quaternion.Euler(0, -BattleFacingRules.Initial(BattleSide.Ally), 0);
            Assert.That(Quaternion.Angle(body.rotation, formation), Is.EqualTo(180).Within(.01f));
            session.StepBattle();
            Assert.That(session.ResultPresentation, Is.Not.Null);
            Assert.That(Quaternion.Angle(ActorRenderer(session.Preview, "A").transform.rotation, formation), Is.LessThan(.01f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ResultBodyClipChangesRetainTheCapturedExternalBlend(bool toon)
        {
            using BattleEditorSession session = Start(false, toon: toon);
            session.Battle.SetEffectActorBlend("A", .2f);
            session.StepBattle();
            Assert.That(session.ResultPresentation, Is.Not.Null);
            Assert.That(ActorOpacity(session.Preview, "A"), Is.EqualTo(.2f).Within(.00001f));
            Mv3ModelRenderer before = ActorRenderer(session.Preview, "A");
            session.StepBattle();
            Assert.That(ActorRenderer(session.Preview, "A"), Is.Not.SameAs(before), "Victory starts its own body clip.");
            Assert.That(session.Battle.EffectActor("A").Blend, Is.EqualTo(.2f).Within(.00001f));
            session.Preview.View.Render(800, 600);
            Material[] materials = ActorRenderer(session.Preview, "A").GetComponentsInChildren<MeshRenderer>()
                .SelectMany(renderer => renderer.sharedMaterials).Where(material => material.HasProperty("_TintColor")).ToArray();
            Assert.That(materials, Is.Not.Empty);
            Assert.That(materials.All(material => Math.Abs(material.GetColor("_TintColor").a - .2f) < .00001f), Is.True,
                "The rendered body and attachments retain the captured alpha after replacing their clip.");
            Assert.That(ActorOpacity(session.Preview, "A"), Is.EqualTo(.2f).Within(.00001f));
        }

        private static int ShadowCount(BattlePreview preview) =>
            ((GameBoxBattleShadowRenderer)typeof(BattlePreview).GetField("_shadows", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(preview)).Count;

        private static void ReplayResults(BattleEditorSession session)
        {
            BattleSession original = session.Battle;
            session.ReplayResults();
            Assert.That(session.Battle, Is.SameAs(original), "Result replay must be queued.");
            MethodInfo tick = typeof(BattleEditorSession).GetMethod("TickReplay", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int slice = 0; slice < 5000 && session.Battle == original; slice++)
                tick.Invoke(session, null);
            Assert.That(session.Battle, Is.Not.SameAs(original));
        }

        [Test]
        public void ResultRecreatesAnActorViewWhenItHasNotBeenBound()
        {
            using BattleEditorSession session = Start(true);
            BattlePreview preview = session.Preview;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var actors = (IDictionary)typeof(BattlePreview).GetField("_actors", flags).GetValue(preview);
            object old = actors["A"];
            preview.View.ReleaseCompositeObject((GameObject)old.GetType().GetField("Object").GetValue(old));
            actors.Remove("A");

            var catalog = (BattleAnimationCatalog)typeof(BattleEditorSession).GetField("_battleCatalog", flags).GetValue(session);
            preview.BeginResultPresentation(session.Battle, catalog, session.ResultPresentation);
            Assert.That(ActorView(preview, "A"), Is.Not.SameAs(old));
            Assert.That(ActorRenderer(preview, "A"), Is.Not.Null);
            session.StepBattle();
            Assert.That(ResultBody(preview, "A"), Is.Not.Null);
        }

        [Test]
        public void OriginalFleeRetainsDeadAllyHpPoseVisibilityAndShadowUntilPresentationCompletes()
        {
            using BattleEditorSession session = Start(true, deadAlly: true);
            BattleResult final = session.Battle.Result;
            BattleActorSnapshot before = final.PreSettlementActors.Single(actor => actor.Id == "D");
            BattleActorSnapshot after = final.Actors.Single(actor => actor.Id == "D");
            Assert.That(before.Hp, Is.Zero);
            Assert.That(after.Hp, Is.EqualTo(BattleData.Variant == "PAL3A" ? 1 : 0));
            Assert.That(after.AliveFlag, Is.False);
            Assert.That(after.ModelAliveFlag, Is.False);
            Assert.That(after.States.Select(state => state.State), Is.EqualTo(before.States.Select(state => state.State)));
            int events = session.Battle.Events.Count;
            int money = session.Battle.Money;
            for (int pass = 0; pass < 10; pass++)
            {
                Assert.That(session.ResultPresentation.Actors.Single(actor => actor.Id == "D").Hp, Is.Zero, "Statistics/body use presentation HP");
                Assert.That(ActorVisible(session.Preview, "D"), Is.False, "Hidden even before first result step");
                object view = ActorView(session.Preview, "D");
                var root = (GameObject)view.GetType().GetField("Object").GetValue(view);
                Assert.That(root.GetComponentsInChildren<UnityEngine.Renderer>(true).Any(renderer => renderer.enabled), Is.False);
                Assert.That(ShadowCount(session.Preview), Is.EqualTo(2), "Only live ally and enemy cast shadows");
                session.StepBattle();
                Assert.That(ResultBody(session.Preview, "D").Binding.RequestedNativeActionIndex, Is.EqualTo(47));
            }
            for (int frame = 0; frame < 1000 && !session.ResultPresentation.Completed; frame++)
            {
                session.StepBattle();
                session.SkipResultStep();
            }
            Assert.That(session.ResultPresentation.Completed, Is.True);
            Assert.That(session.ResultPresentation.Actors.Single(actor => actor.Id == "D").Hp, Is.EqualTo(after.Hp));
            Assert.That(ActorVisible(session.Preview, "D"), Is.EqualTo(BattleData.Variant == "PAL3A"));
            Assert.That(ShadowCount(session.Preview), Is.EqualTo(BattleData.Variant == "PAL3A" ? 3 : 2));
            Assert.That(session.Battle.Events.Count, Is.EqualTo(events));
            Assert.That(session.Battle.Money, Is.EqualTo(money));
            ReplayResults(session);
            session.Preview.Audio.Muted = true;
            session.Preview.Effects.Muted = true;
            Assert.That(session.ResultPresentation.Actors.Single(actor => actor.Id == "D").Hp, Is.Zero);
            Assert.That(ActorVisible(session.Preview, "D"), Is.False);
            Assert.That(ShadowCount(session.Preview), Is.EqualTo(2));
            session.StepBattle();
            Assert.That(ResultBody(session.Preview, "D").Binding.RequestedNativeActionIndex, Is.EqualTo(47));
            Assert.That(session.Battle.Events.Count, Is.EqualTo(events));
        }

        [Test]
        public void WinPresentsRecoveredHpBeforeItsTasksAndReturnsRecoveredAllyToNativeDyingAction()
        {
            using BattleEditorSession session = Start(false, deadAlly: true);
            session.StepBattle();
            Assert.That(session.Battle.Result.PreSettlementActors.Single(actor => actor.Id == "D").Hp, Is.Zero);
            Assert.That(session.Battle.Result.Actors.Single(actor => actor.Id == "D").Hp, Is.EqualTo(1));
            Assert.That(session.ResultPresentation.Actors.Single(actor => actor.Id == "D").Hp, Is.EqualTo(1));
            Assert.That(ActorVisible(session.Preview, "D"), Is.True);
            Assert.That(ShadowCount(session.Preview), Is.EqualTo(2), "Both recovered/living allies cast shadows; dead enemy does not");
            BattleResultRoleBody body = ResultBody(session.Preview, "D");
            for (int frame = 0; frame < 1000 && body.Binding.RequestedNativeActionIndex != 46; frame++)
                session.StepBattle();
            Assert.That(body.Binding.RequestedNativeActionIndex, Is.EqualTo(46));
            Assert.That(session.Battle.Result.Actors.Single(actor => actor.Id == "D").AliveFlag, Is.False);
            Assert.That(session.Battle.Result.Actors.Single(actor => actor.Id == "D").ModelAliveFlag, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FinalFleeWaitReleasesSilentMusicForTimedAndSkippedCompletion(bool skip)
        {
            using BattleEditorSession session = Start(true);
            Clips(session.Preview.Audio).Add("music:silent.mp3", AudioClip.Create("silent result test", 48000, 1, 48000, false));
            session.Preview.Audio.SetMusic("silent");
            while (session.ResultPresentation.Index < session.ResultPresentation.Sequence.Count - 1)
            {
                session.StepBattle();
                session.SkipResultStep();
            }
            session.StepBattle();
            IDictionary desired = Requests(session.Preview.Audio);
            Assert.That(desired.Count, Is.GreaterThanOrEqualTo(1));
            if (skip)
                Assert.That(session.SkipResultStep(), Is.True);
            else
                for (int tick = 0; tick < 1000 && !session.ResultPresentation.Completed; tick++)
                    session.StepBattle();
            Assert.That(session.ResultPresentation.Completed, Is.True);
            Assert.That(desired.Count, Is.Zero);
            session.IsPlaying = true;
            session.Tick(1);
            Assert.That(session.IsPlaying, Is.False);
            Assert.That(session.Preview.Audio.PlaybackEnabled, Is.False);
            Assert.That(desired.Count, Is.Zero);
        }

        [Test]
        public void SeparateLootAwardsRequestSeparateMutedSoundsAtRegistrationTime()
        {
            using BattleEditorSession session = Start(false, 2);
            Clips(session.Preview.Audio).Add("wa006.wav", AudioClip.Create("silent loot test", 48000, 1, 48000, false));
            session.StepBattle();
            BattleResultStep[] awards = session.ResultPresentation.Sequence.Where(step => step.Kind == BattleResultStepKind.Item).ToArray();
            Assert.That(awards.Length, Is.EqualTo(2));
            object[] requests = Requests(session.Preview.Audio).Values.Cast<object>().Where(request =>
                (string)request.GetType().GetField("File").GetValue(request) == "wa006.wav").ToArray();
            Assert.That(requests.Length, Is.EqualTo(awards.Length));
            double time = session.Battle.Tick * BattleSession.StepSeconds;
            Assert.That(requests.Select(request => (double)request.GetType().GetField("Start").GetValue(request)), Is.All.EqualTo(time).Within(1e-5));
            Assert.That(session.ResultPresentation.Tick, Is.Zero);
            Assert.That(session.Preview.Audio.PlaybackEnabled, Is.False);
        }

#if PAL3A
        [Test]
        public void EveryOriginalAlliedWeaponTypeProvidesItsPublishedLevelUpAnimation()
        {
            string root = Environment.GetEnvironmentVariable("PAL3A_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Original game data is required for action59 verification.");
            using var data = new BattleData(root, 936);
            var verified = new HashSet<(int Identity, int Weapon)>();
            foreach (int id in data.Database.CombatActorInfos.Where(pair => pair.Value.Type == CombatActorType.MainActor).Select(pair => pair.Key))
            {
                int identity = BattleData.PlayerIdentity(id);
                foreach (int itemId in data.Database.GameItemInfos.Where(pair => pair.Value.Type == ItemType.Weapon &&
                    pair.Value.ApplicableActors.Contains((PlayerActorId)identity)).GroupBy(pair => pair.Value.WeaponType).Select(group => group.First().Key))
                {
                    var scenario = new BattleScenario { Variant = BattleData.Variant, DataFingerprint = data.Fingerprint, Scene = data.Scenes.First() };
                    BattleParticipant actor = data.CreateParticipant(id, BattleSide.Ally, BattleSlot.Water);
                    actor.Equipment[0] = itemId;
                    scenario.Participants.Add(actor);
                    scenario.Participants.Add(data.CreateParticipant(701, BattleSide.Enemy, BattleSlot.Water));
                    BattleSetup setup = data.Capture(scenario);
                    var catalog = new BattleAnimationCatalog(data, setup);
                    BattlePhysicalAnimation animation = catalog.ResultBodyAnimations(setup.CreateInstance().Participants.First(participant => participant.Side == BattleSide.Ally))[59];
                    int weapon = (int)data.Database.GameItemInfos[itemId].WeaponType;
                    verified.Add((identity, weapon));
                    Assert.That(animation, Is.Not.Null, "actor " + id + ", weapon type " + weapon);
                    Assert.That(animation.Clip, Is.EqualTo(identity == 1 && weapon == 3 || identity == 4 && weapon == 8 ? "z162" : "z16"));
                }
            }
            Assert.That(verified.Contains((1, 3)), Is.True);
            Assert.That(verified.Contains((4, 8)), Is.True);
            Assert.That(verified.Count, Is.GreaterThan(2));
        }
#endif

        [Test]
        public void EndedCombatHasIndependentPausableResultClockAndReplayWithoutASecondRewardCommit()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Original game data is required for result presentation verification.");
            using var session = new BattleEditorSession();
            session.Load(root, 936);
            BattleData data = session.Data;
            BattleParticipant ally = data.CreateParticipant(data.Database.CombatActorInfos.First(pair => pair.Value.Type == CombatActorType.MainActor).Key,
                BattleSide.Ally, BattleSlot.Water);
            BattleParticipant enemy = data.CreateParticipant(701, BattleSide.Enemy, BattleSlot.Water);
            ally.InstanceId = "A";
            enemy.InstanceId = "E";
            enemy.Vitals[0] = 0;
            var scenario = new BattleScenario { Variant = BattleData.Variant, DataFingerprint = data.Fingerprint, Scene = data.Scenes.First() };
            scenario.Participants.Add(ally);
            scenario.Participants.Add(enemy);
            session.StartBattle(scenario);
            session.Preview.Audio.Muted = true;
            session.Preview.Effects.Muted = true;
            session.StepBattle();
            Assert.That(session.Battle.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(session.ResultPresentation, Is.Not.Null);
            long combatTick = session.Battle.Tick;
            int events = session.Battle.Events.Count;
            int money = session.Battle.Money;
            session.IsPlaying = true;
            session.Tick(.2f);
            long presentationTick = session.ResultPresentation.Tick;
            Assert.That(presentationTick, Is.EqualTo(10));
            Assert.That(session.Battle.Tick, Is.EqualTo(combatTick));
            session.IsPlaying = false;
            session.Tick(1);
            Assert.That(session.ResultPresentation.Tick, Is.EqualTo(presentationTick));
            session.StepBattle();
            Assert.That(session.ResultPresentation.Tick, Is.EqualTo(presentationTick + 1));
            Assert.That(session.Preview.View.GetTexture("basedata.cpk\\ui\\combat\\result\\bigwin.tga").width, Is.EqualTo(256));
            foreach (string file in new[] { "back", "face/0", "face/HP", "face/GP", "face/MP", "face/SLOT", "face/HP-GP-MP", "panel/levelup", "panel/getitem", "panel/waterlvup", "panel/doublelvup" })
                Assert.That(session.Preview.View.GetTexture("basedata.cpk\\ui\\combat\\result\\" + file + ".tga"), Is.Not.Null, file);
            session.Speed = 4;
            session.IsPlaying = true;
            for (int frame = 0; frame < 1000 && !session.ResultPresentation.Completed; frame++)
            {
                session.Tick(.1f);
                session.SkipResultStep();
            }
            Assert.That(session.ResultPresentation.Completed, Is.True);
            Assert.That(session.IsPlaying, Is.False);
            Assert.That(session.ResultPresentation.ScreenOpacity, Is.Zero);
            Assert.That(session.Battle.Events.Count, Is.EqualTo(events));
            Assert.That(session.Battle.Money, Is.EqualTo(money));
            ReplayResults(session);
            Assert.That(session.ResultPresentation.Tick, Is.Zero);
            Assert.That(session.Battle.Tick, Is.EqualTo(combatTick));
            Assert.That(session.Battle.Money, Is.EqualTo(money));
            Assert.That(session.Battle.Events.Count, Is.EqualTo(events));
            Assert.That(session.Preview.Diagnostics.Where(message => message.Contains("Missing texture dependency")), Is.Empty);
            session.Stop();
            Assert.That(session.ResultPresentation, Is.Null);
        }
    }
}
