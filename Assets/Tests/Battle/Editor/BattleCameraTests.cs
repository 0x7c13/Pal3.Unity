// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using Battle;
    using NUnit.Framework;

    public sealed class BattleCameraTests
    {
        private const BattleCameraFlags TimedLine = BattleCameraFlags.StartCamera | BattleCameraFlags.EndCamera | BattleCameraFlags.Time | BattleCameraFlags.Line;

        private static BattleCameraTask Line(float duration = 1) => new()
        {
            Flags = TimedLine,
            ExpectedSeconds = duration,
            Start = new BattleCameraPose(new BattleMovementPoint(10, 20, 30), 0, 0, 0),
            End = new BattleCameraPose(new BattleMovementPoint(110, 20, 30), 1, 2, 3)
        };

        private static BattleCameraController Camera(params BattleCameraTask[] tasks)
            => new(new IReadOnlyList<BattleCameraTask>[] { tasks }, new BattleCameraPose(new BattleMovementPoint(1, 2, 3), .1f, .2f, .3f));

        [Test]
        public void TaskCompletionAndNextTaskActivationOccupySeparateStageUpdates()
        {
            BattleCameraTask first = Line(0), second = Line(1);
            second.Flags &= ~BattleCameraFlags.StartCamera;
            second.End = new BattleCameraPose(new BattleMovementPoint(210, 20, 30), 2, 4, 6);
            var camera = Camera(first, second);
            Assert.That(camera.StartList(0), Is.True);
            camera.Advance(.02f, .02f);
            Assert.That(camera.Pose.Position.X, Is.EqualTo(110));
            Assert.That(camera.CurrentTask, Is.Zero);
            Assert.That(camera.CurrentTaskFinished, Is.True);
            camera.Advance(.02f, .02f);
            Assert.That(camera.CurrentTask, Is.EqualTo(1));
            Assert.That(camera.Pose.Position.X, Is.EqualTo(110));
            camera.Advance(.02f, .02f);
            Assert.That(camera.Pose.Position.X, Is.EqualTo(110));
            camera.Advance(.5f, .5f);
            Assert.That(camera.Pose.Position.X, Is.EqualTo(160).Within(.0001));
        }

        [Test]
        public void InitialSpeedSolverUsesBothAccelerationIntervalsBeforeTheirStartTimes()
        {
            BattleCameraTask task = Line(2);
            task.FirstAccelerationStart = 3;
            task.SecondAccelerationStart = 4;
            task.FirstAcceleration = 10;
            task.SecondAcceleration = 20;
            var camera = Camera(task);
            camera.StartList(0);
            camera.Advance(0, 0);
            camera.Advance(1, 1);
            // v0=(100-(10*(-1)^2/2 + 30*(-2)^2/2))/2=17.5.
            Assert.That(camera.Pose.Position.X, Is.EqualTo(27.5f));
            Assert.That(camera.Pose.Yaw, Is.EqualTo(.5f));
        }

        [Test]
        public void AccelerationSecondIntervalAddsTheSumOfBothAccelerations()
        {
            BattleCameraTask task = Line();
            task.Flags = BattleCameraFlags.StartCamera | BattleCameraFlags.Speed | BattleCameraFlags.Time | BattleCameraFlags.Line;
            task.StartSpeed = 5;
            task.ExpectedSeconds = 4;
            task.FirstAccelerationStart = 1;
            task.SecondAccelerationStart = 2;
            task.FirstAcceleration = 2;
            task.SecondAcceleration = 3;
            var camera = Camera(task);
            camera.StartList(0);
            camera.Advance(0, 0);
            camera.Advance(3, 3);
            Assert.That(camera.Pose.Position.X, Is.EqualTo(10 + 15 + 4 + 2.5));
        }

        [Test]
        public void TimeUnknownClampsDistanceAndRetainsItsObservedDurationForReplay()
        {
            BattleCameraTask task = Line();
            task.Flags = BattleCameraFlags.StartCamera | BattleCameraFlags.EndCamera | BattleCameraFlags.Speed | BattleCameraFlags.Line;
            task.StartSpeed = 80;
            var camera = Camera(task);
            camera.StartList(0);
            camera.Advance(0, 0);
            camera.Advance(2, 2);
            Assert.That(camera.Pose.Position.X, Is.EqualTo(110));
            Assert.That(camera.CurrentExpectedSeconds, Is.EqualTo(2));
            Assert.That(camera.CurrentTaskFinished, Is.True);
            camera.StartList(0);
            Assert.That(camera.CurrentExpectedSeconds, Is.EqualTo(2));
            Assert.That(camera.CurrentTaskFinished, Is.False);
        }

        [Test]
        public void TargetUnknownDoesNotClampTravelOrOrientationAtTheLastSample()
        {
            BattleCameraTask task = Line();
            task.Flags &= ~BattleCameraFlags.EndCamera;
            task.Flags |= BattleCameraFlags.Speed;
            task.StartSpeed = 100;
            var camera = Camera(task);
            camera.StartList(0);
            camera.Advance(0, 0);
            camera.Advance(1.2f, 1.2f);
            Assert.That(camera.Pose.Position.X, Is.EqualTo(130).Within(.0001));
            Assert.That(camera.Pose.Yaw, Is.EqualTo(1.2f).Within(.0001));
            Assert.That(camera.CurrentTaskFinished, Is.True);
        }

        [Test]
        public void NegativeSolvedSpeedAndTimeReviseCanFinishAtTheDestinationEarly()
        {
            BattleCameraTask task = Line(10);
            task.FirstAcceleration = 100;
            task.Flags |= BattleCameraFlags.TimeRevise;
            var camera = Camera(task);
            camera.StartList(0);
            camera.Advance(0, 0);
            camera.Advance(2, 2);
            Assert.That(camera.Pose.Position.X, Is.EqualTo(110));
            Assert.That(camera.CurrentExpectedSeconds, Is.EqualTo(2));
            Assert.That(camera.CurrentTaskFinished, Is.True);
            // Native angles use the requested-time ratio before time revision.
            Assert.That(camera.Pose.Yaw, Is.EqualTo(.2f));
        }

        [Test]
        public void AbsoluteTasksContinueWhileTheCombatClockIsFrozen()
        {
            BattleCameraTask task = Line();
            task.Flags |= BattleCameraFlags.AbsoluteTime;
            var camera = Camera(task);
            camera.StartList(0);
            camera.Advance(0, .1f);
            camera.Advance(0, .5f);
            Assert.That(camera.Pose.Position.X, Is.EqualTo(60).Within(.0001));
            camera.StopList();
            Assert.That(camera.IsRunning, Is.False);
            Assert.That(camera.StartList(99), Is.False);
        }

        [Test]
        public void CounterYawTakesTheLongArcWithoutApplyingUnusedAngleFlags()
        {
            BattleCameraTask task = Line();
            task.Flags |= BattleCameraFlags.CounterYaw | BattleCameraFlags.CounterPitch | BattleCameraFlags.CounterRoll;
            var camera = Camera(task);
            camera.StartList(0);
            camera.Advance(0, 0);
            camera.Advance(.5f, .5f);
            Assert.That(camera.Pose.Yaw, Is.EqualTo((float)Math.PI + .5f).Within(.000001));
            Assert.That(camera.Pose.Pitch, Is.EqualTo(1));
            Assert.That(camera.Pose.Roll, Is.EqualTo(1.5));
        }

        [TestCase(90f, 90f, 0f, -100f)]
        [TestCase(-90f, -90f, 0f, 100f)]
        public void CircleUsesDegreesAndTheOriginalHeightRamp(float angle, float speed, float x, float z)
        {
            var task = new BattleCameraTask
            {
                Flags = BattleCameraFlags.StartCamera | BattleCameraFlags.EndCamera | BattleCameraFlags.Speed | BattleCameraFlags.Circle,
                Start = new BattleCameraPose(new BattleMovementPoint(100, 30, 0), .2f, .3f, .4f),
                CircleAngle = angle,
                AngularSpeed = speed,
                CircleCenter = new BattleMovementPoint(0, 10, 0),
                CircleEnd = new BattleMovementPoint(900, 60, 900)
            };
            var camera = Camera(task);
            camera.StartList(0);
            camera.Advance(0, 0);
            camera.Advance(1, 1);
            Assert.That(camera.Pose.Position.X, Is.EqualTo(x).Within(.00002));
            Assert.That(camera.Pose.Position.Z, Is.EqualTo(z).Within(.00002));
            Assert.That(camera.Pose.Position.Y, Is.EqualTo(80));
            Assert.That(camera.Pose.Yaw, Is.EqualTo(.2f));
            Assert.That(camera.CurrentTaskFinished, Is.True);
        }

        [Test]
        public void UnimplementedNativeCircleBranchesAndOverspecifiedLineStayIdle()
        {
            foreach (BattleCameraFlags flags in new[] { BattleCameraFlags.Circle | BattleCameraFlags.Time, TimedLine | BattleCameraFlags.Speed })
            {
                BattleCameraTask task = Line();
                task.Flags = flags;
                var camera = Camera(task);
                camera.StartList(0);
                camera.Advance(10, 10);
                Assert.That(camera.Pose.Position.X, Is.EqualTo(1));
                Assert.That(camera.CurrentTaskFinished, Is.False);
            }
        }

        [Test]
        public void ConsecutiveMechanicsTasksPreserveSpeedRegardlessOfSpliceAndInertia()
        {
            BattleCameraTask first = Line(.1f), second = Line(1);
            first.Flags = second.Flags = BattleCameraFlags.StartCamera | BattleCameraFlags.Mechanics;
            first.Start = new BattleCameraPose(default, 0, 0, 0);
            first.End = new BattleCameraPose(new BattleMovementPoint(10, 0, 0), 0, 0, 0);
            second.Start = new BattleCameraPose(new BattleMovementPoint(10, 0, 0), 0, 0, 0);
            second.End = new BattleCameraPose(new BattleMovementPoint(10, 0, 100), 0, 0, 0);
            second.Inertia = 999;
            second.Splice = false;
            second.TimeCompensation = .5f;
            var camera = Camera(first, second);
            camera.StartList(0);
            camera.Advance(.1f, .1f); // Native mechanics integrates on its activation call.
            camera.Advance(.1f, .1f);
            camera.Advance(.1f, .1f); // Advances only the list cursor.
            camera.Advance(.1f, .1f);
            Assert.That(camera.Pose.Position.X, Is.EqualTo(19.95037).Within(.0001));
            Assert.That(camera.Pose.Position.Z, Is.EqualTo(.995037).Within(.0001));
            camera.Advance(1, 1);
            Assert.That(camera.CurrentTaskFinished, Is.False);
            camera.Advance(.5f, .5f);
            Assert.That(camera.CurrentTaskFinished, Is.True);
        }

        [Test]
        public void PositionTraceCapturesLineOriginAndSightTracePreservesTheNativeDisplacementQuirk()
        {
            BattleCameraTask task = Line();
            task.Flags = BattleCameraFlags.StartCamera | BattleCameraFlags.Time | BattleCameraFlags.Speed | BattleCameraFlags.Line | BattleCameraFlags.PositionTrace | BattleCameraFlags.SightTrace;
            task.StartSpeed = 100;
            BattleMovementPoint trace = new(200, 60, 200);
            var camera = new BattleCameraController(new IReadOnlyList<BattleCameraTask>[] { new[] { task } }, default, _ => trace);
            camera.StartList(0);
            camera.Advance(0, 0);
            trace = new BattleMovementPoint(210, 60, 200);
            camera.Advance(.5f, .5f);
            Assert.That(camera.Pose.Position.X, Is.EqualTo(70));
            // Looking from displacement (60,0,0), rather than camera (70,20,30).
            float expected = (float)Math.Atan(-150f / 200f) + (float)Math.PI;
            Assert.That(camera.Pose.Yaw, Is.EqualTo(expected).Within(.000001));
        }

        [Test]
        public void RandomSwingTogglesTrailWhileStoredFovSwingAndSpliceRemainUnused()
        {
            BattleCameraTask task = Line();
            task.Flags |= BattleCameraFlags.RandomSwing;
            task.Splice = true;
            task.StartFieldOfView = 5;
            task.EndFieldOfView = 100;
            task.SwingAmplitude = 100;
            task.SwingFrequency = 100;
            var camera = Camera(task);
            camera.StartList(0);
            camera.Advance(0, 0);
            camera.Advance(.5f, .5f);
            Assert.That(camera.TrailEnabled, Is.True);
            Assert.That(camera.FieldOfView, Is.EqualTo(50));
            Assert.That(camera.Pose.Position.X, Is.EqualTo(60));
            Assert.That(camera.Pose.Position.Y, Is.EqualTo(20));
        }

        [Test]
        public void QuakeDecaysBeforeSamplingAndUsesOnlyScaledTime()
        {
            var camera = Camera();
            camera.SetQuake(10, 2, 2);
            camera.Advance(0, 1);
            Assert.That(camera.Pose.Position.Y, Is.EqualTo(2));
            camera.Advance(.5f, 0);
            Assert.That(camera.Pose.Position.Y, Is.EqualTo(2 + 9 * (float)Math.Sin(1)).Within(.000001));
            Assert.That(camera.Pose.Position.X, Is.EqualTo(1));
        }

        [Test]
        public void StageTimeAccumulatesInDoubleBeforeItsFloatProjection()
        {
            var camera = Camera();
            double time = 0;
            for (int frame = 0; frame < 100000; frame++)
            {
                time += .02d;
                camera.Advance(.02d, .02d);
            }
            Assert.That(camera.ScaledTime, Is.EqualTo((float)time));
            Assert.That(camera.AbsoluteTime, Is.EqualTo((float)time));
            Assert.That(camera.ScaledTime, Is.EqualTo(2000));
            camera.AdvanceAt(3000.125d, 4000.25d, 1000.125d, 2000.25d);
            Assert.That(camera.ScaledTime, Is.EqualTo(3000.125f));
            Assert.That(camera.AbsoluteTime, Is.EqualTo(4000.25f));
        }
    }
}
