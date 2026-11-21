// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Dat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Primitives;

    // Original tagCam stores its angles in radians and position in GameBox units.
    public readonly struct FlexCameraPose
    {
        public GameBoxVector3 Position
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

        public FlexCameraPose(GameBoxVector3 position, float yaw, float pitch, float roll)
        {
            Position = position;
            Yaw = yaw;
            Pitch = pitch;
            Roll = roll;
        }
    }

    [Flags]
    public enum FlexCameraFlags
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
        PositionTrace = 16384,
    }

    // Saved runtime fields are retained for inspection. Playback resets them;
    // serialized member-function pointers are data and are never executed.
    public sealed class FlexCameraTask
    {
        public IReadOnlyList<uint> SerializedDispatchWords
        {
            get;
        }

        public uint Id
        {
            get;
        }

        public bool SavedValid
        {
            get;
        }

        public bool SavedExecuted
        {
            get;
        }

        public bool SavedFinished
        {
            get;
        }

        public bool Splice
        {
            get;
        }

        public float SavedStartTime
        {
            get;
        }

        public float ExpectedSeconds
        {
            get;
        }

        public FlexCameraPose Start
        {
            get;
        }

        public FlexCameraPose End
        {
            get;
        }

        public int StartCameraIndex
        {
            get;
        }

        public int EndCameraIndex
        {
            get;
        }

        public FlexCameraFlags Flags
        {
            get;
        }

        public int TraceActor
        {
            get;
        }

        public float StartSpeed
        {
            get;
        }

        public float Inertia
        {
            get;
        }

        public float FirstAccelerationStart
        {
            get;
        }

        public float SecondAccelerationStart
        {
            get;
        }

        public float FirstAcceleration
        {
            get;
        }

        public float SecondAcceleration
        {
            get;
        }

        public float CircleAngle
        {
            get;
        }

        public float AngularSpeed
        {
            get;
        }

        public GameBoxVector3 CircleCenter
        {
            get;
        }

        public GameBoxVector3 CircleEnd
        {
            get;
        }

        public float SwingFrequency
        {
            get;
        }

        public float SwingAmplitude
        {
            get;
        }

        public float TimeCompensation
        {
            get;
        }

        public float StartFieldOfView
        {
            get;
        }

        public float EndFieldOfView
        {
            get;
        }

        public GameBoxVector3 SavedTraceStart
        {
            get;
        }

        public IReadOnlyList<uint> ExtensionWords
        {
            get;
        }

        internal FlexCameraTask(uint[] dispatchWords, uint id, bool valid, bool executed, bool finished, bool splice,
            float startTime, float expected, FlexCameraPose start, FlexCameraPose end, int startCamera, int endCamera,
            FlexCameraFlags flags, int traceActor, float[] parameters, GameBoxVector3 center, GameBoxVector3 circleEnd,
            float[] tail, GameBoxVector3 traceStart, uint[] extensionWords)
        {
            SerializedDispatchWords = Array.AsReadOnly(dispatchWords);
            Id = id;
            SavedValid = valid;
            SavedExecuted = executed;
            SavedFinished = finished;
            Splice = splice;
            SavedStartTime = startTime;
            ExpectedSeconds = expected;
            Start = start;
            End = end;
            StartCameraIndex = startCamera;
            EndCameraIndex = endCamera;
            Flags = flags;
            TraceActor = traceActor;
            StartSpeed = parameters[0];
            Inertia = parameters[1];
            FirstAccelerationStart = parameters[2];
            SecondAccelerationStart = parameters[3];
            FirstAcceleration = parameters[4];
            SecondAcceleration = parameters[5];
            CircleAngle = parameters[6];
            AngularSpeed = parameters[7];
            CircleCenter = center;
            CircleEnd = circleEnd;
            SwingFrequency = tail[0];
            SwingAmplitude = tail[1];
            TimeCompensation = tail[2];
            StartFieldOfView = tail[3];
            EndFieldOfView = tail[4];
            SavedTraceStart = traceStart;
            ExtensionWords = Array.AsReadOnly(extensionWords);
        }
    }

    public sealed class FlexCameraFile
    {
        public IReadOnlyList<FlexCameraTask> Pool
        {
            get;
        }

        public IReadOnlyList<bool> InUse
        {
            get;
        }

        public IReadOnlyList<IReadOnlyList<int>> Lists
        {
            get;
        }

        internal FlexCameraFile(FlexCameraTask[] pool, bool[] inUse, int[][] lists)
        {
            Pool = Array.AsReadOnly(pool);
            InUse = Array.AsReadOnly(inUse);
            Lists = Array.AsReadOnly(lists.Select(list => (IReadOnlyList<int>)Array.AsReadOnly(list)).ToArray());
        }
    }
}
