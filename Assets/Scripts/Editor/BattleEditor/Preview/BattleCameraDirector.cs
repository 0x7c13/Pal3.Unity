// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using Battle;
    using Core.DataReader.Dat;
    using Core.Primitives;
    using Game.Effect.Runtime;

    // The stage supplies the role-pool lookup (-1 means the five-lineup
    // origin) and consumes raw GameBox poses. Rendering conversion stays at
    // that boundary; none of the original camera arithmetic uses Unity axes.
    public sealed class BattleCameraDirector : IEffectCamera
    {
        private readonly Action<BattleCameraPose, float, bool> _output;

        public BattleCameraController Controller
        {
            get;
        }

        public bool HasStartedList
        {
            get; private set;
        }

        public BattleCameraDirector(BattleData data, BattleCameraPose initialPose, Func<int, BattleMovementPoint> tracePosition,
            Action<BattleCameraPose, float, bool> output, float fieldOfView = 50)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            FlexCameraFile file = data.Read("basedata.cpk\\cbdata\\flexcam.dat", new FlexCameraFileReader());
            Controller = new BattleCameraController(Compile(file), initialPose, tracePosition, fieldOfView);
            _output = output;
            Publish();
        }

        public bool StartList(int index)
        {
            if (!Controller.StartList(index))
                return false;
            // Native lists retain camera ownership after stopping or finishing.
            HasStartedList = true;
            return true;
        }

        public void StopList() => Controller.StopList();

        public void SetQuake(float amplitude, float frequency, float decay) => Controller.SetQuake(amplitude, frequency, decay);

        public void Advance(double scaledSeconds, double absoluteSeconds)
        {
            Controller.Advance(scaledSeconds, absoluteSeconds);
            Publish();
        }

        public void AdvanceAt(double scaledTime, double absoluteTime, double scaledSeconds, double absoluteSeconds)
        {
            Controller.AdvanceAt(scaledTime, absoluteTime, scaledSeconds, absoluteSeconds);
            Publish();
        }

        public void Publish() => _output?.Invoke(Controller.Pose, Controller.FieldOfView, Controller.TrailEnabled);

        public static IReadOnlyList<IReadOnlyList<BattleCameraTask>> Compile(FlexCameraFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            var pool = new BattleCameraTask[file.Pool.Count];
            for (int i = 0; i < pool.Length; i++)
            {
                FlexCameraTask original = file.Pool[i];
                pool[i] = new BattleCameraTask
                {
                    Flags = (BattleCameraFlags)(int)original.Flags,
                    Start = Pose(original.Start),
                    End = Pose(original.End),
                    ExpectedSeconds = original.ExpectedSeconds,
                    Splice = original.Splice,
                    TraceActor = original.TraceActor,
                    StartSpeed = original.StartSpeed,
                    Inertia = original.Inertia,
                    FirstAccelerationStart = original.FirstAccelerationStart,
                    SecondAccelerationStart = original.SecondAccelerationStart,
                    FirstAcceleration = original.FirstAcceleration,
                    SecondAcceleration = original.SecondAcceleration,
                    CircleAngle = original.CircleAngle,
                    AngularSpeed = original.AngularSpeed,
                    CircleCenter = Point(original.CircleCenter),
                    CircleEnd = Point(original.CircleEnd),
                    SwingFrequency = original.SwingFrequency,
                    SwingAmplitude = original.SwingAmplitude,
                    TimeCompensation = original.TimeCompensation,
                    StartFieldOfView = original.StartFieldOfView,
                    EndFieldOfView = original.EndFieldOfView,
                    SavedTraceStart = Point(original.SavedTraceStart)
                };
            }
            var lists = new IReadOnlyList<BattleCameraTask>[file.Lists.Count];
            for (int i = 0; i < lists.Length; i++)
            {
                var list = new List<BattleCameraTask>();
                foreach (int index in file.Lists[i])
                {
                    if (index == -1)
                        break;
                    list.Add(pool[index]);
                }
                lists[i] = list.AsReadOnly();
            }
            return Array.AsReadOnly(lists);
        }

        public static BattleCameraPose Pose(FlexCameraPose original) => new(Point(original.Position), original.Yaw, original.Pitch, original.Roll);

        private static BattleMovementPoint Point(GameBoxVector3 original) => new(original.X, original.Y, original.Z);
    }
}
