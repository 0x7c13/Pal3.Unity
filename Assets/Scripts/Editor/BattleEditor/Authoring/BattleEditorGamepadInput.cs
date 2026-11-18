// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using Battle.Presentation;
    using UnityEditor;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.Controls;
    using UnityEngine.InputSystem.LowLevel;

    // Input actions intentionally ignore Editor updates. Use the editor's
    // existing event stream and flush once after its native input update.
    public sealed class BattleEditorGamepadInput : IDisposable
    {
        private readonly Func<bool> _canReceive;
        private readonly Action<BattleEditorGamepadIntent> _dispatch;
        private readonly Func<double> _clock;
        private readonly Action<BattleEditorGamepadIntent> _collect;
        private readonly Dictionary<int, DeviceState> _devices = new();
        private readonly List<DeviceState> _iteration = new();
        private readonly List<PendingIntent> _ready = new();
        private readonly List<PendingIntent> _deviceChangePoints = new();
        private Gamepad _pointingDevice;
        private int _suspensionVersion;
        private DeviceState _collectingState;
        private long _eventSequence;
        private bool _disposed;

        private readonly struct PendingIntent
        {
            public readonly DeviceState State;
            public readonly BattleEditorGamepadState Policy;
            public readonly BattleEditorGamepadIntent Intent;
            public readonly Gamepad PointingDevice;
            public readonly int NavigationVersion, SuspensionVersion;

            public PendingIntent(DeviceState state, BattleEditorGamepadIntent intent)
            {
                State = state;
                Policy = state.Policy;
                PointingDevice = null;
                Intent = intent;
                NavigationVersion = Policy.NavigationVersion;
                SuspensionVersion = Policy.SuspensionVersion;
            }

            public PendingIntent(Gamepad device, BattleEditorGamepadIntent intent, int suspensionVersion)
            {
                State = null;
                Policy = null;
                PointingDevice = device;
                Intent = intent;
                NavigationVersion = 0;
                SuspensionVersion = suspensionVersion;
            }
        }

        private PendingIntent PointingReset(Gamepad device)
        {
            return new PendingIntent(device, new BattleEditorGamepadIntent(BattleEditorGamepadIntentKind.Point,
                BattleHudDirection.Neutral, false, ++_eventSequence * 2,
                BattleHudDirection.Neutral), _suspensionVersion);
        }

        private sealed class DeviceState
        {
            public readonly Gamepad Device;

            public BattleEditorGamepadState Policy
            {
                get; private set;
            }

            private bool _baselineDirty;
            private float _stickX, _stickY;
            private bool _up, _down, _left, _right;
            private BattleEditorGamepadButtons _buttons;
            private readonly (ButtonControl Control, BattleEditorGamepadButtons Button)[] _controls;

            public BattleEditorGamepadSnapshot Snapshot => new(_stickX, _stickY, (_right ? 1 : 0) - (_left ? 1 : 0), (_up ? 1 : 0) - (_down ? 1 : 0), _buttons);

            public DeviceState(Gamepad device)
            {
                Device = device;
                _controls = new[]
                {
                    (device.buttonSouth, BattleEditorGamepadButtons.Confirm),
                    (device.buttonEast, BattleEditorGamepadButtons.Cancel),
                    (device.buttonWest, BattleEditorGamepadButtons.Primary),
                    (device.buttonNorth, BattleEditorGamepadButtons.Repeat),
                    (device.leftShoulder, BattleEditorGamepadButtons.PreviousSection),
                    (device.rightShoulder, BattleEditorGamepadButtons.NextSection),
                    (device.startButton, BattleEditorGamepadButtons.Transport)
                };
                RebuildBaseline();
            }

            public void MarkReset()
            {
                // ResetDevice notifies listeners before replacing its state,
                // so reading controls from that callback would cache old data.
                _baselineDirty = true;
                Policy.Suspend();
            }

            public void EnsureBaseline()
            {
                if (_baselineDirty)
                    RebuildBaseline();
            }

            private void RebuildBaseline()
            {
                _stickX = Device.leftStick.x.ReadValue();
                _stickY = Device.leftStick.y.ReadValue();
                _up = Device.dpad.up.isPressed;
                _down = Device.dpad.down.isPressed;
                _left = Device.dpad.left.isPressed;
                _right = Device.dpad.right.isPressed;
                _buttons = BattleEditorGamepadButtons.None;
                foreach ((ButtonControl control, BattleEditorGamepadButtons button) in _controls)
                    if (control.isPressed)
                        _buttons |= button;
                Policy = new BattleEditorGamepadState(Snapshot);
                _baselineDirty = false;
            }

            public void Read(InputEventPtr input)
            {
                // Read actual layout controls rather than assuming GPAD state
                // memory. Partial delta events retain every absent control.
                if (Device.leftStick.x.ReadValueFromEvent(input, out float x))
                    _stickX = x;
                if (Device.leftStick.y.ReadValueFromEvent(input, out float y))
                    _stickY = y;
                ReadButton(Device.dpad.up, input, ref _up);
                ReadButton(Device.dpad.down, input, ref _down);
                ReadButton(Device.dpad.left, input, ref _left);
                ReadButton(Device.dpad.right, input, ref _right);
                foreach ((ButtonControl control, BattleEditorGamepadButtons button) in _controls)
                    if (control.ReadValueFromEvent(input, out float value))
                        _buttons = control.IsValueConsideredPressed(value) ? _buttons | button : _buttons & ~button;
            }

            private static void ReadButton(ButtonControl control, InputEventPtr input, ref bool pressed)
            {
                if (control.ReadValueFromEvent(input, out float value))
                    pressed = control.IsValueConsideredPressed(value);
            }
        }

        public BattleEditorGamepadInput(Func<bool> canReceive, Action<BattleEditorGamepadIntent> dispatch, Func<double> clock = null)
        {
            _canReceive = canReceive ?? throw new ArgumentNullException(nameof(canReceive));
            _dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            _clock = clock ?? (() => EditorApplication.timeSinceStartup);
            _collect = Collect;
            foreach (Gamepad gamepad in Gamepad.all)
                _devices[gamepad.deviceId] = new DeviceState(gamepad);
            InputSystem.onEvent += OnEvent;
            InputSystem.onAfterUpdate += OnAfterUpdate;
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void OnEvent(InputEventPtr input, InputDevice device)
        {
            if (_disposed || device is not Gamepad gamepad || !gamepad.enabled || !(input.IsA<StateEvent>() || input.IsA<DeltaStateEvent>()))
                return;
            if (!_devices.TryGetValue(gamepad.deviceId, out DeviceState state))
                _devices.Add(gamepad.deviceId, state = new DeviceState(gamepad));
            state.EnsureBaseline();
            BattleEditorGamepadSnapshot before = state.Snapshot;
            state.Read(input);
            BattleEditorGamepadSnapshot snapshot = state.Snapshot;
            bool canReceive = InputState.currentUpdateType == InputUpdateType.Editor && !input.handled && _canReceive();
            if (canReceive && (snapshot.Buttons & ~before.Buttons) != BattleEditorGamepadButtons.None)
                foreach (DeviceState other in _devices.Values)
                    if (!ReferenceEquals(other, state))
                        other.Policy.SealNavigation();
            state.Policy.Sample(snapshot, canReceive, ++_eventSequence);
            // Keep the full state event available to Unity and other tools.
        }

        private void OnAfterUpdate()
        {
            if (_disposed || InputState.currentUpdateType != InputUpdateType.Editor)
                return;
            _iteration.Clear();
            _ready.Clear();
            foreach (DeviceState state in _devices.Values)
                _iteration.Add(state);
            double now = _clock();
            foreach (DeviceState state in _iteration)
            {
                if (_disposed)
                    break;
                if (!state.Device.added || !state.Device.enabled)
                {
                    state.Policy.Suspend();
                    continue;
                }
                state.EnsureBaseline();
                _collectingState = state;
                state.Policy.Advance(now, _canReceive(), _collect);
            }
            _collectingState = null;
            _ready.AddRange(_deviceChangePoints);
            _deviceChangePoints.Clear();
            // Native arrival order also applies across controllers. Device
            // dictionary order must never decide which menu a Confirm sees.
            _ready.Sort((left, right) => left.Intent.Sequence.CompareTo(right.Intent.Sequence));
            foreach (PendingIntent pending in _ready)
            {
                if (_disposed)
                    break;
                DeviceState state = pending.State;
                if (state == null)
                {
                    if (pending.SuspensionVersion != _suspensionVersion || !_canReceive())
                        continue;
                    if (UpdatePointingOwner(pending.PointingDevice, pending.Intent))
                        _dispatch(pending.Intent);
                    continue;
                }
                if (!state.Device.added || !state.Device.enabled || !ReferenceEquals(state.Policy, pending.Policy) ||
                    state.Policy.SuspensionVersion != pending.SuspensionVersion ||
                    pending.Intent.IsRepeat && state.Policy.NavigationVersion != pending.NavigationVersion)
                    continue;
                if (!_canReceive())
                {
                    state.Policy.Suspend();
                    continue;
                }
                // A preceding Confirm may have opened another page and reset
                // navigation. This later fresh gesture is still a new gesture.
                state.Policy.NotifyDispatched(pending.Intent, now);
                if (UpdatePointingOwner(state.Device, pending.Intent))
                    _dispatch(pending.Intent);
            }
        }

        private bool UpdatePointingOwner(Gamepad device, BattleEditorGamepadIntent intent)
        {
            if (intent.PointingDirection is not { } direction)
                return true;
            if (intent.IsRepeat && _pointingDevice != device)
                return false;
            if (intent.Kind != BattleEditorGamepadIntentKind.Point || direction != BattleHudDirection.Neutral)
            {
                _pointingDevice = device;
                return true;
            }
            // Releases and device resets only clear the controller that is
            // currently pointing. Another controller may have taken over.
            if (_pointingDevice != device)
                return false;
            _pointingDevice = null;
            return true;
        }

        private void Collect(BattleEditorGamepadIntent intent)
        {
            if (_collectingState != null)
                _ready.Add(new PendingIntent(_collectingState, intent));
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Gamepad gamepad && change is InputDeviceChange.Removed or InputDeviceChange.Disconnected or
                InputDeviceChange.Disabled or InputDeviceChange.SoftReset or InputDeviceChange.HardReset)
            {
                if (_canReceive() && _devices.TryGetValue(device.deviceId, out DeviceState pendingState))
                    pendingState.Policy.CapturePendingPointing(intent =>
                    {
                        // Preserve the accepted position and its original order,
                        // but never revive a command or a list step after reset.
                        var point = new BattleEditorGamepadIntent(BattleEditorGamepadIntentKind.Point, intent.Direction,
                            false, intent.Sequence, intent.PointingDirection);
                        _deviceChangePoints.Add(new PendingIntent(gamepad, point, _suspensionVersion));
                    });
                _deviceChangePoints.Add(PointingReset(gamepad));
            }
            if (change is InputDeviceChange.Removed or InputDeviceChange.Disconnected)
                _devices.Remove(device.deviceId);
            else if ((change is InputDeviceChange.Disabled or InputDeviceChange.SoftReset or InputDeviceChange.HardReset) && _devices.TryGetValue(device.deviceId, out DeviceState state))
                state.MarkReset();
        }

        public void Suspend()
        {
            _suspensionVersion++;
            _pointingDevice = null;
            _deviceChangePoints.Clear();
            foreach (DeviceState state in _devices.Values)
                state.Policy.Suspend();
        }

        public void ResetNavigation()
        {
            foreach (DeviceState state in _devices.Values)
                state.Policy.ResetNavigation();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            InputSystem.onEvent -= OnEvent;
            InputSystem.onAfterUpdate -= OnAfterUpdate;
            InputSystem.onDeviceChange -= OnDeviceChange;
            _devices.Clear();
            _pointingDevice = null;
            _deviceChangePoints.Clear();
        }
    }
}
