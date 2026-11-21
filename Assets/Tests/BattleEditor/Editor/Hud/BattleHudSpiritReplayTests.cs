// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using Battle;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using UnityEngine;

    [Category("LocalGameData")]
    public sealed class BattleHudSpiritReplayTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Actual4102RecordingKeepsTicksEventsAndRandomStateAcrossResizeSafeAreasAndReplay(bool expand)
        {
            string root = Environment.GetEnvironmentVariable("PAL3A_TEST_DATA_ROOT");
            if (root == null || !Directory.Exists(root))
                Assert.Ignore("Original PAL3A data is required.");
            using var editor = new BattleEditorSession();
            editor.Load(root, 936);
            var scenario = new BattleScenario
            {
                Variant = "PAL3A",
                Scene = editor.Data.Scenes.First(),
                DataFingerprint = editor.Data.Fingerprint,
                Encounter = BattleEncounter.AllyAmbush
            };
            scenario.Participants.Add(editor.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            scenario.Participants.Add(editor.Data.CreateParticipant(7311, BattleSide.Enemy, BattleSlot.Water));
            scenario.Participants.Add(editor.Data.CreateParticipant(7311, BattleSide.Enemy, BattleSlot.Fire));
            foreach (BattleParticipant actor in scenario.Participants)
            {
                actor.Attributes = new[] { 50000, 100, 1000, 5000, 1, 5, 0, 0, 0, 0, 0, 0 };
                actor.Vitals = new[] { 50000, 100, 1000 };
                actor.Skills.Clear();
            }
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = 4102, Level = 4, Mastered = true });
            scenario.Participants[1].Vitals[0] = 1;
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = true;
            editor.Preview.Effects.Muted = true;
            editor.Preview.View.ExpandReferenceFraming = expand;
            editor.Preview.View.SetViewportContext(new Rect(23, 41, 800, 600));
            string source = scenario.Participants[0].InstanceId;
            string target = editor.Battle.GetTargetCandidates(new BattleCommand(source, BattleAction.Skill, abilityId: 4102)).First().Id;
            Assert.That(editor.Battle.Submit(new BattleCommand(source, BattleAction.Skill, target, 4102)), Is.True);
            for (int step = 0; step < 600 && editor.Battle.SpiritOrbs.Count == 0; step++)
                editor.StepBattle();
            Assert.That(editor.Battle.SpiritOrbs.Count, Is.EqualTo(4));
            foreach (BattleSpiritOrbSnapshot orb in editor.Battle.SpiritOrbs)
                Assert.That(orb.Origin.X, Is.EqualTo((int)orb.Origin.X));
            long appeared = editor.Battle.Tick;
            int frame = 0;
            while (editor.Battle.SpiritOrbs.Count > 0 && frame++ < 600)
            {
                Rect viewport = frame % 2 == 0 ? new Rect(37, 51, 1600, 900) : new Rect(17, 23, 2520, 1080);
                var context = new BattleHudViewport(viewport, frame % 2 == 0 ? new Vector4(16, 12, 30, 8) : new Vector4(40, 20, 24, 18));
                editor.Preview.View.SetViewportContext(viewport);
                foreach (BattleSpiritOrbSnapshot orb in editor.Battle.SpiritOrbs)
                {
                    Vector2 display = context.SpiritOrbPoint(orb, editor.Preview.View.ReferenceProjectionScale(viewport));
                    Assert.That(float.IsFinite(display.x) && float.IsFinite(display.y), Is.True);
                }
                editor.StepBattle();
            }
            Assert.That(editor.Battle.SpiritOrbs, Is.Empty);
            Assert.That(editor.Battle.Tick, Is.GreaterThan(appeared));
            long ended = editor.Battle.Tick;
            string before = JsonConvert.SerializeObject(new
            {
                editor.Battle.Tick,
                editor.Battle.SimulationTick,
                editor.Battle.Events,
                editor.Battle.Inputs,
                editor.Battle.Pal3AState,
                editor.Battle.Inventory,
                editor.Battle.Actors
            });
            editor.Preview.View.SetViewportContext(new Rect(111, 29, 320, 640));
            editor.ReplayBattle();
            editor.Preview.Audio.Muted = true;
            editor.Preview.Effects.Muted = true;
            Assert.That(editor.Battle.Tick, Is.EqualTo(ended));
            string after = JsonConvert.SerializeObject(new
            {
                editor.Battle.Tick,
                editor.Battle.SimulationTick,
                editor.Battle.Events,
                editor.Battle.Inputs,
                editor.Battle.Pal3AState,
                editor.Battle.Inventory,
                editor.Battle.Actors
            });
            Assert.That(after, Is.EqualTo(before));
        }
    }
}
#endif
