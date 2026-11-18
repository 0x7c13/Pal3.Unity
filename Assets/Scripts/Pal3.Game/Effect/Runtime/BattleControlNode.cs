// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Runtime
{
    using System;
    using System.Collections.Generic;
    using Core.DataReader.Data;
    using Core.DataReader.Cpk;
    using UnityEngine;

    internal sealed class BattleControlNode : EffectNode
    {
        private readonly IBattleEffectContext _context;
        private readonly string _kind;
        private readonly bool _externalNames;
        private readonly Vector3[] _starts;
        private readonly bool[] _silentActions;
        private readonly IEffectMovie[] _movies;
        private readonly IEffectTimeControl _timeControl;
        private readonly double[] _timeStarts;
        private readonly Dictionary<(int Slot, uint Model), IEffectActor> _controlledActors = new();
        private EffectTaskClock _cameraTask;
        private int _pendingMovieTask = -1;
        private float _pendingDelta;
        private bool _aliveBeforeMovie;

        public BattleControlNode(GameObject root, EffectDefinition definition, EffectTask[] tasks,
            Vector3 origin, float birth, IBattleEffectContext context, string kind, bool externalNames)
            : base(root, definition, tasks, origin, birth)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _kind = kind;
            _externalNames = externalNames;
            _starts = new Vector3[Tasks.Length];
            if (kind == "action")
                _silentActions = new bool[Tasks.Length];
            _movies = new IEffectMovie[Tasks.Length];
            if (kind == "time")
            {
                if (Array.Exists(Tasks, task => !SupportsTimeTask(task)))
                    throw new NotSupportedException("Unsupported original time task parameters.");
                _timeControl = context as IEffectTimeControl ??
                    throw new NotSupportedException("The battle context does not control its clock.");
                _timeStarts = new double[Tasks.Length];
            }
        }

        internal bool ControlsTime => _timeControl != null;

        internal static bool SupportsTimeTask(EffectTask task) =>
            task.CreateNew && task.ModifyLast && float.IsFinite(task.XStretchSpeed) && task.XStretchSpeed is >= 0f and <= 1f &&
            float.IsFinite(task.StartTime) && float.IsFinite(task.Duration) && task.Duration >= 0f && task.SpeedY == 0f && task.CircleRadius == 0f;

        internal void StepTimeControl(float time, float delta, double absoluteTime)
        {
            float localTime = time - Birth;
            bool alive = false;
            for (int index = 0; index < Tasks.Length; index++)
            {
                EffectTask task = Tasks[index];
                EffectTaskClock clock = Clocks[index];
                if (delta > 0 && clock.StartedAt < 0 && clock.Tick(localTime, delta, task, out _))
                {
                    _timeStarts[index] = absoluteTime;
                    _timeControl.SetTimeScale(clock, task.XStretchSpeed);
                    clock.KeepRunning();
                }
                // Start waits follow the effect clock. An active slowdown expires in real time.
                if (clock.StartedAt >= 0 && !clock.Finished && absoluteTime - _timeStarts[index] >= task.Duration)
                {
                    _timeControl.ClearTimeScale(clock);
                    clock.Complete();
                }
                alive |= !clock.Finished;
            }
            Alive = alive;
        }

        protected override void Apply(EffectTask task, EffectTaskClock clock, float delta)
        {
            int index = Array.IndexOf(Tasks, task);
            if (_kind == "action")
            {
                if (_silentActions[index])
                    return;
                uint roleId = _externalNames ? task.ReservedWords[1] : 0;
                int slot = (int)MathF.Round(task.CreateTimeSlot);
                bool owned = _controlledActors.TryGetValue((slot, roleId), out IEffectActor actor);
                actor ??= _context.Actor(slot, roleId);
                if (actor == null)
                    return; // Original invalid/missing role identifiers do not control a role.
                Vector3 previous = actor.Position;
                if (clock.StartedNow)
                {
                    if (!owned)
                    {
                        _controlledActors.Add((slot, roleId), actor);
                        actor.SetExternalTask(true, this);
                    }
                    actor.SetAction(ActionName((int)MathF.Round(task.XStretchSpeed), _externalNames ? task.ReservedWords[0] : 0));
                    actor.SetLongBlade(task.RequireLoops > 0);
                    if (task.SpeedBlue > 0)
                        _starts[index] = previous;
                }
                Vector3 current;
                if (task.SpeedBlue <= 0)
                    current = previous + new Vector3(task.SpeedX, task.SpeedY, task.SpeedZ) * delta;
                else
                {
                    Vector3 target = new(task.RotAxisX, task.RotAxisY, task.RotAxisZ);
                    float elapsed = LocalTime - clock.StartedAt;
                    current = task.Duration - elapsed <= 0 ? target : _starts[index] + (target - _starts[index]) * (elapsed / task.Duration);
                }
                actor.Position = current;
                if (task.CreateNew && (current.x != previous.x || current.y != previous.y || current.z != previous.z))
                {
                    Vector3 direction = current - previous;
                    double length = Math.Sqrt((double)direction.x * direction.x + (double)direction.y * direction.y + (double)direction.z * direction.z);
                    direction = length < (double)0.005f ? new Vector3(0, 1, 0) :
                        new Vector3((float)(direction.x / length), (float)(direction.y / length), (float)(direction.z / length));
                    double angle = Math.Acos(Math.Clamp(direction.z, -1f, 1f));
                    if (direction.x < 0)
                        angle = -angle + (double)(MathF.PI * 2);
                    actor.FacingDegrees = (float)(angle * (double)Mathf.Rad2Deg);
                }
                if (task.SpeedAlpha != 0)
                    actor.Blend = Mathf.Clamp01(actor.Blend + task.SpeedAlpha * delta);
                actor.SetWeapons(task.SpeedRed, task.SpeedGreen);
                if (clock.Finished || task.ModifyLast && actor.ActionOver)
                {
                    if (task.YStretchSpeed > 0)
                        actor.FacingDegrees = task.YStretchSpeed;
                    actor.SetLongBlade(false);
                    clock.Complete();
                }
            }
            else if (_kind == "camera")
            {
                if (clock.StartedNow && _context.Camera is { } camera)
                {
                    camera.StopList();
                    _cameraTask = null;
                    if (task.XStretchSpeed > 0 && camera.StartList((int)MathF.Round(task.XStretchSpeed)))
                        _cameraTask = clock;
                }
                // Ordinary duration expiry leaves the camera list running. Only
                // an explicit task end request stops it in CEffCamera.
            }
            else if (_kind == "quake")
            {
                if (clock.StartedNow)
                    _context.Camera?.SetQuake(task.XStretchSpeed, task.SpeedY, task.SpeedX);
            }
            else if (_kind == "bink" && task.XStretchSpeed != 0 && LocalTime - clock.StartedAt >= task.SpeedX)
            {
                _movies[index] ??= _context.PlayMovie(Definition.VideoFileNamePrefix + ((int)MathF.Round(task.XStretchSpeed)).ToString("000") + ".bik");
                if (_movies[index]?.Finished != false)
                    clock.Complete();
                else
                    clock.KeepRunning();
            }
        }

        internal static string ActionName(int index, uint prefix)
        {
            if (index <= 0)
                return null;
            if (prefix == 0)
                return "z" + index.ToString("00");
            var hash = new Crc32Hash();
            hash.Init();
            foreach (string candidate in new[] { "z", "z0", "th", "th0", "tw", "tw0", "tp", "tp0", "tx", "tx0", "tl", "tl0", "hj", "hj0", "x", "t" })
                if (hash.Compute(candidate, 936) == prefix)
                    return candidate + index.ToString();
            throw new NotSupportedException($"Unknown original external action prefix {prefix:X8}.");
        }

        public override void Step(float time, float delta)
        {
            LocalTime = time - Birth;
            AdvanceTasks(0, delta, false);
        }

        internal void StepSilently(float time, float delta)
        {
            LocalTime = time - Birth;
            AdvanceTasks(0, delta, false, silent: true);
        }

        private void AdvanceTasks(int first, float delta, bool alive, bool silent = false)
        {
            for (int index = first; index < Tasks.Length; index++)
            {
                EffectTaskClock clock = Clocks[index];
                if (clock.Tick(LocalTime, delta, Tasks[index], out float activeDelta))
                {
                    if (!silent)
                        Apply(Tasks[index], clock, activeDelta);
                    // A seek cannot capture the external actor's original position or action.
                    else if (_silentActions != null && clock.StartedNow)
                        _silentActions[index] = true;
                }
                if (!silent && _context.IsModalPresentationRunning)
                {
                    _pendingMovieTask = index;
                    _pendingDelta = delta;
                    _aliveBeforeMovie = alive;
                    Alive = true;
                    return;
                }
                alive |= !Clocks[index].Finished;
            }
            Alive = alive;
            if (!Alive)
                ReleaseControl();
        }

        internal bool ResumeModal()
        {
            if (_pendingMovieTask < 0)
                return true;
            if (_context.IsModalPresentationRunning)
                return false;
            int index = _pendingMovieTask;
            if (_movies[index]?.Finished == true)
                Clocks[index].Complete();
            _pendingMovieTask = -1;
            AdvanceTasks(index + 1, _pendingDelta, _aliveBeforeMovie || !Clocks[index].Finished);
            return _pendingMovieTask < 0;
        }

        private void ReleaseControl()
        {
            foreach (IEffectActor actor in _controlledActors.Values)
            {
                actor.SetExternalTask(false, this);
            }
            _controlledActors.Clear();
        }

        public override void Dispose()
        {
            ReleaseControl();
            if (_timeControl != null)
                foreach (EffectTaskClock clock in Clocks)
                    _timeControl.ClearTimeScale(clock);
            if (_cameraTask is { Finished: false })
                _context.Camera?.StopList();
            foreach (IEffectMovie movie in _movies)
                movie?.Dispose();
            base.Dispose();
        }

        public override void Render(Camera camera)
        {
        }
    }
}
