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
    using Core.Contract.Enums;
    using NUnit.Framework;
    using UnityEngine;

    [Category("LocalGameData")]
    public sealed class BattleActorPresentationTests
    {
        private BattleEditorSession _editor;

        [SetUp]
        public void Load()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (!Directory.Exists(root))
                Assert.Ignore("Original data is required.");
            _editor = new BattleEditorSession();
            _editor.Load(root, 936);
        }

        [TearDown]
        public void Dispose() => _editor?.Dispose();

        private BattleScenario Scenario()
        {
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                DataFingerprint = _editor.Data.Fingerprint,
                Scene = _editor.Data.Scenes.First(),
                Encounter = BattleEncounter.AllyAmbush
            };
            int enemy = _editor.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(_editor.Data, id));
            scenario.Participants.Add(_editor.Data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            scenario.Participants.Add(_editor.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            foreach (BattleParticipant actor in scenario.Participants)
            {
                actor.InstanceId = actor.Side.ToString();
                actor.Attributes = new[] { 5000, 100, 500, 70, 10, 12, 0, 0, 0, 0, 0, 0 };
                actor.Attributes[5] = actor.Side == BattleSide.Ally ? 3 : 12;
                actor.Vitals = new[] { 5000, 100, 500 };
                actor.Skills.Clear();
            }
            return scenario;
        }

        private (bool Visible, string Clip, GameObject Body) Body(string id)
        {
            object actor = ((IDictionary)typeof(BattlePreview).GetField("_actors", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_editor.Preview))[id];
            Type type = actor.GetType();
            return ((bool)type.GetField("Visible").GetValue(actor), (string)type.GetField("Action").GetValue(actor),
                (GameObject)type.GetField("Object").GetValue(actor));
        }

        [Test]
        public void AuthoringAttackSamplesTheEquippedHeroAndMonsterAttackClips()
        {
            BattleScenario scenario = Scenario();
            BattleData data = _editor.Data;
            const int identity = 4;
            int hero = data.Database.CombatActorInfos.Keys.First(id => BattleData.PlayerIdentity(id) == identity);
            BattleParticipant ally = data.CreateParticipant(hero, BattleSide.Ally, BattleSlot.Water);
            ally.InstanceId = "Ally";
            int alternateWeaponType = BattleData.Variant == "PAL3" ? 2 : 9;
            ally.Equipment[0] = data.Database.GameItemInfos.First(item => item.Value.Type == ItemType.Weapon && item.Key != BattleMagicSwordRules.WeaponId &&
                (int)item.Value.WeaponType == alternateWeaponType && item.Value.ApplicableActors.Contains((PlayerActorId)identity)).Key;
            scenario.Participants[0] = ally;
            _editor.Start(scenario);
            void AssertAttack(string source, string target, string expected)
            {
                _editor.Preview.PlayAttack(source, target);
                _editor.Preview.Sample(_editor.Preview.Duration / 2);
                Assert.That(Body(source).Clip, Is.EqualTo(expected));
            }
            AssertAttack("Ally", "Enemy", "z02");
            AssertAttack("Enemy", "Ally", "g");

            BattleScenario unequipped = Scenario();
            unequipped.Participants[0].Equipment = Array.Empty<int>();
            Assert.That(data.AttackAction(unequipped.Participants[0]), Is.EqualTo("z01"));
            _editor.Start(unequipped);
            AssertAttack("Ally", "Enemy", "z01");
        }

        [TestCase(0, 4101)]
        [TestCase(1, 4205)]
        public void RealSkillReturnRestoresTheActorAndRenderedBodyFacing(int identity, int skill)
        {
            BattleScenario scenario = Scenario();
            int id = _editor.Data.Database.CombatActorInfos.Keys.First(actor => BattleData.PlayerIdentity(actor) == identity);
            BattleParticipant ally = _editor.Data.CreateTestParticipant(id, BattleSide.Ally, BattleSlot.Water);
            ally.InstanceId = "Ally";
            ally.Attributes = new[] { 50000, 100, 1000, 70, 10, 3, 0, 0, 0, 0, 0, 0 };
            ally.Vitals = ally.Attributes.Take(3).ToArray();
            scenario.Participants[0] = ally;
            scenario.Participants[1].Attributes[0] = scenario.Participants[1].Vitals[0] = 50000;
            _editor.StartBattle(scenario);
            _editor.Preview.Audio.Muted = _editor.Preview.Effects.Muted = true;
            var command = new BattleCommand("Ally", BattleAction.Skill, "Enemy", skill);
            Assert.That(_editor.Battle.Submit(command), Is.True, _editor.Battle.CanSubmit(command));
            for (int step = 0; step < 5000 && !_editor.Battle.Events.Any(entry => entry.Source == "Ally" && entry.Kind == BattleEventKind.ActionEnded); step++)
                _editor.StepBattle();
            Assert.That(_editor.Battle.Events.Any(entry => entry.Source == "Ally" && entry.Kind == BattleEventKind.ActionEnded), Is.True, _editor.Battle.Result?.Reason);
            BattleActorSnapshot state = _editor.Battle.Actors.Single(actor => actor.Id == "Ally");
            double x = state.Position.X - state.Home.X, y = state.Position.Y - state.Home.Y, z = state.Position.Z - state.Home.Z;
            Assert.That(Math.Sqrt(x * x + y * y + z * z), Is.LessThanOrEqualTo(13));
            Assert.That(state.FacingDegrees, Is.EqualTo(BattleFacingRules.Initial(BattleSide.Ally)));
            Assert.That(Quaternion.Angle(Body("Ally").Body.transform.rotation, Quaternion.Euler(0, -state.FacingDegrees, 0)), Is.LessThan(.001f));
        }

        [Test]
        public void ActualGuardHitUsesTheBoundClipWithoutHidingTheCharacter()
        {
            _editor.StartBattle(Scenario());
            _editor.Preview.Audio.Muted = _editor.Preview.Effects.Muted = true;
            Assert.That(_editor.Battle.Submit("Ally", BattleAction.Defend), Is.True);
            for (int step = 0; step < 1000 && !_editor.Battle.Actors.Single(actor => actor.Id == "Enemy").Ready; step++)
                _editor.StepBattle();
            Assert.That(_editor.Battle.Actors.Single(actor => actor.Id == "Ally").Guarding, Is.True);
            Assert.That(_editor.Battle.Submit("Enemy", BattleAction.Attack, "Ally"), Is.True);
            for (int step = 0; step < 500 && _editor.Battle.Actors.Single(actor => actor.Id == "Ally").Pose != BattlePose.GuardHurt; step++)
                _editor.StepBattle();
            BattleActorSnapshot state = _editor.Battle.Actors.Single(actor => actor.Id == "Ally");
            Assert.That(state.Pose, Is.EqualTo(BattlePose.GuardHurt));
            Assert.That(state.NativeAnimationClip, Is.EqualTo(BattleData.Variant == "PAL3" ? "z142" : "z063"));
            var body = Body("Ally");
            Assert.That(body.Visible, Is.True);
            Assert.That(body.Clip, Is.EqualTo(state.NativeAnimationClip));
            Assert.That(body.Body.GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled), Is.True);
            Assert.That(_editor.Preview.View.Render(800, 600), Is.Not.Null);
        }

        [TestCase(BattleState.Sleep)]
        [TestCase(BattleState.Paralysis)]
        public void InactiveBodiesStillUseTheirNativeStatusAnimation(BattleState status)
        {
            BattleScenario scenario = Scenario();
            scenario.Participants[0].Statuses.Add(new BattleStatus { Id = (int)status, Rounds = 5 });
            _editor.StartBattle(scenario);
            _editor.StepBattle();
            BattleActorSnapshot state = _editor.Battle.Actors.Single(actor => actor.Id == "Ally");
            Assert.That(Body("Ally").Visible, Is.True);
            Assert.That(Body("Ally").Clip, Is.EqualTo(state.NativeAnimationClip));
        }

        [Test]
        public void RetainedNativeIdleDoesNotResurrectAHiddenDeadMonster()
        {
            BattleScenario scenario = Scenario();
            scenario.Participants[1].Vitals[0] = 0;
            _editor.StartBattle(scenario);
            BattleActorSnapshot state = _editor.Battle.Actors.Single(actor => actor.Id == "Enemy");
            Assert.That(state.ModelAliveFlag, Is.False);
            Assert.That(state.NativeAnimationClip, Is.Not.Null);
            Assert.That(Body("Enemy").Visible, Is.False);
            Assert.That(Body("Enemy").Body.GetComponentsInChildren<Renderer>().All(renderer => !renderer.enabled), Is.True);
        }
    }
}
