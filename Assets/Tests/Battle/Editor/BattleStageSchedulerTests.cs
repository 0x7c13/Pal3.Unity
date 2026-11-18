// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using Pal3.Battle;

    public sealed class BattleStageSchedulerTests
    {
        // Golden cases execute the released EXE allocator, DoTask, and numeric
        // comparison suffix. The activation/body callbacks are explicit fixtures.
        // workspace/Analysis/Battle-Stage4/review-scheduler-native.py records them.
        public static IEnumerable<TestCaseData> NativeNumberCases()
        {
            yield return new TestCaseData("PAL3", 2.0f, 0L, false, 0L, 101L, 102L);
            yield return new TestCaseData("PAL3", 2.0f, 0L, true, 0L, 101L, 102L);
            yield return new TestCaseData("PAL3", 2.0f, 1L, false, 0L, 102L, 103L);
            yield return new TestCaseData("PAL3", 2.0f, 1L, true, 0L, 102L, 103L);
            yield return new TestCaseData("PAL3", 2.0f, 7L, false, 0L, 107L, 108L);
            yield return new TestCaseData("PAL3", 2.0f, 7L, true, 0L, 107L, 108L);
            yield return new TestCaseData("PAL3", 2.0f, 51L, false, 0L, 152L, 153L);
            yield return new TestCaseData("PAL3", 2.0f, 51L, true, 0L, 152L, 153L);
            yield return new TestCaseData("PAL3", 2.0f, 1000L, false, 0L, 1101L, 1102L);
            yield return new TestCaseData("PAL3", 2.0f, 1000L, true, 0L, 1101L, 1102L);
            yield return new TestCaseData("PAL3", 2.5f, 0L, false, 0L, 126L, 127L);
            yield return new TestCaseData("PAL3", 2.5f, 0L, true, 0L, 126L, 127L);
            yield return new TestCaseData("PAL3", 2.5f, 1L, false, 0L, 127L, 128L);
            yield return new TestCaseData("PAL3", 2.5f, 1L, true, 0L, 127L, 128L);
            yield return new TestCaseData("PAL3", 2.5f, 7L, false, 0L, 132L, 133L);
            yield return new TestCaseData("PAL3", 2.5f, 7L, true, 0L, 132L, 133L);
            yield return new TestCaseData("PAL3", 2.5f, 51L, false, 0L, 177L, 178L);
            yield return new TestCaseData("PAL3", 2.5f, 51L, true, 0L, 177L, 178L);
            yield return new TestCaseData("PAL3", 2.5f, 1000L, false, 0L, 1126L, 1127L);
            yield return new TestCaseData("PAL3", 2.5f, 1000L, true, 0L, 1126L, 1127L);
            yield return new TestCaseData("PAL3", 2.0f, 0L, false, 1L, 101L, 102L);
            yield return new TestCaseData("PAL3", 2.0f, 0L, true, 1L, 102L, 103L);
            yield return new TestCaseData("PAL3", 2.0f, 1L, false, 1L, 102L, 103L);
            yield return new TestCaseData("PAL3", 2.0f, 1L, true, 1L, 103L, 104L);
            yield return new TestCaseData("PAL3", 2.0f, 7L, false, 1L, 107L, 108L);
            yield return new TestCaseData("PAL3", 2.0f, 7L, true, 1L, 108L, 109L);
            yield return new TestCaseData("PAL3", 2.0f, 51L, false, 1L, 152L, 153L);
            yield return new TestCaseData("PAL3", 2.0f, 51L, true, 1L, 153L, 154L);
            yield return new TestCaseData("PAL3", 2.0f, 1000L, false, 1L, 1101L, 1102L);
            yield return new TestCaseData("PAL3", 2.0f, 1000L, true, 1L, 1102L, 1103L);
            yield return new TestCaseData("PAL3", 2.5f, 0L, false, 1L, 126L, 127L);
            yield return new TestCaseData("PAL3", 2.5f, 0L, true, 1L, 127L, 128L);
            yield return new TestCaseData("PAL3", 2.5f, 1L, false, 1L, 127L, 128L);
            yield return new TestCaseData("PAL3", 2.5f, 1L, true, 1L, 128L, 129L);
            yield return new TestCaseData("PAL3", 2.5f, 7L, false, 1L, 132L, 133L);
            yield return new TestCaseData("PAL3", 2.5f, 7L, true, 1L, 133L, 134L);
            yield return new TestCaseData("PAL3", 2.5f, 51L, false, 1L, 177L, 178L);
            yield return new TestCaseData("PAL3", 2.5f, 51L, true, 1L, 178L, 179L);
            yield return new TestCaseData("PAL3", 2.5f, 1000L, false, 1L, 1126L, 1127L);
            yield return new TestCaseData("PAL3", 2.5f, 1000L, true, 1L, 1127L, 1128L);
            yield return new TestCaseData("PAL3A", 2.0f, 0L, false, 0L, 101L, 102L);
            yield return new TestCaseData("PAL3A", 2.0f, 0L, true, 0L, 101L, 102L);
            yield return new TestCaseData("PAL3A", 2.0f, 1L, false, 0L, 102L, 103L);
            yield return new TestCaseData("PAL3A", 2.0f, 1L, true, 0L, 102L, 103L);
            yield return new TestCaseData("PAL3A", 2.0f, 7L, false, 0L, 107L, 108L);
            yield return new TestCaseData("PAL3A", 2.0f, 7L, true, 0L, 107L, 108L);
            yield return new TestCaseData("PAL3A", 2.0f, 51L, false, 0L, 152L, 153L);
            yield return new TestCaseData("PAL3A", 2.0f, 51L, true, 0L, 152L, 153L);
            yield return new TestCaseData("PAL3A", 2.0f, 1000L, false, 0L, 1101L, 1102L);
            yield return new TestCaseData("PAL3A", 2.0f, 1000L, true, 0L, 1101L, 1102L);
            yield return new TestCaseData("PAL3A", 2.5f, 0L, false, 0L, 126L, 127L);
            yield return new TestCaseData("PAL3A", 2.5f, 0L, true, 0L, 126L, 127L);
            yield return new TestCaseData("PAL3A", 2.5f, 1L, false, 0L, 127L, 128L);
            yield return new TestCaseData("PAL3A", 2.5f, 1L, true, 0L, 127L, 128L);
            yield return new TestCaseData("PAL3A", 2.5f, 7L, false, 0L, 132L, 133L);
            yield return new TestCaseData("PAL3A", 2.5f, 7L, true, 0L, 132L, 133L);
            yield return new TestCaseData("PAL3A", 2.5f, 51L, false, 0L, 177L, 178L);
            yield return new TestCaseData("PAL3A", 2.5f, 51L, true, 0L, 177L, 178L);
            yield return new TestCaseData("PAL3A", 2.5f, 1000L, false, 0L, 1126L, 1127L);
            yield return new TestCaseData("PAL3A", 2.5f, 1000L, true, 0L, 1126L, 1127L);
            yield return new TestCaseData("PAL3A", 2.0f, 0L, false, 1L, 101L, 102L);
            yield return new TestCaseData("PAL3A", 2.0f, 0L, true, 1L, 102L, 103L);
            yield return new TestCaseData("PAL3A", 2.0f, 1L, false, 1L, 102L, 103L);
            yield return new TestCaseData("PAL3A", 2.0f, 1L, true, 1L, 103L, 104L);
            yield return new TestCaseData("PAL3A", 2.0f, 7L, false, 1L, 107L, 108L);
            yield return new TestCaseData("PAL3A", 2.0f, 7L, true, 1L, 108L, 109L);
            yield return new TestCaseData("PAL3A", 2.0f, 51L, false, 1L, 152L, 153L);
            yield return new TestCaseData("PAL3A", 2.0f, 51L, true, 1L, 153L, 154L);
            yield return new TestCaseData("PAL3A", 2.0f, 1000L, false, 1L, 1101L, 1102L);
            yield return new TestCaseData("PAL3A", 2.0f, 1000L, true, 1L, 1102L, 1103L);
            yield return new TestCaseData("PAL3A", 2.5f, 0L, false, 1L, 126L, 127L);
            yield return new TestCaseData("PAL3A", 2.5f, 0L, true, 1L, 127L, 128L);
            yield return new TestCaseData("PAL3A", 2.5f, 1L, false, 1L, 127L, 128L);
            yield return new TestCaseData("PAL3A", 2.5f, 1L, true, 1L, 128L, 129L);
            yield return new TestCaseData("PAL3A", 2.5f, 7L, false, 1L, 132L, 133L);
            yield return new TestCaseData("PAL3A", 2.5f, 7L, true, 1L, 133L, 134L);
            yield return new TestCaseData("PAL3A", 2.5f, 51L, false, 1L, 177L, 178L);
            yield return new TestCaseData("PAL3A", 2.5f, 51L, true, 1L, 178L, 179L);
            yield return new TestCaseData("PAL3A", 2.5f, 1000L, false, 1L, 1126L, 1127L);
            yield return new TestCaseData("PAL3A", 2.5f, 1000L, true, 1L, 1127L, 1128L);
        }

        [TestCaseSource(nameof(NativeNumberCases))]
        public void NativeNumberLifetime(string variant, float duration, long createdTick, bool blocked,
            long firstAdvanceDelay, long finishTick, long deleteTick)
        {
            var scheduler = new BattleStageScheduler();
            var trace = new List<string>();
            var number = new BattleStageNumber(duration, context => trace.Add("start:" + context.Tick),
                blocked ? context => trace.Add("block:" + context.Tick) : null);
            BattleStageHandle handle = scheduler.Add("target", "number", number, BattleStageScheduler.TimeAt(createdTick));
            Assert.That(trace, Is.Empty, variant);
            for (long tick = createdTick + firstAdvanceDelay; tick <= deleteTick; tick++)
            {
                scheduler.Advance(tick);
                Assert.That(number.Finished, Is.EqualTo(tick >= finishTick), variant + ": " + tick);
                Assert.That(scheduler.IsActive(handle), Is.EqualTo(tick < deleteTick), variant + ": " + tick);
                Assert.That(scheduler.CountOwner("target"), Is.EqualTo(tick < deleteTick ? 1 : 0));
            }
            Assert.That(trace, Is.EqualTo(blocked
                ? new[] { "block:" + (createdTick + firstAdvanceDelay), "start:" + (createdTick + firstAdvanceDelay) }
                : new[] { "start:" + (createdTick + firstAdvanceDelay) }));
        }

        [TestCase("PAL3", "emptyAfter", 1, 0)]
        [TestCase("PAL3", "emptyBefore", 0, 1)]
        [TestCase("PAL3", "filledBefore", 4, 0)]
        [TestCase("PAL3", "bodyAfterStage", 0, 1)]
        [TestCase("PAL3A", "emptyAfter", 1, 0)]
        [TestCase("PAL3A", "emptyBefore", 0, 1)]
        [TestCase("PAL3A", "filledBefore", 4, 0)]
        [TestCase("PAL3A", "bodyAfterStage", 0, 1)]
        public void NativeSlotInsertion(string variant, string layout, int expectedSlot, long expectedActivation)
        {
            var scheduler = new BattleStageScheduler();
            var trace = new List<string>();
            BattleStageHandle number = default;
            bool CreateNumber(BattleStageContext context)
            {
                trace.Add("create:" + context.Tick);
                number = scheduler.Add("target", "number", new BattleStageNumber(2f,
                    numberContext => trace.Add("start:" + numberContext.Tick)), context.Time);
                trace.Add("slot:" + number.Slot);
                return true;
            }
            var holes = new List<BattleStageHandle>();
            if (layout is "emptyBefore" or "filledBefore")
            {
                for (int i = 0; i < 3; i++)
                    holes.Add(scheduler.Add("placeholder", "idle", new Job(_ => false), 0f));
            }
            if (layout != "bodyAfterStage")
                scheduler.Add("source", "ability", new Job(CreateNumber), 0f);
            if (layout == "emptyBefore")
            {
                foreach (BattleStageHandle hole in holes)
                    scheduler.Cancel(hole);
            }
            scheduler.Advance(0);
            if (layout == "bodyAfterStage")
            {
                trace.Add("create:0");
                number = scheduler.Add("target", "number", new BattleStageNumber(2f,
                    context => trace.Add("start:" + context.Tick)), 0f);
                trace.Add("slot:" + number.Slot);
            }
            scheduler.Advance(1);
            Assert.That(trace, Is.EqualTo(new[] { "create:0", "slot:" + expectedSlot, "start:" + expectedActivation }), variant);
        }

        [Test]
        public void FinishingKeepsOwnerCountUntilNextPass()
        {
            var scheduler = new BattleStageScheduler();
            int calls = 0;
            BattleStageHandle handle = scheduler.Add("actor", "action", new Job(_ =>
            {
                calls++;
                return true;
            }), 0f);
            scheduler.Advance(0);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(scheduler.CountOwner("actor"), Is.EqualTo(1));
            Assert.That(scheduler.Snapshot()[0].Finished, Is.True);
            scheduler.Advance(1);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(scheduler.IsActive(handle), Is.False);
            Assert.That(scheduler.Count, Is.Zero);
        }

        [Test]
        public void CancelledHandleCannotCancelReusedSlot()
        {
            var scheduler = new BattleStageScheduler(1);
            BattleStageHandle old = scheduler.Add("old", "number", new Job(_ => false), 0f);
            Assert.That(scheduler.Cancel(old), Is.True);
            BattleStageHandle replacement = scheduler.Add("new", "action", new Job(_ => false), 0f);
            Assert.That(replacement.Slot, Is.EqualTo(old.Slot));
            Assert.That(replacement.Id, Is.Not.EqualTo(old.Id));
            Assert.That(scheduler.Cancel(old), Is.False);
            Assert.That(scheduler.IsActive(replacement), Is.True);
        }

        [Test]
        public void SelfCancellationDoesNotFinishReplacement()
        {
            var scheduler = new BattleStageScheduler(1);
            int replacementCalls = 0;
            BattleStageHandle replacement = default;
            scheduler.Add("old", "action", new Job(context =>
            {
                Assert.That(scheduler.Cancel(context.Handle), Is.True);
                replacement = scheduler.Add("new", "number", new Job(_ =>
                {
                    replacementCalls++;
                    return false;
                }), context.Time);
                return true;
            }), 0f);
            scheduler.Advance(0);
            Assert.That(replacementCalls, Is.Zero);
            Assert.That(scheduler.Snapshot()[0].Finished, Is.False);
            scheduler.Advance(1);
            Assert.That(replacementCalls, Is.EqualTo(1));
            Assert.That(scheduler.IsActive(replacement), Is.True);
        }

        [Test]
        public void CallbackCanCancelLaterSlotBeforeItRuns()
        {
            var scheduler = new BattleStageScheduler();
            BattleStageHandle later = default;
            int calls = 0;
            scheduler.Add("source", "action", new Job(_ =>
            {
                scheduler.Cancel(later);
                return true;
            }), 0f);
            later = scheduler.Add("target", "action", new Job(_ =>
            {
                calls++;
                return false;
            }), 0f);
            scheduler.Advance(0);
            Assert.That(calls, Is.Zero);
            Assert.That(scheduler.IsActive(later), Is.False);
        }

        [Test]
        public void SnapshotsRetainValuesAndRejectCollectionMutation()
        {
            var scheduler = new BattleStageScheduler();
            BattleStageHandle handle = scheduler.Add("actor", "action", new Job(_ => true), 0.14f);
            IReadOnlyList<BattleStageTaskSnapshot> before = scheduler.Snapshot();
            scheduler.Advance(7);
            Assert.That(before.Count, Is.EqualTo(1));
            Assert.That(before[0].Handle, Is.EqualTo(handle));
            Assert.That(before[0].Owner, Is.EqualTo("actor"));
            Assert.That(before[0].Kind, Is.EqualTo("action"));
            Assert.That(before[0].CreatedTime, Is.EqualTo(0.14f));
            Assert.That(before[0].Finished, Is.False);
            IReadOnlyList<BattleStageTaskSnapshot> finished = scheduler.Snapshot();
            scheduler.Advance(8);
            Assert.That(finished[0].Finished, Is.True);
            Assert.That(before[0].Finished, Is.False);
            Assert.Throws<NotSupportedException>(() => ((IList<BattleStageTaskSnapshot>)before).Clear());
        }

        [TestCase(false, 101L)]
        [TestCase(true, 102L)]
        public void DelayedActivationPreservesCreationTimeUnlessBodyBlocks(bool blocked, long expectedFinish)
        {
            var scheduler = new BattleStageScheduler();
            int starts = 0;
            int blocks = 0;
            var number = new BattleStageNumber(2f, _ => starts++, blocked ? _ => blocks++ : null);
            scheduler.Add("target", "number", number, 0f);
            for (long tick = 1; tick <= expectedFinish; tick++)
            {
                scheduler.Advance(tick);
                Assert.That(number.Finished, Is.EqualTo(tick == expectedFinish));
            }
            Assert.That(number.StartTime, Is.EqualTo(blocked ? BattleStageScheduler.TimeAt(1) : 0f));
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(blocks, Is.EqualTo(blocked ? 1 : 0));
        }

        [Test]
        public void FloatStartAndDoubleDifferenceCanFinishOneTickEarlier()
        {
            var scheduler = new BattleStageScheduler();
            var number = new BattleStageNumber(2f, _ => { });
            scheduler.Add("target", "number", number, BattleStageScheduler.TimeAt(7));
            scheduler.Advance(7);
            scheduler.Advance(106);
            Assert.That(number.Finished, Is.False);
            float now = BattleStageScheduler.TimeAt(107);
            Assert.That(now - number.StartTime, Is.EqualTo(2f));
            Assert.That((double)now - number.StartTime, Is.GreaterThan(2d));
            scheduler.Advance(107);
            Assert.That(number.Finished, Is.True);
        }

        [Test]
        public void DurationEqualityDoesNotFinishNumber()
        {
            var scheduler = new BattleStageScheduler();
            var number = new BattleStageNumber(2f, _ => { });
            scheduler.Add("target", "miss", number, 0f);
            scheduler.Advance(0);
            scheduler.Advance(100);
            Assert.That(number.Finished, Is.False);
            scheduler.Advance(101);
            Assert.That(number.Finished, Is.True);
        }

        [Test]
        public void DefaultCapacityMatchesOriginalPool()
        {
            var scheduler = new BattleStageScheduler();
            for (int i = 0; i < 256; i++)
                Assert.That(scheduler.Add("actor", "action", new Job(_ => false), 0f).Slot, Is.EqualTo(i));
            Assert.Throws<InvalidOperationException>(() => scheduler.Add("actor", "number", new Job(_ => false), 0f));
        }

        [Test]
        public void HandleFromOtherPoolCannotCancelTask()
        {
            var first = new BattleStageScheduler();
            var second = new BattleStageScheduler();
            BattleStageHandle handle = first.Add("actor", "action", new Job(_ => false), 0f);
            BattleStageHandle other = second.Add("actor", "action", new Job(_ => false), 0f);
            Assert.That(handle, Is.Not.EqualTo(other));
            Assert.That(second.Cancel(handle), Is.False);
            Assert.That(second.Count, Is.EqualTo(1));
            Assert.That(first.IsActive(default), Is.False);
        }

        [Test]
        public void RecursiveAdvanceIsRejectedAndPassCanRecover()
        {
            var scheduler = new BattleStageScheduler();
            scheduler.Add("actor", "action", new Job(context =>
            {
                Assert.Throws<InvalidOperationException>(() => scheduler.Advance(context.Tick));
                return true;
            }), 0f);
            scheduler.Advance(0);
            scheduler.Advance(1);
            Assert.That(scheduler.Count, Is.Zero);
        }

        [Test]
        public void CancellationDetachesBeforeNotificationAndPreservesReplacementSlot()
        {
            var scheduler = new BattleStageScheduler(1);
            BattleStageHandle handle = default, replacement = default;
            int cancellations = 0, advances = 0;
            handle = scheduler.Add("actor", "owning", new CancellableJob(() =>
            {
                cancellations++;
                Assert.That(scheduler.Cancel(handle), Is.False);
                Assert.That(scheduler.Count, Is.Zero);
                replacement = scheduler.Add("next", "replacement", new Job(_ =>
                {
                    advances++;
                    return true;
                }), 0f);
            }), 0f);
            Assert.That(scheduler.Cancel(handle), Is.True);
            Assert.That(scheduler.Cancel(handle), Is.False);
            Assert.That(cancellations, Is.EqualTo(1));
            Assert.That(scheduler.IsActive(replacement), Is.True);
            scheduler.Advance(0);
            scheduler.Advance(1);
            Assert.That(advances, Is.EqualTo(1));
            Assert.That(scheduler.Count, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FinishedTaskRemovalDoesNotNotifyCancellation(bool autoDelete)
        {
            var scheduler = new BattleStageScheduler();
            int cancellations = 0;
            BattleStageHandle handle = scheduler.Add("actor", "finished", new CancellableJob(() => cancellations++, true), 0f, autoDelete);
            scheduler.Advance(0);
            if (autoDelete)
                scheduler.Advance(1);
            else
                Assert.That(scheduler.Cancel(handle), Is.True);
            Assert.That(cancellations, Is.Zero);
            Assert.That(scheduler.Count, Is.Zero);
        }

        private sealed class CancellableJob : IBattleStageCancellableJob
        {
            private readonly Action _cancel;
            private readonly bool _finished;

            internal CancellableJob(Action cancel, bool finished = false)
            {
                _cancel = cancel;
                _finished = finished;
            }

            public bool Advance(BattleStageContext context) => _finished;

            public void Cancel() => _cancel();
        }

        private sealed class Job : IBattleStageJob
        {
            private readonly Func<BattleStageContext, bool> _advance;

            internal Job(Func<BattleStageContext, bool> advance) => _advance = advance;

            public bool Advance(BattleStageContext context) => _advance(context);
        }
    }
}
