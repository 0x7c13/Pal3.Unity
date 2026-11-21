// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Pal3.Battle;

    public sealed class BattleCoAttackTargetTests
    {
        // Independent outputs execute original GetCoAttackRange in both EXEs.
        public static IEnumerable<TestCaseData> NativeCases()
        {
            yield return new TestCaseData("PAL3", 0, -1, "base", false, -1, "00000010100").SetName("PAL3_CoAttackTargetNative_0");
            yield return new TestCaseData("PAL3", 0, -1, "mixedDeadFledInvalid", false, -1, "00000010100").SetName("PAL3_CoAttackTargetNative_1");
            yield return new TestCaseData("PAL3", 0, -1, "bossFirst", false, -1, "00000010100").SetName("PAL3_CoAttackTargetNative_2");
            yield return new TestCaseData("PAL3", 0, -1, "bossDead", false, -1, "00000010100").SetName("PAL3_CoAttackTargetNative_3");
            yield return new TestCaseData("PAL3", 0, -1, "highestTie", false, -1, "00000010100").SetName("PAL3_CoAttackTargetNative_4");
            yield return new TestCaseData("PAL3", 0, 1, "base", false, 1, "00000010100").SetName("PAL3_CoAttackTargetNative_5");
            yield return new TestCaseData("PAL3", 0, 1, "mixedDeadFledInvalid", false, 1, "00000010100").SetName("PAL3_CoAttackTargetNative_6");
            yield return new TestCaseData("PAL3", 0, 1, "bossFirst", false, 1, "00000010100").SetName("PAL3_CoAttackTargetNative_7");
            yield return new TestCaseData("PAL3", 0, 1, "bossDead", false, 1, "00000010100").SetName("PAL3_CoAttackTargetNative_8");
            yield return new TestCaseData("PAL3", 0, 1, "highestTie", false, 1, "00000010100").SetName("PAL3_CoAttackTargetNative_9");
            yield return new TestCaseData("PAL3", 0, 5, "base", false, 5, "00000010100").SetName("PAL3_CoAttackTargetNative_10");
            yield return new TestCaseData("PAL3", 0, 5, "mixedDeadFledInvalid", false, 5, "00000010100").SetName("PAL3_CoAttackTargetNative_11");
            yield return new TestCaseData("PAL3", 0, 5, "bossFirst", false, 5, "00000010100").SetName("PAL3_CoAttackTargetNative_12");
            yield return new TestCaseData("PAL3", 0, 5, "bossDead", false, 5, "00000010100").SetName("PAL3_CoAttackTargetNative_13");
            yield return new TestCaseData("PAL3", 0, 5, "highestTie", false, 5, "00000010100").SetName("PAL3_CoAttackTargetNative_14");
            yield return new TestCaseData("PAL3", 1, -1, "base", true, -1, "11111000000").SetName("PAL3_CoAttackTargetNative_15");
            yield return new TestCaseData("PAL3", 1, -1, "mixedDeadFledInvalid", true, -1, "11001000000").SetName("PAL3_CoAttackTargetNative_16");
            yield return new TestCaseData("PAL3", 1, -1, "bossFirst", true, -1, "11111000000").SetName("PAL3_CoAttackTargetNative_17");
            yield return new TestCaseData("PAL3", 1, -1, "bossDead", true, -1, "11111000000").SetName("PAL3_CoAttackTargetNative_18");
            yield return new TestCaseData("PAL3", 1, -1, "highestTie", true, -1, "11111000000").SetName("PAL3_CoAttackTargetNative_19");
            yield return new TestCaseData("PAL3", 1, 1, "base", true, -1, "11111000000").SetName("PAL3_CoAttackTargetNative_20");
            yield return new TestCaseData("PAL3", 1, 1, "mixedDeadFledInvalid", true, -1, "11001000000").SetName("PAL3_CoAttackTargetNative_21");
            yield return new TestCaseData("PAL3", 1, 1, "bossFirst", true, -1, "11111000000").SetName("PAL3_CoAttackTargetNative_22");
            yield return new TestCaseData("PAL3", 1, 1, "bossDead", true, -1, "11111000000").SetName("PAL3_CoAttackTargetNative_23");
            yield return new TestCaseData("PAL3", 1, 1, "highestTie", true, -1, "11111000000").SetName("PAL3_CoAttackTargetNative_24");
            yield return new TestCaseData("PAL3", 1, 5, "base", true, -1, "11111000000").SetName("PAL3_CoAttackTargetNative_25");
            yield return new TestCaseData("PAL3", 1, 5, "mixedDeadFledInvalid", true, -1, "11001000000").SetName("PAL3_CoAttackTargetNative_26");
            yield return new TestCaseData("PAL3", 1, 5, "bossFirst", true, -1, "11111000000").SetName("PAL3_CoAttackTargetNative_27");
            yield return new TestCaseData("PAL3", 1, 5, "bossDead", true, -1, "11111000000").SetName("PAL3_CoAttackTargetNative_28");
            yield return new TestCaseData("PAL3", 1, 5, "highestTie", true, -1, "11111000000").SetName("PAL3_CoAttackTargetNative_29");
            yield return new TestCaseData("PAL3", 2, -1, "base", false, 6, "00000010100").SetName("PAL3_CoAttackTargetNative_30");
            yield return new TestCaseData("PAL3", 2, -1, "mixedDeadFledInvalid", false, 6, "00000010100").SetName("PAL3_CoAttackTargetNative_31");
            yield return new TestCaseData("PAL3", 2, -1, "bossFirst", false, 6, "00000010100").SetName("PAL3_CoAttackTargetNative_32");
            yield return new TestCaseData("PAL3", 2, -1, "bossDead", false, 6, "00000010100").SetName("PAL3_CoAttackTargetNative_33");
            yield return new TestCaseData("PAL3", 2, -1, "highestTie", false, 6, "00000010100").SetName("PAL3_CoAttackTargetNative_34");
            yield return new TestCaseData("PAL3", 2, 1, "base", false, 10, "00000010100").SetName("PAL3_CoAttackTargetNative_35");
            yield return new TestCaseData("PAL3", 2, 1, "mixedDeadFledInvalid", false, 10, "00000010100").SetName("PAL3_CoAttackTargetNative_36");
            yield return new TestCaseData("PAL3", 2, 1, "bossFirst", false, 6, "00000010100").SetName("PAL3_CoAttackTargetNative_37");
            yield return new TestCaseData("PAL3", 2, 1, "bossDead", false, 8, "00000010100").SetName("PAL3_CoAttackTargetNative_38");
            yield return new TestCaseData("PAL3", 2, 1, "highestTie", false, 6, "00000010100").SetName("PAL3_CoAttackTargetNative_39");
            yield return new TestCaseData("PAL3", 2, 5, "base", false, 5, "00000010100").SetName("PAL3_CoAttackTargetNative_40");
            yield return new TestCaseData("PAL3", 2, 5, "mixedDeadFledInvalid", false, 5, "00000010100").SetName("PAL3_CoAttackTargetNative_41");
            yield return new TestCaseData("PAL3", 2, 5, "bossFirst", false, 5, "00000010100").SetName("PAL3_CoAttackTargetNative_42");
            yield return new TestCaseData("PAL3", 2, 5, "bossDead", false, 5, "00000010100").SetName("PAL3_CoAttackTargetNative_43");
            yield return new TestCaseData("PAL3", 2, 5, "highestTie", false, 5, "00000010100").SetName("PAL3_CoAttackTargetNative_44");
            yield return new TestCaseData("PAL3", 3, -1, "base", true, -1, "00000111111").SetName("PAL3_CoAttackTargetNative_45");
            yield return new TestCaseData("PAL3", 3, -1, "mixedDeadFledInvalid", true, -1, "00000100111").SetName("PAL3_CoAttackTargetNative_46");
            yield return new TestCaseData("PAL3", 3, -1, "bossFirst", true, -1, "00000111111").SetName("PAL3_CoAttackTargetNative_47");
            yield return new TestCaseData("PAL3", 3, -1, "bossDead", true, -1, "00000111111").SetName("PAL3_CoAttackTargetNative_48");
            yield return new TestCaseData("PAL3", 3, -1, "highestTie", true, -1, "00000111111").SetName("PAL3_CoAttackTargetNative_49");
            yield return new TestCaseData("PAL3", 3, 1, "base", true, -1, "00000111111").SetName("PAL3_CoAttackTargetNative_50");
            yield return new TestCaseData("PAL3", 3, 1, "mixedDeadFledInvalid", true, -1, "00000100111").SetName("PAL3_CoAttackTargetNative_51");
            yield return new TestCaseData("PAL3", 3, 1, "bossFirst", true, -1, "00000111111").SetName("PAL3_CoAttackTargetNative_52");
            yield return new TestCaseData("PAL3", 3, 1, "bossDead", true, -1, "00000111111").SetName("PAL3_CoAttackTargetNative_53");
            yield return new TestCaseData("PAL3", 3, 1, "highestTie", true, -1, "00000111111").SetName("PAL3_CoAttackTargetNative_54");
            yield return new TestCaseData("PAL3", 3, 5, "base", true, -1, "00000111111").SetName("PAL3_CoAttackTargetNative_55");
            yield return new TestCaseData("PAL3", 3, 5, "mixedDeadFledInvalid", true, -1, "00000100111").SetName("PAL3_CoAttackTargetNative_56");
            yield return new TestCaseData("PAL3", 3, 5, "bossFirst", true, -1, "00000111111").SetName("PAL3_CoAttackTargetNative_57");
            yield return new TestCaseData("PAL3", 3, 5, "bossDead", true, -1, "00000111111").SetName("PAL3_CoAttackTargetNative_58");
            yield return new TestCaseData("PAL3", 3, 5, "highestTie", true, -1, "00000111111").SetName("PAL3_CoAttackTargetNative_59");
            yield return new TestCaseData("PAL3A", 0, -1, "base", false, -1, "00000010100").SetName("PAL3A_CoAttackTargetNative_0");
            yield return new TestCaseData("PAL3A", 0, -1, "mixedDeadFledInvalid", false, -1, "00000010100").SetName("PAL3A_CoAttackTargetNative_1");
            yield return new TestCaseData("PAL3A", 0, -1, "bossFirst", false, -1, "00000010100").SetName("PAL3A_CoAttackTargetNative_2");
            yield return new TestCaseData("PAL3A", 0, -1, "bossDead", false, -1, "00000010100").SetName("PAL3A_CoAttackTargetNative_3");
            yield return new TestCaseData("PAL3A", 0, -1, "highestTie", false, -1, "00000010100").SetName("PAL3A_CoAttackTargetNative_4");
            yield return new TestCaseData("PAL3A", 0, 1, "base", false, 1, "00000010100").SetName("PAL3A_CoAttackTargetNative_5");
            yield return new TestCaseData("PAL3A", 0, 1, "mixedDeadFledInvalid", false, 1, "00000010100").SetName("PAL3A_CoAttackTargetNative_6");
            yield return new TestCaseData("PAL3A", 0, 1, "bossFirst", false, 1, "00000010100").SetName("PAL3A_CoAttackTargetNative_7");
            yield return new TestCaseData("PAL3A", 0, 1, "bossDead", false, 1, "00000010100").SetName("PAL3A_CoAttackTargetNative_8");
            yield return new TestCaseData("PAL3A", 0, 1, "highestTie", false, 1, "00000010100").SetName("PAL3A_CoAttackTargetNative_9");
            yield return new TestCaseData("PAL3A", 0, 5, "base", false, 5, "00000010100").SetName("PAL3A_CoAttackTargetNative_10");
            yield return new TestCaseData("PAL3A", 0, 5, "mixedDeadFledInvalid", false, 5, "00000010100").SetName("PAL3A_CoAttackTargetNative_11");
            yield return new TestCaseData("PAL3A", 0, 5, "bossFirst", false, 5, "00000010100").SetName("PAL3A_CoAttackTargetNative_12");
            yield return new TestCaseData("PAL3A", 0, 5, "bossDead", false, 5, "00000010100").SetName("PAL3A_CoAttackTargetNative_13");
            yield return new TestCaseData("PAL3A", 0, 5, "highestTie", false, 5, "00000010100").SetName("PAL3A_CoAttackTargetNative_14");
            yield return new TestCaseData("PAL3A", 1, -1, "base", true, -1, "11111000000").SetName("PAL3A_CoAttackTargetNative_15");
            yield return new TestCaseData("PAL3A", 1, -1, "mixedDeadFledInvalid", true, -1, "11001000000").SetName("PAL3A_CoAttackTargetNative_16");
            yield return new TestCaseData("PAL3A", 1, -1, "bossFirst", true, -1, "11111000000").SetName("PAL3A_CoAttackTargetNative_17");
            yield return new TestCaseData("PAL3A", 1, -1, "bossDead", true, -1, "11111000000").SetName("PAL3A_CoAttackTargetNative_18");
            yield return new TestCaseData("PAL3A", 1, -1, "highestTie", true, -1, "11111000000").SetName("PAL3A_CoAttackTargetNative_19");
            yield return new TestCaseData("PAL3A", 1, 1, "base", true, -1, "11111000000").SetName("PAL3A_CoAttackTargetNative_20");
            yield return new TestCaseData("PAL3A", 1, 1, "mixedDeadFledInvalid", true, -1, "11001000000").SetName("PAL3A_CoAttackTargetNative_21");
            yield return new TestCaseData("PAL3A", 1, 1, "bossFirst", true, -1, "11111000000").SetName("PAL3A_CoAttackTargetNative_22");
            yield return new TestCaseData("PAL3A", 1, 1, "bossDead", true, -1, "11111000000").SetName("PAL3A_CoAttackTargetNative_23");
            yield return new TestCaseData("PAL3A", 1, 1, "highestTie", true, -1, "11111000000").SetName("PAL3A_CoAttackTargetNative_24");
            yield return new TestCaseData("PAL3A", 1, 5, "base", true, -1, "11111000000").SetName("PAL3A_CoAttackTargetNative_25");
            yield return new TestCaseData("PAL3A", 1, 5, "mixedDeadFledInvalid", true, -1, "11001000000").SetName("PAL3A_CoAttackTargetNative_26");
            yield return new TestCaseData("PAL3A", 1, 5, "bossFirst", true, -1, "11111000000").SetName("PAL3A_CoAttackTargetNative_27");
            yield return new TestCaseData("PAL3A", 1, 5, "bossDead", true, -1, "11111000000").SetName("PAL3A_CoAttackTargetNative_28");
            yield return new TestCaseData("PAL3A", 1, 5, "highestTie", true, -1, "11111000000").SetName("PAL3A_CoAttackTargetNative_29");
            yield return new TestCaseData("PAL3A", 2, -1, "base", false, 6, "00000010100").SetName("PAL3A_CoAttackTargetNative_30");
            yield return new TestCaseData("PAL3A", 2, -1, "mixedDeadFledInvalid", false, 6, "00000010100").SetName("PAL3A_CoAttackTargetNative_31");
            yield return new TestCaseData("PAL3A", 2, -1, "bossFirst", false, 6, "00000010100").SetName("PAL3A_CoAttackTargetNative_32");
            yield return new TestCaseData("PAL3A", 2, -1, "bossDead", false, 6, "00000010100").SetName("PAL3A_CoAttackTargetNative_33");
            yield return new TestCaseData("PAL3A", 2, -1, "highestTie", false, 6, "00000010100").SetName("PAL3A_CoAttackTargetNative_34");
            yield return new TestCaseData("PAL3A", 2, 1, "base", false, 10, "00000010100").SetName("PAL3A_CoAttackTargetNative_35");
            yield return new TestCaseData("PAL3A", 2, 1, "mixedDeadFledInvalid", false, 10, "00000010100").SetName("PAL3A_CoAttackTargetNative_36");
            yield return new TestCaseData("PAL3A", 2, 1, "bossFirst", false, 6, "00000010100").SetName("PAL3A_CoAttackTargetNative_37");
            yield return new TestCaseData("PAL3A", 2, 1, "bossDead", false, 8, "00000010100").SetName("PAL3A_CoAttackTargetNative_38");
            yield return new TestCaseData("PAL3A", 2, 1, "highestTie", false, 6, "00000010100").SetName("PAL3A_CoAttackTargetNative_39");
            yield return new TestCaseData("PAL3A", 2, 5, "base", false, 5, "00000010100").SetName("PAL3A_CoAttackTargetNative_40");
            yield return new TestCaseData("PAL3A", 2, 5, "mixedDeadFledInvalid", false, 5, "00000010100").SetName("PAL3A_CoAttackTargetNative_41");
            yield return new TestCaseData("PAL3A", 2, 5, "bossFirst", false, 5, "00000010100").SetName("PAL3A_CoAttackTargetNative_42");
            yield return new TestCaseData("PAL3A", 2, 5, "bossDead", false, 5, "00000010100").SetName("PAL3A_CoAttackTargetNative_43");
            yield return new TestCaseData("PAL3A", 2, 5, "highestTie", false, 5, "00000010100").SetName("PAL3A_CoAttackTargetNative_44");
            yield return new TestCaseData("PAL3A", 3, -1, "base", true, -1, "00000111111").SetName("PAL3A_CoAttackTargetNative_45");
            yield return new TestCaseData("PAL3A", 3, -1, "mixedDeadFledInvalid", true, -1, "00000100111").SetName("PAL3A_CoAttackTargetNative_46");
            yield return new TestCaseData("PAL3A", 3, -1, "bossFirst", true, -1, "00000111111").SetName("PAL3A_CoAttackTargetNative_47");
            yield return new TestCaseData("PAL3A", 3, -1, "bossDead", true, -1, "00000111111").SetName("PAL3A_CoAttackTargetNative_48");
            yield return new TestCaseData("PAL3A", 3, -1, "highestTie", true, -1, "00000111111").SetName("PAL3A_CoAttackTargetNative_49");
            yield return new TestCaseData("PAL3A", 3, 1, "base", true, -1, "00000111111").SetName("PAL3A_CoAttackTargetNative_50");
            yield return new TestCaseData("PAL3A", 3, 1, "mixedDeadFledInvalid", true, -1, "00000100111").SetName("PAL3A_CoAttackTargetNative_51");
            yield return new TestCaseData("PAL3A", 3, 1, "bossFirst", true, -1, "00000111111").SetName("PAL3A_CoAttackTargetNative_52");
            yield return new TestCaseData("PAL3A", 3, 1, "bossDead", true, -1, "00000111111").SetName("PAL3A_CoAttackTargetNative_53");
            yield return new TestCaseData("PAL3A", 3, 1, "highestTie", true, -1, "00000111111").SetName("PAL3A_CoAttackTargetNative_54");
            yield return new TestCaseData("PAL3A", 3, 5, "base", true, -1, "00000111111").SetName("PAL3A_CoAttackTargetNative_55");
            yield return new TestCaseData("PAL3A", 3, 5, "mixedDeadFledInvalid", true, -1, "00000100111").SetName("PAL3A_CoAttackTargetNative_56");
            yield return new TestCaseData("PAL3A", 3, 5, "bossFirst", true, -1, "00000111111").SetName("PAL3A_CoAttackTargetNative_57");
            yield return new TestCaseData("PAL3A", 3, 5, "bossDead", true, -1, "00000111111").SetName("PAL3A_CoAttackTargetNative_58");
            yield return new TestCaseData("PAL3A", 3, 5, "highestTie", true, -1, "00000111111").SetName("PAL3A_CoAttackTargetNative_59");
            yield return new TestCaseData("PAL3", 0, -1, "emptyMask", false, -1, "00000000000").SetName("PAL3_CoAttackTargetEdge_0");
            yield return new TestCaseData("PAL3", 1, 1, "allInvalid", true, -1, "00000000000").SetName("PAL3_CoAttackTargetEdge_1");
            yield return new TestCaseData("PAL3", 2, -1, "emptyMask", false, -1, "00000000000").SetName("PAL3_CoAttackTargetEdge_2");
            yield return new TestCaseData("PAL3", 2, 1, "noLivingEnemies", false, 0, "00000010100").SetName("PAL3_CoAttackTargetEdge_3");
            yield return new TestCaseData("PAL3", 2, 1, "nonpositiveHp", false, 0, "00000010100").SetName("PAL3_CoAttackTargetEdge_4");
            yield return new TestCaseData("PAL3", 3, 1, "allInvalid", true, -1, "00000000000").SetName("PAL3_CoAttackTargetEdge_5");
            yield return new TestCaseData("PAL3A", 0, -1, "emptyMask", false, -1, "00000000000").SetName("PAL3A_CoAttackTargetEdge_0");
            yield return new TestCaseData("PAL3A", 1, 1, "allInvalid", true, -1, "00000000000").SetName("PAL3A_CoAttackTargetEdge_1");
            yield return new TestCaseData("PAL3A", 2, -1, "emptyMask", false, -1, "00000000000").SetName("PAL3A_CoAttackTargetEdge_2");
            yield return new TestCaseData("PAL3A", 2, 1, "noLivingEnemies", false, 0, "00000010100").SetName("PAL3A_CoAttackTargetEdge_3");
            yield return new TestCaseData("PAL3A", 2, 1, "nonpositiveHp", false, 0, "00000010100").SetName("PAL3A_CoAttackTargetEdge_4");
            yield return new TestCaseData("PAL3A", 3, 1, "allInvalid", true, -1, "00000000000").SetName("PAL3A_CoAttackTargetEdge_5");
        }

        [TestCaseSource(nameof(NativeCases))]
        public void MatchesOriginalTargetExpansion(string variant, int rawRange, int originalTarget,
            string scenario, bool expectedMulti, int expectedTarget, string expectedMask)
        {
            var actors = Enumerable.Range(0, 11).Select(index =>
                new BattleCoAttackTargetActorSnapshot(true, true, false, false, 100 + index)).ToArray();
            if (scenario == "mixedDeadFledInvalid")
            {
                foreach (int index in new[] { 1, 5 })
                    actors[index] = new BattleCoAttackTargetActorSnapshot(true, false, false, false, 0);
                foreach (int index in new[] { 2, 6 })
                    actors[index] = new BattleCoAttackTargetActorSnapshot(true, true, true, false, 100 + index);
                foreach (int index in new[] { 3, 7 })
                    actors[index] = new BattleCoAttackTargetActorSnapshot(false, true, false, false, 100 + index);
            }
            if (scenario is "bossFirst" or "bossDead")
            {
                actors[6] = new BattleCoAttackTargetActorSnapshot(true, scenario != "bossDead", false, true, 106);
                actors[8] = new BattleCoAttackTargetActorSnapshot(true, true, false, true, 108);
            }
            if (scenario == "highestTie")
            {
                actors[6] = new BattleCoAttackTargetActorSnapshot(true, true, false, false, 300);
                actors[8] = new BattleCoAttackTargetActorSnapshot(true, true, false, false, 300);
            }
            if (scenario == "noLivingEnemies")
            {
                for (int index = 5; index < 11; index++)
                    actors[index] = new BattleCoAttackTargetActorSnapshot(true, false, false, false, 100 + index);
            }
            if (scenario == "nonpositiveHp")
            {
                for (int index = 5; index < 11; index++)
                    actors[index] = new BattleCoAttackTargetActorSnapshot(true, true, false, false, 0);
            }
            if (scenario == "allInvalid")
            {
                for (int index = 0; index < 11; index++)
                    actors[index] = new BattleCoAttackTargetActorSnapshot(false, true, false, false, 100 + index);
            }
            var previousMask = new bool[11];
            if (scenario != "emptyMask")
                previousMask[6] = previousMask[8] = true;
            var input = new BattleCoAttackTargetInput(originalTarget, previousMask, rawRange, actors);
            BattleCoAttackTargetSelection actual = new BattleCoAttackTargets(variant).Resolve(input);
            Assert.That(actual.Multi, Is.EqualTo(expectedMulti));
            Assert.That(actual.NativeTargetIndex, Is.EqualTo(expectedTarget));
            Assert.That(new string(actual.TargetMask.Select(selected => selected ? '1' : '0').ToArray()), Is.EqualTo(expectedMask));
            int[] expectedIndices = expectedMulti
                ? Enumerable.Range(0, 11).Where(index => expectedMask[index] == '1').ToArray()
                : expectedTarget < 0 ? System.Array.Empty<int>() : new[] { expectedTarget };
            Assert.That(actual.TargetIndices, Is.EqualTo(expectedIndices));
        }

        [Test]
        public void InputAndResultCaptureCallerCollections()
        {
            var actors = Enumerable.Range(0, 11).Select(index =>
                new BattleCoAttackTargetActorSnapshot(true, true, false, false, 100)).ToArray();
            var mask = new bool[11];
            mask[7] = true;
            var input = new BattleCoAttackTargetInput(-1, mask, 2, actors);
            mask[0] = true;
            actors[7] = new BattleCoAttackTargetActorSnapshot(false, false, true, false, 0);
            var rules = new BattleCoAttackTargets("PAL3A");
            BattleCoAttackTargetSelection actual = rules.Resolve(input);
            Assert.That(actual.NativeTargetIndex, Is.EqualTo(7));
            Assert.That(actual.TargetMask[0], Is.False);
            Assert.That(input.Actors[7].Valid, Is.True);
            Assert.That(actual.TargetIndices, Is.EqualTo(new[] { 7 }));
        }
    }
}
