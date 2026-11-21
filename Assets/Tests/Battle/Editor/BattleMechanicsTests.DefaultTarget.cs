// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using Battle;
    using NUnit.Framework;

    public sealed partial class BattleMechanicsTests
    {
        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void FriendlyDefaultPrefersACurableAilmentThenMissingMpForPureMpMedicineThenHp(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[0].Vitals[0] = 125;
            scenario.Participants.Add(Participant("B", BattleSide.Ally, BattleSlot.Fire));
            scenario.Participants.Add(Participant("C", BattleSide.Ally, BattleSlot.Wind));
            scenario.Participants[2].Vitals[2] = scenario.Participants[3].Vitals[2] = 25;
            var medicine = Item();
            medicine.Attributes = new[] { new BattleAttributeEffect(2, BattleValueMode.RecoverToMax, 0) };
            medicine.States = new[] { new BattleStatusEffect(BattleState.Seal, true), new BattleStatusEffect(BattleState.Reflection, true) };
            scenario.Items.Add(new BattleItem { Id = medicine.Id, Count = 1 });

            string Focus()
            {
                BattleSession session = Session(scenario, new[] { medicine });
                return session.GetDefaultTarget(new BattleCommand("A", BattleAction.Item, abilityId: medicine.Id));
            }

            scenario.Participants[0].Statuses.Add(new BattleStatus { Id = (int)BattleState.Reflection, Rounds = 5 });
            scenario.Participants[3].Statuses.Add(new BattleStatus { Id = (int)BattleState.Seal, Rounds = 5 });
            Assert.That(Focus(), Is.EqualTo("C"));
            scenario.Participants[3].Statuses.Clear();
            Assert.That(Focus(), Is.EqualTo("B"), "An exact 25/100 MP tie prefers the earlier native slot.");
            scenario.Participants[2].Vitals[2] = scenario.Participants[3].Vitals[2] = 30;
            Assert.That(Focus(), Is.EqualTo("C"), "The stored float for 30/100 is larger than the next x87 division.");
            scenario.Participants[2].Vitals[2] = scenario.Participants[3].Vitals[2] = 100;
            Assert.That(Focus(), Is.EqualTo("A"), "When everyone's MP is full the HP rule takes over.");
            scenario.Participants[2].Vitals[2] = 1;
            medicine.Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, 20), new BattleAttributeEffect(2, BattleValueMode.Absolute, 20) };
            scenario.Participants[3].Vitals[0] = 125;
            Assert.That(Focus(), Is.EqualTo("C"), "Mixed medicine uses HP and the later native slot wins an exact tie.");
        }

        [TestCase("PAL3", "Hurt")]
        [TestCase("PAL3A", "Caster")]
        public void EnemySinglePrefersTheNextCasterThenFallsBackToHpIfThatRoleCannotBeSelected(string variant, string hiddenFocus)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants.Add(Participant("Caster", BattleSide.Enemy, BattleSlot.Fire));
            scenario.Participants.Add(Participant("Hurt", BattleSide.Enemy, BattleSlot.Wind, 20));
            Learn(scenario.Participants[0], 301);
            Learn(scenario.Participants[2], 302, 1);

            string Focus()
            {
                BattleSession session = Session(scenario, new[] { Magic(301), Magic(302) }, bothSidesReady: true);
                Assert.That(session.Submit(new BattleCommand("Caster", BattleAction.Skill, "A", 302)), Is.True);
                session.Step();
                return session.GetDefaultTarget(new BattleCommand("A", BattleAction.Skill, abilityId: 301));
            }

            Assert.That(Focus(), Is.EqualTo("Caster"));
            scenario.Participants[2].Statuses.Add(new BattleStatus { Id = (int)BattleState.Invisible, Rounds = 5 });
            Assert.That(Focus(), Is.EqualTo(hiddenFocus));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void EnemyDefaultUsesFirstGaugeSpeedBeforeAnyEnemyStartsCasting(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants.Add(Participant("Fast", BattleSide.Enemy, BattleSlot.Fire));
            scenario.Participants[2].Attributes[5] = 20;
            BattleSession session = Session(scenario, new[] { Magic() });
            Assert.That(session.GetDefaultTarget(new BattleCommand("A", BattleAction.Skill, abilityId: 301)), Is.EqualTo("Fast"));
        }

        [TestCase("PAL3", null, "A")]
        [TestCase("PAL3A", "E", null)]
        public void DefaultTargetRespectsWholeSideInvisibilityAndFriendlyTransformation(string variant, string opponent, string friend)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[1].Statuses.Add(new BattleStatus { Id = (int)BattleState.Invisible, Rounds = 5 });
            scenario.Pal3A.NangongStartsTransformed = variant == "PAL3A";
            BattleSession session = Session(scenario, new[] { Magic(), Magic(4401), Item() });
            Assert.That(session.GetDefaultTarget(new BattleCommand("A", BattleAction.Skill, abilityId: 301)), Is.EqualTo(opponent));
            Assert.That(session.GetDefaultTarget(new BattleCommand("A", BattleAction.Skill, abilityId: 4401)), Is.EqualTo("E"));
            Assert.That(session.GetDefaultTarget(new BattleCommand("A", BattleAction.Item, abilityId: 6201)), Is.EqualTo(friend));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void EnemyDefaultsUseNativeLineOrderingAndLowestHpForAnAllEnemyAction(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants.RemoveAt(1);
            for (int slot = 0; slot < 6; slot++)
                scenario.Participants.Add(Participant("E" + slot, BattleSide.Enemy, (BattleSlot)slot));
            scenario.Participants[1].Vitals[0] = 0;
            scenario.Participants[5].Vitals[0] = 10;
            string Focus(BattleTargetRange range)
            {
                BattleSession session = Session(scenario, new[] { Magic(range: range) });
                return session.GetDefaultTarget(new BattleCommand("A", BattleAction.Skill, abilityId: 301));
            }

            // Two front roles tie the two middle roles; the middle line wins and scans 5,10,7.
            Assert.That(Focus(BattleTargetRange.OpponentRow), Is.EqualTo("E5"));
            // Both later columns have two roles; the last column starts at native slot 7.
            Assert.That(Focus(BattleTargetRange.OpponentColumn), Is.EqualTo("E2"));
            Assert.That(Focus(BattleTargetRange.OpponentAll), Is.EqualTo("E4"));
        }
    }
}
