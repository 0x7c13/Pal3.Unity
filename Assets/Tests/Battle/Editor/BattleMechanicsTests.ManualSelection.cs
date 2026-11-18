// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System.Linq;
    using Battle;
    using NUnit.Framework;

    public sealed partial class BattleMechanicsTests
    {
        [TestCase("PAL3", false)]
        [TestCase("PAL3A", true)]
        public void ManualSelectionChecksInvisibleOpponentsByVariantAndCommandId(string variant, bool ordinaryAllowed)
        {
            BattleScenario scenario = Scenario(variant);
            foreach (BattleParticipant actor in scenario.Participants)
                actor.Statuses.Add(new BattleStatus { Id = (int)BattleState.Invisible, Rounds = 5 });
            BattleAbilityData poison = Item(6301, -20);
            poison.Range = BattleTargetRange.OpponentSingle;
            var medicine = Item();
            scenario.Items.Add(new BattleItem { Id = poison.Id, Count = 1 });
            scenario.Items.Add(new BattleItem { Id = medicine.Id, Count = 1 });
            Learn(scenario.Participants[0], 301);
            Learn(scenario.Participants[0], 4401);
            var special = new BattleAbilityData { Id = 4401, SkillTypeId = 0, Range = BattleTargetRange.OpponentSingle };
            if (variant == "PAL3")
                special.States = new[] { new BattleStatusEffect(BattleState.Invisible, true) };
            else
                special.Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -180) };
            BattleSession session = Session(scenario, new[] { Magic(), special, poison, medicine });

            foreach (BattleCommand command in new[]
            {
                new BattleCommand("A", BattleAction.Attack, "E"),
                new BattleCommand("A", BattleAction.Skill, "E", 301),
                new BattleCommand("A", BattleAction.Item, "E", poison.Id)
            })
            {
                Assert.That(session.GetTargetCandidates(command).Any(target => target.Id == "E"), Is.EqualTo(ordinaryAllowed));
                Assert.That(session.CanSubmit(command) == null, Is.EqualTo(ordinaryAllowed));
            }

            var heal = new BattleCommand("A", BattleAction.Item, "A", medicine.Id);
            Assert.That(session.GetTargetCandidates(heal).Single().Id, Is.EqualTo("A"));
            Assert.That(session.CanSubmit(heal), Is.Null);
            var skill4401 = new BattleCommand("A", BattleAction.Skill, "E", 4401);
            Assert.That(session.GetTargetCandidates(skill4401).Single().Id, Is.EqualTo("E"));
            Assert.That(session.Submit(skill4401), Is.True);
            Steps(session, 2);
            Assert.That(Actor(session, "E").States.Any(state => state.State == BattleState.Invisible), Is.EqualTo(variant == "PAL3A"));
            Assert.That(session.GetTargetCandidates(new BattleCommand("A", BattleAction.Attack)).Single().Id, Is.EqualTo("E"));
        }

        [TestCase(BattleTargetRange.OpponentRow, BattleSlot.Wind)]
        [TestCase(BattleTargetRange.OpponentColumn, BattleSlot.Thunder)]
        [TestCase(BattleTargetRange.OpponentAll, BattleSlot.Fire)]
        public void ManualPal3AreaSpellSkipsInvisibleMembersOfTheSelectedArea(BattleTargetRange range, BattleSlot alignedSlot)
        {
            BattleScenario scenario = Scenario();
            BattleParticipant hidden = Participant("Hidden", BattleSide.Enemy, alignedSlot);
            hidden.Statuses.Add(new BattleStatus { Id = (int)BattleState.Invisible, Rounds = 5 });
            scenario.Participants.Add(hidden);
            Learn(scenario.Participants[0], 301);
            var spell = Magic(range: range);
            // SP loss exposes the captured targets even though Hermit rejects HP changes.
            spell.Attributes = new[] { new BattleAttributeEffect(1, BattleValueMode.Absolute, -20) };
            BattleSession session = Session(scenario, new[] { spell });
            var command = new BattleCommand("A", BattleAction.Skill, "E", 301);
            Assert.That(session.GetTargetCandidates(command).Select(actor => actor.Id), Is.EqualTo(new[] { "E" }));
            Assert.That(session.GetPotentialAffectedTargets(command).Select(actor => actor.Id), Is.EqualTo(new[] { "E" }));
            Assert.That(session.Submit(command), Is.True);
            Steps(session, 2);
            Assert.That(Actor(session, "E").Sp, Is.LessThan(100));
            Assert.That(Actor(session, "Hidden").Sp, Is.EqualTo(100));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ManualPal3AFriendlyItemsFollowBothTransformationFlags(bool transformed)
        {
            BattleSession session = FriendlyFormSession(transformed, BattleTargetRange.OwnAll, true);
            string[] expected = transformed ? new[] { "A" } : new[] { "N", "A", "P" };
            var single = new BattleCommand("A", BattleAction.Item, "N", 6201);
            var group = new BattleCommand("A", BattleAction.Item, abilityId: 6202);
            Assert.That(session.GetTargetCandidates(single).Select(actor => actor.Id), Is.EqualTo(expected));
            Assert.That(session.CanSubmit(single) == null, Is.EqualTo(!transformed));
            Assert.That(session.CanSubmit(new BattleCommand("A", BattleAction.Item, "P", 6201)) == null, Is.EqualTo(!transformed));
            Assert.That(session.GetPotentialAffectedTargets(group).Select(actor => actor.Id), Is.EqualTo(expected));
            Assert.That(session.Submit(group), Is.True);
            Steps(session, 2);
            Assert.That(session.Events.Where(entry => entry.Source == "A" && entry.Resource == 0 && entry.RequestedDelta > 0)
                .Select(entry => entry.Target), Is.EquivalentTo(expected));
        }

        [TestCase(BattleTargetRange.OwnSingle)]
        [TestCase(BattleTargetRange.OwnAll)]
        public void Pal3ARecoveryAiCanHealTransformedNangong(BattleTargetRange range)
        {
            BattleSession session = FriendlyFormSession(true, range, false);
            session.SetAiProfile("A", 1 | 8 | 16 | 32);
            Assert.That(session.Submit("N", BattleAction.Defend), Is.True);
            Assert.That(session.Submit("P", BattleAction.Defend), Is.True);
            session.Step();
            Assert.That(session.Inputs.Single(input => input.Kind == BattleInputKind.Command && input.Actor == "A").AbilityId, Is.EqualTo(6202));
            Steps(session, 4);
            Assert.That(Actor(session, "N").Hp, Is.GreaterThan(10));
            if (range == BattleTargetRange.OwnAll)
                Assert.That(session.Events.Any(entry => entry.Source == "A" && entry.Target == "P" && entry.RequestedDelta > 0), Is.True);
        }

        private static BattleSession FriendlyFormSession(bool transformed, BattleTargetRange recoveryRange, bool includeSingle)
        {
            BattleScenario scenario = Scenario("PAL3A");
            scenario.Seed = 107;
            scenario.Pal3A.NangongStartsTransformed = transformed;
            scenario.Pal3A.PengxuStartsTransformed = transformed;
            scenario.Participants[0].ActorId = 102;
            scenario.Participants[0].Vitals[0] = 10;
            scenario.Participants.Add(Participant("N", BattleSide.Ally, BattleSlot.Fire, 10));
            BattleParticipant pengxu = Participant("P", BattleSide.Ally, BattleSlot.Wind, 10);
            pengxu.ActorId = 103;
            scenario.Participants.Add(pengxu);
            var recovery = Item(6202);
            recovery.Range = recoveryRange;
            scenario.Items.Add(new BattleItem { Id = recovery.Id, Count = 1 });
            if (includeSingle)
                scenario.Items.Add(new BattleItem { Id = 6201, Count = 1 });
            BattleSession session = Session(scenario, new[] { Item(), recovery }, new[]
            {
                new BattleActorData { InstanceId = "N", Identity = 0 },
                new BattleActorData { InstanceId = "A", Identity = 1 },
                new BattleActorData { InstanceId = "P", Identity = 2 }
            });
            session.SetAutomatic(BattleSide.Enemy, false);
            return session;
        }
    }
}
