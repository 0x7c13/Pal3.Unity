// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using Engine.Utilities;
    using NUnit.Framework;

    public sealed class DisplayFrameRateTests
    {
        [TestCase(-1, 120, 120)]
        [TestCase(240, 60, 60)]
        [TestCase(30, 120, 30)]
        [TestCase(-1, 59.94, 59)]
        [TestCase(-1, 0, 60)]
        [TestCase(-1, double.NaN, 60)]
        public void DisplayCapsExplicitAndAutomaticLimits(int requested, double refresh, int expected) =>
            Assert.That(DisplayFrameRate.Limit(requested, refresh), Is.EqualTo(expected));

        [TestCase(1, 30, 120, 0)]
        [TestCase(1, 59, 59.94, 0)]
        [TestCase(1, 60, 59.94, 1)]
        [TestCase(1, -1, 120, 1)]
        [TestCase(2, 60, 120, 2)]
        [TestCase(0, 30, 120, 0)]
        public void OnlyAnIncompatibleExplicitCapOverridesConfiguredVSync(int configured, int limit, double refresh, int expected) =>
            Assert.That(DisplayFrameRate.VSyncForLimit(configured, limit, refresh), Is.EqualTo(expected));

        [TestCase(60)]
        [TestCase(120)]
        public void CallbacksOnTheRefreshPeriodAllProduceFrames(int refreshRate)
        {
            var pacer = new DisplayFramePacer();
            for (int frame = 0; frame < refreshRate * 10; frame++)
                Assert.That(pacer.TryAdvance(frame / (double)refreshRate, refreshRate), Is.True, "Frame " + frame);
        }

        [Test]
        public void RepeatedInputCannotExceedRefreshAndLongPausesDoNotProduceCatchupFrames()
        {
            var pacer = new DisplayFramePacer();
            int frames = 0;
            for (int sample = 0; sample < 10000; sample++)
                if (pacer.TryAdvance(sample / 10000d, 60))
                    frames++;
            Assert.That(frames, Is.InRange(59, 60));
            Assert.That(pacer.TryAdvance(10, 60), Is.True);
            Assert.That(pacer.TryAdvance(10, 60), Is.False);
            Assert.That(pacer.TryAdvance(10.001, 60), Is.False);
            Assert.That(pacer.TryAdvance(11, 120), Is.True);
            Assert.That(pacer.TryAdvance(11.009, 120), Is.True);
            Assert.That(pacer.TryAdvance(double.NaN, 120), Is.False);
            Assert.That(pacer.TryAdvance(1, 60), Is.True, "A restarted presentation clock must recover.");
        }
    }
}
