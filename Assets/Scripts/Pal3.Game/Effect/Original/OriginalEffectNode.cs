// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using System;
    using Core.DataReader.Data;
    using UnityEngine;
    using Color = UnityEngine.Color;

    internal sealed class OriginalTaskClock
    {
        private bool _initialized, _finished;
        private float _initAt;

        public float StartedAt { get; private set; } = -1;

        public bool StartedNow
        {
            get; private set;
        }

        public bool Finished => _finished;

        public bool Tick(float time, float dt, EffectTask task, out float elapsed)
        {
            StartedNow = false;
            elapsed = 0;
            if (_finished)
                return false;
            if (!_initialized)
            {
                _initialized = true;
                _initAt = time;
                // A negative wait means "after the previous init subtask",
                // which has already finished in this update in CTaskTpl.
                if (task.StartTime >= 0)
                    return false;
                StartedAt = time;
                StartedNow = true;
            }
            if (StartedAt < 0)
            {
                if (time - _initAt <= Mathf.Max(0, task.StartTime))
                    return false;
                StartedAt = time;
                StartedNow = true;
            }
            elapsed = dt - Mathf.Max(0, time - StartedAt - task.Duration);
            _finished = time - StartedAt + 0.000001f >= task.Duration;
            return true;
        }
    }

    internal abstract class OriginalEffectNode : IDisposable
    {
        protected readonly GameObject Root;
        protected readonly EffectDefinition Definition;
        protected readonly EffectTask[] Tasks;
        protected readonly OriginalTaskClock[] Clocks;
        protected readonly float Birth;
        protected Vector3 Position, Rotation, Orbit, Scale;
        protected Color Tint;
        protected float TextureOffset, LocalTime;
        protected bool Visible;

        public virtual bool Alive { get; protected set; } = true;
        public virtual int QuadCount => 0;

        protected OriginalEffectNode(GameObject root, EffectDefinition d, EffectTask[] tasks, Vector3 origin, float birth)
        {
            Root = root;
            Definition = d;
            Tasks = tasks ?? Array.Empty<EffectTask>();
            Birth = birth;
            Position = origin + new Vector3(d.InitXPosition, d.InitYPosition, d.InitZPosition);
            Rotation = new Vector3(d.InitXRotation, d.InitYRotation, d.InitZRotation);
            Scale = Vector3.one * d.InitSize;
            Tint = PackedColor(d.InitColor);
            Clocks = new OriginalTaskClock[Tasks.Length];
            for (int i = 0; i < Clocks.Length; i++)
                Clocks[i] = new OriginalTaskClock();
        }

        public virtual void Step(float time, float dt)
        {
            LocalTime = time - Birth;
            bool alive = false;
            for (int i = 0; i < Tasks.Length; i++)
            {
                if (Clocks[i].Tick(LocalTime, dt, Tasks[i], out float activeDt))
                {
                    Visible = true;
                    Apply(Tasks[i], Clocks[i], Mathf.Max(0, activeDt));
                }
                alive |= !Clocks[i].Finished;
            }
            Alive = alive;
        }

        protected virtual void Apply(EffectTask t, OriginalTaskClock clock, float dt)
        {
            Position += new Vector3(t.SpeedX, t.SpeedY, t.SpeedZ) * dt;
            ApplyRotationAndStretch(t, dt);
            Tint = ChangedColor(Tint, t, dt);
            Vector3 axis = new(t.CircleAxisX, t.CircleAxisY, t.CircleAxisZ);
            Orbit = OrbitPosition(axis, (LocalTime - clock.StartedAt) * t.CircleSpeed, t.CircleRadius);
            TextureOffset += t.CreateTimeSlot * dt;
        }

        protected virtual void ApplyRotationAndStretch(EffectTask t, float dt)
        {
            Rotation += new Vector3(t.RotAxisX, t.RotAxisY, t.RotAxisZ) * dt;
            Scale += new Vector3(t.XStretchSpeed, t.YStretchSpeed, t.CreateTimeSlot) * dt;
        }

        public abstract void Render(Camera camera);

        public virtual void Dispose()
        {
            if (Root != null)
                Root.SetActive(false);
            OriginalEffectResources.Destroy(Root);
        }

        internal static Vector3 UnityPosition(Vector3 v) => new(-v.x / 20, v.y / 20, v.z / 20);

        // A zero axis produces an identity matrix in GameBox, preserving the
        // input radius vector rather than discarding the offset.
        internal static Vector3 OrbitPosition(Vector3 axis, float angle, float radius) =>
            (axis.sqrMagnitude > 0 ? Quaternion.AngleAxis(angle, axis.normalized) : Quaternion.identity) * new Vector3(0, 0, radius);

        internal static Quaternion UnityRotation(Vector3 r) => Quaternion.AngleAxis(r.x, Vector3.right) * Quaternion.AngleAxis(-r.y, Vector3.up) * Quaternion.AngleAxis(-r.z, Vector3.forward);

        internal static Quaternion RawRotation(Vector3 r) => Quaternion.AngleAxis(r.x, Vector3.right) * Quaternion.AngleAxis(r.y, Vector3.up) * Quaternion.AngleAxis(r.z, Vector3.forward);

        internal static Color PackedColor(uint c) => new((c >> 16 & 255) / 255f, (c >> 8 & 255) / 255f, (c & 255) / 255f, (c >> 24) / 255f);

        internal static Color ChangedColor(Color c, EffectTask t, float dt) => new(Mathf.Clamp01(c.r + t.SpeedRed * dt / 255),
            Mathf.Clamp01(c.g + t.SpeedGreen * dt / 255), Mathf.Clamp01(c.b + t.SpeedBlue * dt / 255), Mathf.Clamp01(c.a + t.SpeedAlpha * dt / 255));

        protected (Vector2 min, Vector2 max) Atlas(float age)
        {
            int columns = Mathf.Max(1, Definition.FramesPerRow), rows = Mathf.Max(1, Definition.FramesPerColumn);
            int frame = Mathf.Abs(Mathf.RoundToInt(age * Definition.FramePerSecond)) % Mathf.Max(1, Definition.FrameCount);
            var uv = new Vector2((float)(frame % columns) / columns, 1f - (float)(frame / columns + 1) / rows);
            return (uv, uv + new Vector2(1f / columns, 1f / rows));
        }
    }

    internal sealed class OriginalControlNode : OriginalEffectNode
    {
        private readonly OriginalEffectPlayer _player;
        private readonly string _kind;
        private readonly byte[] _played;

        public OriginalControlNode(GameObject root, EffectDefinition d, EffectTask[] tasks, Vector3 origin, float birth, OriginalEffectPlayer player, string kind)
            : base(root, d, tasks, origin, birth)
        {
            _player = player;
            _kind = kind;
            _played = new byte[Tasks.Length];
        }

        protected override void Apply(EffectTask t, OriginalTaskClock clock, float dt)
        {
            if (_kind == "callback")
            {
                if (clock.StartedNow)
                    foreach (var child in t.Callbacks())
                        _player.AddGroup(child.groupId, Position + new Vector3(child.position.X, child.position.Y, child.position.Z));
            }
            else
            {
                int index = Array.IndexOf(Tasks, t), bit = 0;
                foreach (var sound in t.Sounds())
                {
                    int mask = 1 << bit++;
                    if ((_played[index] & mask) != 0 || LocalTime - clock.StartedAt < sound.delay)
                        continue;
                    _player.Sound($"{Definition.SfxFileNamePrefix}{sound.index:000}");
                    _played[index] |= (byte)mask;
                }
            }
        }

        public override void Render(Camera camera)
        {
        }
    }
}
