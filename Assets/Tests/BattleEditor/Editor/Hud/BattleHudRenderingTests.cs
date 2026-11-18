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
    using NUnit.Framework;
    using Pal3.Tests.EditorSupport;
    using ResourceBrowser;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;

    [Category("LocalGameData")]
    [Category("OffscreenGraphics")]
    public sealed class BattleHudRenderingTests
    {
        private sealed class Canvas : IDisposable
        {
            public static readonly Vector2 Size = new(1320, 920);
            private readonly OffscreenGui _gui;

            public readonly BattleEditorSession Editor;
            public readonly BattleHudView View = new();
            public Rect Area;
            public bool Touch;
            public bool Enabled = true, InputFocus = true;
            public int Downs;
            public int Repaints;
            public bool BottomVisible, Restored;
            public bool PixelOracle;
            public Rect Clip;

            public Canvas(BattleEditorSession editor)
            {
                Editor = editor;
                _gui = new OffscreenGui(Draw);
            }

            public void Paint()
            {
                int before = Repaints;
                _gui.Paint(Size);
                Assert.That(Repaints, Is.GreaterThan(before), "The real IMGUI view must repaint.");
            }

            public void SendEvent(Event input) => _gui.SendEvent(input, Size);

            public void Dispose() => _gui.Dispose();

            private void Draw()
            {
                if (Editor?.Battle == null)
                    return;
                if (Event.current.type == EventType.MouseDown)
                    Downs++;
                Editor.Preview.View.SetViewportContext(Area);
                View.SetInputFocus(InputFocus);
                var viewport = new BattleHudViewport(Area, new Vector4(13, 9, 17, 11));
                if (PixelOracle)
                    EditorGUI.DrawRect(Area, new Color(0, .65f, .2f));
                Matrix4x4 matrix = GUI.matrix;
                Color color = GUI.color;
                using (new EditorGUI.DisabledScope(!Enabled))
                    View.Draw(Editor.Battle, Editor.Data, Editor.Preview, Area, () => { }, _ => { }, new Vector4(13, 9, 17, 11), interaction: new BattleHudInteractionProfile(Touch));
                Restored = GUI.matrix == matrix && GUI.color == color;
                using (new BattleHudCanvas(viewport))
                {
                    Type clip = typeof(GUI).Assembly.GetType("UnityEngine.GUIClip");
                    Clip = (Rect)clip.GetProperty("visibleRect", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                    BottomVisible = Clip.Contains(new Vector2(viewport.CanvasSize.x - 5, viewport.CanvasSize.y - 5));
                }
                if (Event.current.type == EventType.Repaint)
                    Repaints++;
            }
        }

        private static string DataRoot()
        {
            string configured = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            string root = configured ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root) && configured == null)
                Assert.Ignore("Original local data unavailable.");
            return root;
        }

        [Test]
        public void ActualHudRestoresGuiStateAndKeepsBottomControlsInsideTheScaledClip()
        {
            using var editor = new BattleEditorSession();
            editor.Load(DataRoot(), 936);
            var scenario = new BattleScenario { Variant = BattleData.Variant, Scene = editor.Data.Scenes.First(), DataFingerprint = editor.Data.Fingerprint, Encounter = BattleEncounter.AllyAmbush };
            scenario.Participants.Add(editor.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            foreach (BattleState state in new[] { BattleState.AttackIncrease, BattleState.DefenseIncrease, BattleState.SpeedIncrease, BattleState.LuckIncrease })
                scenario.Participants[0].Statuses.Add(new BattleStatus { Id = (int)state, Rounds = 9 });
            int enemy = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
            scenario.Participants.Add(editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            using var canvas = new Canvas(editor);
            foreach (bool touch in new[] { false, true })
                foreach (Vector2 size in new[] { new Vector2(600, 450), new Vector2(880, 660) })
                {
                    canvas.Touch = touch;
                    canvas.Area = new Rect(new Vector2(40, 70), size);
                    canvas.Paint();
                    Assert.That(canvas.Restored, Is.True, "HUD rendering must restore the caller's matrix and color.");
                    Assert.That(canvas.BottomVisible, Is.True, "The shared production canvas cut its bottom/right controls: " + canvas.Clip);
                }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AbilityMenusUseOutsideTapsWithoutAnInvisibleBackControl()
        {
            using var editor = new BattleEditorSession();
            editor.Load(DataRoot(), 936);
            var scenario = new BattleScenario { Variant = BattleData.Variant, Scene = editor.Data.Scenes.First(), DataFingerprint = editor.Data.Fingerprint, Encounter = BattleEncounter.AllyAmbush };
            scenario.Participants.Add(editor.Data.CreateTestParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
            scenario.Participants.Add(editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            using var canvas = new Canvas(editor);
            canvas.View.Presenter.Synchronize(editor.Battle);
            foreach (bool touch in new[] { false, true })
                foreach (Rect area in new[] { new Rect(40, 70, 800, 600), new Rect(21, 110, 1050, 450), new Rect(40, 70, 700, 700) })
                    foreach (Battle.Presentation.BattleHudPage page in new[] { Battle.Presentation.BattleHudPage.Skills, Battle.Presentation.BattleHudPage.Magic, Battle.Presentation.BattleHudPage.Items })
                    {
                        canvas.Touch = touch;
                        canvas.Area = area;
                        canvas.View.Presenter.Open(page);
                        canvas.Paint();
                        var controls = (System.Collections.Generic.List<(string Id, Rect Rect)>)typeof(BattleHudView).GetField("_hitControls", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(canvas.View);
                        Assert.That(controls.Any(control => control.Id == "back"), Is.False);
                        Assert.That(canvas.View.Presenter.FocusItems().Any(control => control.Id == "back"), Is.False);
                        var viewport = new BattleHudViewport(area, new Vector4(13, 9, 17, 11));
                        Vector2 outside = viewport.ToViewport(new Vector2(viewport.CanvasSize.x - 20, 150));
                        int inputs = editor.Battle.Inputs.Count;
                        Assert.That(canvas.View.SendPointer(Battle.Presentation.BattleHudIntentKind.PointerDown, 71, outside), Is.True);
                        Assert.That(canvas.View.Presenter.Page, Is.EqualTo(page), "A tap confirms on release.");
                        Assert.That(canvas.View.SendPointer(Battle.Presentation.BattleHudIntentKind.PointerDown, 72, outside), Is.True);
                        Assert.That(canvas.View.SendPointer(Battle.Presentation.BattleHudIntentKind.PointerUp, 72, outside), Is.True);
                        Assert.That(canvas.View.Presenter.Page, Is.EqualTo(page), "A second finger cannot cancel the captured tap.");
                        Assert.That(canvas.View.SendPointer(Battle.Presentation.BattleHudIntentKind.PointerUp, 71, outside), Is.True);
                        Assert.That(canvas.View.Presenter.Page, Is.EqualTo(Battle.Presentation.BattleHudPage.Commands));
                        Assert.That(editor.Battle.Inputs.Count, Is.EqualTo(inputs), "Closing a menu must not issue a parent-page battle command.");
                        canvas.View.Presenter.Open(page);
                        canvas.Paint();
                        canvas.View.SendPointer(Battle.Presentation.BattleHudIntentKind.PointerDown, 73, outside);
                        canvas.View.SendPointer(Battle.Presentation.BattleHudIntentKind.PointerMove, 73, outside + new Vector2(30, 0));
                        canvas.View.SendPointer(Battle.Presentation.BattleHudIntentKind.PointerUp, 73, outside + new Vector2(30, 0));
                        Assert.That(canvas.View.Presenter.Page, Is.EqualTo(page), "An outside drag is not a tap.");
                        canvas.View.SendIntent(new Battle.Presentation.BattleHudIntent(Battle.Presentation.BattleHudIntentKind.Back));
                        Assert.That(canvas.View.Presenter.Page, Is.EqualTo(Battle.Presentation.BattleHudPage.Commands));
                    }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ClippedItemRowsCannotReceivePointerInputUntilScrolledIntoView()
        {
            using var editor = new BattleEditorSession();
            editor.Load(DataRoot(), 936);
            bool pal3A = BattleData.Variant == "PAL3A";
            Rect list = BattleHudText.ListBounds(Battle.Presentation.BattleHudPage.Items, pal3A);
            int hiddenIndex = Mathf.CeilToInt(list.height / 19);
            var scenario = new BattleScenario { Variant = BattleData.Variant, Scene = editor.Data.Scenes.First(), DataFingerprint = editor.Data.Fingerprint, Encounter = BattleEncounter.AllyAmbush };
            scenario.Participants.Add(editor.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
            scenario.Participants.Add(editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            scenario.Items = editor.Data.Database.GameItemInfos.Values.Where(item => item.Type == Core.Contract.Enums.ItemType.Healing && item.Id != 6201)
                .Take(hiddenIndex).Select(item => new BattleItem { Id = checked((int)item.Id) }).ToList();
            Assert.That(scenario.Items.Count, Is.EqualTo(hiddenIndex));
            scenario.Items.Add(new BattleItem { Id = 6201 });
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            using var canvas = new Canvas(editor);
            canvas.Area = new Rect(40, 70, 800, 600);
            canvas.View.Presenter.Synchronize(editor.Battle);
            canvas.View.Presenter.Open(Battle.Presentation.BattleHudPage.Items);
            Assert.That(canvas.View.Presenter.Abilities()[hiddenIndex].Definition.Id, Is.EqualTo(6201));
            Assert.That(canvas.View.Presenter.Abilities()[hiddenIndex].Denied, Is.Null);
            Assert.That(canvas.View.Presenter.SetFocus("category:0"), Is.True);
            canvas.Paint();
            var viewport = new BattleHudViewport(canvas.Area, new Vector4(13, 9, 17, 11));
            Vector2 panel = new(10, pal3A ? 258 : 278);
            Vector2 hiddenRow = panel + list.position + new Vector2(30, hiddenIndex * 19 + 9);
            Vector2 canvasPoint = hiddenRow + new Vector2(0, viewport.CanvasSize.y - 600);
            Vector2 point = viewport.ToViewport(canvasPoint);
            canvas.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = point });
            canvas.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = point });
            Assert.That(canvas.View.Presenter.Page, Is.EqualTo(Battle.Presentation.BattleHudPage.Items), "The clipped row below the list must not activate through a drawing fallback.");
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(BattleHudView).GetField("_listScroll", flags).SetValue(canvas.View, new Vector2(0, hiddenIndex * 19));
            canvas.Paint();
            var controls = (System.Collections.Generic.List<(string Id, Rect Rect)>)typeof(BattleHudView).GetField("_hitControls", flags).GetValue(canvas.View);
            Rect visible = controls.Single(control => control.Id == "ability:6201").Rect;
            point = viewport.ToViewport(visible.center);
            canvas.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = point });
            Assert.That(canvas.View.Presenter.Page, Is.EqualTo(Battle.Presentation.BattleHudPage.Items), "Pointer-down only captures; release confirms the same native control.");
            canvas.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = point });
            Assert.That(canvas.View.Presenter.Page, Is.EqualTo(Battle.Presentation.BattleHudPage.Targets));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void UnfocusedTargetPointerCannotBypassTheViewInputGate() => CheckTargetInputGate(false, true);

        [Test]
        public void DisabledTargetPointerCannotBypassTheViewInputGate() => CheckTargetInputGate(true, false);

        private static void CheckTargetInputGate(bool focus, bool enabled)
        {
            using var editor = new BattleEditorSession();
            editor.Load(DataRoot(), 936);
            var scenario = new BattleScenario { Variant = BattleData.Variant, Scene = editor.Data.Scenes.First(), DataFingerprint = editor.Data.Fingerprint, Encounter = BattleEncounter.AllyAmbush };
            scenario.Participants.Add(editor.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemyId = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
            scenario.Participants.Add(editor.Data.CreateParticipant(enemyId, BattleSide.Enemy, BattleSlot.Water));
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            using var canvas = new Canvas(editor);
            canvas.InputFocus = focus;
            canvas.Enabled = enabled;
            canvas.Area = new Rect(40, 70, 800, 600);
            canvas.View.Presenter.Synchronize(editor.Battle);
            Assert.That(canvas.View.Presenter.ChooseAction(BattleAction.Attack), Is.True);
            canvas.Paint();
            BattleActorSnapshot enemy = editor.Battle.Actors.First(actor => actor.Side == BattleSide.Enemy);
            Assert.That(editor.Preview.View.Project(BattleAnimationCatalog.UnityPosition(enemy.Position) + Vector3.up, canvas.Area, out Vector2 point), Is.True);
            int before = editor.Battle.Inputs.Count;
            canvas.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = point });
            Assert.That(canvas.Downs, Is.GreaterThan(0));
            Assert.That(editor.Battle.Inputs.Count, Is.EqualTo(before), "An unfocused/disabled view must block target input.");
            canvas.Enabled = canvas.InputFocus = true;
            canvas.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = point });
            canvas.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = point });
            Assert.That(editor.Battle.Inputs.Count, Is.EqualTo(before + 1), "The exact same point must submit when enabled, validating the negative test.");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RealAttackPortraitAndProgressBottomPixelsSurviveTheProductionSpriteCrop()
        {
            using var data = new BattleData(DataRoot(), 936);
            using var preview = new ResourceModelPreview(data.Resources, false);
            var layout = new BattleHudLayout(BattleData.Variant);
            foreach ((string resource, int width, int height) in new[]
            {
                ("control/attack0", 46, 46), (layout.StateFace(0), 64, layout.IsPal3A ? 93 : 82), ("state/ProgBar_HP", 85, 12)
            })
            {
                Texture2D original = preview.GetTexture("basedata.cpk\\ui\\combat\\" + resource.Replace('/', '\\') + ".tga");
                Color32[] source = original.GetPixels32();
                var opaque = Enumerable.Range(0, source.Length).Where(index => source[index].a > 200).ToArray();
                Assert.That(opaque.Length, Is.GreaterThan(8), resource);
                int bottom = opaque.Min(index => index / original.width);
                int top = opaque.Max(index => index / original.width);
                int band = bottom + Math.Max(1, (top - bottom + 1) / 4);
                // The oracle comes from the complete original bitmap's opaque
                // ink bounds, independently of the production UV calculation.
                int[] samples = opaque.Where(index => index % original.width < width && index / original.width < height && index / original.width < band).ToArray();
                Assert.That(samples.Length, Is.GreaterThan(8), resource);
                Rect uv = BattleHudCanvas.SpriteUv(original, new Vector2(width, height));
                var target = new RenderTexture(width * 2, height * 2, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                var pixels = new Texture2D(width * 2, height * 2, TextureFormat.RGBA32, false);
                RenderTexture previous = RenderTexture.active;
                FilterMode filter = original.filterMode;
                try
                {
                    target.Create();
                    original.filterMode = FilterMode.Point;
                    Graphics.Blit(original, target, uv.size, uv.position);
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    pixels.Apply();
                    Color32[] actual = pixels.GetPixels32();
                    int matched = samples.Count(index =>
                    {
                        int x = index % original.width, y = index / original.width;
                        Color32 expected = source[index], found = actual[(y * 2 + 1) * target.width + x * 2 + 1];
                        return found.a > 200 && Math.Abs(found.r - expected.r) < 48 && Math.Abs(found.g - expected.g) < 48 && Math.Abs(found.b - expected.b) < 48;
                    });
                    Assert.That(matched / (double)samples.Length, Is.GreaterThan(.9), resource + " lost the native bitmap's bottom pixels.");
                }
                finally
                {
                    original.filterMode = filter;
                    RenderTexture.active = previous;
                    UnityEngine.Object.DestroyImmediate(target);
                    UnityEngine.Object.DestroyImmediate(pixels);
                }
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
