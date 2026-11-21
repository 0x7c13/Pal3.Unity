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
    using NUnit.Framework;
    using Pal3.Tests.EditorSupport;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;

    [Category("LocalGameData")]
    [Category("OffscreenGraphics")]
    public sealed class BattleStageMessageRenderingTests
    {
        private sealed class Canvas : IDisposable
        {
            public static readonly Vector2 Size = new(1320, 940);
            private readonly OffscreenGui _gui;

            public readonly BattleEditorSession Editor;
            public Rect Area;
            public int Repaints;
            public bool Restored;

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
                Assert.That(Repaints, Is.GreaterThan(before));
            }

            public void Dispose() => _gui.Dispose();

            private void Draw()
            {
                if (Editor?.Battle == null)
                    return;
                EditorGUI.DrawRect(Area, Color.black);
                Color color = GUI.color;
                Matrix4x4 matrix = GUI.matrix;
                Editor.Preview.DrawBattleMessages(Editor.Battle, Area, new Vector4(13, 9, 17, 11));
                Restored = color == GUI.color && matrix == GUI.matrix;
                if (Event.current.type == EventType.Repaint)
                    Repaints++;
            }
        }

        [Test]
        public void OriginalTitleTextureAndFontReachAllFourViewportBuffers()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT") ??
                Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root))
                Assert.Ignore("Original local data unavailable.");
            using var editor = new BattleEditorSession();
            editor.Load(root, 936);
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = editor.Data.Scenes.First(),
                DataFingerprint = editor.Data.Fingerprint,
                Encounter = BattleEncounter.AllyAmbush,
                MotionBlur = false
            };
            scenario.Participants.Add(editor.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
            scenario.Participants.Add(editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            editor.StepBattle();
            Assert.That(editor.Preview.BattleMessageText(editor.Battle.StageMessages.Single()), Is.EqualTo("我方偷袭成功"));
            using var canvas = new Canvas(editor);
            foreach (Vector2 size in new[] { new Vector2(1050, 450), new Vector2(800, 600), new Vector2(1200, 600), new Vector2(800, 800) })
            {
                canvas.Area = new Rect(new Vector2(30, 60), size);
                canvas.Paint();
                Assert.That(canvas.Restored, Is.True);
                AssertMessagePixels(canvas);
            }
            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertMessagePixels(Canvas canvas)
        {
            RenderTexture buffer = canvas.RenderTarget;
            Assert.That(buffer, Is.Not.Null);
            Vector2 density = new(buffer.width / Canvas.Size.x, buffer.height / Canvas.Size.y);
            var context = new BattleHudViewport(canvas.Area, new Vector4(13, 9, 17, 11));
            float nativeX = BattleData.Variant == "PAL3A" ? ((int)context.CanvasSize.x - 161) / 2 : 300 + (context.CanvasSize.x - 800) / 2;
            // Independent native UIStatic arithmetic: inset8, six 20px Chinese
            // cells, center inside145x41; the glyph top is y30+8+10=48.
            Vector2 location = context.ToViewport(new Vector2(nativeX + 20, 48));
            Rect text = new(Vector2.Scale(location, density), Vector2.Scale(new Vector2(120, 20) * context.Scale, density));
            Vector2 panelLocation = context.ToViewport(new Vector2(nativeX, 30));
            Rect panel = new(Vector2.Scale(panelLocation, density), Vector2.Scale(new Vector2(161, 57) * context.Scale, density));
            var image = new Texture2D(buffer.width, buffer.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = buffer;
                image.ReadPixels(new Rect(0, 0, buffer.width, buffer.height), 0, 0);
                image.Apply();
                Color32[] pixels = image.GetPixels32();
                int ink = 0, background = 0;
                for (int y = Math.Max(0, (int)panel.yMin); y < Math.Min(buffer.height, (int)panel.yMax); y++)
                    for (int x = Math.Max(0, (int)panel.xMin); x < Math.Min(buffer.width, (int)panel.xMax); x++)
                    {
                        Color32 pixel = pixels[(buffer.height - 1 - y) * buffer.width + x];
                        if (text.Contains(new Vector2(x, y)) && pixel.r > 235 && pixel.g > 235 && pixel.b > 235)
                            ink++;
                        if (!text.Contains(new Vector2(x, y)) && Math.Max(pixel.r, Math.Max(pixel.g, pixel.b)) > 35)
                            background++;
                    }
                Assert.That(ink, Is.GreaterThan(40), "The native TFT title must reach the expected text rectangle.");
                Assert.That(background, Is.GreaterThan(30), "The original padded title background must also be rendered.");
                string output = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT");
                if (!string.IsNullOrEmpty(output))
                    File.WriteAllBytes(Path.Combine(output, BattleData.Variant + "-stage-title-" + canvas.Area.width + "x" + canvas.Area.height + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
