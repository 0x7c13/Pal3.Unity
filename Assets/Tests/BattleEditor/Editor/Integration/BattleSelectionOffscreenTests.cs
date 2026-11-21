// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Battle;
    using Battle.Presentation;
    using Core.DataReader.Mv3;
    using NUnit.Framework;
    using ResourceBrowser;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    // Uses only preview scenes and RenderTextures. Never creates an EditorWindow.
    [Category("LocalGameData")]
    [Category("OffscreenGraphics")]
    public sealed class BattleSelectionOffscreenTests
    {
        private static BattleHudView BoundHud(BattleEditorSession session, bool touch, Rect viewport)
        {
            var hud = new BattleHudView();
            hud.BindBattle(session.Battle, session.Data, session.Preview, null, null);
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(BattleHudView).GetField("_viewport", fields).SetValue(hud, new BattleHudViewport(viewport, default));
            typeof(BattleHudView).GetField("_interaction", fields).SetValue(hud, new BattleHudInteractionProfile(touch));
            hud.SetInputFocus(true);
            return hud;
        }

        private static BattleHudView TargetHud(BattleEditorSession session, bool touch, string enemy, Rect viewport)
        {
            BattleHudView hud = BoundHud(session, touch, viewport);
            hud.Presenter.Open(BattleHudPage.Skills);
            int skill = session.Battle.Definitions.Skills.Values.Where(ability => !ability.IsMagic && ability.Range == BattleTargetRange.OpponentSingle)
                .Where(ability => session.Battle.CanSubmit(new BattleCommand(hud.Presenter.Controller, BattleAction.Skill, enemy, ability.Id)) == null)
                .Select(ability => ability.Id).First();
            Assert.That(hud.Presenter.ChooseAbility(skill), Is.True);
            return hud;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OneCompleteTapOnALegalSkillRecipientSubmitsExactlyOnce(bool touch)
        {
            using var session = Create(false, out string enemy);
            var viewport = new Rect(0, 0, 800, 600);
            session.Preview.View.SetViewportContext(viewport);
            BattleHudView hud = TargetHud(session, touch, enemy, viewport);
            Assert.That(session.Preview.TryActorSelectionPoint(enemy, viewport, out Vector2 point), Is.True);
            hud.SendPointer(BattleHudIntentKind.PointerDown, 9, point);
            Assert.That(session.Battle.Inputs.Count(input => input.Kind == BattleInputKind.Command), Is.Zero);
            hud.SendPointer(BattleHudIntentKind.PointerUp, 9, point);
            Assert.That(session.Battle.Inputs.Count(input => input.Kind == BattleInputKind.Command), Is.EqualTo(1));
            Assert.That(session.Battle.Inputs.Last(input => input.Kind == BattleInputKind.Command).Target, Is.EqualTo(enemy));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void InvalidSkillRecipientOrEmptyWorldReturnsToTheSkillListWithoutCasting(bool touch, bool invalidActor)
        {
            using var session = Create(false, out string enemy);
            var viewport = new Rect(0, 0, 800, 600);
            session.Preview.View.SetViewportContext(viewport);
            BattleHudView hud = TargetHud(session, touch, enemy, viewport);
            Vector2 point = new(5, 5);
            if (invalidActor)
                Assert.That(session.Preview.TryActorSelectionPoint(hud.Presenter.Controller, viewport, out point), Is.True);
            else
                Assert.That(session.Preview.PickActor(point, viewport, session.Battle.Actors.Select(actor => actor.Id)), Is.Null);
            hud.SendPointer(BattleHudIntentKind.PointerDown, 9, point);
            hud.SendPointer(BattleHudIntentKind.PointerUp, 9, point);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Skills));
            Assert.That(session.Battle.Inputs.Count(input => input.Kind == BattleInputKind.Command), Is.Zero);
        }

        // A native GUI state and viewport exercise control ownership without
        // creating a window. Unity ignores mouse presses outside its GUI clip.
        private sealed class NativeGuiState : IDisposable
        {
            private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
            private static readonly Type StateType = typeof(GUIUtility).Assembly.GetType("UnityEngine.ObjectGUIState", true);
            private static readonly MethodInfo Begin = typeof(GUIUtility).GetMethod("BeginContainer", Static, null, new[] { StateType }, null);
            private static readonly MethodInfo End = typeof(GUIUtility).GetMethod("EndContainer", Static, null, Type.EmptyTypes, null);
            private static readonly Type ClipType = typeof(GUIUtility).Assembly.GetType("UnityEngine.GUIClip", true);
            private static readonly MethodInfo PushClip = ClipType.GetMethod("Internal_PushParentClip", Static, null, new[] { typeof(Matrix4x4), typeof(Matrix4x4), typeof(Rect) }, null);
            private static readonly MethodInfo PopClip = ClipType.GetMethod("Internal_PopParentClip", Static, null, Type.EmptyTypes, null);
            private readonly object _state = Activator.CreateInstance(StateType);

            public void Run(BattleHudView hud, Event input, Action<Event> body)
            {
                Event previous = Event.current;
                Vector2 mousePosition = input.mousePosition;
                Begin.Invoke(null, new[] { _state });
                bool clipped = false;
                try
                {
                    Event.current = input;
                    PushClip.Invoke(null, new object[] { Matrix4x4.identity, Matrix4x4.identity, new Rect(0, 0, 800, 600) });
                    clipped = true;
                    Event.current.mousePosition = mousePosition;
                    GUI.enabled = true;
                    typeof(BattleHudView).GetMethod("PrepareGuiInput", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
                    body(Event.current);
                }
                finally
                {
                    if (clipped)
                        PopClip.Invoke(null, null);
                    End.Invoke(null, null);
                    Event.current = previous;
                }
            }

            public void Dispose() => ((IDisposable)_state).Dispose();
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void DrawerKeyboardFocusSurvivesPointerMovementUntilTheCanvasIsClicked(string variant)
        {
            BattleSession battle = BattleHudTests.Session(variant);
            var hud = new BattleHudView();
            hud.BindBattle(battle, null, null, null, null);
            hud.SetInputFocus(true);
            var canvas = new Rect(0, 0, 400, 500);
            var drawer = new Rect(420, 0, 280, 500);
            const BindingFlags methods = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo handle = typeof(BattleHudView).GetMethod("HandleGuiInput", methods);
            MethodInfo complete = typeof(BattleHudView).GetMethod("CompleteGuiInput", methods);
            using var gui = new NativeGuiState();
            int tab = 0;
            gui.Run(hud, new Event { type = EventType.MouseDown, mousePosition = drawer.center }, _ =>
            {
                tab = GUIUtility.GetControlID(73401, FocusType.Keyboard, drawer);
                GUIUtility.keyboardControl = tab;
                Assert.That(tab, Is.Not.Zero);
            });
            gui.Run(hud, new Event { type = EventType.KeyDown, mousePosition = canvas.center, keyCode = KeyCode.RightArrow }, input =>
            {
                Assert.That(GUIUtility.keyboardControl, Is.EqualTo(tab));
                Assert.That(EditorGUIUtility.editingTextField, Is.False);
                handle.Invoke(hud, new object[] { input, canvas });
                Assert.That(input.type, Is.EqualTo(EventType.KeyDown));
                Assert.That(hud.Presenter.FocusedId, Is.EqualTo("attack"));
            });
            gui.Run(hud, new Event { type = EventType.MouseDown, mousePosition = canvas.center }, input =>
            {
                complete.Invoke(hud, new object[] { input, canvas });
                Assert.That(GUIUtility.keyboardControl, Is.Zero);
            });
            gui.Run(hud, new Event { type = EventType.MouseUp, mousePosition = canvas.center }, input =>
                complete.Invoke(hud, new object[] { input, canvas }));
            gui.Run(hud, new Event { type = EventType.KeyDown, mousePosition = canvas.center, keyCode = KeyCode.RightArrow }, input =>
            {
                handle.Invoke(hud, new object[] { input, canvas });
                Assert.That(input.type, Is.EqualTo(EventType.Used));
                Assert.That(hud.Presenter.FocusedId, Is.EqualTo("defend"));
            });
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void GamepadTargetNavigationUsesScreenPositionsBeforeTheFirstTargetPageDraw(string variant)
        {
            BattleSession battle = BattleHudTests.Session(variant);
            var command = new BattleCommand(battle.WaitingActor, BattleAction.Attack);
            string[] candidates = battle.GetTargetCandidates(command).Select(actor => actor.Id).ToArray();
            string center = battle.GetDefaultTarget(command);
            int index = Array.IndexOf(candidates, center);
            string left = candidates[(index + 1) % candidates.Length];
            string right = candidates[(index + 2) % candidates.Length];
            using var view = new ResourceModelPreview(null, false);
            view.SetCamera(new Vector3(0, 0, -10), Vector3.zero, 45);
            var viewport = new Rect(0, 0, 800, 600);
            view.SetViewportContext(viewport);

            // This fixture supplies only actor bounds and a real preview camera.
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var preview = (BattlePreview)FormatterServices.GetUninitializedObject(typeof(BattlePreview));
            Type type = typeof(BattlePreview);
            type.GetField("<View>k__BackingField", fields).SetValue(preview, view);
            FieldInfo field = type.GetField("_actors", fields);
            var actorViews = (IDictionary)Activator.CreateInstance(field.FieldType);
            field.SetValue(preview, actorViews);
            Type actorType = type.GetNestedType("ActorView", BindingFlags.NonPublic);
            var model = new Mv3File(0, null, null, new[] { new Mv3Mesh() }, null);
            foreach ((string id, float x) in new[] { (left, -2f), (center, 0f), (right, 2f) })
            {
                object actor = Activator.CreateInstance(actorType);
                GameObject body = view.CreateObject(id);
                body.transform.position = new Vector3(x, 0, 0);
                actorType.GetField("Object").SetValue(actor, body);
                actorType.GetField("Model").SetValue(actor, model);
                actorViews.Add(id, actor);
            }
            Assert.That(preview.TryActorSelectionPoint(center, viewport, out Vector2 origin), Is.True);
            Assert.That(preview.TryActorSelectionPoint(right, viewport, out Vector2 expected), Is.True);
            Assert.That(expected.x, Is.GreaterThan(origin.x));

            var hud = new BattleHudView();
            hud.BindBattle(battle, null, preview, null, null);
            typeof(BattleHudView).GetField("_viewport", fields).SetValue(hud, new BattleHudViewport(viewport, default));
            hud.Presenter.SetFocusLayout(new[] { ("attack", 40f, 410f) });
            var gamepad = new BattleEditorGamepadState();
            gamepad.Sample(new BattleEditorGamepadSnapshot(buttons: BattleEditorGamepadButtons.Confirm), true);
            gamepad.Sample(default, true);
            gamepad.Sample(new BattleEditorGamepadSnapshot(dpadX: 1), true);
            gamepad.Sample(new BattleEditorGamepadSnapshot(dpadX: 1, buttons: BattleEditorGamepadButtons.Confirm), true);
            gamepad.Advance(1, true, intent =>
            {
                hud.TakeGamepadInput();
                Assert.That(hud.SendGamepadIntent(intent), Is.True);
            });
            Assert.That(battle.Inputs.Single(input => input.Kind == BattleInputKind.Command).Target, Is.EqualTo(right));
        }

        [TestCase(false, 0)]
        [TestCase(false, 1)]
        [TestCase(false, 2)]
        [TestCase(true, 0)]
        [TestCase(true, 1)]
        [TestCase(true, 2)]
        public void NativePointerCaptureIsReleasedByOutsideReleaseEscapeAndFocusLoss(bool touch, int cancellation)
        {
            using var session = Create(false, out string enemy);
            var viewport = new Rect(0, 0, 800, 600);
            session.Preview.View.SetViewportContext(viewport);
            BattleHudView hud = TargetHud(session, touch, enemy, viewport);
            Assert.That(session.Preview.TryActorSelectionPoint(enemy, viewport, out Vector2 point), Is.True);
            MethodInfo handler = typeof(BattleHudView).GetMethod("HandleGuiInput", BindingFlags.Instance | BindingFlags.NonPublic);
            using var gui = new NativeGuiState();
            try
            {
                gui.Run(hud, new Event { type = EventType.Layout }, _ => { });
                gui.Run(hud, new Event { type = EventType.MouseDown, button = 0, mousePosition = point }, input =>
                {
                    Assert.That(input.type, Is.EqualTo(EventType.MouseDown));
                    handler.Invoke(hud, new object[] { input, viewport });
                    Assert.That(input.type, Is.EqualTo(EventType.Used));
                    Assert.That(GUIUtility.hotControl, Is.Not.Zero);
                    Assert.That(hud.Presenter.CapturedPointerId, Is.EqualTo(0));
                });
                Event end = cancellation == 0 ? new Event { type = EventType.MouseUp, button = 0, mousePosition = new Vector2(-20, -20) } :
                    cancellation == 1 ? new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape } : new Event { type = EventType.Layout };
                gui.Run(hud, end, input =>
                {
                    if (cancellation == 2)
                        hud.SetInputFocus(false);
                    else
                        handler.Invoke(hud, new object[] { input, viewport });
                    Assert.That(GUIUtility.hotControl, Is.Zero);
                    Assert.That(hud.Presenter.CapturedPointerId, Is.Null);
                    Assert.That(session.Battle.Inputs.Count(entry => entry.Kind == BattleInputKind.Command), Is.Zero);
                });
            }
            finally
            {
                gui.Run(hud, new Event { type = EventType.Layout }, _ => hud.Release());
            }
        }

        [Test]
        public void ScrollbarClaimAfterTouchDownReleasesPendingPanForTheNextFinger()
        {
            var hud = new BattleHudView();
            var list = new Rect(10, 10, 160, 100);
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(BattleHudView);
            type.GetField("_interaction", fields).SetValue(hud, new BattleHudInteractionProfile(true));
            type.GetField("_touchPanel", fields).SetValue(hud, list);
            MethodInfo gesture = type.GetMethod("TouchGesture", fields);
            FieldInfo candidate = type.GetField("_touchCandidatePointer", fields);
            using var gui = new NativeGuiState();
            gui.Run(hud, new Event { type = EventType.MouseDown, button = 0, mousePosition = list.center }, input =>
            {
                gesture.Invoke(hud, new object[] { BattleHudIntentKind.PointerDown, 0, input.mousePosition });
                Assert.That(candidate.GetValue(hud), Is.EqualTo(0));
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
            });
            gui.Run(hud, new Event { type = EventType.MouseUp, button = 0 }, _ =>
            {
                Assert.That(GUIUtility.hotControl, Is.Not.Zero);
                Assert.That(candidate.GetValue(hud), Is.Null);
                GUIUtility.hotControl = 0;
            });
            gui.Run(hud, new Event { type = EventType.MouseDown, button = 0, mousePosition = list.center }, input =>
            {
                gesture.Invoke(hud, new object[] { BattleHudIntentKind.PointerDown, 7, input.mousePosition });
                Assert.That(candidate.GetValue(hud), Is.EqualTo(7));
            });
        }

        [Test]
        public void NativeScrollbarCaptureCannotBeStolenByHudClicksBackOrCancellation()
        {
            using var session = Create(false, out string enemy);
            var viewport = new Rect(0, 0, 800, 600);
            session.Preview.View.SetViewportContext(viewport);
            BattleHudView hud = TargetHud(session, true, enemy, viewport);
            Assert.That(session.Preview.TryActorSelectionPoint(enemy, viewport, out Vector2 point), Is.True);
            MethodInfo handler = typeof(BattleHudView).GetMethod("HandleGuiInput", BindingFlags.Instance | BindingFlags.NonPublic);
            using var gui = new NativeGuiState();
            try
            {
                foreach (int button in new[] { 0, 1 })
                    gui.Run(hud, new Event { type = EventType.MouseDown, button = button, mousePosition = point }, input =>
                    {
                        int scrollbar = GUIUtility.GetControlID(0x5363726c, FocusType.Passive);
                        GUIUtility.hotControl = scrollbar;
                        handler.Invoke(hud, new object[] { input, viewport });
                        Assert.That(input.type, Is.EqualTo(EventType.MouseDown));
                        hud.CancelInput();
                        Assert.That(GUIUtility.hotControl, Is.EqualTo(scrollbar));
                        Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Targets));
                        Assert.That(hud.Presenter.CapturedPointerId, Is.Null);
                        Assert.That(session.Battle.Inputs.Count(entry => entry.Kind == BattleInputKind.Command), Is.Zero);
                        GUIUtility.hotControl = 0;
                    });
            }
            finally
            {
                gui.Run(hud, new Event { type = EventType.Layout }, _ => hud.Release());
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FocusLossOutsideTheOwnerGuiStateReleasesOnlyTheOwnerOnItsNextPass(bool anotherGuiActive)
        {
            using var session = Create(false, out string enemy);
            var viewport = new Rect(0, 0, 800, 600);
            session.Preview.View.SetViewportContext(viewport);
            BattleHudView hud = TargetHud(session, true, enemy, viewport);
            Assert.That(session.Preview.TryActorSelectionPoint(enemy, viewport, out Vector2 point), Is.True);
            MethodInfo handler = typeof(BattleHudView).GetMethod("HandleGuiInput", BindingFlags.Instance | BindingFlags.NonPublic);
            using var owner = new NativeGuiState();
            using var other = new NativeGuiState();
            var otherHud = new BattleHudView();
            try
            {
                owner.Run(hud, new Event { type = EventType.MouseDown, button = 0, mousePosition = point }, input =>
                {
                    handler.Invoke(hud, new object[] { input, viewport });
                    Assert.That(GUIUtility.hotControl, Is.Not.Zero);
                    Assert.That(hud.Presenter.CapturedPointerId, Is.EqualTo(0));
                });
                owner.Run(hud, new Event { type = EventType.Layout }, _ => Assert.That(GUIUtility.hotControl, Is.Not.Zero,
                    "The native owner state must retain capture between GUI passes for this lifecycle fixture."));
                if (anotherGuiActive)
                    other.Run(otherHud, new Event { type = EventType.Layout }, _ =>
                    {
                        int foreign = GUIUtility.GetControlID(0x5363726c, FocusType.Passive);
                        GUIUtility.hotControl = foreign;
                        hud.SetInputFocus(false);
                        Assert.That(GUIUtility.hotControl, Is.EqualTo(foreign), "Losing focus must not clear another window's capture.");
                        GUIUtility.hotControl = 0;
                    });
                else
                    hud.SetInputFocus(false);
                Assert.That(hud.Presenter.CapturedPointerId, Is.Null);
                owner.Run(hud, new Event { type = EventType.Layout }, _ =>
                    Assert.That(GUIUtility.hotControl, Is.Zero, "The next pass must release the original window's capture, rather than lose its ID."));
            }
            finally
            {
                owner.Run(hud, new Event { type = EventType.Layout }, _ => hud.Release());
                other.Run(otherHud, new Event { type = EventType.Layout }, _ => otherHud.Release());
            }
        }

        private static BattleEditorSession Create(bool toon, out string enemy)
        {
            var session = new BattleEditorSession();
            session.Load(Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT"), 936);
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = session.Data.Scenes.First(),
                DataFingerprint = session.Data.Fingerprint,
                Encounter = BattleEncounter.AllyAmbush,
                ToonRendering = toon,
                Music = "NONE"
            };
            scenario.Participants.Add(session.Data.CreateTestParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int id = session.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(session.Data, id));
            scenario.Participants.Add(session.Data.CreateParticipant(id, BattleSide.Enemy, BattleSlot.Water));
            enemy = scenario.Participants[1].InstanceId;
            session.StartBattle(scenario);
            session.Preview.Audio.Muted = session.Preview.Effects.Muted = true;
            Vector3 center = session.Battle.Actors.Select(actor => BattleAnimationCatalog.UnityPosition(actor.Position)).Aggregate(Vector3.zero, (sum, value) => sum + value) / 2;
            session.Preview.View.SetCamera(center + new Vector3(9, 9, -14), center + Vector3.up, 50);
            return session;
        }

        private static Color32[] Pixels(RenderTexture texture, string name)
        {
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0);
                image.Apply();
                string root = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT");
                if (!string.IsNullOrEmpty(root))
                {
                    string folder = Path.Combine(root, "SelectionOffscreen");
                    Directory.CreateDirectory(folder);
                    File.WriteAllBytes(Path.Combine(folder, BattleData.Variant + "-" + name + ".png"), image.EncodeToPNG());
                }
                return image.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SelectedBodyChangesActualRgbAndPreservesItsAlphaAndSimulation(bool toon)
        {
            using var session = Create(toon, out string enemy);
            Transform actor = session.Preview.View.Root.transform.Find(enemy);
            var body = actor.GetComponentInChildren<Pal3.Game.Rendering.Renderer.Mv3ModelRenderer>();
            body.SetOpacity(.44f);
            var mesh = body.GameEntity.GetComponentsInChildren<MeshRenderer>().First();
            var properties = new MaterialPropertyBlock();
            mesh.GetPropertyBlock(properties);
            properties.SetFloat("_SelectionTestSentinel", .375f);
            mesh.SetPropertyBlock(properties);
            Material[] materials = mesh.sharedMaterials;
            Assert.That(materials, Is.Not.Empty);
            Assert.That(materials.All(material => Math.Abs(material.GetColor("_TintColor").a - .44f) < .00001f), Is.True,
                "The fixture must inspect the actual faded materials, rather than an unset property-block value.");
            long tick = session.Battle.Tick;
            int events = session.Battle.Events.Count;
            var hud = new BattleHudView();
            hud.TakeNavigationInput(session.Battle);
            Assert.That(hud.Presenter.ChooseAction(BattleAction.Attack), Is.True);
            Assert.That(hud.Presenter.Target, Is.EqualTo(enemy));
            hud.PreparePointers(session.Battle, session.Data, session.Preview, Math.PI * .5 / 7.8);
            mesh.GetPropertyBlock(properties);
            Assert.That(properties.GetFloat("_BattleSelection"), Is.EqualTo(1f).Within(.001f));
            Color32[] high = Pixels(session.Preview.View.Render(800, 600), (toon ? "toon" : "native") + "-high");
            hud.PreparePointers(session.Battle, session.Data, session.Preview, Math.PI * 1.5 / 7.8);
            mesh.GetPropertyBlock(properties);
            Assert.That(properties.GetFloat("_BattleSelection"), Is.EqualTo(.3f).Within(.001f));
            Color32[] low = Pixels(session.Preview.View.Render(800, 600), (toon ? "toon" : "native") + "-low");
            double energy = 0;
            int changed = 0;
            Assert.That(session.Preview.TryActorSelectionPoint(enemy, new Rect(0, 0, 800, 600), out Vector2 point), Is.True);
            int centerX = Mathf.RoundToInt(point.x);
            int centerY = 599 - Mathf.RoundToInt(point.y);
            for (int y = Math.Max(0, centerY - 15); y <= Math.Min(599, centerY + 15); y++)
                for (int x = Math.Max(0, centerX - 15); x <= Math.Min(799, centerX + 15); x++)
                {
                    int index = y * 800 + x;
                    int delta = high[index].r + high[index].g + high[index].b - low[index].r - low[index].g - low[index].b;
                    energy += delta;
                    if (delta > 5)
                        changed++;
                }
            Assert.That(changed, Is.GreaterThan(40), "The body pulse must reach the rendered pixels in both material modes.");
            Assert.That(energy, Is.GreaterThan(200));
            mesh.GetPropertyBlock(properties);
            Assert.That(properties.GetFloat("_SelectionTestSentinel"), Is.EqualTo(.375f), "Selection must merge the existing property block.");
            Assert.That(materials.All(material => Math.Abs(material.GetColor("_TintColor").a - .44f) < .00001f), Is.True);
            hud.Presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Back));
            hud.PreparePointers(session.Battle, session.Data, session.Preview, 0);
            mesh.GetPropertyBlock(properties);
            Assert.That(properties.GetFloat("_BattleSelection"), Is.EqualTo(-1));
            Assert.That(materials.All(material => Math.Abs(material.GetColor("_TintColor").a - .44f) < .00001f), Is.True);
            Assert.That(session.Battle.Tick, Is.EqualTo(tick));
            Assert.That(session.Battle.Events.Count, Is.EqualTo(events));
        }

        [Test]
        public void ReturningFromTargetSelectionPreservesTheSkillToBeReconsidered()
        {
            using var session = Create(false, out _);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session.Battle);
            presenter.Open(BattleHudPage.Skills);
            BattleHudAbility row = presenter.Abilities().Where(entry => entry.Denied == null).Skip(1).First();
            string control = "ability:" + row.Definition.Id;
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, control)), Is.True);
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Back));
            Assert.That(presenter.FocusedId, Is.EqualTo(control));
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm)), Is.True);
            Assert.That(presenter.Command.AbilityId, Is.EqualTo(row.Definition.Id));
        }

        [Test]
        public void TouchCanLeaveProtectionWithoutIssuingACommand()
        {
            using var session = Create(false, out _);
            BattleScenario scenario = session.CapturedScenario;
            scenario.Participants[0].Slot = BattleSlot.Center;
            int companionId = BattleData.Variant == "PAL3A" ? 102 : 104;
            scenario.Participants.Add(session.Data.CreateParticipant(companionId, BattleSide.Ally, BattleSlot.Fire));
            session.StartBattle(scenario);
            var viewport = new Rect(0, 0, 800, 600);
            BattleHudView hud = BoundHud(session, true, viewport);
            Assert.That(hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Confirm, "protection")), Is.True);
            int inputs = session.Battle.RecordedInputCount;
            Vector2 ground = new(5, 5);
            Assert.That(session.Preview.PickActor(ground, viewport, session.Battle.Actors.Select(actor => actor.Id)), Is.Null);
            hud.SendPointer(BattleHudIntentKind.PointerDown, 1, ground);
            hud.SendPointer(BattleHudIntentKind.PointerUp, 1, ground);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Commands));
            Assert.That(session.Battle.RecordedInputCount, Is.EqualTo(inputs));
        }

        [Test]
        public void TouchOnTheDisabledSideOfASkillRowBoundaryCannotActivateItsNeighbour()
        {
            using var session = Create(false, out _);
            var viewport = new Rect(0, 0, 800, 600);
            BattleHudView hud = BoundHud(session, true, viewport);
            hud.Presenter.Open(BattleHudPage.Skills);
            var rows = hud.Presenter.Abilities();
            BattleHudAbility available = rows.First(row => row.Denied == null);
            BattleHudAbility disabled = rows.First(row => row.Denied != null);
            // Place a disabled row directly below an enabled row in the native
            // skill list, at its actual scroll-view position and row height.
            Rect list = BattleHudText.ListBounds(BattleHudPage.Skills, BattleData.Variant == "PAL3A");
            list.position += BattleHudText.AbilityPanelBounds(BattleHudPage.Skills, BattleData.Variant == "PAL3A").position;
            const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
            var hitControls = (System.Collections.Generic.List<(string Id, Rect Rect)>)typeof(BattleHudView).GetField("_hitControls", members).GetValue(hud);
            Rect rowRect = new(list.x, list.y, list.width - 16, 19);
            hitControls.Add(("ability:" + available.Definition.Id, rowRect));
            rowRect.y += rowRect.height;
            hitControls.Add(("ability:" + disabled.Definition.Id, rowRect));
            typeof(BattleHudView).GetField("_touchPanel", members).SetValue(hud, list);
            Vector2 tap = new(rowRect.center.x, rowRect.yMin + 1);
            hud.SendPointer(BattleHudIntentKind.PointerDown, 2, tap);
            hud.SendPointer(BattleHudIntentKind.PointerUp, 2, tap);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Skills));
        }

        [Test]
        public void OffensiveGroupSkillOnlyMarksAndSelectsSurvivingEnemies()
        {
            using var session = Create(false, out string defeated);
            BattleScenario scenario = session.CapturedScenario;
            BattleParticipant first = scenario.Participants.Single(actor => actor.InstanceId == defeated);
            first.Vitals[0] = 0;
            BattleParticipant survivor = session.Data.CreateParticipant(first.ActorId, BattleSide.Enemy, BattleSlot.Fire);
            scenario.Participants.Add(survivor);
            session.StartBattle(scenario);

            var hud = new BattleHudView();
            hud.TakeNavigationInput(session.Battle);
            hud.Presenter.Open(BattleHudPage.Magic);
            int skill = session.Battle.Definitions.Skills.Values.First(ability => ability.IsMagic &&
                ability.Range == BattleTargetRange.OpponentAll && hud.Presenter.Reason(BattleAction.Skill, ability.Id) == null).Id;
            Assert.That(hud.Presenter.ChooseAbility(skill), Is.True);
            hud.PreparePointers(session.Battle, session.Data, session.Preview, .1);

            Transform root = session.Preview.View.Root.transform;
            Assert.That(root.Find("Battle selection pointer " + defeated), Is.Null);
            Transform marker = root.Find("Battle selection pointer " + survivor.InstanceId);
            Assert.That(marker, Is.Not.Null);
            Assert.That(marker.GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled), Is.True);
            Assert.That(hud.Presenter.Target, Is.EqualTo(survivor.InstanceId));
            Assert.That(hud.Presenter.SelectTarget(defeated), Is.False);
            Assert.That(hud.Presenter.SelectTarget(survivor.InstanceId), Is.True);
        }

        [Test]
        public void ObbPickingAndOriginalCvdPointersWorkAcrossAllFourAspectsWithoutAWindow()
        {
            using var session = Create(false, out string enemy);
            var hud = new BattleHudView();
            hud.TakeNavigationInput(session.Battle);
            Assert.That(hud.Presenter.ChooseAction(BattleAction.Attack), Is.True);
            int inputs = session.Battle.Inputs.Count;
            foreach (Vector2Int size in new[] { new Vector2Int(800, 600), new Vector2Int(1050, 450), new Vector2Int(1000, 500), new Vector2Int(700, 700) })
            {
                Rect viewport = new(0, 0, size.x, size.y);
                session.Preview.View.SetViewportContext(viewport);
                Assert.That(session.Preview.TryActorSelectionPoint(enemy, viewport, out Vector2 center), Is.True);
                Assert.That(session.Preview.PickActor(center, viewport, hud.Presenter.Targets.Select(target => target.Id)), Is.EqualTo(enemy));
                Assert.That(session.Preview.PickActor(new Vector2(-1, center.y), viewport, new[] { enemy }), Is.Null);
                hud.PreparePointers(session.Battle, session.Data, session.Preview, .1);
                var marker = session.Preview.View.Root.transform.Find("Battle selection pointer " + enemy);
                Assert.That(marker, Is.Not.Null);
                Assert.That(marker.GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled), Is.True);
                // Bind only the input adapter's world-picking dependencies.
                // This exercises real SendPointer/PreparePointers transitions
                // without claiming to execute the IMGUI Draw/window lifecycle.
                const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(BattleHudView).GetField("_preview", fields).SetValue(hud, session.Preview);
                typeof(BattleHudView).GetField("_viewport", fields).SetValue(hud, new BattleHudViewport(viewport, default));
                typeof(BattleHudView).GetField("_inputEnabled", fields).SetValue(hud, true);
                Assert.That(hud.SendPointer(BattleHudIntentKind.PointerMove, 0, center), Is.True);
                hud.PreparePointers(session.Battle, session.Data, session.Preview, .1);
                Assert.That(marker.GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled), Is.True, "Mouse hover must preserve the navigation marker.");
                hud.SendPointer(BattleHudIntentKind.PointerMove, 0, new Vector2(-1, center.y));
                hud.PreparePointers(session.Battle, session.Data, session.Preview, .1);
                Assert.That(marker.GetComponentsInChildren<Renderer>().All(renderer => !renderer.enabled), Is.True, "Leaving the body must hide the marker.");
                Assert.That(hud.SendPointer(BattleHudIntentKind.PointerMove, 0, center), Is.True);
                hud.PreparePointers(session.Battle, session.Data, session.Preview, .1);
                Assert.That(marker.GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled), Is.True, "Returning to the body must reuse and show the cached marker.");
                Pixels(session.Preview.View.Render(size.x, size.y), "marker-" + size.x + "x" + size.y);
                hud.TakeNavigationInput();
            }
            Assert.That(session.Battle.Inputs.Count, Is.EqualTo(inputs));
        }

        [TestCase(-1, false)]
        [TestCase(1, false)]
        [TestCase(3, true)]
        [TestCase(4, true)]
        public void ProtectionMenuChecksThePresenceOfNativeCompanions(int role, bool alive)
        {
            using var session = Create(false, out _);
            BattleScenario scenario = session.CapturedScenario;
            scenario.Participants[0].Slot = BattleSlot.Center;
            bool pal3A = BattleData.Variant == "PAL3A";
            if (role != -1)
            {
                int actorId = pal3A ? 101 + role : new[] { 101, 104, 106, 107, 102 }[role];
                BattleParticipant companion = session.Data.CreateParticipant(actorId, BattleSide.Ally, BattleSlot.Fire);
                if (!alive)
                    companion.Vitals[0] = 0;
                scenario.Participants.Add(companion);
            }
            session.StartBattle(scenario);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session.Battle);
            bool offered = role == 1 || role == 3 && !pal3A;
            Assert.That(presenter.FocusItems().Single(control => control.Id == "protection").Enabled, Is.EqualTo(offered));
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "protection")), Is.EqualTo(offered));
            if (offered)
                Assert.That(presenter.FocusItems().Any(control => control.Id.StartsWith("protect:", StringComparison.Ordinal)), Is.EqualTo(alive));
        }

#if PAL3A
        [TestCase(101)]
        [TestCase(103)]
        public void TransformedCommandWheelPreventsForbiddenMenusAndAiGaugeCosts(int playerId)
        {
            using var session = Create(false, out _);
            BattleScenario scenario = session.CapturedScenario;
            scenario.Participants[0] = session.Data.CreateTestParticipant(playerId, BattleSide.Ally, BattleSlot.Water);
            bool fireBeast = playerId == 101;
            scenario.Pal3A.NangongStartsTransformed = fireBeast;
            scenario.Pal3A.PengxuStartsTransformed = !fireBeast;
            session.StartBattle(scenario);
            BattleSession battle = session.Battle;
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(battle);
            string actor = presenter.Controller;
            Assert.That(battle.IsPal3ATransformed(actor), Is.True);
            var controls = presenter.FocusItems().ToDictionary(control => control.Id);
            Assert.That(controls["attack"].Enabled, Is.EqualTo(fireBeast));
            Assert.That(controls["magic"].Enabled, Is.EqualTo(!fireBeast));
            Assert.That(controls["defend"].Enabled, Is.True);
            int inputs = battle.RecordedInputCount;
            int profile = battle.GetAiProfile(actor);
            BattleActorSnapshot before = battle.Actors.Single(player => player.Id == actor);
            foreach (string id in new[] { "skills", "items", "ai" })
            {
                Assert.That(controls[id].Enabled, Is.False, id);
                Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, id)), Is.False);
                Assert.That(presenter.Page, Is.EqualTo(BattleHudPage.Commands));
            }
            Assert.That(battle.CommitAiControl(actor, BattleHudPresenter.ToggleAiFlag(profile, 5)), Is.False);
            Assert.That(battle.GetAiProfile(actor), Is.EqualTo(profile));
            Assert.That(battle.RecordedInputCount, Is.EqualTo(inputs));
            BattleActorSnapshot after = battle.Actors.Single(player => player.Id == actor);
            Assert.That((after.Gauge, after.CastGauge), Is.EqualTo((before.Gauge, before.CastGauge)));
            Assert.That(controls["flee"].Enabled, Is.True);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "flee")), Is.True);
            Assert.That(battle.Inputs.Last(input => input.Kind == BattleInputKind.Command).Action, Is.EqualTo(BattleAction.Flee));
        }
#endif

        [TestCase(false)]
        [TestCase(true)]
        public void OnlyACompletedTapOnEmptyGroundDiscardsTheAiDraft(bool touch)
        {
            using var session = Create(false, out string enemy);
            var viewport = new Rect(0, 0, 800, 600);
            session.Preview.View.SetViewportContext(viewport);
            BattleHudView hud = BoundHud(session, touch, viewport);
            BattleSession battle = session.Battle;
            string controller = battle.WaitingActor;
            BattleActorSnapshot before = battle.Actors.Single(actor => actor.Id == controller);
            int inputs = battle.RecordedInputCount;
            int profile = battle.GetAiProfile(controller);
            Assert.That(hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Confirm, "ai")), Is.True);
            Assert.That(hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Confirm, "flag:5")), Is.True);
            int draft = hud.Presenter.AiFlags(controller);
            Assert.That(draft, Is.Not.EqualTo(profile));

            void Tap(Vector2 point)
            {
                hud.SendPointer(BattleHudIntentKind.PointerDown, 1, point);
                hud.SendPointer(BattleHudIntentKind.PointerUp, 1, point);
            }

            // This point is inside the native AI panel, above its buttons,
            // in both desktop and enlarged touch layout at 800 x 600.
            Tap(new Vector2(80, 325));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Ai));
            Assert.That(session.Preview.TryActorSelectionPoint(enemy, viewport, out Vector2 target), Is.True);
            Tap(target);
            if (touch)
                Tap(target);
            Assert.That(hud.Presenter.AiActor, Is.EqualTo(enemy));

            Vector2 ground = new(5, 5);
            hud.SendPointer(BattleHudIntentKind.PointerDown, 1, ground);
            hud.SendPointer(BattleHudIntentKind.PointerMove, 1, ground + Vector2.right * 24);
            hud.SendPointer(BattleHudIntentKind.PointerUp, 1, ground);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Ai));
            hud.SendPointer(BattleHudIntentKind.PointerDown, 1, ground);
            hud.SendPointer(BattleHudIntentKind.PointerUp, 1, ground + Vector2.right * 24);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Ai));
            Assert.That(hud.Presenter.AiFlags(controller), Is.EqualTo(draft));

            hud.SendPointer(BattleHudIntentKind.PointerDown, 1, ground);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Ai));
            hud.SendPointer(BattleHudIntentKind.PointerUp, 1, ground);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Commands));
            Assert.That(hud.Presenter.AiFlags(controller), Is.EqualTo(profile));
            Assert.That(battle.GetAiProfile(controller), Is.EqualTo(profile));
            Assert.That(battle.WaitingActor, Is.EqualTo(controller));
            Assert.That(battle.RecordedInputCount, Is.EqualTo(inputs));
            BattleActorSnapshot after = battle.Actors.Single(actor => actor.Id == controller);
            Assert.That((after.Gauge, after.CastGauge), Is.EqualTo((before.Gauge, before.CastGauge)));
        }

        [Test]
        public void TouchReleaseBeyondTheTapThresholdOverTheSameTargetDoesNotSubmit()
        {
            using var session = Create(false, out string enemy);
            var viewport = new Rect(0, 0, 800, 600);
            session.Preview.View.SetViewportContext(viewport);
            BattleHudView hud = TargetHud(session, true, enemy, viewport);
            Assert.That(session.Preview.TryActorSelectionPoint(enemy, viewport, out Vector2 start), Is.True);
            Vector2 end = start + Vector2.right * 12;
            BattleActorSnapshot before = session.Battle.Actors.Single(actor => actor.Id == hud.Presenter.Controller);
            int inputs = session.Battle.RecordedInputCount;

            hud.SendPointer(BattleHudIntentKind.PointerDown, 9, start);
            hud.SendPointer(BattleHudIntentKind.PointerUp, 9, end);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Targets));
            Assert.That(session.Battle.RecordedInputCount, Is.EqualTo(inputs));
            BattleActorSnapshot after = session.Battle.Actors.Single(actor => actor.Id == before.Id);
            Assert.That((after.Gauge, after.CastGauge), Is.EqualTo((before.Gauge, before.CastGauge)));

            hud.SendPointer(BattleHudIntentKind.PointerDown, 9, end);
            hud.SendPointer(BattleHudIntentKind.PointerUp, 9, end);
            Assert.That(session.Battle.Inputs.Single(input => input.Kind == BattleInputKind.Command).Target, Is.EqualTo(enemy));
        }
    }
}
