// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using NUnit.Framework;
    using Pal3.Battle;

    public sealed class BattleThrownWeaponTests
    {
        // Original 500b29 + 50112e in actual Role order (updater before body).
        [TestCase(0, 3, 5)]
        [TestCase(100, 5, 10)]
        [TestCase(300, 23, 46)]
        [TestCase(600, 50, 99)]
        [TestCase(2000, 175, 350)]
        public void PublishedOutboundHitAndReturnRestoreFrames(int distance, int hitFrame, int restoreFrame)
        {
            var projectile = new BattleThrownWeapon(new(-17, 12, 42), new(-17 + distance, 0, 42));
            int hits = 0;
            for (int frame = 0; frame <= restoreFrame; frame++)
            {
                projectile.Advance(true);
                if (!projectile.BodyFinished)
                    projectile.AdvanceBody(frame == 2 ? BattlePhysicalMarkerKind.Throwed : null, true);
                if (projectile.HitThisPass)
                {
                    hits++;
                    Assert.That(frame, Is.EqualTo(hitFrame));
                }
                Assert.That(projectile.RestoreRequested, Is.EqualTo(frame == restoreFrame));
            }
            Assert.That(hits, Is.EqualTo(1));
            Assert.That(projectile.Active, Is.False);
            Assert.That(projectile.WeaponVisible, Is.False); // restore body still owns the hidden weapon
            projectile.FinishRestore();
            Assert.That(projectile.WeaponVisible, Is.True);
        }

        [Test]
        public void SourceDeathCancelsIndependentProjectileBeforeDamage()
        {
            var projectile = new BattleThrownWeapon(default, new(600, 0, 0));
            projectile.AdvanceBody(BattlePhysicalMarkerKind.Throwed, true);
            for (int frame = 0; frame < 5; frame++)
                projectile.Advance(true);
            projectile.Advance(false);
            Assert.That(projectile.Active, Is.False);
            Assert.That(projectile.WeaponVisible, Is.True);
            Assert.That(projectile.HitThisPass, Is.False);
            Assert.That(projectile.RestoreRequested, Is.False);
        }

        [Test]
        public void ActivationStartsSecondModelWithoutMovingTheSourceOrAdvancingFlight()
        {
            var projectile = new BattleThrownWeapon(default, new(300, 0, 0));
            BattleMovementPoint origin = projectile.Position;
            projectile.Advance(true);
            projectile.AdvanceBody(BattlePhysicalMarkerKind.Throwed, true);
            Assert.That(projectile.Position.X, Is.EqualTo(origin.X));
            Assert.That(projectile.Active, Is.True);
            Assert.That(projectile.BodyFinished, Is.True);
            projectile.Advance(true);
            Assert.That(projectile.Position.X, Is.GreaterThan(origin.X));
        }
    }
}
