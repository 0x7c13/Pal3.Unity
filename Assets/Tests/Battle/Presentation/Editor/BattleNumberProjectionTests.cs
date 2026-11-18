// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using Battle.Presentation;
    using NUnit.Framework;

    public sealed class BattleNumberProjectionTests
    {
        // Both released ShowDamage/ShowMiss callbacks, with a stub camera
        // projecting the captured world anchor to (400,300), produce these
        // vectors. See Battle-Stage41-Feedback/Numbers/native-number-vectors.json.
        [TestCase(-123, 0, 2f, false, false, 0, 10, 255, 0x3232ff)]
        [TestCase(-123, 0, 2f, false, false, 25, -5, 191, 0x3232ff)]
        [TestCase(-123, 0, 2f, false, false, 50, -20, 128, 0x3232ff)]
        [TestCase(0, 0, 2.5f, false, true, 25, -5, 204, 0x00ff00)]
        [TestCase(0, 0, 2.5f, false, true, 50, -20, 153, 0x00ff00)]
        [TestCase(-10, 1, 2.5f, false, false, 25, -5, 204, 0xfbe0a2)]
        [TestCase(20, 2, 2.5f, false, false, 25, -5, 204, 0xfaa8d5)]
        [TestCase(0, 0, 1f, true, false, 0, 16, 255, 0x32ffff)]
        [TestCase(0, 0, 1f, true, false, 25, 0, 128, 0x32ffff)]
        [TestCase(0, 0, 1f, true, false, 50, -16, 0, 0x32ffff)]
        [TestCase(-1, 16, 3.5f, false, false, 25, -5, 219, 0x32ffff)]
        public void MatchesReleasedNativeVectors(int value, int resource, float duration, bool miss, bool recovery,
            int tick, int baselineOffset, int alpha, int color)
        {
            var projection = new BattleNumberProjection();
            projection.Synchronize(new[] { Number(value, resource, duration, miss, recovery, offset: 9) }, tick);
            BattleNumberFrame frame = projection.Sample().Single();
            Assert.That(frame.OffsetX, Is.EqualTo(resource == 16 ? -23 : -7));
            Assert.That(frame.OffsetY, Is.EqualTo(baselineOffset));
            Assert.That(frame.Alpha, Is.EqualTo(alpha));
            Assert.That(frame.Color, Is.EqualTo((uint)color));
        }

        private static BattleEvent Number(int value = -123, int resource = 0, float duration = 2,
            bool miss = false, bool recovery = false, long tick = 0, float start = 0, int offset = 31)
        {
            var entry = new BattleEvent();
            void Set(string name, object value) => typeof(BattleEvent).GetProperty(name).SetValue(entry, value);
            Set(nameof(BattleEvent.Kind), BattleEventKind.NumberShown);
            Set(nameof(BattleEvent.Target), "target");
            Set(nameof(BattleEvent.RequestedDelta), value);
            Set(nameof(BattleEvent.Resource), resource);
            Set(nameof(BattleEvent.DurationSeconds), duration);
            Set(nameof(BattleEvent.NumberIsMiss), miss);
            Set(nameof(BattleEvent.NumberIsRecovery), recovery);
            Set(nameof(BattleEvent.NumberActivationTick), tick);
            Set(nameof(BattleEvent.NumberStartedTime), start);
            Set(nameof(BattleEvent.NumberOffset), offset);
            Set(nameof(BattleEvent.NumberPosition), new BattleMovementPoint(120, 50, -30));
            return entry;
        }

        [Test]
        public void NativeDamageKeepsItsSignAndAnchorAndRisesThirtyPixelsPerSecond()
        {
            var projection = new BattleNumberProjection();
            BattleEvent entry = Number();
            projection.Synchronize(new[] { entry }, 25);
            BattleNumberFrame frame = projection.Sample().Single();
            Assert.That(frame.Text, Is.EqualTo("-123"));
            Assert.That(frame.Color, Is.EqualTo(0x3232ff));
            Assert.That(frame.Position.X, Is.EqualTo(120));
            Assert.That(frame.Position.Y, Is.EqualTo(50));
            Assert.That(frame.OffsetX, Is.EqualTo(15));
            Assert.That(frame.OffsetY, Is.EqualTo(-5));
            Assert.That(frame.Alpha, Is.EqualTo(191));
            projection.Synchronize(new[] { entry }, 25);
            Assert.That(projection.Sample().Single().OffsetY, Is.EqualTo(frame.OffsetY), "A frozen simulation clock must freeze text.");
            projection.Synchronize(new[] { entry }, 101);
            Assert.That(projection.Sample(), Is.Empty);
        }

        [TestCase(0, 0, true, 0x00ff00)]
        [TestCase(0, 15, true, 0x00ff00)]
        [TestCase(1, -10, false, 0xfbe0a2)]
        [TestCase(2, 20, false, 0xfaa8d5)]
        public void ResourceNumbersUseTheReleasedColorsIncludingCappedHealing(int resource, int value, bool recovery, int color)
        {
            var projection = new BattleNumberProjection();
            projection.Synchronize(new[] { Number(value, resource, 2.5f, recovery: recovery) }, 50);
            BattleNumberFrame frame = projection.Sample().Single();
            Assert.That(frame.Text, Is.EqualTo(value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Assert.That(frame.Color, Is.EqualTo((uint)color));
            Assert.That(frame.Alpha, Is.EqualTo(153));
        }

        [Test]
        public void NativeMissUsesItsOwnOneSecondThirtyTwoPixelRiseAndCyanColor()
        {
            var projection = new BattleNumberProjection();
            projection.Synchronize(new[] { Number(0, duration: 1, miss: true) }, 25);
            BattleNumberFrame frame = projection.Sample().Single();
            Assert.That(frame.Text, Is.EqualTo("MISS"));
            Assert.That(frame.Color, Is.EqualTo(0x32ffff));
            Assert.That(frame.OffsetY, Is.Zero);
            Assert.That(frame.Alpha, Is.EqualTo(128));
        }

        [Test]
        public void GuCostKeepsTheCostCaptionAndWiderNativeOffset()
        {
            var projection = new BattleNumberProjection("蠱");
            projection.Synchronize(new[] { Number(-1, 16, 3.5f) }, 0);
            BattleNumberFrame frame = projection.Sample().Single();
            Assert.That(frame.Text, Is.EqualTo("蠱 -1"));
            Assert.That(frame.OffsetX, Is.EqualTo(-1));
            Assert.That(frame.Color, Is.EqualTo(0x32ffff));
        }

        [Test]
        public void SimultaneousNumbersStackAndBatchedSteppingPreservesTheirOrder()
        {
            var events = new List<BattleEvent> { Number(), Number(), Number(tick: 10, start: .2f) };
            var projection = new BattleNumberProjection();
            projection.Synchronize(events, 10);
            BattleNumberFrame[] frames = projection.Sample().ToArray();
            Assert.That(frames.Select(frame => frame.OffsetY), Is.EqualTo(new[] { 4, 14, 24 }));
            // Exceeding the cap in one pass resets to 160 before the next pass;
            // it does not cap each number registered within that same pass.
            events = Enumerable.Range(0, 20).Select(_ => Number()).Append(Number(tick: 1)).ToList();
            projection = new BattleNumberProjection();
            projection.Synchronize(events, 1);
            Assert.That(projection.Sample().Last().OffsetY, Is.EqualTo(169));
        }

        [Test]
        public void MissDoesNotConsumeTheNumericVerticalStack()
        {
            var projection = new BattleNumberProjection();
            projection.Synchronize(new[] { Number(), Number(miss: true), Number() }, 0);
            Assert.That(projection.Sample().Select(frame => frame.OffsetY), Is.EqualTo(new[] { 10, 16, 20 }));
        }

        [Test]
        public void NativeTftUsesContinuousLowBitFirstGlyphBitsAndHalfWidthAscii()
        {
            byte[] bytes = FontBytes();
            int start = 20 + '0' * 50;
            foreach (int pixel in new[] { 0, 19, 20, 399 })
                bytes[start + pixel / 8] |= (byte)(1 << (pixel % 8));
            byte[] alpha = new BattleNumberFont(bytes).GlyphAlpha('0');
            Assert.That(alpha.Select((value, index) => (value, index)).Where(pair => pair.value != 0).Select(pair => pair.index),
                Is.EqualTo(new[] { 0, 19, 20, 399 }));
            Assert.That(BattleNumberFont.Advance('0'), Is.EqualTo(10));
            Assert.That(BattleNumberFont.Advance('M'), Is.EqualTo(10));
            Assert.That(BattleNumberFont.Advance('蛊'), Is.EqualTo(20));
            Assert.Throws<InvalidDataException>(() => new BattleNumberFont(bytes[..^1]));
            bytes[8] = 16;
            Assert.Throws<InvalidDataException>(() => new BattleNumberFont(bytes));
        }

        private static byte[] FontBytes()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(0u);
            writer.Write(1u);
            writer.Write(20u);
            writer.Write(1u);
            writer.Write(128);
            writer.Write(new byte[128 * 50]);
            return stream.ToArray();
        }
    }
}
