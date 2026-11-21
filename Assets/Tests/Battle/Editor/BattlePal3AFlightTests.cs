// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using Battle;
    using NUnit.Framework;

    public sealed class BattlePal3AFlightTests
    {
        // Published PAL3A ProcFlyRun 0x4ffb98: the near-distance test is
        // eight units after integrating; only over or death finishes the task.
        [TestCase(0f)]
        [TestCase(1.99f)]
        [TestCase(8f)]
        public void ShortFlightsStillWaitForTheActualOverMarker(float distance)
        {
            var movement = new BattleMovement("PAL3A", new BattleMovementPlan(BattleMovementKind.Fly, default,
                new BattleMovementPoint(0, 0, distance), flyUpTick: 5, flyDownTick: 30, overTick: 35));
            for (int call = 0; call < 35; call++)
            {
                movement.Step();
                Assert.That(movement.Finished, Is.False, "call " + call);
            }
            movement.Step();
            Assert.That(movement.Finished, Is.True);
            Assert.That(movement.Position.Z, Is.EqualTo(distance));
        }

        [Test]
        public void FlightDownStopsAnUnfinishedPal3aFlightImmediately()
        {
            var movement = new BattleMovement("PAL3A", new BattleMovementPlan(BattleMovementKind.Fly, default,
                new BattleMovementPoint(0, 0, 100), flyUpTick: 5, flyDownTick: 6, overTick: 35));
            for (int call = 0; call <= 5; call++)
                movement.Step();
            Assert.That(movement.Position.Z, Is.EqualTo(3.3333334922790527f));
            movement.Step();
            Assert.That(movement.Position.Z, Is.EqualTo(3.3333334922790527f));
            Assert.That(movement.Finished, Is.False);
            for (int call = 7; call <= 35; call++)
                movement.Step();
            Assert.That(movement.Position.Z, Is.EqualTo(100));
            Assert.That(movement.Finished, Is.True);
        }

        [Test]
        public void ActivationDiscardsFlyUpAndDoesNotInventAnOver()
        {
            var movement = new BattleMovement("PAL3A", new BattleMovementPlan(BattleMovementKind.Fly, default,
                new BattleMovementPoint(0, 0, 100), flyUpTick: 0));
            for (int call = 0; call < 120; call++)
                movement.Step();
            Assert.That(movement.Position.Z, Is.Zero);
            Assert.That(movement.Finished, Is.False);
        }

        [Test]
        public void DeathKeepsTheNativeFirstIntegratedPosition()
        {
            var movement = new BattleMovement("PAL3A", new BattleMovementPlan(BattleMovementKind.Fly, default,
                new BattleMovementPoint(0, 0, 100), flyUpTick: 5, overTick: 35));
            for (int call = 0; call < 5; call++)
                movement.Step();
            movement.Step(alive: false);
            Assert.That(movement.Position.Z, Is.EqualTo(3.3333334922790527f));
            Assert.That(movement.Finished, Is.True);
        }
    }
}
