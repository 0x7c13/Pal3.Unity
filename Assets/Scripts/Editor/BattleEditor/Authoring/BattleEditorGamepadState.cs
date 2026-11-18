// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using Battle.Presentation;

    public enum BattleEditorGamepadIntentKind
    {
        Navigate, Confirm, Cancel, PreviousSection, NextSection, Primary, Repeat, Transport, Point
    }

    public readonly struct BattleEditorGamepadIntent
    {
        public BattleEditorGamepadIntentKind Kind
        {
            get;
        }

        public BattleHudDirection Direction
        {
            get;
        }

        // Null denotes an intent without a native device-position snapshot.
        public BattleHudDirection? PointingDirection
        {
            get;
        }

        public bool IsRepeat
        {
            get;
        }

        internal long Sequence
        {
            get;
        }

        public BattleEditorGamepadIntent(BattleEditorGamepadIntentKind kind, BattleHudDirection direction = BattleHudDirection.Down, bool isRepeat = false, BattleHudDirection? pointingDirection = null)
            : this(kind, direction, isRepeat, 0, pointingDirection)
        {
        }

        internal BattleEditorGamepadIntent(BattleEditorGamepadIntentKind kind, BattleHudDirection direction, bool isRepeat, long sequence, BattleHudDirection? pointingDirection = null)
        {
            Kind = kind;
            Direction = direction;
            PointingDirection = pointingDirection;
            IsRepeat = isRepeat;
            Sequence = sequence;
        }
    }

    [Flags]
    public enum BattleEditorGamepadButtons
    {
        None = 0, Confirm = 1, Cancel = 2, PreviousSection = 4, NextSection = 8,
        Primary = 16, Repeat = 32, Transport = 64
    }

    public readonly struct BattleEditorGamepadSnapshot
    {
        public float StickX
        {
            get;
        }

        public float StickY
        {
            get;
        }

        public int DpadX
        {
            get;
        }

        public int DpadY
        {
            get;
        }

        public BattleEditorGamepadButtons Buttons
        {
            get;
        }

        public BattleEditorGamepadSnapshot(float stickX = 0, float stickY = 0, int dpadX = 0, int dpadY = 0, BattleEditorGamepadButtons buttons = BattleEditorGamepadButtons.None)
        {
            StickX = stickX;
            StickY = stickY;
            DpadX = Math.Sign(dpadX);
            DpadY = Math.Sign(dpadY);
            Buttons = buttons;
        }
    }

    // Pure UI timing and gesture policy. Simulation speed and Input System
    // settings do not affect its clock, button edges, or repeat intervals.
    public sealed class BattleEditorGamepadState
    {
        public const double RepeatDelay = .35;
        public const double RepeatInterval = .10;
        public const float StickEnter = .5f;
        public const float StickExit = .35f;
        private const double SectorWidth = Math.PI / 4;
        private const double SectorHysteresis = Math.PI / 36;
        private static readonly (BattleEditorGamepadButtons Button, BattleEditorGamepadIntentKind Intent)[] ButtonPriority =
        {
            (BattleEditorGamepadButtons.Cancel, BattleEditorGamepadIntentKind.Cancel),
            (BattleEditorGamepadButtons.Transport, BattleEditorGamepadIntentKind.Transport),
            (BattleEditorGamepadButtons.PreviousSection, BattleEditorGamepadIntentKind.PreviousSection),
            (BattleEditorGamepadButtons.NextSection, BattleEditorGamepadIntentKind.NextSection),
            (BattleEditorGamepadButtons.Primary, BattleEditorGamepadIntentKind.Primary),
            (BattleEditorGamepadButtons.Repeat, BattleEditorGamepadIntentKind.Repeat),
            (BattleEditorGamepadButtons.Confirm, BattleEditorGamepadIntentKind.Confirm)
        };

        private readonly List<BattleEditorGamepadIntent> _pending = new();
        private readonly List<BattleEditorGamepadIntent> _draining = new();
        private BattleEditorGamepadSnapshot _snapshot;
        private BattleHudDirection? _direction;
        private BattleEditorGamepadIntent? _navigationPulse;
        private long _sequence;
        private int? _stickSector;
        private bool _navigationArmed, _pointingArmed;
        private double _nextRepeat = double.PositiveInfinity;
        private double _lastTime = double.NegativeInfinity;

        internal int NavigationVersion
        {
            get; private set;
        }

        internal int SuspensionVersion
        {
            get; private set;
        }

        public BattleEditorGamepadState(BattleEditorGamepadSnapshot initial = default)
        {
            _snapshot = initial;
            _navigationArmed = _pointingArmed = Neutral(initial);
        }

        private static bool Neutral(BattleEditorGamepadSnapshot snapshot) => snapshot.DpadX == 0 && snapshot.DpadY == 0 &&
            snapshot.StickX * snapshot.StickX + snapshot.StickY * snapshot.StickY < StickExit * StickExit;

        public void Sample(BattleEditorGamepadSnapshot snapshot, bool canReceive, long sequence = 0)
        {
            if (sequence <= 0)
                sequence = ++_sequence;
            else
                _sequence = Math.Max(_sequence, sequence);
            BattleHudDirection? previousDirection = ReadDirection();
            BattleEditorGamepadButtons rising = snapshot.Buttons & ~_snapshot.Buttons;
            bool buttonPressed = TryButton(rising, out BattleEditorGamepadIntentKind button);
            bool wasNeutral = Neutral(_snapshot);
            bool freshDpad = _snapshot.DpadX == 0 && _snapshot.DpadY == 0 && (snapshot.DpadX != 0 || snapshot.DpadY != 0);
            if (canReceive && (wasNeutral || freshDpad))
                _navigationArmed = _pointingArmed = true;
            _snapshot = snapshot;
            if (!canReceive)
            {
                Suspend();
                return;
            }
            BattleHudDirection? sampledDirection = ReadDirection();
            BattleHudDirection pointingDirection = sampledDirection ?? BattleHudDirection.Neutral;
            if (buttonPressed && button != BattleEditorGamepadIntentKind.Confirm)
            {
                // A button is a stream-order barrier. Earlier directions stay
                // earlier; coincident Cancel/transport wins over navigation.
                SealNavigation();
                _pending.Add(new BattleEditorGamepadIntent(button, BattleHudDirection.Down, false, sequence * 2 + 1, pointingDirection));
                ResetHeldNavigation();
                return;
            }
            if (_pointingArmed && sampledDirection != previousDirection)
            {
                // Axis components may coalesce, but a release preserves the
                // preceding list step and immediately points the wheel home.
                if (wasNeutral || !sampledDirection.HasValue)
                    SealNavigation();
                BattleEditorGamepadIntentKind kind = _navigationArmed && sampledDirection.HasValue
                    ? BattleEditorGamepadIntentKind.Navigate : BattleEditorGamepadIntentKind.Point;
                _navigationPulse = new BattleEditorGamepadIntent(kind, pointingDirection, false, sequence * 2, pointingDirection);
            }
            if (buttonPressed)
            {
                // Direction and Confirm in one state event navigate first.
                // Separate events retain their actual order and every edge.
                SealNavigation();
                _pending.Add(new BattleEditorGamepadIntent(button, BattleHudDirection.Down, false, sequence * 2 + 1, pointingDirection));
            }
        }

        internal void SealNavigation()
        {
            if (!_navigationPulse.HasValue)
                return;
            _pending.Add(_navigationPulse.Value);
            _navigationPulse = null;
        }

        internal void CapturePendingPointing(Action<BattleEditorGamepadIntent> collect)
        {
            SealNavigation();
            foreach (BattleEditorGamepadIntent intent in _pending)
                if (intent.Kind is BattleEditorGamepadIntentKind.Navigate or BattleEditorGamepadIntentKind.Point)
                    collect(intent);
        }

        public void Suspend()
        {
            _pending.Clear();
            _navigationPulse = null;
            SuspensionVersion++;
            _pointingArmed = false;
            _stickSector = null;
            ResetHeldNavigation();
        }

        public void ResetNavigation()
        {
            // A context change invalidates held repeats, not fresh events that
            // have already arrived later in the same input stream.
            SealNavigation();
            ResetHeldNavigation();
        }

        private void ResetHeldNavigation()
        {
            _navigationArmed = false;
            _direction = null;
            _nextRepeat = double.PositiveInfinity;
            NavigationVersion++;
        }

        internal void NotifyDispatched(BattleEditorGamepadIntent intent, double now)
        {
            if (intent.Kind == BattleEditorGamepadIntentKind.Navigate)
            {
                _navigationArmed = true;
                _direction = intent.Direction;
                _nextRepeat = now + (intent.IsRepeat ? RepeatInterval : RepeatDelay);
            }
            else if (intent.Kind is not (BattleEditorGamepadIntentKind.Confirm or BattleEditorGamepadIntentKind.Point))
                ResetHeldNavigation();
        }

        public void Advance(double now, bool canReceive, Action<BattleEditorGamepadIntent> dispatch)
        {
            if (!double.IsFinite(now))
                throw new ArgumentOutOfRangeException(nameof(now));
            if (dispatch == null)
                throw new ArgumentNullException(nameof(dispatch));
            if (now < _lastTime)
                _nextRepeat = now + RepeatDelay;
            _lastTime = now;
            if (!canReceive)
            {
                Suspend();
                return;
            }
            if (Neutral(_snapshot))
                _navigationArmed = _pointingArmed = true;
            SealNavigation();
            _draining.Clear();
            _draining.AddRange(_pending);
            _pending.Clear();
            int suspension = SuspensionVersion;
            foreach (BattleEditorGamepadIntent intent in _draining)
            {
                if (suspension != SuspensionVersion)
                    break;
                NotifyDispatched(intent, now);
                dispatch(intent);
            }
            if (suspension != SuspensionVersion)
                return;
            BattleHudDirection? direction = ReadDirection();
            if (!direction.HasValue)
            {
                _direction = null;
                _nextRepeat = double.PositiveInfinity;
            }
            else if (_draining.Count == 0 && _navigationArmed && _direction == direction && now + 1e-9 >= _nextRepeat)
            {
                // Fresh events always precede a timed repeat. Never replay a
                // backlog after a stalled editor update.
                var repeat = new BattleEditorGamepadIntent(BattleEditorGamepadIntentKind.Navigate, direction.Value, true, long.MaxValue, direction.Value);
                NotifyDispatched(repeat, now);
                dispatch(repeat);
            }
        }

        private static bool TryButton(BattleEditorGamepadButtons pressed, out BattleEditorGamepadIntentKind kind)
        {
            foreach ((BattleEditorGamepadButtons button, BattleEditorGamepadIntentKind intent) in ButtonPriority)
                if ((pressed & button) != 0)
                {
                    kind = intent;
                    return true;
                }
            kind = default;
            return false;
        }

        private BattleHudDirection? ReadDirection()
        {
            double magnitude = _snapshot.StickX * _snapshot.StickX + _snapshot.StickY * _snapshot.StickY;
            if (magnitude < StickExit * StickExit || !_stickSector.HasValue && magnitude < StickEnter * StickEnter)
                _stickSector = null;
            else
            {
                double angle = Math.Atan2(_snapshot.StickY, _snapshot.StickX);
                if (_stickSector is int previous)
                {
                    double difference = Math.Atan2(Math.Sin(angle - previous * SectorWidth), Math.Cos(angle - previous * SectorWidth));
                    if (Math.Abs(difference) > SectorWidth / 2 + SectorHysteresis)
                        _stickSector = Sector(angle);
                }
                else
                    _stickSector = Sector(angle);
            }
            int? sector = _snapshot.DpadX != 0 || _snapshot.DpadY != 0 ? Sector(Math.Atan2(_snapshot.DpadY, _snapshot.DpadX)) : _stickSector;
            return sector switch
            {
                0 => BattleHudDirection.Right,
                1 => BattleHudDirection.UpRight,
                2 => BattleHudDirection.Up,
                3 => BattleHudDirection.UpLeft,
                4 => BattleHudDirection.Left,
                5 => BattleHudDirection.DownLeft,
                6 => BattleHudDirection.Down,
                7 => BattleHudDirection.DownRight,
                _ => null
            };
        }

        private static int Sector(double angle) => ((int)Math.Floor(angle / SectorWidth + .5) % 8 + 8) % 8;
    }
}
