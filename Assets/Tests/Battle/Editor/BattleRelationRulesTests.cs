// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using NUnit.Framework;
    using Pal3.Battle;

    public sealed class BattleRelationRulesTests
    {
        [TestCase("PAL3", 3, true)]
        [TestCase("PAL3", 4, false)]
        [TestCase("PAL3A", 3, false)]
        [TestCase("PAL3A", 4, false)]
        [TestCase("PAL3", 0, true)]
        [TestCase("PAL3A", 0, true)]
        public void PublishedWriterFiltersOnlyItsSpecificNativeSlots(string variant, int index, bool applies)
        {
            BattleRelationChange change = BattleRelationRules.Change(variant, index, 40, 1, true);
            Assert.That(change.Applied, Is.EqualTo(applies));
            Assert.That(change.Value, Is.EqualTo(applies ? 41 : 40));
            Assert.That(change.Sound, Is.EqualTo(applies ? "wa010" : null));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AbsentHeroLeavesValueAndSoundUntouched(string variant)
        {
            BattleRelationChange change = BattleRelationRules.Change(variant, 1, 999, -2, false);
            Assert.That(change.Applied, Is.False);
            Assert.That(change.Value, Is.EqualTo(999));
            Assert.That(change.Sound, Is.Null);
        }

        [TestCase(999, 1, 999, "wa010")]
        [TestCase(0, -2, 0, "wa048")]
        [TestCase(999, 0, 999, null)]
        [TestCase(int.MaxValue, 1, 0, "wa010")]
        [TestCase(int.MinValue, -1, 999, "wa048")]
        public void SoundUsesRequestSignBeforeUncheckedAdditionAndClamping(int before, int requested, int expected, string sound)
        {
            foreach (string variant in new[] { "PAL3", "PAL3A" })
            {
                BattleRelationChange change = BattleRelationRules.Change(variant, 1, before, requested, true);
                Assert.That(change.Value, Is.EqualTo(expected));
                Assert.That(change.Sound, Is.EqualTo(sound));
            }
        }
    }
}
