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
    using Battle;
    using NUnit.Framework;
    using Pal3.Tests.EditorSupport;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

#if PAL3A
    [Category("LocalGameData")]
    [Category("OffscreenGraphics")]
    public sealed class BattleSceneFieldRenderingTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private sealed class Canvas : IDisposable
        {
            public static readonly Vector2 Size = new(1180, 800);
            private readonly OffscreenGui _gui;

            public readonly BattleEditorSession Editor;
            public readonly BattleHudView Hud = new();
            public Rect Area = new(20, 40, 800, 600);
            public Texture2D Reference;
            public Rect ReferenceArea;

            public Canvas(BattleEditorSession editor)
            {
                Editor = editor;
                _gui = new OffscreenGui(Draw);
            }

            public RenderTexture RenderTarget => _gui.RenderTarget;

            public void Paint() => _gui.Paint(Size);

            public void Dispose() => _gui.Dispose();

            private void Draw()
            {
                if (Editor?.Battle == null)
                    return;
                EditorGUI.DrawRect(Area, Color.black);
                Hud.Draw(Editor.Battle, Editor.Data, Editor.Preview, Area, () => { }, _ => { });
                if (Reference != null)
                    using (new BattleHudCanvas(new BattleHudViewport(Area, default)))
                        GUI.DrawTextureWithTexCoords(ReferenceArea, Reference,
                            new Rect(0, 0, 45f / Reference.width, 60f / Reference.height), true);
            }
        }

        private static object Actor(BattleSession battle, BattleSide side) =>
            ((IDictionary)typeof(BattleSession).GetField("_byId", Flags).GetValue(battle))[
                battle.Actors.First(actor => actor.Side == side).Id];

        private static Color32[] Region(Canvas canvas, Rect native) => Regions(canvas, native)[0];

        private static Color32[][] Regions(Canvas canvas, params Rect[] regions)
        {
            RenderTexture buffer = canvas.RenderTarget;
            Assert.That(buffer, Is.Not.Null);
            var viewport = new BattleHudViewport(canvas.Area, default);
            Vector2 pixelsPerPoint = new(buffer.width / Canvas.Size.x, buffer.height / Canvas.Size.y);
            var image = new Texture2D(buffer.width, buffer.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = buffer;
                image.ReadPixels(new Rect(0, 0, buffer.width, buffer.height), 0, 0);
                image.Apply();
                Color32[] pixels = image.GetPixels32();
                string evidenceRoot = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT");
                if (regions.Length > 1 && !string.IsNullOrEmpty(evidenceRoot))
                {
                    string folder = Path.Combine(evidenceRoot, "SceneField");
                    Directory.CreateDirectory(folder);
                    string name = $"{canvas.Area.width}x{canvas.Area.height}-{canvas.Editor.Battle.FiveSpiritForce}-{canvas.Editor.Battle.FiveSpiritElement}";
                    File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
                    string pixelsAt = string.Join("; ", regions.Select(rect => Vector2.Scale(viewport.ToViewport(rect.position), pixelsPerPoint).ToString("F3")));
                    File.WriteAllText(Path.Combine(folder, name + ".txt"), $"Area={canvas.Area}; buffer={buffer.width}x{buffer.height}; pixelsPerPoint={pixelsPerPoint}; pixel origins={pixelsAt}; regions={string.Join("; ", regions.Select(rect => rect.ToString()))}");
                }
                return regions.Select(native => Enumerable.Range(0, 45 * 60).Select(index =>
                {
                    Vector2 sample = native.position + new Vector2(index % 45 + .5f, index / 45 + .5f);
                    Vector2 point = Vector2.Scale(viewport.ToViewport(sample), pixelsPerPoint);
                    int x = Mathf.Clamp((int)point.x, 0, buffer.width - 1);
                    int y = Mathf.Clamp((int)point.y, 0, buffer.height - 1);
                    return pixels[(buffer.height - 1 - y) * buffer.width + x];
                }).ToArray()).ToArray();
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }

        [Test]
        public void ActualHudShowsAllTenNativeTemporaryFieldPicturesAndHidesAfterTheFifthAnchorAction()
        {
            string root = Environment.GetEnvironmentVariable("PAL3A_TEST_DATA_ROOT") ??
                Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data/PAL3A"));
            if (!Directory.Exists(root))
                Assert.Ignore("Original local data unavailable.");
            using var editor = new BattleEditorSession();
            editor.Load(root, 936);
            var scenario = new BattleScenario { Variant = BattleData.Variant, Scene = editor.Data.Scenes.First(), DataFingerprint = editor.Data.Fingerprint };
            scenario.Participants.Add(editor.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
            scenario.Participants.Add(editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            string initialScene = editor.Battle.ActiveScene;
            int initialElement = editor.Battle.SceneElement;
            using var canvas = new Canvas(editor);
            foreach (Rect area in new[] { new Rect(20, 40, 800, 600), new Rect(20, 100, 1050, 450) })
            {
                canvas.Area = area;
                canvas.Reference = null;
                canvas.Paint();
                var viewport = new BattleHudViewport(area, default);
                Rect indicator = new(viewport.CanvasSize.x - 56, 81, 45, 60);
                // Keep both Point-filtered images at the same subpixel phase.
                // The tested scales (1 and .75) map this gap to 600/450 points.
                canvas.ReferenceArea = new Rect(indicator.x - 600, indicator.y, 45, 60);
                Color32[] neutral = Region(canvas, indicator);
                foreach (int force in new[] { 1, 2 })
                    foreach (int element in Enumerable.Range(1, 5))
                    {
                        int skill = new[] { 264, 265, 267, 266, 268 }[element - 1];
                        typeof(BattleSession).GetMethod("ResolveAiSceneMagic", Flags).Invoke(editor.Battle,
                            new[] { Actor(editor.Battle, force == 1 ? BattleSide.Ally : BattleSide.Enemy), editor.Battle.Definitions.Skills[skill] });
                        Assert.That((editor.Battle.FiveSpiritElement, editor.Battle.FiveSpiritForce), Is.EqualTo((element, force)));
                        int resource = new[] { 0, 1, 3, 2, 4 }[element - 1];
                        canvas.Reference = editor.Preview.View.GetTexture($"basedata.cpk\\ui\\combat\\control\\{(force == 1 ? "w" : "e")}_sx{resource}.tga");
                        Assert.That((canvas.Reference.width, canvas.Reference.height), Is.EqualTo((64, 128)));
                        FilterMode filter = canvas.Reference.filterMode;
                        canvas.Reference.filterMode = FilterMode.Point;
                        canvas.Paint();
                        Color32[][] regions = Regions(canvas, indicator, canvas.ReferenceArea);
                        Color32[] shown = regions[0], reference = regions[1];
                        Color32[] source = canvas.Reference.GetPixels32();
                        int samples = 0, matches = 0;
                        for (int y = 1; y < 59; y++)
                            for (int x = 1; x < 44; x++)
                            {
                                if (source[(59 - y) * 64 + x].a < 250)
                                    continue;
                                samples++;
                                Color32 a = shown[y * 45 + x], b = reference[y * 45 + x];
                                if (Math.Abs(a.r - b.r) < 16 && Math.Abs(a.g - b.g) < 16 && Math.Abs(a.b - b.b) < 16)
                                    matches++;
                            }
                        canvas.Reference.filterMode = filter;
                        Assert.That(samples, Is.GreaterThan(30));
                        Assert.That(matches / (double)samples, Is.GreaterThan(.9), $"field {element}:{force} at {area} must show its original cropped bitmap at the native right anchor.");
                        for (int action = 0; action < 5; action++)
                            typeof(BattleSession).GetMethod("AdvanceFiveSpiritFieldTurn", Flags).Invoke(editor.Battle, new[] { Actor(editor.Battle, BattleSide.Ally) });
                        Assert.That((editor.Battle.FiveSpiritElement, editor.Battle.FiveSpiritForce), Is.EqualTo((0, 0)));
                        canvas.Reference = null;
                        canvas.Paint();
                        Assert.That(Region(canvas, indicator), Is.EqualTo(neutral), "Expiry must hide the persistent indicator and restore the underlying HUD.");
                        Assert.That(editor.Battle.ActiveScene, Is.EqualTo(initialScene));
                        Assert.That(editor.Battle.SceneElement, Is.EqualTo(initialElement), "Temporary field UI must not recolor the base scene formation.");
                    }
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
#endif
}
