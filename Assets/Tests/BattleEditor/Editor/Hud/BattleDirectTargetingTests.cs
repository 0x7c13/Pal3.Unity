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
    using NUnit.Framework;
    using Pal3.Tests.EditorSupport;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    public sealed class BattleDirectNavigationTests
    {
        [TestCase(BattleHudDirection.Up, "skills")]
        [TestCase(BattleHudDirection.UpRight, "protection")]
        [TestCase(BattleHudDirection.Right, "defend")]
        [TestCase(BattleHudDirection.DownRight, "flee")]
        [TestCase(BattleHudDirection.Down, "items")]
        [TestCase(BattleHudDirection.DownLeft, "formation")]
        [TestCase(BattleHudDirection.Left, "magic")]
        [TestCase(BattleHudDirection.UpLeft, "ai")]
        public void RingDirectionSelectsItsNativePositionWithoutSubmitting(BattleHudDirection direction, string expected)
        {
            foreach (string variant in new[] { "PAL3", "PAL3A" })
            {
                var battle = BattleHudTests.Session(variant);
                var presenter = new BattleHudPresenter();
                presenter.Synchronize(battle);
                int inputs = battle.Inputs.Count;
                presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Down));
                Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: direction)), Is.True);
                Assert.That(presenter.FocusedId, Is.EqualTo(expected));
                Assert.That(battle.Inputs.Count, Is.EqualTo(inputs));
                presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Primary));
                Assert.That(presenter.FocusedId, Is.EqualTo("attack"));
            }
        }

        [Test]
        public void AiDefaultsToTheCurrentControllerInsteadOfTheFirstActor()
        {
            var battle = BattleHudTests.Session();
            Assert.That(battle.RequestAiNormal(battle.WaitingActor), Is.True);
            for (int step = 0; step < 500 && (battle.WaitingActor == null || battle.WaitingActor == "A"); step++)
                battle.Step();
            Assert.That(battle.WaitingActor, Is.Not.Null.And.Not.EqualTo("A"));
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(battle);
            presenter.Open(BattleHudPage.Ai);
            Assert.That(presenter.FocusedId, Is.EqualTo("flag:0"));
            Assert.That(presenter.AiActor, Is.EqualTo(battle.WaitingActor));
            Assert.That(BattleHudView.PointerActors(battle, presenter), Is.EqualTo(new[] { battle.WaitingActor }));
        }

        [Test]
        public void TargetsUseProjectedDirectionsAndHaveNoHiddenBackControl()
        {
            var battle = BattleHudTests.Session();
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(battle);
            presenter.ChooseAction(BattleAction.Attack);
            presenter.SetFocusLayout(new[] { ("target:E", 100f, 100f), ("target:E2", 200f, 90f), ("target:E3", 150f, 200f) });
            Assert.That(presenter.FocusItems().Select(item => item.Id), Does.Not.Contain("back"));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Right));
            Assert.That(presenter.Target, Is.EqualTo("E2"));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Down));
            Assert.That(presenter.Target, Is.EqualTo("E3"));
            Assert.That(battle.Inputs.Count(input => input.Kind == BattleInputKind.Command), Is.Zero);
        }
    }

    [Category("LocalGameData")]
    [Category("OffscreenGraphics")]
    public sealed class BattleDirectTargetingTests
    {
        private BattleEditorSession _editor;
        private SelectionCanvas _canvas;
        private string _enemy;

        private sealed class SelectionCanvas : IDisposable
        {
            private static readonly Vector2 Size = new(1260, 850);
            private readonly OffscreenGui _gui;
            private readonly BattleEditorSession _editor;

            public readonly BattleHudView Hud = new();
            public Rect Area = new(20, 30, 800, 600);
            public bool Touch;
            public int Repaints;
            public double Clock;
            public int DrawerClicks;
            public Vector2? Pointer;

            public SelectionCanvas(BattleEditorSession editor)
            {
                _editor = editor;
                _gui = new OffscreenGui(Draw);
            }

            public RenderTexture RenderTarget => _gui.RenderTarget;

            public void Paint()
            {
                int previous = Repaints;
                _gui.Paint(Size);
                Assert.That(Repaints, Is.GreaterThan(previous));
            }

            public void SendEvent(Event input) => _gui.SendEvent(input, Size);

            public void Dispose() => _gui.Dispose();

            private void Draw()
            {
                if (Pointer is { } point)
                    Event.current.mousePosition = point;
                _editor.Preview.View.SetViewportContext(Area);
                if (Event.current.type != EventType.Repaint)
                    DrawHud();
                Hud.PreparePointers(_editor.Battle, _editor.Data, _editor.Preview, Clock);
                _editor.Preview.View.Draw(Area);
                if (Event.current.type == EventType.Repaint)
                {
                    DrawHud();
                    Repaints++;
                }
                if (GUI.Button(new Rect(Area.xMax + 4, Area.y + 24, 180, 24), "Drawer test"))
                    DrawerClicks++;
            }

            private void DrawHud() => Hud.Draw(_editor.Battle, _editor.Data, _editor.Preview, Area,
                () => _editor.SyncBattle(), _ => { }, interaction: new BattleHudInteractionProfile(Touch));
        }

        [SetUp]
        public void SetUp()
        {
            _editor = new BattleEditorSession();
            string configured = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            string root = configured ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root) && configured == null)
                Assert.Ignore("Original local data unavailable.");
            _editor.Load(root, 936);
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = _editor.Data.Scenes.First(),
                DataFingerprint = _editor.Data.Fingerprint,
                Encounter = BattleEncounter.AllyAmbush,
                Music = "NONE"
            };
            scenario.Participants.Add(_editor.Data.CreateTestParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = _editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(_editor.Data, id));
            scenario.Participants.Add(_editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            scenario.Participants.Add(_editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Fire));
            _enemy = scenario.Participants[1].InstanceId;
            _editor.StartBattle(scenario);
            _editor.Preview.Audio.Muted = _editor.Preview.Effects.Muted = true;
            Vector3 center = _editor.Battle.Actors.Select(actor => BattleAnimationCatalog.UnityPosition(actor.Position)).Aggregate(Vector3.zero, (sum, value) => sum + value) / 3;
            _editor.Preview.View.SetCamera(center + new Vector3(9, 9, -14), center + Vector3.up, 50);
            _canvas = new SelectionCanvas(_editor);
            _canvas.Hud.TakeNavigationInput(_editor.Battle);
            Assert.That(_canvas.Hud.Presenter.ChooseAction(BattleAction.Attack), Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            _canvas?.Dispose();
            _editor?.Dispose();
        }

        private Vector2 TargetPoint()
        {
            Assert.That(_editor.Preview.TryActorSelectionPoint(_enemy, _canvas.Area, out Vector2 point), Is.True);
            Assert.That(_canvas.Area.Contains(point), Is.True);
            Assert.That(_editor.Preview.PickActor(point, _canvas.Area, new[] { _enemy }), Is.EqualTo(_enemy));
            return point;
        }

        private int Commands => _editor.Battle.Inputs.Count(input => input.Kind == BattleInputKind.Command);

        [Test]
        public void MouseHoversTheBodyAndClicksOnceAcrossLandscapeAspects()
        {
            foreach (Vector2 size in new[] { new Vector2(800, 600), new Vector2(1050, 450), new Vector2(1000, 500), new Vector2(700, 700) })
            {
                _canvas.Area = new Rect(new Vector2(20, 30), size);
                _canvas.Paint();
                Vector2 point = TargetPoint();
                _canvas.Pointer = point;
                Assert.That(_canvas.Hud.SendPointer(BattleHudIntentKind.PointerMove, 0, point), Is.True);
                Assert.That(_canvas.Hud.Presenter.Target, Is.EqualTo(_enemy));
                _canvas.Clock = .1;
                _canvas.Paint();
                Assert.That(Commands, Is.Zero);
                Transform pointer = _editor.Preview.View.Root.transform.Find("Battle selection pointer " + _enemy);
                Assert.That(pointer, Is.Not.Null);
                Assert.That(pointer.GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled), Is.True);
                Capture("mouse-" + size.x + "x" + size.y);
            }
            Vector2 target = TargetPoint();
            _canvas.Hud.SendPointer(BattleHudIntentKind.PointerDown, 0, target);
            _canvas.Hud.SendPointer(BattleHudIntentKind.PointerUp, 0, target);
            Assert.That(Commands, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TouchTapConfirmsAndIdleMouseCannotOverrideNavigation()
        {
            _canvas.Touch = true;
            _canvas.Paint();
            Vector2 target = TargetPoint();
            Vector2 outside = new(_canvas.Area.xMax + 1, target.y);
            Assert.That(_canvas.Hud.SendPointer(BattleHudIntentKind.PointerDown, 40, outside), Is.False);
            _canvas.Hud.Presenter.Open(BattleHudPage.Magic);
            _canvas.Paint();
            Vector2 background = _canvas.Area.position + new Vector2(620, 170);
            int takeovers = 0;
            void TakePointer() => takeovers++;
            _canvas.Hud.PointerTookControl += TakePointer;
            _canvas.Hud.TakeNavigationInput(_editor.Battle);
            _canvas.Hud.SendPointer(BattleHudIntentKind.PointerDown, 100, background);
            Assert.That(takeovers, Is.EqualTo(1));
            _canvas.Hud.PointerTookControl -= TakePointer;
            Assert.That(_canvas.Hud.SendPointer(BattleHudIntentKind.PointerDown, 101, outside), Is.True);
            Assert.That(_canvas.Hud.SendPointer(BattleHudIntentKind.PointerUp, 101, outside), Is.True);
            _canvas.Hud.SendPointer(BattleHudIntentKind.PointerCancel, 100, background);
            Assert.That(_canvas.Hud.Presenter.ChooseAction(BattleAction.Attack), Is.True);
            _canvas.Paint();
            _canvas.Hud.SendPointer(BattleHudIntentKind.PointerDown, 0, target);
            Vector2 drawer = new(_canvas.Area.xMax + 40, _canvas.Area.y + 36);
            _canvas.Pointer = drawer;
            _canvas.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = drawer });
            _canvas.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = drawer });
            Assert.That(_canvas.DrawerClicks, Is.EqualTo(1));
            _canvas.Hud.SendPointer(BattleHudIntentKind.PointerDown, 6, target);
            _canvas.Hud.SendPointer(BattleHudIntentKind.PointerCancel, 6, target);
            Assert.That(Commands, Is.Zero, "An incomplete or cancelled touch cannot cast.");
            _canvas.Hud.SendPointer(BattleHudIntentKind.PointerDown, 6, target);
            _canvas.Hud.SendPointer(BattleHudIntentKind.PointerDown, 6, target);
            Assert.That(Commands, Is.Zero, "A fresh Down after a missing Up still waits for release.");
            _canvas.Hud.SendPointer(BattleHudIntentKind.PointerCancel, 6, target);
            _canvas.Hud.TakeNavigationInput(_editor.Battle);
            _canvas.Hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Down));
            string navigated = _canvas.Hud.Presenter.Target;
            _canvas.Pointer = target;
            _canvas.Paint();
            Assert.That(_canvas.Hud.Presenter.Target, Is.EqualTo(navigated));
            _canvas.Hud.SendPointer(BattleHudIntentKind.PointerDown, 8, target);
            Assert.That(Commands, Is.Zero);
            _canvas.Hud.SendPointer(BattleHudIntentKind.PointerUp, 8, target);
            Assert.That(Commands, Is.EqualTo(1), "One complete tap on the recipient casts once.");
            LogAssert.NoUnexpectedReceived();
        }

        private void Capture(string name)
        {
            string root = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT");
            if (string.IsNullOrEmpty(root))
                return;
            string folder = Path.Combine(root, "DirectSelection");
            Directory.CreateDirectory(folder);
            RenderTexture buffer = _canvas.RenderTarget;
            var image = new Texture2D(buffer.width, buffer.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = buffer;
                image.ReadPixels(new Rect(0, 0, buffer.width, buffer.height), 0, 0);
                image.Apply();

                File.WriteAllBytes(Path.Combine(folder, BattleData.Variant + (_editor.CapturedScenario.ToonRendering ? "-toon-" : "-native-") + name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }
    }
}
