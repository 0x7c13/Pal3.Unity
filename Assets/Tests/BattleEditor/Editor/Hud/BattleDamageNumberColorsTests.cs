// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System.Linq;
    using Battle;
    using Battle.Presentation;
    using NUnit.Framework;

    public sealed class BattleDamageNumberColorsTests
    {
        [TestCase(-123, BattleDamageNumberColorMode.Original, 0x3232ff)]
        [TestCase(0, BattleDamageNumberColorMode.Original, 0x3232ff)]
        [TestCase(-123, BattleDamageNumberColorMode.Yellow, 0xffff00)]
        [TestCase(0, BattleDamageNumberColorMode.Yellow, 0xffff00)]
        [TestCase(-123, BattleDamageNumberColorMode.White, 0xffffff)]
        [TestCase(0, BattleDamageNumberColorMode.White, 0xffffff)]
        public void OnlyTheDrawingColorChangesForHpDamageAndZero(int value, BattleDamageNumberColorMode mode, int expected)
        {
            BattleNumberFrame frame = Frame(value, 0, false, false);
            Assert.That(BattleDamageNumberColors.Resolve(frame.Color, mode), Is.EqualTo((uint)expected));
            Assert.That(frame.Color, Is.EqualTo(0x3232ffu), "The native projection must retain the released color.");
            Assert.That(frame.Text, Is.EqualTo(value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Assert.That(frame.Alpha, Is.EqualTo(191));
        }

        [TestCase(0, 0, true, false, 0x00ff00)]
        [TestCase(123, 0, true, false, 0x00ff00)]
        [TestCase(-10, 1, false, false, 0xfbe0a2)]
        [TestCase(10, 1, true, false, 0xfbe0a2)]
        [TestCase(-10, 2, false, false, 0xfaa8d5)]
        [TestCase(10, 2, true, false, 0xfaa8d5)]
        [TestCase(0, 0, false, true, 0x32ffff)]
        [TestCase(-1, 16, false, false, 0x32ffff)]
        public void EveryModePreservesRecoveryGpMpMissAndGu(int value, int resource, bool recovery, bool miss, int expected)
        {
            BattleNumberFrame frame = Frame(value, resource, recovery, miss);
            foreach (BattleDamageNumberColorMode mode in System.Enum.GetValues(typeof(BattleDamageNumberColorMode)))
                Assert.That(BattleDamageNumberColors.Resolve(frame.Color, mode), Is.EqualTo((uint)expected));
        }

        [Test]
        public void UnrecognizedColorsAndModesKeepTheirOriginalValue()
        {
            Assert.That(default(BattleDamageNumberColorMode), Is.EqualTo(BattleDamageNumberColorMode.Original));
            Assert.That(BattleDamageNumberColors.Resolve(0x3232ffu, (BattleDamageNumberColorMode)99), Is.EqualTo(0x3232ffu));
            Assert.That(BattleDamageNumberColors.Resolve(0x123456u, BattleDamageNumberColorMode.Yellow), Is.EqualTo(0x123456u));
        }

        private static BattleNumberFrame Frame(int value, int resource, bool recovery, bool miss)
        {
            var entry = new BattleEvent();
            void Set(string name, object value) => typeof(BattleEvent).GetProperty(name).SetValue(entry, value);
            Set(nameof(BattleEvent.Kind), BattleEventKind.NumberShown);
            Set(nameof(BattleEvent.Target), "target");
            Set(nameof(BattleEvent.RequestedDelta), value);
            Set(nameof(BattleEvent.Resource), resource);
            Set(nameof(BattleEvent.NumberIsRecovery), recovery);
            Set(nameof(BattleEvent.NumberIsMiss), miss);
            Set(nameof(BattleEvent.DurationSeconds), 2f);
            var projection = new BattleNumberProjection();
            projection.Synchronize(new[] { entry }, 25);
            return projection.Sample().Single();
        }
    }
}
