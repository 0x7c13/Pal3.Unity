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
    using Core.Contract.Enums;
    using NUnit.Framework;

    [Category("LocalGameData")]
    public sealed class BattleStockAttackModeIntegrationTests
    {
        private string Root()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Original game data is required.");
            return root;
        }

        [Test]
        public void EveryStockMonsterRecordHasAVerifiedPhysicalDispatch()
        {
            using var data = new BattleData(Root(), 936);
            foreach (var entry in data.Database.CombatActorInfos.Where(entry => entry.Value.Type != CombatActorType.MainActor))
            {
                BattleParticipant actor = data.CreateParticipant(entry.Key, BattleSide.Enemy, BattleSlot.Water);
                var scenario = new BattleScenario
                {
                    Variant = BattleData.Variant,
                    Scene = data.Scenes.First(),
                    DataFingerprint = data.Fingerprint,
                    Participants = { data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water), actor }
                };
                BattleActorDefinition definition = BattleDefinitionCompiler.Build(data, new BattleSetup(scenario)).Actors[actor.InstanceId];
                Assert.That(definition.UnsupportedReason, Is.Empty, $"Actor {entry.Key}: {entry.Value.Name}");
                Assert.That(definition.NormalAttackRange, Is.Not.EqualTo(BattleTargetRange.None));
            }
        }

#if PAL3A
        [Test]
        public void Actual7398NormalAttack7714RemainsPhysicalAndCompletesWithItsOriginalBody()
        {
            using var session = new BattleEditorSession();
            session.Load(Root(), 936);
            BattleData data = session.Data;
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = data.Scenes.First(),
                DataFingerprint = data.Fingerprint,
                Encounter = BattleEncounter.EnemyAmbush
            };
            BattleParticipant player = data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water);
            player.Attributes[0] = player.Vitals[0] = 50000;
            BattleParticipant enemy = data.CreateParticipant(7398, BattleSide.Enemy, BattleSlot.Water);
            scenario.Participants.Add(player);
            scenario.Participants.Add(enemy);
            Assert.That(data.Database.CombatActorInfos[7398].NormalAttackModeId, Is.EqualTo(7714));
            session.StartBattle(scenario);
            var command = new BattleCommand(enemy.InstanceId, BattleAction.Attack, player.InstanceId);
            Assert.That(session.Battle.Submit(command), Is.True, session.Battle.CanSubmit(command));
            for (int tick = 0; tick < 2000 && !session.Battle.Events.Any(entry => entry.Kind == BattleEventKind.ActionEnded); tick++)
                session.StepBattle();
            Assert.That(session.Battle.Events.Any(entry => entry.Kind == BattleEventKind.ActionEnded), Is.True);
            Assert.That(session.Battle.Events.Where(entry => entry.Kind == BattleEventKind.Command).Select(entry => entry.AbilityId), Is.All.Zero);
            session.ReplayBattle();
        }
#endif
    }
}
