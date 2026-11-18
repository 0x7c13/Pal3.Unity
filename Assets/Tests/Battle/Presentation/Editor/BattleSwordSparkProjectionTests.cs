// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Linq;
    using Battle.Presentation;
    using NUnit.Framework;

    public sealed class BattleSwordSparkProjectionTests
    {
        [Test]
        public void StrictEmissionBoundaryAndNativeRandomStoresMatchTheSource()
        {
            var projection = new BattleSwordSparkProjection(17);
            projection.Advance(.1, true, 100);
            Assert.That(projection.Frames, Is.Empty);
            projection.Advance(.02, true, 100);
            BattleSwordSparkFrame frame = projection.Frames.Single();
            // Independent MSVC rand vector for seed 17: 94, 26602, 30017,
            // 18297. Native CreateStars consumes luminosity, x, y, then ay.
            Assert.That(frame.X, Is.EqualTo(12));
            Assert.That(frame.Y, Is.EqualTo(561.7812f).Within(.0001f));
            Assert.That(frame.Size, Is.EqualTo(10));
            Assert.That(frame.Alpha, Is.EqualTo(204));
            projection.Advance(.02, false, 100);
            Assert.That(projection.Frames.Single().Y, Is.EqualTo(561.5436f).Within(.0001f));
            Assert.That(projection.Frames.Single().Alpha, Is.EqualTo(202));
        }

        [TestCase(199.9f, 1)]
        [TestCase(200f, 2)]
        public void FullChargeAddsTheSecondStarWithoutReplayingAStalledBacklog(float energy, int count)
        {
            var projection = new BattleSwordSparkProjection(17);
            projection.Advance(1, true, energy);
            Assert.That(projection.Frames, Has.Count.EqualTo(count));
            Assert.That(projection.Time, Is.EqualTo(1));
        }

        [Test]
        public void SamplingAndZeroDeltaNeverAdvanceThePrivateClockOrRandomStream()
        {
            var sampled = new BattleSwordSparkProjection(17);
            var untouched = new BattleSwordSparkProjection(17);
            sampled.Advance(.12, true, 200);
            untouched.Advance(.12, true, 200);
            BattleSwordSparkFrame[] before = sampled.Frames.ToArray();
            for (int repaint = 0; repaint < 200; repaint++)
            {
                _ = sampled.Frames;
                sampled.Advance(0, true, 200);
            }
            Assert.That(sampled.Time, Is.EqualTo(.12));
            Assert.That(sampled.Frames, Is.EqualTo(before));
            sampled.Advance(.12, true, 200);
            untouched.Advance(.12, true, 200);
            Assert.That(sampled.Frames, Is.EqualTo(untouched.Frames));
        }

        [Test]
        public void HiddenOrDeadHudStopsEmissionAndClearReleasesEveryParticle()
        {
            var projection = new BattleSwordSparkProjection(17);
            projection.Advance(.12, false, 200);
            Assert.That(projection.Frames, Is.Empty);
            projection.Advance(.02, true, 200);
            Assert.That(projection.Frames, Has.Count.EqualTo(2));
            for (int step = 0; step < 150; step++)
                projection.Advance(.02, false, 200);
            Assert.That(projection.Frames, Is.Empty);
            projection.Advance(.02, true, 200);
            Assert.That(projection.Frames, Is.Not.Empty);
            projection.Clear();
            Assert.That(projection.Frames, Is.Empty);
        }

        [Test]
        public void ZeroEnergyHasNoVisibleParticleAndInvalidClockInputsAreRejected()
        {
            var projection = new BattleSwordSparkProjection(17);
            projection.Advance(.12, true, 0);
            Assert.That(projection.Frames, Is.Empty);
            Assert.Throws<ArgumentOutOfRangeException>(() => projection.Advance(-1, true, 200));
            Assert.Throws<ArgumentOutOfRangeException>(() => projection.Advance(double.NaN, true, 200));
            Assert.Throws<ArgumentOutOfRangeException>(() => projection.Advance(.02, true, float.NaN));
        }
    }
}
