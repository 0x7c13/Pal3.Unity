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
    using NUnit.Framework;

    [Category("LocalGameData")]
    public sealed class BattleElementWheelIntegrationTests
    {
        private BattleEditorSession _session;

        [SetUp]
        public void LoadData()
        {
            string root = Environment.GetEnvironmentVariable("PAL3A_TEST_DATA_ROOT");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                Assert.Ignore("Original PAL3A data is required for the wheel presentation tests.");
            _session = new BattleEditorSession();
            _session.Load(root, 936);
        }

        [TearDown]
        public void Dispose() => _session?.Dispose();

        [TestCase(264, 0, 1)]
        [TestCase(265, 1, 2)]
        [TestCase(266, 3, 4)]
        [TestCase(267, 2, 3)]
        [TestCase(268, 4, 5)]
        public void ActualWheelUsesZ24AndBothLinkerGroupsWithoutMagicCostsAndReplays(int skillId, int coreElement, int field)
        {
            BattleData data = _session.Data;
            var scenario = new BattleScenario
            {
                Variant = "PAL3A",
                Scene = data.Scenes.First(),
                DataFingerprint = data.Fingerprint,
                Encounter = BattleEncounter.AllyAmbush
            };
            scenario.Pal3A.SpiritWheel.Charges = Enumerable.Repeat(9, 5).ToArray();
            scenario.Participants.Add(data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            scenario.Participants.Add(data.CreateParticipant(7311, BattleSide.Enemy, BattleSlot.Water));
            foreach (BattleParticipant actor in scenario.Participants)
            {
                actor.Attributes = new[] { 5000, 100, 1000, 50, 50, 5, 0, 0, 0, 0, 0, 0 };
                actor.Vitals = new[] { 5000, 100, 0 };
                actor.Skills.Clear();
            }
            scenario.Participants[0].Statuses.Add(new BattleStatus { Id = (int)BattleState.Seal, Rounds = 20 });
            _session.StartBattle(scenario);
            BattleSession combat = _session.Battle;
            string source = scenario.Participants[0].InstanceId;
            var catalog = new BattleAnimationCatalog(data, new BattleSetup(scenario), combat.Definitions);
            BattleAbilityDefinition ability = combat.Definitions.Skills[skillId];
            Assert.That((ability.SkillTypeId, ability.Range, ability.IsMagic), Is.EqualTo((0, BattleTargetRange.None, true)));
            Assert.That(ability.Costs.Single(cost => cost.Resource == 2).Value, Is.EqualTo(5));
            Assert.That(catalog.AbilityStage(source, ability).Kind, Is.EqualTo(BattleAbilityStageKind.Magic), "AI keeps the ordinary magic stage.");
            BattleAbilityStageProfile wheel = catalog.ElementWheelStage(source, ability);
            Assert.That(wheel.Kind, Is.EqualTo(BattleAbilityStageKind.ElementWheel));
            Assert.That(catalog.ElementWheelBody(source, ability).Clip, Is.EqualTo("z24"));
            Assert.That(combat.CanSubmitElementWheel(source, skillId), Is.Null);
            Assert.That(combat.SubmitElementWheel(source, skillId), Is.True);
            for (int tick = 0; tick < 1500 && combat.Status == BattleSessionStatus.Running && combat.Actors.Single(actor => actor.Id == source).ActionId != 0; tick++)
                _session.StepBattle();
            Assert.That(combat.Status, Is.EqualTo(BattleSessionStatus.Running), combat.Result?.Reason);
            Assert.That(combat.Actors.Single(actor => actor.Id == source).ActionId, Is.Zero);
            Assert.That((combat.FiveSpiritElement, combat.FiveSpiritForce), Is.EqualTo((field, 1)));
            int[] expected = Enumerable.Repeat(9, 5).ToArray();
            expected[coreElement] = 0;
            Assert.That(combat.Pal3AState.SpiritWheel.Charges, Is.EqualTo(expected));
            Assert.That(combat.Actors.Single(actor => actor.Id == source).Mp, Is.Zero);
            Assert.That(combat.Actors.Single(actor => actor.Id == source).Skills, Is.Empty);
            Assert.That(combat.Events.Any(entry => entry.Detail.StartsWith("skill cost", StringComparison.Ordinal)), Is.False);
            var events = combat.Events.Select(entry => (entry.Tick, entry.Kind, entry.Source, entry.Target, entry.RandomState, entry.Detail)).ToArray();
            _session.ReplayBattle();
            Assert.That(_session.Battle.Events.Select(entry => (entry.Tick, entry.Kind, entry.Source, entry.Target, entry.RandomState, entry.Detail)), Is.EqualTo(events));
            Assert.That(_session.Battle.Pal3AState.SpiritWheel.Charges, Is.EqualTo(expected));
        }
    }
}
#endif
