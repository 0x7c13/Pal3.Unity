// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using Battle;
    using NUnit.Framework;

    public sealed class BattleStockAttackModeTests
    {
        [TestCase("PAL3", 0)]
        [TestCase("PAL3A", 0)]
        [TestCase("PAL3A", 7714)]
        public void StockExceptionsUseThePublishedDefaultPhysicalBranches(string variant, int mode)
        {
            // Full InitRoleState and AttackSpecAffect execution, both releases.
            Assert.That(BattlePhysicalAttackModes.UnsupportedReason(variant, mode), Is.Empty);
            Assert.That(BattlePhysicalAttackModes.Range(mode), Is.EqualTo(BattleTargetRange.OpponentSingle));
            Assert.That(BattlePhysicalAttackModes.AddedState(mode), Is.Null);
        }

        [TestCase("PAL3", 7714)]
        [TestCase("PAL3A", 7711)]
        [TestCase("PAL3", -1)]
        [TestCase("PAL3A", 999)]
        public void AnUnverifiedDataValueStillHasAnExplicitDiagnostic(string variant, int mode)
        {
            Assert.That(BattlePhysicalAttackModes.UnsupportedReason(variant, mode), Is.Not.Empty);
        }
    }
}
