// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using Battle;
    using Battle.Presentation;
    using NUnit.Framework;

    public sealed class BattleLineupProjectionTests
    {
        [TestCase(0, 2, 0)]
        [TestCase(.02, 2, 0)]
        [TestCase(.52, 2, 85)]
        [TestCase(1.02, 2, 170)]
        [TestCase(1.52, 2, 255)]
        [TestCase(1.82, 2, 102)]
        [TestCase(2, 2, 10)]
        [TestCase(2.02, 2, 0)]
        [TestCase(2.02, 4, 170)]
        [TestCase(3.02, 4, 255)]
        [TestCase(4, 4, 5)]
        public void NativeFadeSubmitsThePreviousPassAlpha(double elapsed, double duration, int alpha) =>
            Assert.That(BattleLineupProjection.Alpha(elapsed, duration), Is.EqualTo(alpha));

        [Test]
        public void RingUsesNativeGroundHeightWhileTheCenterTileIgnoresRoll()
        {
            var center = new BattleMovementPoint(10, 99, -20);
            BattleMovementPoint ring = BattleLineupProjection.Corner(center, 2, 0, 0, false);
            Assert.That((ring.X, ring.Y, ring.Z), Is.EqualTo((8f, .5f, -22f)));
            BattleMovementPoint tile = BattleLineupProjection.Corner(center, 2, 1.234f, 2, true);
            Assert.That((tile.X, tile.Y, tile.Z), Is.EqualTo((11f, 0f, -19f)));
            Assert.That(BattleLineupProjection.CenterColor(3), Is.EqualTo(0x52b66b));
            Assert.That(BattleLineupProjection.CenterColor(4), Is.EqualTo(0xde75bd));
        }
    }
}
