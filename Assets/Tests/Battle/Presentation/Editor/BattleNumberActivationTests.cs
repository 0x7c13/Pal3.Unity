// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using Battle.Presentation;
    using NUnit.Framework;

    public sealed class BattleNumberActivationTests
    {
        private sealed class Catalog : IBattleCatalog
        {
            public AttackTiming Attack(string source, string target) => new(0, 2, new[] { 1 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 1;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => 0;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class Rules : IBattleRules
        {
            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => 0;

            public int GaugePerStep(BattleParticipant actor) => 0;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) => new(-10, false, false);
        }

        [TestCase("PAL3", false, false)]
        [TestCase("PAL3A", false, false)]
        [TestCase("PAL3", true, false)]
        [TestCase("PAL3A", true, false)]
        [TestCase("PAL3", false, true)]
        [TestCase("PAL3A", false, true)]
        public void ActivationFreezesTheNativeAnchorAndConsumesOnlyTheOwnedRandomDraw(string variant, bool miss, bool block)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "fixture", DataFingerprint = "numbers", Seed = 1 };
            foreach (BattleSide side in Enum.GetValues(typeof(BattleSide)))
                scenario.Participants.Add(new BattleParticipant
                {
                    InstanceId = side.ToString(),
                    ActorId = side == BattleSide.Ally ? 101 : 701,
                    Side = side,
                    Slot = BattleSlot.Water,
                    Attributes = new[] { 100, 100, 100, 10, 0, 10, 0, 0, 0, 0, 0, 0 },
                    Vitals = new[] { 100, 100, 100 }
                });
            var definitions = new BattleDefinitions(variant, scenario.DataFingerprint,
                Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Ally ? 0 : -1
                })));
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), new Rules(), definitions);
            var actors = (IDictionary)typeof(BattleSession).GetField("_byId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            object target = actors["Enemy"];
            void Position(string name, BattleMovementPoint point) => target.GetType().GetField(name).SetValue(target, point);
            Position("Position", new BattleMovementPoint(123, 456, 789));
            Position("Home", new BattleMovementPoint(12, 34, 56));
            typeof(BattleSession).GetMethod("QueueNumber", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session,
                new object[] { target, 0, -123, miss ? 1f : 2f, block, miss, false, false });
            session.Step();
            BattleEvent number = session.Events.Single(entry => entry.Kind == BattleEventKind.NumberShown);
            Assert.That(number.NumberActivationTick, Is.EqualTo(1));
            Assert.That(number.NumberStartedTime, Is.EqualTo(block ? .02f : 0));
            Assert.That(number.NumberPosition.X, Is.EqualTo(miss ? 12 : 123));
            Assert.That(number.NumberPosition.Y, Is.EqualTo(miss ? 114 : 50));
            Assert.That(number.NumberPosition.Z, Is.EqualTo(miss ? 56 : 789));
            var expectedRandom = new BattleRandom(1);
            int expectedOffset = expectedRandom.Next() % 32;
            Assert.That(number.NumberOffset, Is.EqualTo(expectedOffset));
            Assert.That(number.RandomState, Is.EqualTo(expectedRandom.State));
            Position("Position", new BattleMovementPoint(999, 999, 999));
            var projection = new BattleNumberProjection();
            projection.Synchronize(session.Events, session.SimulationTick);
            BattleNumberFrame frame = projection.Sample().Single();
            Assert.That(frame.Position.X, Is.EqualTo(number.NumberPosition.X));
            Assert.That(frame.Position.Z, Is.EqualTo(number.NumberPosition.Z));
            var random = (BattleRandom)typeof(BattleSession).GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            Assert.That(random.State, Is.EqualTo(number.RandomState), "Projection must never draw from battle RNG.");
        }
    }
}
