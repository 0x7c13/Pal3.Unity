// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using Battle.Presentation;
    using NUnit.Framework;
    using Pal3.Tests.EditorSupport;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;

    [Category("LocalGameData")]
    [Category("OffscreenGraphics")]
    public sealed class BattleHudSpriteRoutingTests
    {
        private static readonly Vector4 Insets = new(13, 9, 17, 11);
        private static readonly Rect[] Areas = { new(30, 40, 1050, 450), new(30, 40, 800, 600), new(30, 40, 1000, 500), new(30, 40, 600, 600) };

        private sealed class Canvas : IDisposable
        {
            public static readonly Vector2 Size = new(1200, 780);
            private readonly OffscreenGui _gui;

            public readonly BattleEditorSession Editor;
            public readonly BattleHudView Hud = new();
            public Rect Area;
            public bool Touch;
            public int ProbeCore = -1;
            public int Repaints;

            public Canvas(BattleEditorSession editor)
            {
                Editor = editor;
                _gui = new OffscreenGui(Draw);
            }

            public RenderTexture RenderTarget => _gui.RenderTarget;

            public void Paint()
            {
                int before = Repaints;
                _gui.Paint(Size);
                Assert.That(Repaints, Is.GreaterThan(before), "The production Draw/RegisterControl path must repaint.");
            }

            public void Dispose() => _gui.Dispose();

            private void Draw()
            {
                if (Editor?.Battle == null)
                    return;
                Editor.Preview.View.SetViewportContext(Area);
                Hud.Draw(Editor.Battle, Editor.Data, Editor.Preview, Area, () => { }, _ => { }, Insets,
                    interaction: new BattleHudInteractionProfile(Touch));
                if (ProbeCore >= 0)
                {
                    // Keep production Draw/RegisterControl, then isolate one
                    // asymmetric enabled sprite for a GPU-only pixel oracle.
                    EditorGUI.DrawRect(Area, Color.black);
                    int[] ids = { 264, 265, 267, 266, 268 };
                    var control = Controls(this).LastOrDefault(control => control.Id == "wheel:" + ids[ProbeCore]);
                    if (control.Id != null)
                    {
                        var viewport = new BattleHudViewport(Area, Insets);
                        int ui = ProbeCore switch
                        {
                            2 => 3,
                            3 => 2,
                            _ => ProbeCore
                        };
                        Texture2D sprite = Editor.Preview.View.GetTexture("basedata.cpk\\ui\\combat\\control\\wt" + (4 - ui) + "0.tga");
                        Rect drawn = new(viewport.ToViewport(control.Rect.position), control.Rect.size * viewport.Scale);
                        BattleSpiritRect native = BattlePreview.SpiritWheelTarget(ProbeCore);
                        GUI.DrawTextureWithTexCoords(drawn, sprite, BattleHudCanvas.SpriteUv(sprite,
                            new Vector2(native.Right - native.Left, native.Bottom - native.Top)), true);
                    }
                }
                if (Event.current.type == EventType.Repaint)
                    Repaints++;
            }
        }

        private readonly struct SpriteOracle
        {
            public readonly Color32[] Pixels;
            public readonly int Width;
            public readonly Vector2 Size;

            public SpriteOracle(Texture2D texture, Vector2 size)
            {
                Pixels = texture.GetPixels32();
                Width = texture.width;
                Size = new Vector2(Math.Min(size.x, texture.width), Math.Min(size.y, texture.height));
            }

            public bool Contains(Rect rect, Vector2 point)
            {
                if (!rect.Contains(point))
                    return false;
                int x = (int)((point.x - rect.x) * Size.x / rect.width);
                int y = (int)Size.y - 1 - (int)((point.y - rect.y) * Size.y / rect.height);
                return Pixels[y * Width + x].a >= 6;
            }
        }

        private static BattleScenario Scenario(BattleEditorSession editor)
        {
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
#if PAL3
            scenario.Participants[0].Equipment[0] = BattleMagicSwordRules.WeaponId;
            scenario.Participants[0].MagicSword = new BattleMagicSword { Energy = 200, SkillId = 2201 };
#endif
            return scenario;
        }

        private static string DataRoot()
        {
            string configured = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            string root = configured ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root) && configured == null)
                Assert.Ignore("Original local data unavailable.");
            return root;
        }

        private static List<(string Id, Rect Rect)> Controls(Canvas canvas) =>
            (List<(string Id, Rect Rect)>)typeof(BattleHudView).GetField("_hitControls", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(canvas.Hud);

        private static void AssertGpuVisiblePixelsRouteToTheIsolatedWheel(Canvas canvas, string expected)
        {
            RenderTexture buffer = canvas.RenderTarget;
            Assert.That(buffer, Is.Not.Null);
            Vector2 pixelsPerPoint = new(buffer.width / Canvas.Size.x, buffer.height / Canvas.Size.y);
            var viewport = new BattleHudViewport(canvas.Area, Insets);
            Rect control = Controls(canvas).Single(control => control.Id == expected).Rect;
            Vector2 origin = viewport.ToViewport(control.position);
            var drawn = new Rect(Vector2.Scale(origin, pixelsPerPoint), Vector2.Scale(control.size * viewport.Scale, pixelsPerPoint));
            var texture = new Texture2D(buffer.width, buffer.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = buffer;
                texture.ReadPixels(new Rect(0, 0, buffer.width, buffer.height), 0, 0);
                texture.Apply();
                Color32[] pixels = texture.GetPixels32();
                bool Visible(int x, int y)
                {
                    if (x < 0 || x >= buffer.width || y < 0 || y >= buffer.height)
                        return false;
                    int row = buffer.height - 1 - y;
                    Color32 pixel = pixels[row * buffer.width + x];
                    return Math.Max(pixel.r, Math.Max(pixel.g, pixel.b)) > 30;
                }
                int checkedPixels = 0;
                for (int y = Math.Max(1, Mathf.CeilToInt(drawn.y)); y < Math.Min(buffer.height - 1, drawn.yMax); y++)
                    for (int x = Math.Max(1, Mathf.CeilToInt(drawn.x)); x < Math.Min(buffer.width - 1, drawn.xMax); x++)
                    {
                        // Erode one physical pixel to avoid bilinear alpha
                        // fringes. Expected hits come only from the GPU image.
                        if (!Visible(x, y) || !Visible(x - 1, y) || !Visible(x + 1, y) || !Visible(x, y - 1) || !Visible(x, y + 1))
                            continue;
                        Vector2 point = new Vector2((x + .5f) / pixelsPerPoint.x, (y + .5f) / pixelsPerPoint.y);
                        Assert.That(canvas.Hud.SendPointer(BattleHudIntentKind.PointerMove, 84, point), Is.True,
                            $"GPU-visible {expected}, aspect={canvas.Area.size}, touch={canvas.Touch}, point={point}");
                        Assert.That(canvas.Hud.Presenter.FocusedId, Is.EqualTo(expected), $"GPU-visible point={point}");
                        checkedPixels++;
                    }
                Assert.That(checkedPixels, Is.GreaterThan(20), "The offscreen buffer must expose the asymmetric wheel sprite.");
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Dictionary<string, SpriteOracle> Sprites(Canvas canvas)
        {
            var sprites = new Dictionary<string, SpriteOracle>(StringComparer.Ordinal);
            void Add(string id, string image, Vector2 size) => sprites.Add(id, new SpriteOracle(canvas.Editor.Preview.View.GetTexture("basedata.cpk\\ui\\combat\\control\\" + image + ".tga"), size));
            foreach ((string id, string image) in new[] { ("attack", "attack0"), ("skills", "skill0"), ("magic", "magic0"), ("items", "item0"), ("defend", "defence0"), ("flee", "flee0"), ("formation", "lineup0"), ("ai", "ai0"), ("protection", "protect0") })
                Add(id, image, new Vector2(46, 46));
#if PAL3
            Add("sword", "mj0", new Vector2(84, 84));
#else
            int[] ids = { 264, 265, 267, 266, 268 };
            for (int core = 0; core < 5; core++)
            {
                int ui = core switch
                {
                    2 => 3,
                    3 => 2,
                    _ => core
                };
                int tier = canvas.Editor.Battle.Pal3AState.SpiritWheel.Charges[core] / 3;
                string image = (tier < 3 ? "unwt" : "wt") + (4 - ui) + (tier < 3 ? tier : 0);
                BattleSpiritRect rect = BattlePreview.SpiritWheelTarget(core);
                Add("wheel:" + ids[core], image, new Vector2(rect.Right - rect.Left, rect.Bottom - rect.Top));
            }
            Add("transform", "bs0", new Vector2(45, 37));
#endif
            return sprites;
        }

        // The oracle independently samples the resource pixels in native draw
        // order. It does not read the production mask or its hit-test helper.
        private static Dictionary<string, Vector2> AssertPixelRouting(Canvas canvas, params string[] required)
        {
            var viewport = new BattleHudViewport(canvas.Area, Insets);
            var enabled = new HashSet<string>(canvas.Hud.Presenter.FocusItems().Where(item => item.Enabled).Select(item => item.Id));
            var sprites = Sprites(canvas);
            var controls = Controls(canvas).Where(control => enabled.Contains(control.Id) && sprites.ContainsKey(control.Id)).ToArray();
            var exposed = new Dictionary<string, Vector2>();
            int checkedPixels = 0;
            foreach (var control in controls.Where(control => required.Contains(control.Id)))
            {
                SpriteOracle sprite = sprites[control.Id];
                for (int y = 0; y < sprite.Size.y; y += 3)
                    for (int x = 0; x < sprite.Size.x; x += 3)
                    {
                        Vector2 point = control.Rect.position + new Vector2((x + .5f) * control.Rect.width / sprite.Size.x, (y + .5f) * control.Rect.height / sprite.Size.y);
                        var top = controls.LastOrDefault(candidate => sprites[candidate.Id].Contains(candidate.Rect, point));
                        if (top.Id == null)
                            continue;
                        Assert.That(canvas.Hud.SendPointer(BattleHudIntentKind.PointerMove, 83, viewport.ToViewport(point)), Is.True, top.Id);
                        Assert.That(canvas.Hud.Presenter.FocusedId, Is.EqualTo(top.Id), $"{canvas.Area.size}, touch={canvas.Touch}, pixel={point}");
                        exposed[top.Id] = viewport.ToViewport(point);
                        checkedPixels++;
                    }
            }
            Assert.That(checkedPixels, Is.GreaterThan(20));
            foreach (string id in required)
            {
                Assert.That(exposed.ContainsKey(id), Is.True, id + " needs its own visible, clickable sprite pixels.");
                Assert.That(canvas.Hud.SendPointer(BattleHudIntentKind.PointerDown, 83, exposed[id]), Is.True, id);
                Assert.That(canvas.Hud.Presenter.FocusedId, Is.EqualTo(id));
                Assert.That(canvas.Hud.SendPointer(BattleHudIntentKind.PointerCancel, 83, exposed[id]), Is.True);
            }
            return exposed;
        }

        [Test]
        public void OverlappingNativeSpritesRouteVisiblePixelsAcrossFourAspectsAndBothInteractionProfiles()
        {
            using var editor = new BattleEditorSession();
            editor.Load(DataRoot(), 936);
            BattleScenario scenario = Scenario(editor);
            using var canvas = new Canvas(editor);
            foreach (bool touch in new[] { false, true })
                foreach (Rect area in Areas)
                {
                    canvas.Touch = touch;
                    canvas.Area = area;
#if PAL3
                    editor.StartBattle(scenario);
                    editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
                    canvas.Paint();
                    var points = AssertPixelRouting(canvas, "sword", "formation", "items");
                    int before = editor.Battle.Inputs.Count;
                    Assert.That(canvas.Hud.SendPointer(BattleHudIntentKind.PointerDown, 83, points["formation"]), Is.True);
                    Assert.That(canvas.Hud.SendPointer(BattleHudIntentKind.PointerUp, 83, points["formation"]), Is.True);
                    Assert.That(canvas.Hud.Presenter.Page, Is.EqualTo(BattleHudPage.Formation));
                    Assert.That(editor.Battle.Inputs.Skip(before).Any(input => input.Kind == BattleInputKind.Command && input.AbilityId == 2201), Is.False, "Opening formation must never fire the charged sword.");
#else
                    foreach (int fullElement in Enumerable.Range(-1, 6))
                    {
                        canvas.ProbeCore = fullElement;
                        scenario.Pal3A.SpiritWheel.Charges = Enumerable.Range(0, 5).Select(index => fullElement < 0 || index == fullElement ? 9 : 0).ToArray();
                        editor.StartBattle(scenario);
                        editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
                        canvas.Paint();
                        int[] ids = { 264, 265, 267, 266, 268 };
                        string[] required = fullElement < 0 ? ids.Select(id => "wheel:" + id).Append("transform").ToArray() : new[] { "wheel:" + ids[fullElement] };
                        var points = AssertPixelRouting(canvas, required);
                        if (fullElement >= 0)
                            AssertGpuVisiblePixelsRouteToTheIsolatedWheel(canvas, required[0]);
                        string chosen = fullElement < 0 ? "transform" : required[0];
                        int before = editor.Battle.Inputs.Count;
                        Assert.That(canvas.Hud.SendPointer(BattleHudIntentKind.PointerDown, 83, points[chosen]), Is.True);
                        Assert.That(canvas.Hud.SendPointer(BattleHudIntentKind.PointerUp, 83, points[chosen]), Is.True);
                        Assert.That(editor.Battle.Inputs.Count, Is.EqualTo(before + 1));
                        Assert.That(editor.Battle.Inputs.Last().Kind, Is.EqualTo(fullElement < 0 ? BattleInputKind.Command : BattleInputKind.ElementWheel));
                        if (fullElement < 0)
                            Assert.That(editor.Battle.Inputs.Last().Action, Is.EqualTo(BattleAction.Transform));
                        else
                            Assert.That(editor.Battle.Inputs.Last().AbilityId, Is.EqualTo(int.Parse(chosen[6..])));
                    }
#endif
                }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
