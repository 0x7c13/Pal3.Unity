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

    public sealed class BattleBodyAnimationBindingTests
    {
        // Executed installed PAL3 gbActor.Update + MVAction.GetTriggeredEvent.
        // The synthetic fixtures contain no original resource content. A has no
        // engine DLL locally; its published Role.Update supplies rate 1 or .65.
        private static IEnumerable<TestCaseData> EngineGoldens()
        {
            yield return new TestCaseData(96u, 0.0f, false, new uint[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new string[] { "", "", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(96u, 0.0f, true, new uint[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new string[] { "", "", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(96u, 0.65f, false, new uint[] { 62, 124, 186, 248, 310, 372, 434, 496, 558, 620, 682, 744 }, new string[] { "257,258", "over,259,260,256,257", "258", "over,259,260,256,257", "over,258,259,260,256,257", "258", "over,259,260,256,257", "over,258,259,260,256,257", "258", "over,259,260,256,257", "over,258,259,260,256,257", "258" });
            yield return new TestCaseData(96u, 0.65f, true, new uint[] { 62, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92 }, new string[] { "257,258", "over", "over", "over", "over", "over", "over", "over", "over", "over", "over", "over" });
            yield return new TestCaseData(96u, 1.0f, false, new uint[] { 96, 192, 288, 384, 480, 576, 672, 768, 864, 960, 1056, 1152 }, new string[] { "over,257,258,259,260,256", "over,257,258,259,260,256", "over,257,258,259,260,256", "over,257,258,259,260,256", "over,257,258,259,260,256", "over,257,258,259,260,256", "over,257,258,259,260,256", "over,257,258,259,260,256", "over,257,258,259,260,256", "over,257,258,259,260,256", "over,257,258,259,260,256", "over,257,258,259,260,256" });
            yield return new TestCaseData(96u, 1.0f, true, new uint[] { 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92 }, new string[] { "over", "over", "over", "over", "over", "over", "over", "over", "over", "over", "over", "over" });
            yield return new TestCaseData(96u, 1.3f, true, new uint[] { 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92 }, new string[] { "over", "over", "over", "over", "over", "over", "over", "over", "over", "over", "over", "over" });
            yield return new TestCaseData(125u, 0.0f, false, new uint[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new string[] { "", "", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(125u, 0.0f, true, new uint[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new string[] { "", "", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(125u, 0.65f, false, new uint[] { 62, 124, 186, 248, 310, 372, 434, 496, 558, 620, 682, 744 }, new string[] { "257,258", "259", "over,260,256,257", "258,259", "over,260,256,257", "258,259", "over,260,256,257", "258,259", "over,260,256,257", "258,259", "over,260,256,257", "258,259" });
            yield return new TestCaseData(125u, 0.65f, true, new uint[] { 62, 124, 121, 121, 121, 121, 121, 121, 121, 121, 121, 121 }, new string[] { "257,258", "259", "over", "over", "over", "over", "over", "over", "over", "over", "over", "over" });
            yield return new TestCaseData(125u, 1.0f, false, new uint[] { 96, 192, 288, 384, 480, 576, 672, 768, 864, 960, 1056, 1152 }, new string[] { "257,258,259", "over,260,256,257,258", "over,259,260,256,257", "over,258,259,260,256,257", "258,259", "over,260,256,257,258", "over,259,260,256,257", "over,258,259,260,256,257", "258,259", "over,260,256,257,258", "over,259,260,256,257", "over,258,259,260,256,257" });
            yield return new TestCaseData(125u, 1.0f, true, new uint[] { 96, 121, 121, 121, 121, 121, 121, 121, 121, 121, 121, 121 }, new string[] { "257,258,259", "over", "over", "over", "over", "over", "over", "over", "over", "over", "over", "over" });
            yield return new TestCaseData(125u, 1.3f, false, new uint[] { 124, 248, 372, 496, 620, 744, 868, 992, 1116, 1240, 1364, 1488 }, new string[] { "257,258,259", "over,260,256,257,258,259", "over,260,256,257,258,259", "over,260,256,257,258,259", "over,260,256,257,258,259", "over,260,256,257,258,259", "over,260,256,257,258,259", "over,260,256,257,258,259", "over,260,256,257,258,259", "over,260,256,257,258,259", "over,260,256,257,258,259", "over,260,256,257,258,259" });
            yield return new TestCaseData(125u, 1.3f, true, new uint[] { 124, 121, 121, 121, 121, 121, 121, 121, 121, 121, 121, 121 }, new string[] { "257,258,259", "over", "over", "over", "over", "over", "over", "over", "over", "over", "over", "over" });
            yield return new TestCaseData(240u, 0.0f, false, new uint[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new string[] { "", "", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(240u, 0.0f, true, new uint[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new string[] { "", "", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(240u, 0.65f, false, new uint[] { 62, 124, 186, 248, 310, 372, 434, 496, 558, 620, 682, 744 }, new string[] { "257,258", "259", "", "over,260,256,257", "258", "259", "", "over,260,256,257", "258", "259", "", "over,260,256,257" });
            yield return new TestCaseData(240u, 0.65f, true, new uint[] { 62, 124, 186, 236, 236, 236, 236, 236, 236, 236, 236, 236 }, new string[] { "257,258", "259", "", "over", "over", "over", "over", "over", "over", "over", "over", "over" });
            yield return new TestCaseData(240u, 1.0f, false, new uint[] { 96, 192, 288, 384, 480, 576, 672, 768, 864, 960, 1056, 1152 }, new string[] { "257,258,259", "", "over,260,256,257", "258,259", "over,260,256", "257,258,259", "", "over,260,256,257", "258,259", "over,260,256", "257,258,259", "" });
            yield return new TestCaseData(240u, 1.0f, true, new uint[] { 96, 192, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236 }, new string[] { "257,258,259", "", "over", "over", "over", "over", "over", "over", "over", "over", "over", "over" });
            yield return new TestCaseData(240u, 1.3f, false, new uint[] { 124, 248, 372, 496, 620, 744, 868, 992, 1116, 1240, 1364, 1488 }, new string[] { "257,258,259", "over,260,256,257", "258,259", "over,260,256,257", "258,259", "over,260,256,257", "258,259", "over,260,256,257", "258,259", "over,260,256,257", "258,259", "over,260,256,257" });
            yield return new TestCaseData(240u, 1.3f, true, new uint[] { 124, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236 }, new string[] { "257,258,259", "over", "over", "over", "over", "over", "over", "over", "over", "over", "over", "over" });
            yield return new TestCaseData(480u, 0.0f, false, new uint[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new string[] { "", "", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(480u, 0.0f, true, new uint[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new string[] { "", "", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(480u, 0.65f, false, new uint[] { 62, 124, 186, 248, 310, 372, 434, 496, 558, 620, 682, 744 }, new string[] { "257,258", "259", "", "", "", "", "", "over,260,256,257", "258", "259", "", "" });
            yield return new TestCaseData(480u, 0.65f, true, new uint[] { 62, 124, 186, 248, 310, 372, 434, 476, 476, 476, 476, 476 }, new string[] { "257,258", "259", "", "", "", "", "", "over", "over", "over", "over", "over" });
            yield return new TestCaseData(480u, 1.0f, false, new uint[] { 96, 192, 288, 384, 480, 576, 672, 768, 864, 960, 1056, 1152 }, new string[] { "257,258,259", "", "", "", "over,260,256", "257,258,259", "", "", "", "over,260,256", "257,258,259", "" });
            yield return new TestCaseData(480u, 1.0f, true, new uint[] { 96, 192, 288, 384, 476, 476, 476, 476, 476, 476, 476, 476 }, new string[] { "257,258,259", "", "", "", "over", "over", "over", "over", "over", "over", "over", "over" });
            yield return new TestCaseData(480u, 1.3f, false, new uint[] { 124, 248, 372, 496, 620, 744, 868, 992, 1116, 1240, 1364, 1488 }, new string[] { "257,258,259", "", "", "over,260,256,257", "258,259", "", "", "over,260,256,257", "258,259", "", "", "over,260,256,257" });
            yield return new TestCaseData(480u, 1.3f, true, new uint[] { 124, 248, 372, 476, 476, 476, 476, 476, 476, 476, 476, 476 }, new string[] { "257,258,259", "", "", "over", "over", "over", "over", "over", "over", "over", "over", "over" });
            yield return new TestCaseData(4800u, 0.0f, false, new uint[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new string[] { "", "", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(4800u, 0.0f, true, new uint[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new string[] { "", "", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(4800u, 0.65f, false, new uint[] { 62, 124, 186, 248, 310, 372, 434, 496, 558, 620, 682, 744 }, new string[] { "257,258", "259", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(4800u, 0.65f, true, new uint[] { 62, 124, 186, 248, 310, 372, 434, 496, 558, 620, 682, 744 }, new string[] { "257,258", "259", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(4800u, 1.0f, false, new uint[] { 96, 192, 288, 384, 480, 576, 672, 768, 864, 960, 1056, 1152 }, new string[] { "257,258,259", "", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(4800u, 1.0f, true, new uint[] { 96, 192, 288, 384, 480, 576, 672, 768, 864, 960, 1056, 1152 }, new string[] { "257,258,259", "", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(4800u, 1.3f, false, new uint[] { 124, 248, 372, 496, 620, 744, 868, 992, 1116, 1240, 1364, 1488 }, new string[] { "257,258,259", "", "", "", "", "", "", "", "", "", "", "" });
            yield return new TestCaseData(4800u, 1.3f, true, new uint[] { 124, 248, 372, 496, 620, 744, 868, 992, 1116, 1240, 1364, 1488 }, new string[] { "257,258,259", "", "", "", "", "", "", "", "", "", "", "" });
        }

        [TestCaseSource(nameof(EngineGoldens))]
        public void ActorUpdateMatchesPublishedRawClockAndTriggerOrder(uint duration, float rate, bool once, uint[] times, string[] messages)
        {
            var markers = new[]
            {
                Marker(0, "256"), Marker(1, "257"), Marker(Math.Min(62u, duration), "258"),
                Marker(Math.Min(96u, duration), "259"), Marker(duration, "260")
            };
            var binding = new BattleBodyAnimationBinding(new BattlePhysicalAnimation("fixture", 100, 1, markers, duration), !once);
            for (int index = 0; index < times.Length; index++)
            {
                binding.Advance(rate);
                Assert.That(binding.GameBoxTime, Is.EqualTo(times[index]), "frame " + index);
                Assert.That(Drain(binding), Is.EqualTo(messages[index]), "frame " + index);
            }
        }

        [Test]
        public void UnknownMessageConsumesOneRolePassBeforeWork()
        {
            var animation = new BattlePhysicalAnimation("attack", 5, 1, new[]
            {
                Marker(1, "sound"), new BattlePhysicalMarker(0, BattlePhysicalMarkerKind.Work, 1, "work")
            });
            var binding = new BattleBodyAnimationBinding(animation, false);
            binding.Advance();
            binding.PopMessage();
            Assert.That(binding.LastPoppedMarker, Is.EqualTo(BattlePhysicalMarkerKind.Other));
            binding.PopMessage();
            Assert.That(binding.LastPoppedMarker, Is.EqualTo(BattlePhysicalMarkerKind.Work));
        }

        [Test]
        public void FailedGotoAndResetKeepPendingMessagesButSuccessfulGotoClearsThem()
        {
            var animation = new BattlePhysicalAnimation("attack", 5, 1, new[] { Marker(1, "old1"), Marker(1, "old2") });
            var binding = new BattleBodyAnimationBinding(animation);
            binding.Advance();
            binding.PopMessage();
            Assert.That(binding.TryPlay(null, false), Is.False);
            Assert.That(binding.GameBoxTime, Is.EqualTo(96u));
            binding.Reset();
            Assert.That(binding.GameBoxTime, Is.Zero);
            Assert.That(binding.PendingMessageCount, Is.EqualTo(1));
            Assert.That(binding.LastPoppedName, Is.EqualTo("old1"));
            binding.TryPlay(animation, false);
            Assert.That(binding.PendingMessageCount, Is.Zero);
            Assert.That(binding.LastPoppedName, Is.EqualTo("old1"));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void RealMessageOverridesEstimatedFlightMarkerTick(string variant)
        {
            var movement = new BattleMovement(variant, new BattleMovementPlan(BattleMovementKind.Fly,
                new BattleMovementPoint(0, 0, 0), new BattleMovementPoint(100, 0, 0), 1, 3, 4));
            movement.Step(true, BattleMovementMessage.FlyUp); // activation ignores old clip's message
            movement.Step(true, BattleMovementMessage.None);
            Assert.That(movement.Position.X, Is.Zero);
            movement.Step(true, BattleMovementMessage.FlyUp);
            Assert.That(movement.Position.X, Is.GreaterThan(0));
            movement.Step(true, BattleMovementMessage.None);
            movement.Step(true, BattleMovementMessage.None);
            Assert.That(movement.Finished, Is.False);
            movement.Step(true, BattleMovementMessage.Over);
            Assert.That(movement.Finished, Is.True);
            Assert.That(movement.Position.X, Is.EqualTo(100));
        }

        private static BattlePhysicalMarker Marker(uint tick, string name) => new(0, BattlePhysicalMarkerKind.Other, tick, name);

        private static string Drain(BattleBodyAnimationBinding binding)
        {
            var names = new List<string>();
            while (binding.PendingMessageCount > 0)
            {
                binding.PopMessage();
                names.Add(binding.LastPoppedName);
            }
            return string.Join(",", names);
        }
    }
}
