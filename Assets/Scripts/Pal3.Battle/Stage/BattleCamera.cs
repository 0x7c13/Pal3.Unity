// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [Flags]
    public enum BattleCameraFlags
    {
        None = 0,
        StartCamera = 1,
        EndCamera = 2,
        Time = 4,
        Speed = 8,
        Line = 16,
        Circle = 32,
        Mechanics = 64,
        TimeRevise = 128,
        AbsoluteTime = 256,
        CounterYaw = 512,
        CounterPitch = 1024,
        CounterRoll = 2048,
        RandomSwing = 4096,
        SightTrace = 8192,
        PositionTrace = 16384
    }

    // Position is in original GameBox units; all three angles are radians.
    public readonly struct BattleCameraPose
    {
        public BattleMovementPoint Position
        {
            get;
        }

        public float Yaw
        {
            get;
        }

        public float Pitch
        {
            get;
        }

        public float Roll
        {
            get;
        }

        public BattleCameraPose(BattleMovementPoint position, float yaw, float pitch, float roll)
        {
            Position = position;
            Yaw = yaw;
            Pitch = pitch;
            Roll = roll;
        }

        public BattleMovementPoint Forward => new((float)(Math.Sin(Yaw) * Math.Cos(Pitch)), (float)Math.Sin(Pitch), (float)(-Math.Cos(Pitch) * Math.Cos(Yaw)));

        public BattleMovementPoint Up => new(
            (float)(-Math.Cos(Yaw) * Math.Sin(Roll) - Math.Sin(Yaw) * Math.Sin(Pitch) * Math.Cos(Roll)),
            (float)(Math.Cos(Pitch) * Math.Cos(Roll)),
            (float)(-Math.Sin(Yaw) * Math.Sin(Roll) + Math.Sin(Pitch) * Math.Cos(Roll) * Math.Cos(Yaw)));

        public BattleCameraPose WithPosition(BattleMovementPoint position) => new(position, Yaw, Pitch, Roll);

        internal BattleCameraPose WithAngles(float yaw, float pitch) => new(Position, yaw, pitch, Roll);

        internal static BattleCameraPose Interpolate(BattleCameraPose start, BattleCameraPose end, float ratio)
            => new(start.Position, start.Yaw + (end.Yaw - start.Yaw) * ratio, start.Pitch + (end.Pitch - start.Pitch) * ratio, start.Roll + (end.Roll - start.Roll) * ratio);
    }

    // This deliberately retains fields that the published controller does not
    // consume. Splice, inertia, counter-pitch/roll, swing and FOV are not extra
    // algorithms: assigning invented behavior to them changes original playback.
    public sealed class BattleCameraTask
    {
        public BattleCameraFlags Flags
        {
            get; set;
        }

        public BattleCameraPose Start
        {
            get; set;
        }

        public BattleCameraPose End
        {
            get; set;
        }

        public float ExpectedSeconds
        {
            get; set;
        }

        public bool Splice
        {
            get; set;
        }

        public int TraceActor { get; set; } = -1;

        public float StartSpeed
        {
            get; set;
        }

        public float Inertia
        {
            get; set;
        }

        public float FirstAccelerationStart
        {
            get; set;
        }

        public float SecondAccelerationStart
        {
            get; set;
        }

        public float FirstAcceleration
        {
            get; set;
        }

        public float SecondAcceleration
        {
            get; set;
        }

        public float CircleAngle
        {
            get; set;
        }

        public float AngularSpeed
        {
            get; set;
        }

        public BattleMovementPoint CircleCenter
        {
            get; set;
        }

        public BattleMovementPoint CircleEnd
        {
            get; set;
        }

        public float SwingFrequency
        {
            get; set;
        }

        public float SwingAmplitude
        {
            get; set;
        }

        public float TimeCompensation
        {
            get; set;
        }

        public float StartFieldOfView
        {
            get; set;
        }

        public float EndFieldOfView
        {
            get; set;
        }

        public BattleMovementPoint SavedTraceStart
        {
            get; set;
        }
    }

    // CCBCamControl, shared by both games. The owner calls Advance exactly once
    // per stage update, including the native one-update gap between list tasks.
    // No engine objects, original pointers or wall-clock services are involved.
    public sealed class BattleCameraController
    {
        private sealed class Runtime
        {
            public BattleCameraTask Definition;
            public BattleCameraPose Start;
            public BattleMovementPoint TraceStart;
            public float Expected, StartTime;
            public bool Executed, Finished;
        }

        private readonly Runtime[][] _lists;
        private readonly Func<int, BattleMovementPoint> _tracePosition;
        private BattleCameraPose _start, _current;
        private BattleMovementPoint _position, _speed, _nextSpeed;
        private float _distance, _startSpeed, _time, _absoluteTime;
        private double _timeFund, _absoluteTimeFund;
        private float _quake, _quakeFrequency = 80, _quakeDecay = 1;

        public BattleCameraPose Pose
        {
            get; private set;
        }

        public float FieldOfView
        {
            get; private set;
        }

        public bool TrailEnabled
        {
            get; private set;
        }

        public bool IsRunning => CurrentList >= 0;

        public int CurrentList { get; private set; } = -1;

        public int CurrentTask { get; private set; } = -1;

        public float CurrentExpectedSeconds => IsRunning ? Active.Expected : 0;

        public bool CurrentTaskFinished => IsRunning && Active.Finished;

        public float ScaledTime => _time;

        public float AbsoluteTime => _absoluteTime;

        public BattleCameraController(IReadOnlyList<IReadOnlyList<BattleCameraTask>> lists, BattleCameraPose initialPose,
            Func<int, BattleMovementPoint> tracePosition = null, float fieldOfView = 50)
        {
            if (lists == null)
                throw new ArgumentNullException(nameof(lists));
            var pool = new Dictionary<BattleCameraTask, Runtime>();
            _lists = lists.Select(list => list.Select(task =>
            {
                if (task == null)
                    throw new ArgumentException("Camera lists cannot contain null tasks.", nameof(lists));
                if (!pool.TryGetValue(task, out Runtime runtime))
                {
                    runtime = new Runtime { Definition = task, Start = task.Start, Expected = task.ExpectedSeconds, TraceStart = task.SavedTraceStart };
                    pool.Add(task, runtime);
                }
                return runtime;
            }).ToArray()).ToArray();
            _tracePosition = tracePosition ?? (_ => default);
            SetPose(initialPose, fieldOfView);
        }

        public void SetPose(BattleCameraPose pose, float fieldOfView = 50)
        {
            Pose = _current = pose;
            FieldOfView = fieldOfView;
        }

        public bool StartList(int index, int task = 0)
        {
            if (index < 0 || index >= _lists.Length || task < 0 || task >= _lists[index].Length)
                return false;
            foreach (Runtime runtime in _lists[index])
            {
                runtime.Executed = false;
                runtime.Finished = false;
            }
            CurrentList = index;
            CurrentTask = task;
            return true;
        }

        public void StopList()
        {
            CurrentList = CurrentTask = -1;
        }

        public void SetQuake(float amplitude, float frequency, float decay)
        {
            if (!Finite(amplitude) || !Finite(frequency) || !Finite(decay))
                throw new ArgumentException("Camera quake parameters must be finite.");
            _quake = amplitude;
            _quakeFrequency = frequency;
            _quakeDecay = decay;
        }

        public void Advance(double scaledSeconds, double absoluteSeconds)
        {
            if (!Finite(scaledSeconds) || !Finite(absoluteSeconds) || scaledSeconds < 0 || absoluteSeconds < 0)
                throw new ArgumentException("Camera time deltas must be nonnegative and finite.");
            AdvanceAt(_timeFund + scaledSeconds, _absoluteTimeFund + absoluteSeconds, scaledSeconds, absoluteSeconds);
        }

        // The original system accumulates gbLongTime (double), then publishes
        // float task times. An owner with an authoritative stage clock can
        // supply it directly, without accumulating a second independent clock.
        public void AdvanceAt(double scaledTime, double absoluteTime, double scaledSeconds, double absoluteSeconds)
        {
            if (!Finite(scaledTime) || !Finite(absoluteTime) || !Finite(scaledSeconds) || !Finite(absoluteSeconds) ||
                scaledTime < _timeFund || absoluteTime < _absoluteTimeFund || scaledSeconds < 0 || absoluteSeconds < 0)
                throw new ArgumentException("Camera clocks must be ordered and time deltas must be nonnegative and finite.");
            _timeFund = scaledTime;
            _absoluteTimeFund = absoluteTime;
            _time = (float)scaledTime;
            _absoluteTime = (float)absoluteTime;
            float scaledDelta = (float)scaledSeconds, absoluteDelta = (float)absoluteSeconds;
            if (IsRunning)
                DoList(scaledDelta, absoluteDelta);
            // Quake always uses the ordinary combat clock, even when the active
            // camera task uses absolute time. Decay precedes the sine sample.
            if (_quake > 0)
            {
                _quake -= scaledDelta * _quakeDecay;
                BattleMovementPoint p = _current.Position;
                Pose = _current.WithPosition(new BattleMovementPoint(p.X, p.Y + _quake * (float)Math.Sin(_time * _quakeFrequency), p.Z));
            }
            else
                _quake = 0;
        }

        private Runtime Active => _lists[CurrentList][CurrentTask];

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static bool Has(BattleCameraTask task, BattleCameraFlags flag) => (task.Flags & flag) != 0;

        private float Now(BattleCameraTask task) => Has(task, BattleCameraFlags.AbsoluteTime) ? _absoluteTime : _time;

        private void DoList(float scaledSeconds, float absoluteSeconds)
        {
            Runtime runtime = Active;
            if (runtime.Finished)
            {
                CurrentTask++;
                if (CurrentTask >= _lists[CurrentList].Length)
                    StopList();
                return;
            }
            BattleCameraTask task = runtime.Definition;
            // bRandSwing toggles CTrail; it never perturbs the camera pose.
            TrailEnabled = Has(task, BattleCameraFlags.RandomSwing);
            if (Has(task, BattleCameraFlags.Line))
                Line(runtime);
            if (Has(task, BattleCameraFlags.Circle))
                Circle(runtime);
            if (Has(task, BattleCameraFlags.Mechanics))
                Mechanics(runtime, Has(task, BattleCameraFlags.AbsoluteTime) ? absoluteSeconds : scaledSeconds);
        }

        private void Begin(Runtime runtime, bool captureTrace)
        {
            BattleCameraTask task = runtime.Definition;
            if (!Has(task, BattleCameraFlags.StartCamera))
                runtime.Start = Pose;
            _start = runtime.Start;
            if (Has(task, BattleCameraFlags.CounterYaw))
            {
                float difference = task.End.Yaw - _start.Yaw;
                float yaw = _start.Yaw + (difference > 0 ? 2 * (float)Math.PI : difference < 0 ? -2 * (float)Math.PI : 0);
                _start = _start.WithAngles(yaw, _start.Pitch);
            }
            runtime.StartTime = Now(task);
            if (captureTrace && Has(task, BattleCameraFlags.PositionTrace))
                runtime.TraceStart = _tracePosition(task.TraceActor);
            runtime.Executed = true;
        }

        private static float AcceleratedDistance(BattleCameraTask task, float speed, float elapsed)
        {
            float distance = speed * elapsed;
            if (elapsed >= task.FirstAccelerationStart)
            {
                float delta = elapsed - task.FirstAccelerationStart;
                distance += task.FirstAcceleration * delta * delta / 2;
            }
            if (elapsed >= task.SecondAccelerationStart)
            {
                float delta = elapsed - task.SecondAccelerationStart;
                distance += (task.FirstAcceleration + task.SecondAcceleration) * delta * delta / 2;
            }
            return distance;
        }

        private void Line(Runtime runtime)
        {
            BattleCameraTask task = runtime.Definition;
            bool targetUnknown = !Has(task, BattleCameraFlags.EndCamera);
            bool speedUnknown = !targetUnknown && !Has(task, BattleCameraFlags.Speed);
            bool timeUnknown = !targetUnknown && !speedUnknown && !Has(task, BattleCameraFlags.Time);
            if (!targetUnknown && !speedUnknown && !timeUnknown)
                return; // The published dispatch also leaves this overspecified task idle.
            if (!runtime.Executed)
            {
                Begin(runtime, true);
                _distance = (float)BattleMovement.Distance(runtime.Start.Position, task.End.Position);
                _startSpeed = task.StartSpeed;
                if (speedUnknown)
                {
                    float t1 = runtime.Expected - task.FirstAccelerationStart;
                    float t2 = runtime.Expected - task.SecondAccelerationStart;
                    _startSpeed = runtime.Expected != 0 ? (_distance - (task.FirstAcceleration * t1 * t1 / 2 +
                        (task.FirstAcceleration + task.SecondAcceleration) * t2 * t2 / 2)) / runtime.Expected : 0;
                    if (_startSpeed < 0)
                        _startSpeed = 0;
                }
            }
            float elapsed = Now(task) - runtime.StartTime;
            float ratio = runtime.Expected <= 0 ? 1 : elapsed / runtime.Expected;
            if (speedUnknown && ratio >= 1)
                ratio = 1;
            float distance = AcceleratedDistance(task, _startSpeed, elapsed);
            if (!targetUnknown && distance > _distance)
                distance = _distance;
            if (timeUnknown)
                ratio = _distance == 0 ? 1 : distance / _distance;
            BattleMovementPoint offset = Scale(BattleMovement.NormalizeDirection(Subtract(task.End.Position, runtime.Start.Position)), distance);
            BattleMovementPoint trace = Trace(runtime, ref offset);
            BattleMovementPoint position = Add(runtime.Start.Position, offset);
            if (Has(task, BattleCameraFlags.SightTrace))
            {
                _current = _current.WithPosition(position);
                // The two speed-specified native branches pass displacement,
                // whereas the time-specified branch passes world position.
                LookAt(speedUnknown ? position : offset, trace);
            }
            else
            {
                BattleCameraPose from = timeUnknown ? runtime.Start : _start;
                _current = BattleCameraPose.Interpolate(from, task.End, ratio).WithPosition(position);
            }
            if (timeUnknown)
            {
                if (distance >= _distance)
                {
                    runtime.Expected = elapsed;
                    runtime.Finished = true;
                }
            }
            else
            {
                if (speedUnknown && runtime.Expected == 0 && _distance > 0)
                    _current = task.End;
                if (ratio >= 1)
                    runtime.Finished = true;
                if (speedUnknown && distance == _distance && Has(task, BattleCameraFlags.TimeRevise))
                {
                    if (ratio < 1)
                        runtime.Expected = elapsed;
                    runtime.Finished = true;
                }
            }
            Pose = _current;
        }

        private void Circle(Runtime runtime)
        {
            BattleCameraTask task = runtime.Definition;
            // The other two circle branches in both published executables are
            // empty functions, including lists shipped as editor experiments.
            if (!Has(task, BattleCameraFlags.EndCamera) || !Has(task, BattleCameraFlags.Speed) || Has(task, BattleCameraFlags.Time))
                return;
            if (!runtime.Executed)
            {
                Begin(runtime, false);
                _distance = task.CircleAngle;
                _startSpeed = task.AngularSpeed;
            }
            float elapsed = Now(task) - runtime.StartTime;
            float distance = AcceleratedDistance(task, _startSpeed, elapsed);
            if (_distance > 0 ? distance > _distance : distance < _distance)
                distance = _distance;
            float ratio = distance / _distance;
            BattleMovementPoint relative = Subtract(runtime.Start.Position, task.CircleCenter);
            // gbQuaternion::SetYawPitchRoll + GetMatrix + RotateIn. Retaining
            // the half-angle construction also retains its float rounding.
            // gbSin/gbCos take degrees even though tagCam's Euler angles are
            // radians. Published flexcam circles contain values such as 250
            // and -180; interpreting these as radians reverses their framing.
            double radians = (double)(distance * 0.5f) * 0.01745329238474369f;
            float sine = (float)Math.Sin(radians), cosine = (float)Math.Cos(radians);
            float xx = 1 - 2 * sine * sine, xz = -2 * sine * cosine;
            BattleMovementPoint position = new(relative.X * xx - relative.Z * xz + task.CircleCenter.X,
                relative.Y + task.CircleCenter.Y + (task.CircleEnd.Y - task.CircleCenter.Y) * ratio,
                relative.X * xz + relative.Z * xx + task.CircleCenter.Z);
            BattleMovementPoint trace = Trace(runtime, ref position);
            if (Has(task, BattleCameraFlags.SightTrace))
            {
                _current = _current.WithPosition(position);
                LookAt(position, trace);
            }
            else
                _current = _start.WithPosition(position);
            Pose = _current;
            if (_distance > 0 ? distance >= _distance : distance <= _distance)
            {
                runtime.Expected = elapsed;
                runtime.Finished = true;
            }
        }

        private void Mechanics(Runtime runtime, float delta)
        {
            BattleCameraTask task = runtime.Definition;
            if (!runtime.Executed)
            {
                Begin(runtime, false);
                _position = runtime.Start.Position;
                BattleMovementPoint direction = Subtract(task.End.Position, runtime.Start.Position);
                if (runtime.Expected > 0)
                    direction = Scale(direction, 1 / runtime.Expected);
                if (CurrentTask == 0 || !Has(_lists[CurrentList][CurrentTask - 1].Definition, BattleCameraFlags.Mechanics))
                    _speed = direction;
                _nextSpeed = direction;
                _current = runtime.Start;
            }
            float elapsed = Now(task) - runtime.StartTime;
            float ratio = runtime.Expected <= 0 ? 1 : elapsed / (runtime.Expected + task.TimeCompensation);
            float speed = (float)BattleMovement.Distance(default, _speed);
            _speed = Scale(BattleMovement.NormalizeDirection(Add(_speed, Scale(_nextSpeed, delta))), speed);
            _position = Add(_position, Scale(_speed, delta));
            BattleMovementPoint position = _position;
            BattleMovementPoint trace = Trace(runtime, ref position);
            if (Has(task, BattleCameraFlags.SightTrace))
            {
                _current = _current.WithPosition(position);
                LookAt(position, trace);
            }
            else
                _current = BattleCameraPose.Interpolate(_start, task.End, ratio).WithPosition(position);
            Pose = _current;
            if (ratio >= 1)
                runtime.Finished = true;
        }

        private BattleMovementPoint Trace(Runtime runtime, ref BattleMovementPoint position)
        {
            BattleCameraTask task = runtime.Definition;
            BattleMovementPoint trace = default;
            if (Has(task, BattleCameraFlags.PositionTrace | BattleCameraFlags.SightTrace))
                trace = _tracePosition(task.TraceActor);
            if (Has(task, BattleCameraFlags.PositionTrace))
                position = Subtract(Add(position, trace), runtime.TraceStart);
            return trace;
        }

        private void LookAt(BattleMovementPoint from, BattleMovementPoint target)
        {
            BattleMovementPoint direction = Subtract(target, from);
            float yaw = (float)Math.Atan(-direction.X / direction.Z);
            if (target.Z > from.Z)
                yaw += (float)Math.PI;
            float pitch = (float)Math.Asin(direction.Y / (float)BattleMovement.Distance(from, target));
            _current = _current.WithAngles(yaw, pitch);
        }

        private static BattleMovementPoint Add(BattleMovementPoint a, BattleMovementPoint b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        private static BattleMovementPoint Subtract(BattleMovementPoint a, BattleMovementPoint b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        private static BattleMovementPoint Scale(BattleMovementPoint point, float factor) => new(point.X * factor, point.Y * factor, point.Z * factor);
    }
}
