// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;

    // The actor owns its raw 4800-tick animation clock and message FIFO. Body
    // tasks consume one message before proceeding; actor Update runs afterward.
    public sealed class BattleBodyAnimationBinding
    {
        private readonly Queue<BattlePhysicalMarker> _pending = new();
        private bool _loop;

        public BattlePhysicalAnimation CurrentAnimation
        {
            get; private set;
        }

        public int LocalTick
        {
            get; private set;
        }

        public uint GameBoxTime
        {
            get; private set;
        }

        public BattlePose LogicalPose { get; private set; } = BattlePose.Idle;

        public int RequestedNativeActionIndex { get; private set; } = 32;

        public BattlePhysicalMarkerKind? LastPoppedMarker
        {
            get; private set;
        }

        public string LastPoppedName
        {
            get; private set;
        }

        public int PendingMessageCount => _pending.Count;

        public double CurrentAnimationTimeSeconds => CurrentAnimation == null ? 0 :
            (_loop ? GameBoxTime % CurrentAnimation.GameBoxDuration : GameBoxTime) / 4800d;

        public BattleBodyAnimationBinding(BattlePhysicalAnimation initial = null, bool loop = true)
        {
            CurrentAnimation = initial;
            _loop = loop;
        }

        public BattleBodyAnimationBinding Clone()
        {
            var copy = new BattleBodyAnimationBinding(CurrentAnimation, _loop)
            {
                LocalTick = LocalTick,
                GameBoxTime = GameBoxTime,
                LogicalPose = LogicalPose,
                RequestedNativeActionIndex = RequestedNativeActionIndex,
                LastPoppedMarker = LastPoppedMarker,
                LastPoppedName = LastPoppedName
            };
            foreach (BattlePhysicalMarker marker in _pending)
                copy._pending.Enqueue(marker);
            return copy;
        }

        public void ClearMessages()
        {
            _pending.Clear();
            LastPoppedMarker = null;
            LastPoppedName = null;
        }

        public void SetLogicalPose(BattlePose pose) => LogicalPose = pose;

        public void SetRequestedNativeAction(int action)
        {
            if (action < 0 || action > 63)
                throw new ArgumentOutOfRangeException(nameof(action));
            RequestedNativeActionIndex = action;
        }

        public bool TryPlay(BattlePhysicalAnimation animation, bool loop)
        {
            if (animation == null)
                return false;
            CurrentAnimation = animation;
            _loop = loop;
            _pending.Clear();
            Reset();
            return true;
        }

        public void Reset()
        {
            LocalTick = 0;
            GameBoxTime = 0;
        }

        public void PopMessage()
        {
            if (_pending.Count == 0)
            {
                LastPoppedMarker = null;
                LastPoppedName = null;
                return;
            }
            BattlePhysicalMarker marker = _pending.Dequeue();
            LastPoppedMarker = marker.Kind;
            LastPoppedName = marker.NativeName;
        }

        public void Advance(float playbackRate = 1f)
        {
            if (!float.IsFinite(playbackRate) || playbackRate < 0)
                throw new ArgumentOutOfRangeException(nameof(playbackRate));
            if (CurrentAnimation == null)
                return;
            LocalTick++;
            // Installed PAL3 gbActor.Update retains dt*FPS in x87 through the
            // 4800 conversion despite spilling a copy of dtime to float.
            uint delta = checked((uint)(BattleSession.StepSeconds * playbackRate * 4800d));
            uint oldTime = GameBoxTime;
            GameBoxTime = unchecked(GameBoxTime + delta);
            uint duration = CurrentAnimation.GameBoxDuration;
            if (!_loop && GameBoxTime >= duration)
            {
                // Native gbTick/start/end are unsigned, including max(start, end - 4).
                GameBoxTime = unchecked(duration - 4);
                EnqueueOver();
                return;
            }
            if (GameBoxTime == oldTime)
                return;
            uint first = oldTime % duration;
            ulong last = first + (ulong)delta;
            if (last >= duration)
                EnqueueOver();
            // GetTriggeredEvent emits the automatic Over first, then the file's
            // triggers in stored order, including loop-boundary tick-zero events.
            for (ulong cycle = 0; cycle * duration <= last; cycle++)
                foreach (BattlePhysicalMarker marker in CurrentAnimation.Markers)
                {
                    ulong tick = marker.GameBoxTick + cycle * duration;
                    if (tick > last)
                        break;
                    if (tick > first)
                        _pending.Enqueue(marker);
                }
        }

        private void EnqueueOver() => _pending.Enqueue(new BattlePhysicalMarker(0, BattlePhysicalMarkerKind.Over, 0, "over"));
    }
}
