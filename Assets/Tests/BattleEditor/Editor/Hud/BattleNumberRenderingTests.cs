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
    using NUnit.Framework;
    using Pal3.Tests.EditorSupport;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;

    [Category("LocalGameData")]
    [Category("OffscreenGraphics")]
    public sealed class BattleNumberRenderingTests
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
                Editor.Preview.DrawBattleNumbers(Editor.Battle, Area, new Vector4(13, 9, 17, 11));
                Restored = GUI.color == color && GUI.matrix == matrix;
                if (Event.current.type == EventType.Repaint)
                    Repaints++;
            }
        }

        [Test]
        public void OriginalGlyphInkIsActuallyDrawnAtTheWorldProjectionInAllFourViewports()
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
                Encounter = BattleEncounter.AllyAmbush
            };
            scenario.Participants.Add(editor.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
            scenario.Participants.Add(editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            editor.Preview.View.SetHorizontalCameraOrientation(new Vector3(0, 0, -10), Vector3.forward, Vector3.up, 50);
            // A published activation is an immutable presentation input. Avoid
            // running AI while independently testing the real IMGUI draw path.
            var events = (List<BattleEvent>)typeof(BattleSession).GetField("_events", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(editor.Battle);
            var number = new BattleEvent();
            void Set(string name, object value) => typeof(BattleEvent).GetProperty(name).SetValue(number, value);
            Set(nameof(BattleEvent.Kind), BattleEventKind.NumberShown);
            Set(nameof(BattleEvent.Target), scenario.Participants[0].InstanceId);
            Set(nameof(BattleEvent.RequestedDelta), -123);
            Set(nameof(BattleEvent.Resource), 0);
            Set(nameof(BattleEvent.DurationSeconds), 2f);
            Set(nameof(BattleEvent.NumberPosition), new BattleMovementPoint(0, 0, 0));
            Set(nameof(BattleEvent.NumberOffset), 16);
            events.Add(number);
            using var canvas = new Canvas(editor);
            foreach (Vector2 size in new[] { new Vector2(1050, 450), new Vector2(800, 600), new Vector2(1200, 600), new Vector2(800, 800) })
            {
                canvas.Area = new Rect(new Vector2(30, 60), size);
                canvas.Paint();
                Assert.That(canvas.Restored, Is.True, "Number drawing must restore the caller's GUI state.");
                AssertGlyphPixels(canvas);
            }
            var cache = (Dictionary<char, Texture2D>)typeof(BattlePreview).GetField("_numberGlyphs", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(editor.Preview);
            Texture2D[] glyphs = cache.Values.ToArray();
            Assert.That(cache.ContainsKey('1'), Is.True);
            editor.Dispose();
            Assert.That(glyphs.All(texture => texture == null), Is.True,
                "Disposing the preview must release its generated native glyph cache.");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RealPhysicalWorkPublishesAndDrawsItsDamageNumber()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
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
            BattleParticipant ally = editor.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water);
            int enemyId = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
            BattleParticipant enemy = editor.Data.CreateParticipant(enemyId, BattleSide.Enemy, BattleSlot.Water);
            foreach (BattleParticipant actor in new[] { ally, enemy })
            {
                actor.Attributes = new[] { 5000, 100, 100, 70, 10, 3, 0, 0, 0, 0, 0, 0 };
                actor.Vitals = new[] { 5000, 100, 100 };
                actor.Skills.Clear();
                scenario.Participants.Add(actor);
            }
#if PAL3
            ally.Equipment[0] = 6601;
#endif
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            Assert.That(editor.Battle.Submit(ally.InstanceId, BattleAction.Attack, enemy.InstanceId), Is.True);
            for (int step = 0; step < 500 && !editor.Battle.Events.Any(entry => entry.Kind == BattleEventKind.NumberShown); step++)
                editor.StepBattle();
            BattleEvent number = editor.Battle.Events.Single(entry => entry.Kind == BattleEventKind.NumberShown);
            Assert.That(number.Target, Is.EqualTo(enemy.InstanceId));
            Assert.That(number.RequestedDelta, Is.LessThan(0));
            Assert.That(editor.Battle.Events.Any(entry => entry.Kind == BattleEventKind.Hit && entry.Target == number.Target), Is.True);
            Assert.That(editor.Preview.NumberFrames.Count, Is.EqualTo(1), "Session synchronization must feed the production projection.");
            Vector3 anchor = BattleAnimationCatalog.UnityPosition(number.NumberPosition);
            editor.Preview.View.SetHorizontalCameraOrientation(anchor + new Vector3(0, 0, -10), Vector3.forward, Vector3.up, 50);
            using var canvas = new Canvas(editor);
            canvas.Area = new Rect(30, 60, 800, 600);
            canvas.Paint();
            Assert.That(canvas.Restored, Is.True);
            AssertGlyphPixels(canvas, number);
            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertGlyphPixels(Canvas canvas, BattleEvent activation = null)
        {
            RenderTexture buffer = canvas.RenderTarget;
            Assert.That(buffer, Is.Not.Null);
            Vector2 density = new(buffer.width / Canvas.Size.x, buffer.height / Canvas.Size.y);
            // The independent camera centers the anchor. The first stack raises
            // the glyphs by ten points and the native glyph height is twenty.
            var context = new BattleHudViewport(canvas.Area, new Vector4(13, 9, 17, 11));
            float elapsed = activation == null ? 0 : Math.Max(0, BattleStageScheduler.TimeAt(canvas.Editor.Battle.SimulationTick) - activation.NumberStartedTime);
            int offset = activation == null ? 0 : activation.NumberOffset - 16;
            int rise = (int)Math.Round(elapsed * 30, MidpointRounding.ToEven);
            int width = activation == null ? 40 : activation.RequestedDelta.ToString(System.Globalization.CultureInfo.InvariantCulture).Length * 10;
            Vector2 start = canvas.Area.center + new Vector2(offset, -10 - rise) * context.Scale;
            Rect expected = new(Vector2.Scale(start, density), Vector2.Scale(new Vector2(width, 20) * context.Scale, density));
            Rect area = new(Vector2.Scale(canvas.Area.position, density), Vector2.Scale(canvas.Area.size, density));
            var texture = new Texture2D(buffer.width, buffer.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = buffer;
                texture.ReadPixels(new Rect(0, 0, buffer.width, buffer.height), 0, 0);
                texture.Apply();
                Color32[] pixels = texture.GetPixels32();
                int ink = 0, outside = 0;
                Rect tolerance = Rect.MinMaxRect(expected.xMin - 2, expected.yMin - 2, expected.xMax + 2, expected.yMax + 2);
                for (int y = Math.Max(0, (int)area.yMin); y < Math.Min(buffer.height, area.yMax); y++)
                    for (int x = Math.Max(0, (int)area.xMin); x < Math.Min(buffer.width, area.xMax); x++)
                    {
                        Color32 pixel = pixels[(buffer.height - 1 - y) * buffer.width + x];
                        if (pixel.b < 140 || pixel.r > 120 || pixel.g > 120)
                            continue;
                        ink++;
                        if (!tolerance.Contains(new Vector2(x, y)))
                            outside++;
                    }
                Assert.That(ink, Is.GreaterThan(20), "The production draw path must render native TFT glyph ink.");
                Assert.That(outside, Is.Zero, "Actual glyph pixels drifted away from the projected 10-pixel cells: " + expected);
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
