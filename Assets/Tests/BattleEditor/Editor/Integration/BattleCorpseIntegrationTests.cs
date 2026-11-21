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
    public sealed class BattleCorpseIntegrationTests
    {
        private sealed class CompletedEffects : IBattleEffectDirector
        {
            public long Start(BattleEffectRequest request) => 1;

            public bool IsRunning(long handle) => false;

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
            }

            public void Stop(long handle)
            {
            }
        }

        [Test]
        public void ActualKillingSpellAwardsAndDisplaysTheOriginalCorpseModelAndSkin()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Original game data is required for corpse geometry verification.");
            using var data = new BattleData(root, 936);
            int skill = BattleData.Variant == "PAL3" ? 315 : 319;
            BattleParticipant source = data.CreateParticipant(104, BattleSide.Ally, BattleSlot.Water);
            BattleParticipant target = data.CreateParticipant(7354, BattleSide.Enemy, BattleSlot.Water);
            source.InstanceId = "S";
            target.InstanceId = "T";
            source.Level = 100;
            foreach (BattleParticipant actor in new[] { source, target })
            {
                actor.Attributes = new[] { 50000, 9999, 9999, 5000, 0, 5, 0, 0, 0, 0, 0, 0 };
                actor.Vitals = new[] { 50000, 9999, 9999 };
                actor.Skills.Clear();
            }
            target.Vitals[0] = 1;
            source.Skills.Add(new BattleSkill { Id = skill, Mastered = true, Level = 4 });
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = data.Scenes.First(),
                DataFingerprint = data.Fingerprint,
                Encounter = BattleEncounter.AllyAmbush,
                Seed = 123
            };
            scenario.Participants.Add(source);
            scenario.Participants.Add(target);
            var setup = new BattleSetup(scenario);
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(data, setup);
            var catalog = new BattleAnimationCatalog(data, setup, definitions);
            // This case verifies the corpse downstream of real damage and
            // settlement. Ordinary effect rendering has its own GPU fixtures.
            var combat = new BattleSession(setup, catalog, definitions: definitions, effects: new CompletedEffects());
            combat.SetAutomatic(BattleSide.Enemy, false);
            using var preview = new BattlePreview(data, setup);
            preview.BindBattle(combat, catalog);
            var command = new BattleCommand("S", BattleAction.Skill, "T", skill);
            Assert.That(combat.Submit(command), Is.True, combat.CanSubmit(command));
            for (int tick = 0; tick < 3000 && combat.Status == BattleSessionStatus.Running; tick++)
            {
                combat.Step();
                preview.ApplyBattle(combat, catalog);
            }
            Assert.That(combat.Status, Is.EqualTo(BattleSessionStatus.Completed), combat.Result?.Reason);
            Assert.That(combat.Result.Loot.Any(item => item.Id == 6527 && item.Count == 1), Is.True);
            Transform corpse = preview.View.Root.transform.Find("Corpse T");
            Assert.That(corpse, Is.Not.Null);
            var center = data.Layout.AllyFormationConfig.CenterGameBoxPosition;
            Vector3 expected = BattleAnimationCatalog.UnityPosition(new BattleMovementPoint(center.X, center.Y + 2, center.Z + 20));
            Assert.That(Vector3.Distance(corpse.position, expected), Is.LessThan(.000001f));
            Assert.That(corpse.GetComponentsInChildren<MeshFilter>().Sum(mesh => mesh.sharedMesh.triangles.Length), Is.GreaterThan(0));
            Assert.That(preview.View.Dependencies.Any(entry => entry.Value && (entry.Key.EndsWith("dye3.dds", StringComparison.OrdinalIgnoreCase) || entry.Key.EndsWith("dye3.tga", StringComparison.OrdinalIgnoreCase))), Is.True);
            Capture(preview);
        }

        private static void Capture(BattlePreview preview)
        {
            RenderTexture render = preview.View.Render(800, 600);
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(800, 600, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = render;
                image.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
                image.Apply();
                Assert.That(image.GetPixels32().Select(pixel => (pixel.r, pixel.g, pixel.b)).Distinct().Take(100).Count(), Is.GreaterThan(20));
                string output = Environment.GetEnvironmentVariable("PAL3_BATTLE_QA_ROOT");
                if (output != null)
                {
                    Directory.CreateDirectory(output);
                    File.WriteAllBytes(Path.Combine(output, BattleData.Variant + "-corpse-result.png"), image.EncodeToPNG());
                }
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }
    }
}
