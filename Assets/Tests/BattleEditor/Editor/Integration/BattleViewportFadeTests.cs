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
    using Object = UnityEngine.Object;

    [Category("LocalGameData")]
    [Category("OffscreenGraphics")]
    public sealed class BattleViewportFadeTests
    {
        [Test]
        public void ExitFadeCoversTheWholeViewportAtAllSupportedAspectRatios()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
                Assert.Ignore("Original game data is required.");
            using var editor = new BattleEditorSession();
            editor.Load(root, 936);
            var scenario = new BattleScenario { Variant = BattleData.Variant, Scene = editor.Data.Scenes.First(), DataFingerprint = editor.Data.Fingerprint };
            BattleParticipant ally = editor.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water);
            ally.Level = 100;
            scenario.Participants.Add(ally);
            int enemy = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
            BattleParticipant target = editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water);
            target.Vitals[0] = 0;
            scenario.Participants.Add(target);
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            editor.StepBattle();
            Assert.That(editor.Battle.Status, Is.EqualTo(BattleSessionStatus.Completed));
            for (int step = 0; step < 5000 && editor.ResultPresentation.ScreenOpacity < .9f && !editor.ResultPresentation.Completed; step++)
            {
                editor.StepBattle();
                if (editor.ResultPresentation.Current?.CanSkip == true && editor.ResultPresentation.HasExecuted &&
                    editor.ResultPresentation.Current.Kind != BattleResultStepKind.ScreenFadeOut)
                    editor.SkipResultStep();
            }
            Assert.That(editor.ResultPresentation.ScreenOpacity, Is.GreaterThanOrEqualTo(.9f));
            Assert.That(editor.ResultPresentation.Completed, Is.False);
            Vector2 canvasSize = new(1100, 760);
            Rect area = default;
            using var canvas = new OffscreenGui(() =>
            {
                EditorGUI.DrawRect(new Rect(Vector2.zero, canvasSize), Color.magenta);
                EditorGUI.DrawRect(area, Color.green);
                editor.Preview.DrawResultOverlay(editor.ResultPresentation, area, new Vector4(21, 13, 19, 17));
            });
            foreach (Vector2 size in new[] { new Vector2(840, 360), new Vector2(640, 480), new Vector2(840, 420), new Vector2(480, 480) })
            {
                area = new Rect(new Vector2(35, 80), size);
                canvas.Paint(canvasSize);
                AssertPixels(canvas.RenderTarget, canvasSize, area);
            }
            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertPixels(RenderTexture buffer, Vector2 canvasSize, Rect area)
        {
            Assert.That(buffer, Is.Not.Null);
            Vector2 scale = new(buffer.width / canvasSize.x, buffer.height / canvasSize.y);
            var pixels = new Texture2D(buffer.width, buffer.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = buffer;
                pixels.ReadPixels(new Rect(0, 0, buffer.width, buffer.height), 0, 0);
                pixels.Apply();
                Color At(Vector2 point)
                {
                    int x = Mathf.Clamp(Mathf.RoundToInt(point.x * scale.x), 0, buffer.width - 1);
                    int y = Mathf.Clamp(Mathf.RoundToInt(point.y * scale.y), 0, buffer.height - 1);
                    return pixels.GetPixel(x, buffer.height - y - 1);
                }
                foreach (Vector2 point in new[]
                {
                    area.min + new Vector2(3, 3), new Vector2(area.xMax - 3, area.yMin + 3),
                    area.max - new Vector2(3, 3), new Vector2(area.xMin + 3, area.yMax - 3),
                    new Vector2(area.xMin + 3, area.center.y), new Vector2(area.xMax - 3, area.center.y),
                    new Vector2(area.center.x, area.yMin + 3), new Vector2(area.center.x, area.yMax - 3)
                })
                {
                    Color color = At(point);
                    Assert.That(Math.Max(color.r, Math.Max(color.g, color.b)), Is.LessThan(.17f),
                        $"Fade leaked at {point}, viewport {area}, color {color}.");
                }
                Color outside = At(area.min - new Vector2(8, 8));
                Assert.That(outside.r, Is.GreaterThan(.9f));
                Assert.That(outside.b, Is.GreaterThan(.9f), "The viewport fade must not paint over editor controls.");
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(pixels);
            }
        }
    }
}
