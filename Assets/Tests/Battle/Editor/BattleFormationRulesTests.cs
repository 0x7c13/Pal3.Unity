// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using NUnit.Framework;

    // The 232 expected outcomes were obtained by executing the two original EXEs.
    // Fixtures and image fingerprints remain in workspace/Analysis/Battle-Stage4.
    public sealed class BattleFormationRulesTests
    {
        private static readonly string[] ComboCases =
        {
            "base", "auxInvalid", "auxDeadZeroHp", "auxFled", "auxParalyzed", "auxTransformed", "sourceDead",
            "weaponZeroRequired", "wrongTrigger", "wrongLead", "missingRequiredState", "matchingRequiredState",
            "anyPoisonMissing", "anyPoisonPresent",
        };

        private static readonly string[] ProtectCases =
        {
            "base", "noSelection", "differentSelection", "notMiddle", "ownDead", "ownParalyzed", "targetDead",
            "ownBusy", "ownAway", "targetAway", "targetInvalid", "guardianInvalid",
        };

        private static readonly string[] LineupCases =
        {
            "base", "actor0Busy", "actor4Busy", "enemyBusy", "invalidActorBusy", "deadActorBusy",
        };

        public static IEnumerable<TestCaseData> NativeCases()
        {
            foreach (string variant in new[] { "PAL3", "PAL3A" })
            {
                string comboOutcomes = variant == "PAL3" ? "10111110000101" : "10111011000101";
                for (int index = 0; index < ComboCases.Length; index++)
                    yield return Case(variant, "combo", ComboCases[index], 0, 0, comboOutcomes[index] == '1');
                int[] nativePositionMasks = { 1, 2, 4, 8, 16, 32, 14, 19 };
                for (int required = 1; required <= 8; required++)
                {
                    for (int actual = 1; actual <= 6; actual++)
                        yield return Case(variant, "combo", "auxPosition", required, actual, (nativePositionMasks[required - 1] & (1 << (actual - 1))) != 0);
                }
                string[] hpOutcomes = { "000", "110", "110" };
                for (int own = 49; own <= 51; own++)
                {
                    for (int target = 9; target <= 11; target++)
                        yield return Case(variant, "protect", "hpBoundary", own, target, hpOutcomes[own - 49][target - 9] == '1');
                }
                const string protectOutcomes = "100001100010";
                for (int index = 0; index < ProtectCases.Length; index++)
                    yield return Case(variant, "protect", ProtectCases[index], 0, 0, protectOutcomes[index] == '1');
                const string lineupOutcomes = "100110";
                for (int index = 0; index < LineupCases.Length; index++)
                    yield return Case(variant, "lineup", LineupCases[index], 0, 0, lineupOutcomes[index] == '1');
                for (int state = 0; state <= 26; state++)
                    yield return Case(variant, "move", "state", state, 0, state is not (14 or 17 or 18 or 19));
            }
        }

        [TestCaseSource(nameof(NativeCases))]
        public void MatchesOriginalEligibility(string variant, string category, string name, int first, int second, bool expected)
        {
            var rules = new BattleFormationRules(variant);
            int[] ids = variant == "PAL3" ? new[] { 101, 104, 105, 107, 102 } : new[] { 101, 102, 103, 104, 105 };
            BattleFormationDecision decision;
            if (category == "combo")
            {
                var actors = Enumerable.Range(0, 5).Select(index => Actor(index, ids)).ToArray();
                bool dead = name == "auxDeadZeroHp";
                BattleState[] states = name is "auxParalyzed" or "matchingRequiredState" ? new[] { BattleState.Paralysis }
                    : name == "anyPoisonPresent" ? new[] { BattleState.PoisonWater } : Array.Empty<BattleState>();
                actors[1] = Actor(1, ids, valid: name != "auxInvalid", alive: !dead, hp: dead ? 0 : 100, states: states,
                    fled: name == "auxFled", transformed: name == "auxTransformed", slot: name == "auxPosition" ? (BattleSlot)(second - 1) : null);
                if (name == "sourceDead")
                    actors[0] = Actor(0, ids, alive: false, hp: 0);
                var requirement = new BattleComboRequirement(9001, name == "wrongTrigger" ? 43 : 42,
                    new[] { name == "wrongLead" ? ids[1] : ids[0], ids[1], 0, 0 },
                    new[] { 0, name == "auxPosition" ? first : 0, 0, 0 },
                    new[] { 2, name == "weaponZeroRequired" ? 0 : 3, 0, 0 },
                    new[] { name is "missingRequiredState" or "matchingRequiredState" ? 13
                        : name is "anyPoisonMissing" or "anyPoisonPresent" ? 26 : -1, -1, -1 });
                decision = rules.CanCoAttack(0, 42, requirement, actors);
            }
            else if (category == "protect")
            {
                bool hpCase = name == "hpBoundary";
                var guardian = Actor(0, ids, hp: hpCase ? first : 50, maxHp: hpCase ? 101 : 100,
                    valid: name != "guardianInvalid",
                    slot: name == "notMiddle" ? BattleSlot.Water : BattleSlot.Center,
                    alive: name != "ownDead", states: name == "ownParalyzed" ? new[] { BattleState.Paralysis } : null,
                    idle: name != "ownBusy", home: name != "ownAway");
                var target = Actor(1, ids, hp: name == "targetDead" ? 0 : hpCase ? second : 10,
                    valid: name != "targetInvalid",
                    maxHp: hpCase ? 109 : 100, alive: name != "targetDead", home: name != "targetAway");
                decision = rules.CanProtect(name == "noSelection" ? 0 : name == "differentSelection" ? 2 : 1, guardian, target);
            }
            else if (category == "lineup")
            {
                var actors = Enumerable.Range(0, 5).Select(index => Actor(index, ids)).ToList();
                if (name == "actor0Busy")
                    actors[0] = Actor(0, ids, tasks: 1);
                if (name == "actor4Busy")
                    actors[4] = Actor(4, ids, tasks: 2);
                if (name == "enemyBusy")
                    actors.Add(Actor(5, ids, tasks: 1));
                if (name == "invalidActorBusy")
                    actors[1] = Actor(1, ids, valid: false, tasks: 1);
                if (name == "deadActorBusy")
                    actors[1] = Actor(1, ids, alive: false, tasks: 1);
                decision = rules.CanChangeLineup(actors);
            }
            else
            {
                decision = rules.CanMove(Actor(0, ids, states: first == 0 ? null : new[] { (BattleState)(first - 1) }));
            }
            Assert.That(decision.Allowed, Is.EqualTo(expected));
            if (!expected)
                Assert.That(decision.FailureReason, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void SnapshotsCaptureCallerCollections()
        {
            var states = new[] { BattleState.Seal };
            var actor = new BattleFormationActor(0, 101, BattleSlot.Water, 2, states: states);
            var members = new[] { 101, 104, 0, 0 };
            var requirement = new BattleComboRequirement(9001, 42, members, new int[4], new int[4], new[] { -1, -1, -1 });
            states[0] = BattleState.Sleep;
            members[0] = 999;
            Assert.That(actor.States[0], Is.EqualTo(BattleState.Seal));
            Assert.That(requirement.Members[0], Is.EqualTo(101));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ZeroMaximumHpDoesNotAddALineupCondition(string variant)
        {
            var rules = new BattleFormationRules(variant);
            var deadActor = new BattleFormationActor(1, 104, BattleSlot.Fire, 0, alive: false, hp: 0, maxHp: 0);
            var absentActor = new BattleFormationActor(2, 105, BattleSlot.Wind, 0, valid: false, hp: 0, maxHp: 0, taskCount: 1);
            Assert.That(rules.CanChangeLineup(new[] { deadActor, absentActor }).Allowed, Is.True);
        }

        [TestCase("PAL3", 1, true)]
        [TestCase("PAL3", 2, true)]
        [TestCase("PAL3", 3, true)]
        [TestCase("PAL3", 4, false)]
        [TestCase("PAL3A", 1, true)]
        [TestCase("PAL3A", 2, true)]
        [TestCase("PAL3A", 3, false)]
        [TestCase("PAL3A", 4, false)]
        public void ProtectPanelExposesOriginalTargets(string variant, int targetIndex, bool expected)
        {
            var rules = new BattleFormationRules(variant);
            var guardian = new BattleFormationActor(0, 101, BattleSlot.Center, 0);
            var target = new BattleFormationActor(targetIndex, 102, BattleSlot.Fire, 0);
            Assert.That(rules.CanSetProtect(guardian, target).Allowed, Is.EqualTo(expected));
            target = new BattleFormationActor(targetIndex, 102, BattleSlot.Fire, 0, alive: false);
            Assert.That(rules.CanSetProtect(guardian, target).Allowed, Is.False);
            target = new BattleFormationActor(targetIndex, 102, BattleSlot.Fire, 0, valid: false);
            Assert.That(rules.CanSetProtect(guardian, target).Allowed, Is.False);
        }

        private static TestCaseData Case(string variant, string category, string name, int first, int second, bool expected)
            => new TestCaseData(variant, category, name, first, second, expected).SetName($"{variant}_{category}_{name}_{first}_{second}");

        private static BattleFormationActor Actor(int index, IReadOnlyList<int> ids, bool valid = true, bool alive = true,
            int hp = 100, int maxHp = 100, IEnumerable<BattleState> states = null, bool fled = false,
            bool transformed = false, BattleSlot? slot = null, bool idle = true, bool home = true, int tasks = 0)
            => new(index, index < 5 ? ids[index] : 701, slot ?? (index < 5 ? (BattleSlot)index : BattleSlot.Center), index + 2,
                valid, alive, hp, maxHp, states, tasks, home, idle, transformed, fled);
    }
}
