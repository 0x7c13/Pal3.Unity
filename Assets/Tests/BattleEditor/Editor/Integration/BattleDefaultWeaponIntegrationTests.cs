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
    using Core.Contract.Constants;
    using Game.Rendering.Renderer;
    using NUnit.Framework;

    [Category("LocalGameData")]
    public sealed class BattleDefaultWeaponIntegrationTests
    {
#if PAL3
        [Test]
        public void NativeFallbackSharesCapturedCultivationAttributesWithoutSharingSwordEnergy()
        {
            string root = Environment.GetEnvironmentVariable("PAL3_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Original game data is required.");
            using var session = new BattleEditorSession();
            session.Load(root, 936);
            BattleData data = session.Data;
            var scenario = new BattleScenario { Variant = "PAL3", Scene = data.Scenes.First(), DataFingerprint = data.Fingerprint };
            BattleParticipant jingtian = data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water);
            jingtian.Equipment[0] = 6603;
            jingtian.MagicSword = new BattleMagicSword { AttackBonus = 777, SelectedElement = BattleSwordPurificationElement.Earth, Energy = 123 };
            BattleParticipant xuejian = data.CreateParticipant(104, BattleSide.Ally, BattleSlot.Fire);
            xuejian.Equipment[0] = 0;
            xuejian.AttributeSource = BattleAttributeSource.BaseWithEquipment;
            scenario.Participants.Add(jingtian);
            scenario.Participants.Add(xuejian);
            scenario.Participants.Add(data.CreateParticipant(7311, BattleSide.Enemy, BattleSlot.Water));
            session.StartBattle(scenario);
            BattleActorDefinition definition = session.Battle.Definitions.Actors[xuejian.InstanceId];
            Assert.That(definition.MagicSword, Is.Null);
            BattleMagicSwordSnapshot cultivation = definition.Equipment.Single(item => item.Slot == 0).MagicSwordCultivation;
            Assert.That(cultivation.AttackBonus, Is.EqualTo(777));
            Assert.That(cultivation.SelectedElement, Is.EqualTo(BattleSwordPurificationElement.Earth));
            Assert.That(session.Battle.Actors.Single(actor => actor.Id == xuejian.InstanceId).Attributes[3], Is.EqualTo(xuejian.Attributes[3] + 777));
            Assert.That(session.Battle.MagicSwords.Keys, Is.EquivalentTo(new[] { jingtian.InstanceId }));
            jingtian.MagicSword.AttackBonus = 1;
            jingtian.MagicSword.Energy = 0;
            Assert.That(cultivation.AttackBonus, Is.EqualTo(777));
            Assert.That(session.Battle.MagicSwords[jingtian.InstanceId].Energy, Is.EqualTo(123));
            session.ReplayBattle();
        }
#endif

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void EveryPlayersEmptyWeaponUsesOneCapturedLoadoutForRulesBodiesAndPreview(int identity)
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Original game data is required.");
            using var session = new BattleEditorSession();
            session.Load(root, 936);
            BattleData data = session.Data;
            int actorId = ActorConstants.MainActorCombatActorIdMap.Single(pair => (int)pair.Key == identity).Value;
            Assert.That(BattleLoadoutRules.PlayerIdentity(BattleData.Variant, actorId), Is.EqualTo(identity));
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = data.Scenes.First(),
                DataFingerprint = data.Fingerprint,
                Encounter = BattleEncounter.AllyAmbush
            };
            BattleParticipant player = data.CreateParticipant(actorId, BattleSide.Ally, BattleSlot.Water);
            player.Equipment[0] = 0;
            player.MagicSword = null;
            player.AttributeSource = BattleAttributeSource.BaseWithEquipment;
            scenario.Participants.Add(player);
            scenario.Participants.Add(data.CreateParticipant(7311, BattleSide.Enemy, BattleSlot.Fire));
            session.StartBattle(scenario);
            BattleParticipant captured = session.CapturedScenario.Participants[0];
#if PAL3
            const int weapon = 6603;
#else
            int weapon = new[] { 82110, 82209, 82310, 82402, 82508 }[identity];
#endif
            Assert.That(captured.Equipment[0], Is.EqualTo(weapon));
            Assert.That(captured.UsesNativeDefaultWeapon, Is.True);
            Assert.That(session.Battle.Definitions.Actors[player.InstanceId].UnsupportedReason, Is.Empty);
            Assert.That(session.Battle.Definitions.Actors[player.InstanceId].Equipment.Select(item => item.UnsupportedReason), Is.All.Empty);
            Assert.That(session.Battle.Definitions.Actors[player.InstanceId].Equipment.Single(item => item.Slot == 0).Id, Is.EqualTo(weapon));
            Assert.That(session.Battle.MagicSwordAbilityId(player.InstanceId), Is.EqualTo(BattleData.Variant == "PAL3" && identity == 0 ? 2224 : 0));
            Assert.That(player.Equipment[0], Is.Zero);
            Assert.That(player.UsesNativeDefaultWeapon, Is.False);
            Assert.That(player.MagicSword, Is.Null);
            BattleScenario roundtrip = BattleScenarioJson.Deserialize(BattleScenarioJson.Serialize(session.CapturedScenario));
            Assert.That(roundtrip.Validate(), Is.Empty);
            Assert.That(roundtrip.Participants[0].UsesNativeDefaultWeapon, Is.True);
            Assert.That(roundtrip.Participants[1].MagicSword, Is.Null);
            var command = new BattleCommand(player.InstanceId, BattleAction.Attack, scenario.Participants[1].InstanceId);
            Assert.That(session.Battle.Submit(command), Is.True, session.Battle.CanSubmit(command));
            for (int tick = 0; tick < 2000 && !session.Battle.Events.Any(entry => entry.Kind == BattleEventKind.ActionEnded); tick++)
                session.StepBattle();
            Assert.That(session.Battle.Events.Any(entry => entry.Kind == BattleEventKind.ActionEnded), Is.True);
            Assert.That(session.Preview.View.Root.transform.Find(player.InstanceId).GetComponentsInChildren<PolyModelRenderer>(true), Is.Not.Empty);
            string weaponPath = FileConstants.GetWeaponModelFileVirtualPath(data.Database.GameItemInfos[weapon].ModelName);
            string weaponFolder = weaponPath.Substring(0, weaponPath.LastIndexOf('\\') + 1);
            Assert.That(session.Preview.View.Dependencies.Keys.Any(path => path.StartsWith(weaponFolder, StringComparison.OrdinalIgnoreCase)), Is.True);
            session.ReplayBattle();
            Assert.That(session.CapturedScenario.Participants[0].Equipment[0], Is.EqualTo(weapon));
        }
    }
}
