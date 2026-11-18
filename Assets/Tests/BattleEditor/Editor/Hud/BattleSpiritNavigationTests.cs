// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Battle;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using Pal3.Tests.EditorSupport;
    using UnityEngine;

    [Category("LocalGameData")]
    [Category("OffscreenGraphics")]
    public sealed class BattleSpiritNavigationTests
    {
        private static string Frame(BattleEditorSession editor) => JsonConvert.SerializeObject(new
        {
            editor.Battle.Tick,
            editor.Battle.SimulationTick,
            editor.Battle.SpiritOrbs,
            editor.Battle.Pal3AState,
            Events = editor.Battle.Events.ToArray(),
            Inputs = editor.Battle.Inputs.ToArray(),
            editor.Battle.Actors
        });

        private static void Start(BattleEditorSession editor, BattleScenario scenario)
        {
            editor.StartBattle(scenario);
            editor.Preview.Audio.Muted = true;
            editor.Preview.Effects.Muted = true;
            editor.Preview.View.SetViewportContext(new Rect(0, 0, 800, 600));
            string source = scenario.Participants[0].InstanceId;
            string target = editor.Battle.GetTargetCandidates(new BattleCommand(source, BattleAction.Skill, abilityId: 4102)).First().Id;
            Assert.That(editor.Battle.Submit(new BattleCommand(source, BattleAction.Skill, target, 4102)), Is.True);
        }

        private static void Navigate(BattleEditorSession editor, string kind)
        {
            if (kind == "frame")
            {
                editor.Preview.View.Frame();
                return;
            }
            Vector2 size = new(820, 620);
            using var gui = new OffscreenGui(() => editor.Preview.View.Draw(new Rect(0, 0, 800, 600)));
            gui.Paint(size);
            if (kind == "middle")
                gui.SendEvent(new Event { type = EventType.MouseDown, button = 2, mousePosition = new Vector2(100, 100) }, size);
            gui.SendEvent(new Event
            {
                mousePosition = new Vector2(200, 150),
                type = kind == "scroll" ? EventType.ScrollWheel : EventType.MouseDrag,
                button = kind == "scroll" ? 0 : 2,
                delta = kind == "scroll" ? new Vector2(0, 8) : new Vector2(100, 50)
            }, size);
            if (kind == "middle")
                gui.SendEvent(new Event { type = EventType.MouseUp, button = 2, mousePosition = new Vector2(200, 150) }, size);
        }

        [TestCase("scroll", false)]
        [TestCase("middle", false)]
        [TestCase("frame", false)]
        [TestCase("scroll", true)]
        public void ActualCaptureTrajectoryAndReplayIgnoreUnrecordedPreviewNavigation(string navigation, bool authoredCamera)
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
                Encounter = BattleEncounter.AllyAmbush,
                Seed = 1
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
            if (authoredCamera)
            {
                Start(editor, scenario);
                editor.Preview.View.GetCamera(out Vector3 position, out Vector3 target, out float fieldOfView);
                scenario.Camera = new BattleCamera
                {
                    Override = true,
                    Position = new BattlePoint { X = position.x, Y = position.y, Z = position.z },
                    Target = new BattlePoint { X = target.x, Y = target.y, Z = target.z },
                    FieldOfView = fieldOfView
                };
            }
            Start(editor, scenario);
            while (editor.Battle.SpiritOrbs.Count == 0 && editor.Battle.Tick < 600)
                editor.StepBattle();
            Assert.That(editor.Battle.SpiritOrbs.Count, Is.EqualTo(4));
            long birth = editor.Battle.Tick;
            var expected = new List<string> { Frame(editor) };
            while (editor.Battle.SpiritOrbs.Count > 0 && expected.Count < 900)
            {
                editor.StepBattle();
                expected.Add(Frame(editor));
            }
            Assert.That(editor.Battle.SpiritOrbs, Is.Empty);
            Start(editor, scenario);
            while (editor.Battle.Tick < birth - 1)
                editor.StepBattle();
            editor.Preview.View.GetCamera(out Vector3 before, out _, out _);
            Navigate(editor, navigation);
            editor.Preview.View.GetCamera(out Vector3 changed, out _, out _);
            Assert.That(changed, Is.Not.EqualTo(before), "The public preview navigation must actually change its camera.");
            editor.StepBattle();
            Assert.That(Frame(editor), Is.EqualTo(expected[0]));
            string atBirth = Frame(editor);
            editor.ReplayBattle();
            Assert.That(Frame(editor), Is.EqualTo(atBirth));
            for (int index = 1; index < expected.Count; index++)
            {
                editor.StepBattle();
                Assert.That(Frame(editor), Is.EqualTo(expected[index]), $"trajectory index={index}");
            }
        }
    }
}
#endif
