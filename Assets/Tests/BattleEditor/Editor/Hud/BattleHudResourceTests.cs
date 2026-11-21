// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;
    using Core.DataReader.Cvd;
    using Battle;
    using System.Linq;
    using ResourceBrowser;
    using UnityEngine;

    [Category("LocalGameData")]
    public sealed class BattleHudResourceTests
    {
        [Test]
        public void CompositeCvdDoesNotFrameOrJoinStandaloneSamplingAndRollsBackFailure()
        {
            using var preview = new ResourceModelPreview(null, false);
            preview.SetCamera(new Vector3(11, 12, 13), new Vector3(2, 3, 4), 54);
            preview.GetCamera(out Vector3 beforePosition, out Vector3 beforeTarget, out float beforeFov);
            GameObject target = preview.CreateObject("CVD owner");
            var renderer = preview.SetHierarchicalModel(target, "fixture.cvd", new CvdFile(5, Array.Empty<CvdGeometryNode>()));
            renderer.SetCurrentTime(3);
            preview.Sample(12);
            Assert.That(renderer.GetCurrentAnimationTime(), Is.EqualTo(3));
            Assert.That(preview.Duration, Is.Zero);
            preview.GetCamera(out Vector3 position, out Vector3 aim, out float fov);
            Assert.That(position, Is.EqualTo(beforePosition));
            Assert.That(aim, Is.EqualTo(beforeTarget));
            Assert.That(fov, Is.EqualTo(beforeFov));
            Assert.Throws<NullReferenceException>(() => preview.SetHierarchicalModel(target, "bad.cvd", new CvdFile(1, null)));
            Assert.That(target.GetComponentsInChildren<Pal3.Game.Rendering.Renderer.CvdModelRenderer>().Length, Is.EqualTo(1));
            Assert.That(renderer == null, Is.False);
            preview.ReleaseCompositeObject(target);
            Assert.That(renderer == null, Is.True);
            Assert.That(target == null, Is.True);
            preview.Dispose();
            Assert.Throws<ObjectDisposedException>(() => preview.SetHierarchicalModel(target, "fixture.cvd", new CvdFile(1, Array.Empty<CvdGeometryNode>())));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PointerCvdFollowsThePreviewOwnerAcrossRestartAndNeverSamplesDisposedModels(bool toon)
        {
            string configured = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            string root = configured ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root) && configured == null)
                Assert.Ignore("Original local data unavailable.");
            using var editor = new BattleEditorSession();
            editor.Load(root, 936);
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = editor.Data.Scenes.First(),
                DataFingerprint = editor.Data.Fingerprint,
                Encounter = BattleEncounter.AllyAmbush
            };
            scenario.Participants.Add(editor.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
            scenario.Participants.Add(editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            scenario.ToonRendering = toon;
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            var hud = new BattleHudView();
            hud.PreparePointers(editor.Battle, editor.Data, editor.Preview);
            string name = "Battle selection pointer " + scenario.Participants[0].InstanceId;
            Transform oldPointer = editor.Preview.View.Root.transform.Find(name);
            Assert.That(oldPointer, Is.Not.Null);
            bool nativePhase = !toon && Pal3.Game.Rendering.Native.GameBoxBattleRenderOrder.Actor(BattleData.Variant == "PAL3A", true, 101) > 0;
            Assert.That(oldPointer.GetComponent<Pal3.Game.Rendering.Native.GameBoxBattleRenderPhase>() != null, Is.EqualTo(nativePhase));
            foreach (Material material in oldPointer.GetComponentsInChildren<MeshRenderer>().SelectMany(renderer => renderer.sharedMaterials))
            {
                int expectedQueue = nativePhase ? material.shader.renderQueue <= 2500 ? 3002 : 3003 : material.shader.renderQueue;
                Assert.That(material.renderQueue, Is.EqualTo(expectedQueue));
                if (toon && material.renderQueue <= 2500)
                    Assert.That(material.FindPass("DepthNormals"), Is.GreaterThanOrEqualTo(0));
            }
            var mesh = oldPointer.GetComponentInChildren<MeshFilter>().sharedMesh;
            hud.Presenter.Open(Battle.Presentation.BattleHudPage.Skills);
            hud.PreparePointers(editor.Battle, editor.Data, editor.Preview);
            hud.Presenter.Back();
            hud.PreparePointers(editor.Battle, editor.Data, editor.Preview);
            Assert.That(oldPointer.GetComponentInChildren<MeshFilter>().sharedMesh, Is.SameAs(mesh));
            BattlePreview previous = editor.Preview;
            BattleSession previousCombat = editor.Battle;
            editor.Restart();
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            Assert.That(oldPointer == null, Is.True);
            Assert.DoesNotThrow(() => hud.PreparePointers(previousCombat, editor.Data, previous));
            Assert.DoesNotThrow(() => hud.PreparePointers(editor.Battle, editor.Data, editor.Preview));
            Assert.That(editor.Preview.View.Root.transform.Find(name), Is.Not.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PointerFormationGestureCommitsOneSwapAndOnlyConfirmationSpendsTheControllerGauge(bool externalEntry)
        {
            string configured = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            string root = configured ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root) && configured == null)
                Assert.Ignore("Original local data unavailable.");
            using var editor = new BattleEditorSession();
            editor.Load(root, 936);
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = editor.Data.Scenes.First(),
                DataFingerprint = editor.Data.Fingerprint,
                Encounter = BattleEncounter.AllyAmbush
            };
            scenario.Participants.Add(editor.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            scenario.Participants.Add(editor.Data.CreateParticipant(BattleData.Variant == "PAL3" ? 104 : 102, BattleSide.Ally, BattleSlot.Fire));
            int enemy = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
            scenario.Participants.Add(editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            var view = new BattleHudView();
            var presenter = view.Presenter;
            presenter.Synchronize(editor.Battle);
            string controller = presenter.Controller;
            string moved = scenario.Participants[1].InstanceId;
            int gauge = editor.Battle.Actors.First(actor => actor.Id == controller).Gauge;
            Assert.That(editor.Battle.CanBeginFormation(controller), Is.Null);
            if (externalEntry)
            {
                Assert.That(presenter.ChooseAction(BattleAction.Attack), Is.True);
                string target = presenter.Target;
                Assert.That(target, Is.Not.Null);
                Assert.That(presenter.Handle(new Battle.Presentation.BattleHudIntent(Battle.Presentation.BattleHudIntentKind.PointerDown, "target:" + target, 37)), Is.True);
                Assert.That(presenter.CapturedPointerId, Is.EqualTo(37));
                Assert.That(editor.Battle.BeginFormation(controller), Is.True);
                presenter.Synchronize(editor.Battle);
                Assert.That(presenter.CapturedPointerId, Is.Null);
                Assert.That(presenter.Target, Is.Null);
            }
            else
            {
                Assert.That(presenter.Handle(new Battle.Presentation.BattleHudIntent(Battle.Presentation.BattleHudIntentKind.Confirm, "formation")), Is.True);
            }
            int before = editor.Battle.Inputs.Count;
            void Set(string name, object value) => typeof(BattleHudView).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(view, value);
            Set("_battle", editor.Battle);
            Set("_inputEnabled", true);
            Set("_interaction", new BattleHudInteractionProfile(true));
            Set("_viewport", new BattleHudViewport(new Rect(0, 0, 800, 600), default));
            var hits = (List<(string, Rect)>)typeof(BattleHudView).GetField("_hitControls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(view);
            hits.Add(("slot:1", new Rect(100, 100, 40, 38)));
            hits.Add(("actor:" + moved, new Rect(100, 100, 40, 38)));
            hits.Add(("slot:2", new Rect(180, 100, 40, 38)));

            Assert.That(view.SendPointer(Battle.Presentation.BattleHudIntentKind.PointerDown, 9, new Vector2(120, 119)), Is.True);
            view.SetInputFocus(false);
            Assert.That(presenter.FormationActor, Is.Null, "Losing input focus must release the dragged actor along with its pointer.");
            Assert.That(presenter.CapturedPointerId, Is.Null);
            Assert.That(editor.Battle.Inputs.Count, Is.EqualTo(before));
            view.SetInputFocus(true);
            Assert.That(view.SendPointer(Battle.Presentation.BattleHudIntentKind.PointerDown, 9, new Vector2(120, 119)), Is.True);
            Assert.That(presenter.CapturedPointerId, Is.EqualTo(9));
            view.SendPointer(Battle.Presentation.BattleHudIntentKind.PointerMove, 9, new Vector2(200, 119));
            Assert.That(editor.Battle.Inputs.Count, Is.EqualTo(before));
            Assert.That(editor.Battle.Actors.First(actor => actor.Id == controller).Gauge, Is.EqualTo(gauge));
            Assert.That(view.SendPointer(Battle.Presentation.BattleHudIntentKind.PointerUp, 9, new Vector2(200, 119)), Is.True);
            Assert.That(editor.Battle.Inputs.Count, Is.EqualTo(before + 1));
            Assert.That(editor.Battle.Actors.First(actor => actor.Id == moved).Slot, Is.EqualTo(BattleSlot.Wind));
            Assert.That(view.SendPointer(Battle.Presentation.BattleHudIntentKind.PointerUp, 9, new Vector2(200, 119)), Is.False);
            Assert.That(editor.Battle.Inputs.Count, Is.EqualTo(before + 1));
            Assert.That(presenter.Handle(new Battle.Presentation.BattleHudIntent(Battle.Presentation.BattleHudIntentKind.Confirm, "end")), Is.True);
            Assert.That(editor.Battle.Actors.First(actor => actor.Id == controller).Gauge, Is.Zero);
        }

        [Test]
        public void ProtectionDraftBackAndUnchangedConfirmationAreFreeWhileAChangedConfirmationPaysOnce()
        {
            string configured = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            string root = configured ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root) && configured == null)
                Assert.Ignore("Original local data unavailable.");
            using var editor = new BattleEditorSession();
            editor.Load(root, 936);
            var scenario = new BattleScenario { Variant = BattleData.Variant, Scene = editor.Data.Scenes.First(), DataFingerprint = editor.Data.Fingerprint, Encounter = BattleEncounter.AllyAmbush };
            scenario.Participants.Add(editor.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Center));
            scenario.Participants.Add(editor.Data.CreateParticipant(BattleData.Variant == "PAL3" ? 104 : 102, BattleSide.Ally, BattleSlot.Fire));
            int enemy = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
            scenario.Participants.Add(editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            var presenter = new Battle.Presentation.BattleHudPresenter();
            presenter.Synchronize(editor.Battle);
            string controller = presenter.Controller, target = scenario.Participants[1].InstanceId;
            int before = editor.Battle.Inputs.Count;
            presenter.Open(Battle.Presentation.BattleHudPage.Protection);
            Assert.That(presenter.SetProtection(target), Is.True);
            Assert.That(editor.Battle.ProtectionTarget, Is.Null);
            presenter.Back();
            Assert.That(editor.Battle.Inputs.Count, Is.EqualTo(before));
            presenter.Open(Battle.Presentation.BattleHudPage.Protection);
            Assert.That(presenter.CommitProtection(), Is.True);
            Assert.That(editor.Battle.Inputs.Count, Is.EqualTo(before));
            Assert.That(editor.Battle.WaitingActor, Is.EqualTo(controller));
            presenter.Open(Battle.Presentation.BattleHudPage.Protection);
            Assert.That(presenter.SetProtection(target), Is.True);
            Assert.That(presenter.CommitProtection(), Is.True);
            Assert.That(editor.Battle.ProtectionTarget, Is.EqualTo(target));
            Assert.That(editor.Battle.Inputs.Count, Is.EqualTo(before + 1));
            Assert.That(editor.Battle.Inputs.Last().Kind, Is.EqualTo(BattleInputKind.SetProtection));
            Assert.That(editor.Battle.Actors.First(actor => actor.Id == controller).Gauge, Is.Zero);
        }

#if PAL3A
        [Test]
        public void ElementWheelHudUsesTheDedicatedNativeInputWithoutEnteringOrdinaryMagicTargets()
        {
            string configured = Environment.GetEnvironmentVariable("PAL3A_TEST_DATA_ROOT");
            string root = configured ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data/PAL3A"));
            if (!Directory.Exists(root) && configured == null)
                Assert.Ignore("Original local data unavailable.");
            using var editor = new BattleEditorSession();
            editor.Load(root, 936);
            var scenario = new BattleScenario { Variant = "PAL3A", Scene = editor.Data.Scenes.First(), DataFingerprint = editor.Data.Fingerprint, Encounter = BattleEncounter.AllyAmbush };
            scenario.Pal3A.SpiritWheel.Charges = new[] { 9, 9, 9, 9, 9 };
            scenario.Participants.Add(editor.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
            scenario.Participants.Add(editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            var presenter = new Battle.Presentation.BattleHudPresenter();
            presenter.Synchronize(editor.Battle);
            string controller = presenter.Controller;
            BattleActorSnapshot before = editor.Battle.Actors.First(actor => actor.Id == controller);
            int inputs = editor.Battle.Inputs.Count;
            for (int attempt = 0; attempt < 10; attempt++)
                Assert.That(presenter.WheelReason(267), Is.Null);
            Assert.That(editor.Battle.Inputs.Count, Is.EqualTo(inputs));
            var reached = new HashSet<string> { presenter.FocusedId };
            var pending = new Queue<string>(reached);
            while (pending.Count > 0)
            {
                string from = pending.Dequeue();
                foreach (Battle.Presentation.BattleHudDirection direction in Enum.GetValues(typeof(Battle.Presentation.BattleHudDirection)))
                {
                    presenter.SetFocus(from);
                    presenter.Handle(new Battle.Presentation.BattleHudIntent(Battle.Presentation.BattleHudIntentKind.Navigate, direction: direction));
                    if (reached.Add(presenter.FocusedId))
                        pending.Enqueue(presenter.FocusedId);
                }
                // The eight directions address the original command ring;
                // shoulder buttons reach the separate wheel/special controls.
                foreach (Battle.Presentation.BattleHudIntentKind kind in new[] { Battle.Presentation.BattleHudIntentKind.CyclePrevious, Battle.Presentation.BattleHudIntentKind.CycleNext })
                {
                    presenter.SetFocus(from);
                    presenter.Handle(new Battle.Presentation.BattleHudIntent(kind));
                    if (reached.Add(presenter.FocusedId))
                        pending.Enqueue(presenter.FocusedId);
                }
            }
            Assert.That(Enumerable.Range(264, 5).All(id => reached.Contains("wheel:" + id)), Is.True, "All native wheel controls must be reachable through command navigation and shoulder buttons.");
            Assert.That(editor.Battle.Inputs.Count, Is.EqualTo(inputs), "Browsing the ring and wheel must not submit a battle input.");
            Assert.That(presenter.Handle(new Battle.Presentation.BattleHudIntent(Battle.Presentation.BattleHudIntentKind.Confirm, "wheel:267")), Is.True);
            Assert.That(presenter.Page, Is.EqualTo(Battle.Presentation.BattleHudPage.Commands));
            Assert.That(editor.Battle.Inputs.Last().Kind, Is.EqualTo(BattleInputKind.ElementWheel));
            Assert.That(editor.Battle.Inputs.Last().AbilityId, Is.EqualTo(267));
            Assert.That(editor.Battle.Inputs.Last().Target, Is.Null);
            BattleActorSnapshot after = editor.Battle.Actors.First(actor => actor.Id == controller);
            Assert.That((after.Sp, after.Mp), Is.EqualTo((before.Sp, before.Mp)));
        }
#endif

        [Test]
        public void ReleasedHudProfilesResolveRealResourcesAndBorrowThePreviewTextureCache()
        {
            string configured = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            string root = configured ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root) && configured == null)
                Assert.Ignore("Original local data unavailable.");
            using var data = new BattleData(root, 936);
            using var preview = new ResourceModelPreview(data.Resources, false);
            var layout = new BattleHudLayout(BattleData.Variant);
            var paths = new List<string> { "state/StatePanel", "state/ProgBar", "state/ProgBar_HP", "state/ProgBar_GP", "state/ProgBar_MP",
                "AttackSequen/Back", "control/back", "control/ItemList/ItemPanel", "control/MagicList/MagicPanel", "control/SkillList/SkillPanel",
                "control/Lineup/back", "control/AI/back", "control/Protect/back" };
            if (!layout.IsPal3A)
                paths.AddRange(new[] { "state/StatePanel0", "state/sword", "state/swordbar", "control/mj0", "control/mj1" });
            else
            {
                paths.AddRange(new[] { "control/bs0", "control/bs1", "control/wll", "control/unbs" });
                for (int element = 0; element < 5; element++)
                {
                    paths.Add("control/wt" + element + "0");
                    for (int tier = 0; tier < 3; tier++)
                        paths.Add("control/unwt" + element + tier);
                }
            }
            foreach (int actorId in (layout.IsPal3A ? new[] { 101, 102, 103, 104, 105 } : new[] { 101, 104, 106, 107, 102 }))
                paths.Add("attacksequen/" + data.Model(data.CreateParticipant(actorId, BattleSide.Ally, BattleSlot.Water)));
            for (int identity = 0; identity < 5; identity++)
                paths.AddRange(new[] { layout.StateFace(identity), layout.StateName(identity), layout.FormationFace(identity, false),
                    layout.FormationFace(identity, true), layout.AiName(identity) });
            foreach (string control in new[] { "attack", "skill", "magic", "item", "defence", "flee", "lineup", "ai", "protect" })
                for (int frame = 0; frame < 4; frame++)
                    paths.Add("control/" + control + frame);
            foreach (string path in paths)
            {
                string original = "basedata.cpk\\ui\\combat\\" + path.Replace('/', '\\') + ".tga";
                Assert.That(data.Resources.Find(original), Is.Not.Null, original);
                Texture2D first = preview.GetTexture(original);
                Assert.That(first, Is.Not.SameAs(Texture2D.whiteTexture), original);
                Assert.That(first.width, Is.GreaterThan(1), original);
                Assert.That(preview.GetTexture(original), Is.SameAs(first), "HUD must borrow the cached resource: " + original);
            }
        }
    }
}
