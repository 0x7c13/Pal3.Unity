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
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Category("LocalGameData")]
    public sealed class BattleLineupRenderingTests
    {
        [Test]
        public void RealMagicPreparationShowsTheNativeGroundLineupAndRepaintDoesNotAgeIt()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
                Assert.Ignore("Original data is required.");
            using var editor = new BattleEditorSession();
            editor.Load(root, 936);
            Assert.That(editor.Data.Layout.AllyFormationConfig.RollRadians, Is.EqualTo(-.500003f));
            Assert.That(editor.Data.Layout.EnemyFormationConfig.RollRadians, Is.EqualTo(.156633f));
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                DataFingerprint = editor.Data.Fingerprint,
                Scene = editor.Data.Scenes.First(),
                SceneElement = 1,
                Encounter = BattleEncounter.AllyAmbush
            };
            BattleParticipant ally = editor.Data.CreateTestParticipant(101, BattleSide.Ally, BattleSlot.Water);
            int enemyId = editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(editor.Data, id));
            BattleParticipant enemy = editor.Data.CreateParticipant(enemyId, BattleSide.Enemy, BattleSlot.Water);
            foreach (BattleParticipant actor in new[] { ally, enemy })
            {
                actor.Attributes = new[] { 5000, 100, 1000, 70, 10, 12, 0, 0, 0, 0, 0, 0 };
                actor.Vitals = new[] { 5000, 100, 1000 };
                scenario.Participants.Add(actor);
            }
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = editor.Preview.Effects.Muted = true;
            for (int step = 0; step < 2000 && (editor.Battle.SimulationTick <= 210 || !editor.Battle.Actors.Single(actor => actor.Id == ally.InstanceId).Ready); step++)
                editor.StepBattle();
            Assert.That(editor.Battle.Submit(new BattleCommand(ally.InstanceId, BattleAction.Skill, enemy.InstanceId, 301)), Is.True);
            for (int step = 0; step < 2000 && !editor.Battle.AbilityPresentations.Any(stage => stage.LineupVisible && stage.LineupElapsedSeconds > .5); step++)
                editor.StepBattle();
            Assert.That(editor.Battle.AbilityPresentations.Any(stage => stage.LineupVisible), Is.True);
            Assert.That(editor.Preview.LineupFrames.Any(frame => frame.Side == BattleSide.Ally && frame.Alpha > 0), Is.True);
            GameObject[] quads = editor.Preview.View.Root.GetComponentsInChildren<MeshRenderer>()
                .Where(renderer => renderer.name.StartsWith("Original Ally", StringComparison.Ordinal) && renderer.name.Contains("lineup"))
                .Select(renderer => renderer.gameObject).ToArray();
            Assert.That(quads.Length, Is.EqualTo(2));
            var center = editor.Data.Layout.AllyFormationConfig.CenterGameBoxPosition;
            Vector3 focus = BattleAnimationCatalog.UnityPosition(new BattleMovementPoint(center.X, 0, center.Z));
            editor.Preview.View.SetHorizontalCameraOrientation(focus + Vector3.up * 50, Vector3.down, Vector3.forward, 50);
            Texture2D shown = Capture(editor.Preview.View.Render(800, 600));
            Texture2D hidden = null;
            try
            {
                foreach (GameObject quad in quads)
                    quad.GetComponent<MeshRenderer>().enabled = false;
                hidden = Capture(editor.Preview.View.Render(800, 600));
                Color32[] a = shown.GetPixels32(), b = hidden.GetPixels32();
                int changed = a.Zip(b, (x, y) => Math.Abs(x.r - y.r) + Math.Abs(x.g - y.g) + Math.Abs(x.b - y.b)).Count(delta => delta > 6);
                Assert.That(changed, Is.GreaterThan(100), "A real spell's lineup must reach actual GPU pixels.");
                string output = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT");
                if (!string.IsNullOrEmpty(output))
                {
                    File.WriteAllBytes(Path.Combine(output, BattleData.Variant + "-lineup-shown.png"), shown.EncodeToPNG());
                    File.WriteAllBytes(Path.Combine(output, BattleData.Variant + "-lineup-hidden.png"), hidden.EncodeToPNG());
                }
            }
            finally
            {
                foreach (GameObject quad in quads)
                    quad.GetComponent<MeshRenderer>().enabled = true;
                Object.DestroyImmediate(shown);
                if (hidden != null)
                    Object.DestroyImmediate(hidden);
            }
            var frames = editor.Preview.LineupFrames.Select(frame => (frame.Side, frame.Alpha)).ToArray();
            editor.Preview.View.Render(800, 600);
            Assert.That(editor.Preview.LineupFrames.Select(frame => (frame.Side, frame.Alpha)), Is.EqualTo(frames));
            AssertNativeTextureOrientation(editor, quads.Single(quad => quad.name.Contains("five-spirit")));
            for (int step = 0; step < 150; step++)
                editor.StepBattle();
            Assert.That(editor.Preview.LineupFrames, Is.Empty);
            Assert.That(quads.All(quad => !quad.GetComponent<MeshRenderer>().enabled), Is.True);
        }

        private static void AssertNativeTextureOrientation(BattleEditorSession editor, GameObject ring)
        {
            // Native RenderGeomQuad maps its (-radius,-radius) corner to (0,0).
            // A deliberately asymmetric texture detects a v inversion even
            // when both orientations have the same number of bright pixels.
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            Color[] pattern = Enumerable.Range(0, 16).Select(index => index / 4 < 2
                ? index % 4 < 2 ? Color.red : Color.green
                : index % 4 < 2 ? Color.blue : Color.white).ToArray();
            texture.SetPixels(pattern);
            texture.Apply();
            Renderer[] renderers = editor.Preview.View.Root.GetComponentsInChildren<Renderer>();
            bool[] enabled = renderers.Select(renderer => renderer.enabled).ToArray();
            var renderer = ring.GetComponent<MeshRenderer>();
            Texture original = renderer.sharedMaterial.mainTexture;
            Texture2D shown = null, hidden = null;
            try
            {
                foreach (Renderer other in renderers)
                    other.enabled = false;
                hidden = Capture(editor.Preview.View.Render(800, 600));
                renderer.sharedMaterial.mainTexture = texture;
                renderer.enabled = true;
                shown = Capture(editor.Preview.View.Render(800, 600));
                var layout = editor.Data.Layout.AllyFormationConfig;
                foreach ((float u, float v, Color expected) in new[] { (.25f, .25f, Color.red), (.75f, .25f, Color.green), (.25f, .75f, Color.blue) })
                {
                    float x = (2 * u - 1) * layout.GameBoxRadius, z = (2 * v - 1) * layout.GameBoxRadius;
                    double sine = Math.Sin(layout.RollRadians), cosine = Math.Cos(layout.RollRadians);
                    var native = new BattleMovementPoint((float)(layout.CenterGameBoxPosition.X + x * cosine + z * sine), .5f,
                        (float)(layout.CenterGameBoxPosition.Z - x * sine + z * cosine));
                    Assert.That(editor.Preview.View.Project(BattleAnimationCatalog.UnityPosition(native), new Rect(0, 0, 800, 600), out Vector2 point), Is.True);
                    int px = Mathf.RoundToInt(point.x), py = 599 - Mathf.RoundToInt(point.y);
                    Color difference = shown.GetPixel(px, py) - hidden.GetPixel(px, py);
                    float wanted = expected.r * difference.r + expected.g * difference.g + expected.b * difference.b;
                    float unwanted = (1 - expected.r) * difference.r + (1 - expected.g) * difference.g + (1 - expected.b) * difference.b;
                    Assert.That(wanted, Is.GreaterThan(.05f), $"Native UV ({u},{v}) must reach its own asymmetric texel, delta={difference}.");
                    Assert.That(unwanted, Is.LessThan(.02f), $"Native UV ({u},{v}) sampled another texture row, delta={difference}.");
                }
            }
            finally
            {
                renderer.sharedMaterial.mainTexture = original;
                for (int index = 0; index < renderers.Length; index++)
                    renderers[index].enabled = enabled[index];
                Object.DestroyImmediate(texture);
                if (shown != null)
                    Object.DestroyImmediate(shown);
                if (hidden != null)
                    Object.DestroyImmediate(hidden);
            }
        }

        private static Texture2D Capture(RenderTexture target)
        {
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }
    }
}
