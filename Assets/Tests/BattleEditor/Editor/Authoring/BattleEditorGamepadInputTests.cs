// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using Battle;
    using Battle.Presentation;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.LowLevel;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    public sealed class BattleEditorGamepadInputTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private Gamepad _gamepad, _otherGamepad;
        private BattleEditorGamepadInput _input;
        private BattleEditorWindow _window;
        private EditorWindow _otherWindow, _focusedWindow;
        private InputDevice _awaitedDevice;
        private int _receivedEvents;
        private InputUpdateType _receivedUpdate;
        private bool _eventHandled, _savedPreference, _hadPreference;
        private string _path;

        // Point-only events carry wheel position without becoming list steps.
        private static Action<BattleEditorGamepadIntent> CaptureActions(List<BattleEditorGamepadIntent> actions) => input =>
        {
            if (input.Kind != BattleEditorGamepadIntentKind.Point)
                actions.Add(input);
        };

        private string Preference => BattleData.Variant + ".BattleEditor.Path";

        private sealed class Catalog : IBattleCatalog
        {
            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => 0;

            public int DamageDivisor(string actor) => 1;
        }

        [SetUp]
        public void SetUp()
        {
            _input = null;
            _otherGamepad = null;
            _window = null;
            _otherWindow = null;
            _focusedWindow = null;
            _savedPreference = false;
            _receivedEvents = 0;
            _gamepad = InputSystem.AddDevice<Gamepad>();
            InputSystem.onEvent += ObserveEvent;
        }

        [TearDown]
        public void TearDown()
        {
            _input?.Dispose();
            InputSystem.onEvent -= ObserveEvent;
            if (_window != null)
            {
                _window.DiscardChanges();
                Object.DestroyImmediate(_window);
            }
            if (_otherWindow != null)
                Object.DestroyImmediate(_otherWindow);
            if (_otherGamepad != null && _otherGamepad.added)
                InputSystem.RemoveDevice(_otherGamepad);
            if (_gamepad != null && _gamepad.added)
                InputSystem.RemoveDevice(_gamepad);
            if (_savedPreference)
            {
                if (_hadPreference)
                    EditorPrefs.SetString(Preference, _path);
                else
                    EditorPrefs.DeleteKey(Preference);
            }
        }

        private void ObserveEvent(InputEventPtr input, InputDevice device)
        {
            if (device != _awaitedDevice || !(input.IsA<StateEvent>() || input.IsA<DeltaStateEvent>()))
                return;
            _receivedEvents++;
            _receivedUpdate = InputState.currentUpdateType;
            _eventHandled = input.handled;
        }

        private void CreateOffscreenWindow()
        {
            _savedPreference = true;
            _hadPreference = EditorPrefs.HasKey(Preference);
            _path = EditorPrefs.GetString(Preference);
            EditorPrefs.SetString(Preference, Path.Combine(Application.temporaryCachePath, "missing-gamepad-test-" + Guid.NewGuid().ToString("N")));
            _window = ScriptableObject.CreateInstance<BattleEditorWindow>();
            typeof(BattleEditorWindow).GetField("_gamepadFocusedWindow", Members).SetValue(_window, (Func<EditorWindow>)(() => _focusedWindow));
            ChangeFocus(_window);
        }

        private void ChangeFocus(EditorWindow next)
        {
            if (_focusedWindow == _window)
                typeof(BattleEditorWindow).GetMethod("OnLostFocus", Members).Invoke(_window, null);
            _focusedWindow = next;
            if (_focusedWindow == _window)
                typeof(BattleEditorWindow).GetMethod("OnFocus", Members).Invoke(_window, null);
        }

        private IEnumerator Send(GamepadState state, Gamepad gamepad = null)
        {
            _awaitedDevice = gamepad ?? _gamepad;
            // Observe after the adapter to detect accidental event consumption.
            InputSystem.onEvent -= ObserveEvent;
            InputSystem.onEvent += ObserveEvent;
            int before = _receivedEvents;
            InputSystem.QueueStateEvent((Gamepad)_awaitedDevice, state);
            double deadline = EditorApplication.timeSinceStartup + 5;
            while (_receivedEvents == before && EditorApplication.timeSinceStartup < deadline)
                yield return null;
            Assert.That(_receivedEvents, Is.GreaterThan(before), "Unity must pump the queued virtual gamepad event without a manual InputSystem.Update.");
            Assert.That(_receivedUpdate, Is.EqualTo(InputUpdateType.Editor), "Exercise the real Edit Mode event route, where ordinary InputAction callbacks are suppressed.");
            Assert.That(_eventHandled, Is.False, "Leave the complete gamepad state event available to Unity and other tools.");
        }

        private static BattleSession Battle()
        {
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = "gamepad-cancel",
                DataFingerprint = "gamepad-cancel",
                Encounter = BattleEncounter.AllyAmbush
            };
            foreach ((string id, BattleSide side, int actor) in new[] { ("A", BattleSide.Ally, 101), ("E", BattleSide.Enemy, 701) })
                scenario.Participants.Add(new BattleParticipant
                {
                    InstanceId = id,
                    ActorId = actor,
                    Side = side,
                    Slot = BattleSlot.Water,
                    Attributes = new[] { 100, 100, 100, 20, 10, 3, 0, 0, 0, 0, 0, 0 },
                    Vitals = new[] { 100, 100, 100 }
                });
            var battle = new BattleSession(new BattleSetup(scenario), new Catalog());
            battle.SetAutomatic(BattleSide.Enemy, false);
            battle.Step();
            Assert.That(battle.WaitingActor, Is.EqualTo("A"));
            return battle;
        }

        private static BattleHudView Hud(BattleSession battle)
        {
            var hud = new BattleHudView();
            PrepareHud(hud, battle);
            return hud;
        }

        private static void PrepareHud(BattleHudView hud, BattleSession battle)
        {
            typeof(BattleHudView).GetField("_battle", Members).SetValue(hud, battle);
            hud.Presenter.Synchronize(battle);
            hud.SetInputFocus(true);
            hud.Presenter.Open(BattleHudPage.Magic);
            Assert.That(hud.Presenter.ChooseAction(BattleAction.Attack), Is.True);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Targets));
        }

        [UnityTest]
        public IEnumerator EastButtonReturnsOneHudLevelPerPressAndOtherFaceButtonsDoNotCancel()
        {
            BattleSession battle = Battle();
            BattleHudView hud = Hud(battle);
            int inputs = battle.Inputs.Count;
            _input = new BattleEditorGamepadInput(() => true, input =>
            {
                if (input.Kind == BattleEditorGamepadIntentKind.Cancel)
                    hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Back));
            });
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Magic));
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Magic), "Holding B/Circle must not skip another submenu level.");
            yield return Send(new GamepadState());
            yield return Send(new GamepadState().WithButton(GamepadButton.South).WithButton(GamepadButton.West).WithButton(GamepadButton.North));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Magic));
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Commands));
            Assert.That(battle.Inputs.Count, Is.EqualTo(inputs), "Cancel must not submit a battle command.");
        }

        [UnityTest]
        public IEnumerator UnfocusedPressIsNotReplayedAndDisposalRemovesTheCallback()
        {
            bool focused = false;
            int cancels = 0;
            _input = new BattleEditorGamepadInput(() => focused, input =>
            {
                if (input.Kind == BattleEditorGamepadIntentKind.Cancel)
                    cancels++;
            });
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(cancels, Is.Zero);
            focused = true;
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(cancels, Is.Zero, "Regaining focus while held must wait for a release and a new press.");
            yield return Send(new GamepadState());
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(cancels, Is.EqualTo(1));
            _input.Dispose();
            _input.Dispose();
            yield return Send(new GamepadState());
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(cancels, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ConnectedGamepadsKeepIndependentPressEdges()
        {
            int cancels = 0;
            _input = new BattleEditorGamepadInput(() => true, input =>
            {
                if (input.Kind == BattleEditorGamepadIntentKind.Cancel)
                    cancels++;
            });
            _otherGamepad = InputSystem.AddDevice<Gamepad>();
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            yield return Send(new GamepadState().WithButton(GamepadButton.East), _otherGamepad);
            Assert.That(cancels, Is.EqualTo(2));
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            yield return Send(new GamepadState().WithButton(GamepadButton.East), _otherGamepad);
            Assert.That(cancels, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator FocusedBattleEditorWindowReceivesNativeCancelAndIgnoresItAfterLosingFocus()
        {
            CreateOffscreenWindow();
            var session = (BattleEditorSession)typeof(BattleEditorWindow).GetField("_session", Members).GetValue(_window);
            BattleSession battle = BattleHudTests.Session(BattleData.Variant);
            typeof(BattleEditorSession).GetProperty(nameof(BattleEditorSession.Battle)).SetValue(session, battle);
            var hud = (BattleHudView)typeof(BattleEditorWindow).GetField("_hud", Members).GetValue(_window);
            PrepareHud(hud, battle);
            yield return Send(new GamepadState().WithButton(GamepadButton.Start));
            Assert.That(session.Battle, Is.SameAs(battle), "Start without an existing preview must not start authoring actions or open a data-folder dialog.");
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Targets));
            yield return Send(new GamepadState());
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Magic), "The real window lifecycle must connect gamepad Cancel to its HUD.");
            _otherWindow = ScriptableObject.CreateInstance<EditorWindow>();
            ChangeFocus(_otherWindow);
            yield return Send(new GamepadState());
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Magic));
            ChangeFocus(_window);
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Magic), "Restoring focus must not replay a held cancel.");
            yield return Send(new GamepadState());
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Commands));
            _window.DiscardChanges();
            Object.DestroyImmediate(_window);
            Assert.That(typeof(BattleEditorWindow).GetField("_gamepadInput", Members).GetValue(_window), Is.Null);
            _window = null;
        }

        [UnityTest]
        public IEnumerator GamepadCannotSubmitTheTargetBehindAComboCurtain()
        {
            CreateOffscreenWindow();
            var session = (BattleEditorSession)typeof(BattleEditorWindow).GetField("_session", Members).GetValue(_window);
            BattleSession battle = BattleHudTests.Session(BattleData.Variant);
            typeof(BattleEditorSession).GetProperty(nameof(BattleEditorSession.Battle)).SetValue(session, battle);
            var hud = (BattleHudView)typeof(BattleEditorWindow).GetField("_hud", Members).GetValue(_window);
            PrepareHud(hud, battle);
            PropertyInfo opacity = typeof(BattleSession).GetProperty(nameof(BattleSession.CoAttackScreenOpacity));
            opacity.SetValue(battle, 1f);
            int inputs = battle.Inputs.Count;
            yield return Send(new GamepadState().WithButton(GamepadButton.South));
            yield return Send(new GamepadState());
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Targets));
            Assert.That(battle.Inputs.Count, Is.EqualTo(inputs));

            opacity.SetValue(battle, 0f);
            yield return Send(new GamepadState());
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Magic));
        }

        private static IEnumerator EditorInputUpdate()
        {
            bool updated = false;
            void AfterUpdate()
            {
                if (InputState.currentUpdateType == InputUpdateType.Editor)
                    updated = true;
            }
            InputSystem.onAfterUpdate += AfterUpdate;
            try
            {
                double deadline = EditorApplication.timeSinceStartup + 5;
                while (!updated && EditorApplication.timeSinceStartup < deadline)
                    yield return null;
                Assert.That(updated, Is.True, "Wait for the engine's own Editor input update.");
            }
            finally
            {
                InputSystem.onAfterUpdate -= AfterUpdate;
            }
        }

        private IEnumerator SendDelta(UnityEngine.InputSystem.Controls.AxisControl control, float value)
        {
            _awaitedDevice = control.device;
            InputSystem.onEvent -= ObserveEvent;
            InputSystem.onEvent += ObserveEvent;
            int before = _receivedEvents;
            InputSystem.QueueDeltaStateEvent(control, value);
            double deadline = EditorApplication.timeSinceStartup + 5;
            while (_receivedEvents == before && EditorApplication.timeSinceStartup < deadline)
                yield return null;
            Assert.That(_receivedEvents, Is.GreaterThan(before));
            Assert.That(_receivedUpdate, Is.EqualTo(InputUpdateType.Editor));
            Assert.That(_eventHandled, Is.False);
        }

        [TestCase(1, 0, BattleHudDirection.Right)]
        [TestCase(1, 1, BattleHudDirection.UpRight)]
        [TestCase(0, 1, BattleHudDirection.Up)]
        [TestCase(-1, 1, BattleHudDirection.UpLeft)]
        [TestCase(-1, 0, BattleHudDirection.Left)]
        [TestCase(-1, -1, BattleHudDirection.DownLeft)]
        [TestCase(0, -1, BattleHudDirection.Down)]
        [TestCase(1, -1, BattleHudDirection.DownRight)]
        public void DpadAndStickProduceTheSameEightDirections(int x, int y, BattleHudDirection expected)
        {
            foreach (bool dpad in new[] { false, true })
            {
                var policy = new BattleEditorGamepadState();
                var intents = new List<BattleEditorGamepadIntent>();
                policy.Sample(dpad ? new BattleEditorGamepadSnapshot(dpadX: x, dpadY: y) : new BattleEditorGamepadSnapshot(x, y), true);
                policy.Advance(0, true, CaptureActions(intents));
                Assert.That(intents.Count, Is.EqualTo(1));
                Assert.That(intents[0].Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Navigate));
                Assert.That(intents[0].Direction, Is.EqualTo(expected));
                Assert.That(intents[0].IsRepeat, Is.False);
            }
        }

        [Test]
        public void StickUsesRadialAndSectorHysteresisAndDpadHasPriority()
        {
            var policy = new BattleEditorGamepadState();
            var intents = new List<BattleEditorGamepadIntent>();
            policy.Sample(new BattleEditorGamepadSnapshot(.49f, 0), true);
            policy.Advance(0, true, CaptureActions(intents));
            Assert.That(intents, Is.Empty);
            policy.Sample(new BattleEditorGamepadSnapshot(.51f, 0), true);
            policy.Advance(.01, true, CaptureActions(intents));
            Assert.That(intents[0].Direction, Is.EqualTo(BattleHudDirection.Right));
            policy.Sample(new BattleEditorGamepadSnapshot(.4f, 0), true);
            policy.Advance(.02, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(1), "Stay active above the lower exit threshold.");
            float X(double degrees) => (float)Math.Cos(degrees * Math.PI / 180);
            float Y(double degrees) => (float)Math.Sin(degrees * Math.PI / 180);
            policy.Sample(new BattleEditorGamepadSnapshot(X(24), Y(24)), true);
            policy.Advance(.03, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(1), "Do not chatter at the 22.5-degree sector boundary.");
            policy.Sample(new BattleEditorGamepadSnapshot(X(29), Y(29)), true);
            policy.Advance(.04, true, CaptureActions(intents));
            Assert.That(intents[1].Direction, Is.EqualTo(BattleHudDirection.UpRight));
            policy.Sample(new BattleEditorGamepadSnapshot(1, 0, dpadY: -1), true);
            policy.Advance(.05, true, CaptureActions(intents));
            Assert.That(intents[2].Direction, Is.EqualTo(BattleHudDirection.Down));
        }

        [Test]
        public void RepeatUsesUiTimeAndNeverReplaysAStalledBacklog()
        {
            var policy = new BattleEditorGamepadState();
            var intents = new List<BattleEditorGamepadIntent>();
            policy.Sample(new BattleEditorGamepadSnapshot(dpadY: 1), true);
            policy.Advance(0, true, CaptureActions(intents));
            policy.Advance(.349, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(1));
            policy.Advance(.35, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(2));
            Assert.That(intents[1].IsRepeat, Is.True);
            policy.Advance(.449, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(2));
            policy.Advance(.45, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(3));
            policy.Advance(100, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(4));
            policy.Advance(100, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(4));
        }

        [Test]
        public void NavigationResetKeepsFreshConfirmButRequiresStickToReturnToNeutral()
        {
            var policy = new BattleEditorGamepadState();
            var intents = new List<BattleEditorGamepadIntent>();
            policy.Sample(new BattleEditorGamepadSnapshot(1, 0), true);
            policy.Advance(0, true, CaptureActions(intents));
            policy.ResetNavigation();
            policy.Sample(new BattleEditorGamepadSnapshot(1, 0, buttons: BattleEditorGamepadButtons.Confirm), true);
            policy.Advance(10, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(2));
            Assert.That(intents[1].Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Confirm));
            policy.Sample(default, true);
            policy.Advance(10.1, true, CaptureActions(intents));
            policy.Sample(new BattleEditorGamepadSnapshot(1, 0), true);
            policy.Advance(10.2, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(3));
            Assert.That(intents[2].IsRepeat, Is.False);
        }

        [Test]
        public void UnfocusedDirectionDoesNotReplayUntilReleasedAndPressedAgain()
        {
            var policy = new BattleEditorGamepadState();
            var intents = new List<BattleEditorGamepadIntent>();
            policy.Sample(new BattleEditorGamepadSnapshot(dpadX: -1), false);
            policy.Advance(0, false, CaptureActions(intents));
            policy.Advance(1, true, CaptureActions(intents));
            policy.Sample(new BattleEditorGamepadSnapshot(dpadX: -1), true);
            policy.Advance(2, true, CaptureActions(intents));
            Assert.That(intents, Is.Empty);
            policy.Sample(default, true);
            policy.Advance(3, true, CaptureActions(intents));
            policy.Sample(new BattleEditorGamepadSnapshot(dpadX: -1), true);
            policy.Advance(4, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(1));
            Assert.That(intents[0].Direction, Is.EqualTo(BattleHudDirection.Left));
        }

        [Test]
        public void FreshDpadRemainsUsableWithAnInactiveDriftingStick()
        {
            var policy = new BattleEditorGamepadState(new BattleEditorGamepadSnapshot(.4f, 0));
            var intents = new List<BattleEditorGamepadIntent>();
            policy.Sample(new BattleEditorGamepadSnapshot(.4f, 0, dpadY: 1), true);
            policy.Advance(0, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(1));
            Assert.That(intents[0].Direction, Is.EqualTo(BattleHudDirection.Up));
        }

        [Test]
        public void ShortDirectionPulseAndSameDirectionRepressAreNotLost()
        {
            var policy = new BattleEditorGamepadState();
            var intents = new List<BattleEditorGamepadIntent>();
            policy.Sample(new BattleEditorGamepadSnapshot(dpadY: 1), true);
            policy.Sample(new BattleEditorGamepadSnapshot(dpadX: 1, dpadY: 1), true);
            policy.Sample(default, true);
            policy.Advance(0, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(1));
            Assert.That(intents[0].Direction, Is.EqualTo(BattleHudDirection.UpRight));
            policy.Sample(new BattleEditorGamepadSnapshot(dpadX: 1), true);
            policy.Advance(.01, true, CaptureActions(intents));
            policy.Sample(default, true);
            policy.Sample(new BattleEditorGamepadSnapshot(dpadX: 1), true);
            policy.Advance(.02, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(3), "A new physical press is immediate, without waiting for the repeat delay.");
            Assert.That(intents[2].IsRepeat, Is.False);
        }

        [Test]
        public void ReleasingAnExistingHeldDirectionNeverSynthesizesAnotherPress()
        {
            var policy = new BattleEditorGamepadState();
            var intents = new List<BattleEditorGamepadIntent>();
            policy.Sample(new BattleEditorGamepadSnapshot(dpadX: 1), true);
            policy.Advance(0, true, CaptureActions(intents));
            policy.Sample(new BattleEditorGamepadSnapshot(dpadX: 1), true);
            policy.Sample(default, true);
            policy.Advance(.01, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(1));
            policy.Sample(new BattleEditorGamepadSnapshot(dpadX: 1), false);
            policy.Sample(new BattleEditorGamepadSnapshot(dpadX: 1), true);
            policy.Sample(default, true);
            policy.Advance(.02, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(1), "Releasing a press that began unfocused cannot replay it.");
        }

        [TestCase(BattleEditorGamepadButtons.Confirm, BattleEditorGamepadIntentKind.Confirm)]
        [TestCase(BattleEditorGamepadButtons.Cancel, BattleEditorGamepadIntentKind.Cancel)]
        [TestCase(BattleEditorGamepadButtons.PreviousSection, BattleEditorGamepadIntentKind.PreviousSection)]
        [TestCase(BattleEditorGamepadButtons.NextSection, BattleEditorGamepadIntentKind.NextSection)]
        [TestCase(BattleEditorGamepadButtons.Primary, BattleEditorGamepadIntentKind.Primary)]
        [TestCase(BattleEditorGamepadButtons.Repeat, BattleEditorGamepadIntentKind.Repeat)]
        [TestCase(BattleEditorGamepadButtons.Transport, BattleEditorGamepadIntentKind.Transport)]
        public void ButtonsAreEdgesAndShortPressReleaseIsNotLost(BattleEditorGamepadButtons button, BattleEditorGamepadIntentKind expected)
        {
            var policy = new BattleEditorGamepadState();
            var intents = new List<BattleEditorGamepadIntent>();
            policy.Sample(new BattleEditorGamepadSnapshot(buttons: button), true);
            policy.Sample(default, true);
            policy.Advance(0, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(1));
            Assert.That(intents[0].Kind, Is.EqualTo(expected));
            policy.Advance(100, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ConfirmAndDirectionFollowTheirNativeEventOrder(bool confirmFirst)
        {
            var policy = new BattleEditorGamepadState();
            var intents = new List<BattleEditorGamepadIntent>();
            var confirm = new BattleEditorGamepadSnapshot(buttons: BattleEditorGamepadButtons.Confirm);
            var down = new BattleEditorGamepadSnapshot(dpadY: -1);
            policy.Sample(confirmFirst ? confirm : down, true);
            policy.Sample(default, true);
            policy.Sample(confirmFirst ? down : confirm, true);
            policy.Sample(default, true);
            policy.Advance(0, true, input =>
            {
                if (input.Kind != BattleEditorGamepadIntentKind.Point)
                    intents.Add(input);
                if (input.Kind == BattleEditorGamepadIntentKind.Confirm)
                    policy.ResetNavigation();
            });
            Assert.That(intents.Count, Is.EqualTo(2));
            Assert.That(intents[0].Kind, Is.EqualTo(confirmFirst ? BattleEditorGamepadIntentKind.Confirm : BattleEditorGamepadIntentKind.Navigate));
            Assert.That(intents[1].Kind, Is.EqualTo(confirmFirst ? BattleEditorGamepadIntentKind.Navigate : BattleEditorGamepadIntentKind.Confirm));
            Assert.That(intents.Find(input => input.Kind == BattleEditorGamepadIntentKind.Navigate).Direction, Is.EqualTo(BattleHudDirection.Down));
            Assert.That(intents.TrueForAll(input => !input.IsRepeat), Is.True);
        }

        [TestCase(BattleEditorGamepadButtons.Confirm, BattleEditorGamepadButtons.Cancel)]
        [TestCase(BattleEditorGamepadButtons.Cancel, BattleEditorGamepadButtons.Confirm)]
        [TestCase(BattleEditorGamepadButtons.Confirm, BattleEditorGamepadButtons.Confirm)]
        public void SeparateButtonEdgesAreNeverCollapsedByPriorityOrPageReset(BattleEditorGamepadButtons first, BattleEditorGamepadButtons second)
        {
            var policy = new BattleEditorGamepadState();
            var intents = new List<BattleEditorGamepadIntent>();
            policy.Sample(new BattleEditorGamepadSnapshot(buttons: first), true);
            policy.Sample(default, true);
            policy.Sample(new BattleEditorGamepadSnapshot(buttons: second), true);
            policy.Sample(default, true);
            policy.Advance(0, true, input =>
            {
                if (input.Kind != BattleEditorGamepadIntentKind.Point)
                    intents.Add(input);
                policy.ResetNavigation();
            });
            Assert.That(intents.Count, Is.EqualTo(2));
            Assert.That(intents[0].Kind, Is.EqualTo(first == BattleEditorGamepadButtons.Confirm ? BattleEditorGamepadIntentKind.Confirm : BattleEditorGamepadIntentKind.Cancel));
            Assert.That(intents[1].Kind, Is.EqualTo(second == BattleEditorGamepadButtons.Confirm ? BattleEditorGamepadIntentKind.Confirm : BattleEditorGamepadIntentKind.Cancel));
        }

        [Test]
        public void ComponentsMergeBeforeConfirmButSimultaneousCancelHasPriority()
        {
            var policy = new BattleEditorGamepadState();
            var intents = new List<BattleEditorGamepadIntent>();
            policy.Sample(new BattleEditorGamepadSnapshot(dpadY: 1), true);
            policy.Sample(new BattleEditorGamepadSnapshot(dpadX: 1, dpadY: 1, buttons: BattleEditorGamepadButtons.Confirm), true);
            policy.Sample(default, true);
            policy.Advance(0, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(2));
            Assert.That(intents[0].Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Navigate));
            Assert.That(intents[0].Direction, Is.EqualTo(BattleHudDirection.UpRight));
            Assert.That(intents[1].Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Confirm));
            intents.Clear();
            policy.Sample(new BattleEditorGamepadSnapshot(dpadY: -1, buttons: BattleEditorGamepadButtons.Confirm | BattleEditorGamepadButtons.Cancel), true);
            policy.Sample(default, true);
            policy.Advance(.01, true, CaptureActions(intents));
            Assert.That(intents.Count, Is.EqualTo(1));
            Assert.That(intents[0].Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Cancel));
        }

        [UnityTest]
        public IEnumerator NativeEditorStreamPreservesShortTapsAndCancelWinsSimultaneousConfirm()
        {
            var intents = new List<BattleEditorGamepadIntent>();
            _input = new BattleEditorGamepadInput(() => true, CaptureActions(intents));
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.South));
            InputSystem.QueueStateEvent(_gamepad, new GamepadState());
            yield return EditorInputUpdate();
            Assert.That(intents.Count, Is.EqualTo(1));
            Assert.That(intents[0].Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Confirm));
            intents.Clear();
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.DpadUp));
            InputSystem.QueueStateEvent(_gamepad, new GamepadState());
            yield return EditorInputUpdate();
            Assert.That(intents.Count, Is.EqualTo(1));
            Assert.That(intents[0].Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Navigate));
            Assert.That(intents[0].Direction, Is.EqualTo(BattleHudDirection.Up));
            intents.Clear();
            yield return Send(new GamepadState().WithButton(GamepadButton.South).WithButton(GamepadButton.East));
            Assert.That(intents.Count, Is.EqualTo(1));
            Assert.That(intents[0].Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Cancel));
            yield return EditorInputUpdate();
            Assert.That(intents.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DeltaEventsKeepAbsentButtonsAndTheOtherStickAxis()
        {
            var intents = new List<BattleEditorGamepadIntent>();
            _input = new BattleEditorGamepadInput(() => true, CaptureActions(intents), () => 0);
            yield return Send(new GamepadState().WithButton(GamepadButton.East));
            yield return SendDelta(_gamepad.leftStick.x, 1);
            yield return SendDelta(_gamepad.leftStick.y, 1);
            yield return SendDelta(_gamepad.leftStick.x, 0);
            Assert.That(intents.FindAll(input => input.Kind == BattleEditorGamepadIntentKind.Cancel).Count, Is.EqualTo(1));
            Assert.That(intents.FindAll(input => input.Kind == BattleEditorGamepadIntentKind.Navigate).ConvertAll(input => input.Direction),
                Is.EqualTo(new[] { BattleHudDirection.Right, BattleHudDirection.UpRight, BattleHudDirection.Up }));
        }

        [UnityTest]
        public IEnumerator NativeEditorUpdatesRepeatWithoutAnotherDeviceEvent()
        {
            double now = 0;
            var intents = new List<BattleEditorGamepadIntent>();
            _input = new BattleEditorGamepadInput(() => true, CaptureActions(intents), () => now);
            yield return Send(new GamepadState().WithButton(GamepadButton.DpadDown));
            Assert.That(intents.Count, Is.EqualTo(1));
            now = .349;
            yield return EditorInputUpdate();
            Assert.That(intents.Count, Is.EqualTo(1));
            now = .35;
            yield return EditorInputUpdate();
            Assert.That(intents.Count, Is.EqualTo(2));
            Assert.That(intents[1].Direction, Is.EqualTo(BattleHudDirection.Down));
            Assert.That(intents[1].IsRepeat, Is.True);
            now = 100;
            yield return EditorInputUpdate();
            Assert.That(intents.Count, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator NativeDpadAndStickReachAllEightDirections()
        {
            var intents = new List<BattleEditorGamepadIntent>();
            _input = new BattleEditorGamepadInput(() => true, CaptureActions(intents), () => 0);
            foreach (bool dpad in new[] { true, false })
                foreach ((int x, int y, BattleHudDirection direction) in new[]
                {
                    (1, 0, BattleHudDirection.Right), (1, 1, BattleHudDirection.UpRight),
                    (0, 1, BattleHudDirection.Up), (-1, 1, BattleHudDirection.UpLeft),
                    (-1, 0, BattleHudDirection.Left), (-1, -1, BattleHudDirection.DownLeft),
                    (0, -1, BattleHudDirection.Down), (1, -1, BattleHudDirection.DownRight)
                })
                {
                    yield return Send(new GamepadState());
                    intents.Clear();
                    var state = new GamepadState();
                    if (dpad)
                    {
                        if (x != 0)
                            state = state.WithButton(x > 0 ? GamepadButton.DpadRight : GamepadButton.DpadLeft);
                        if (y != 0)
                            state = state.WithButton(y > 0 ? GamepadButton.DpadUp : GamepadButton.DpadDown);
                    }
                    else
                        state.leftStick = new Vector2(x, y);
                    yield return Send(state);
                    Assert.That(intents.Count, Is.EqualTo(1));
                    Assert.That(intents[0].Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Navigate));
                    Assert.That(intents[0].Direction, Is.EqualTo(direction));
                }
        }

        [UnityTest]
        public IEnumerator NativeButtonsMapToSemanticActionsWithoutHeldRepeats()
        {
            var intents = new List<BattleEditorGamepadIntent>();
            _input = new BattleEditorGamepadInput(() => true, CaptureActions(intents), () => 0);
            foreach ((GamepadButton button, BattleEditorGamepadIntentKind kind) in new[]
            {
                (GamepadButton.South, BattleEditorGamepadIntentKind.Confirm),
                (GamepadButton.East, BattleEditorGamepadIntentKind.Cancel),
                (GamepadButton.West, BattleEditorGamepadIntentKind.Primary),
                (GamepadButton.North, BattleEditorGamepadIntentKind.Repeat),
                (GamepadButton.LeftShoulder, BattleEditorGamepadIntentKind.PreviousSection),
                (GamepadButton.RightShoulder, BattleEditorGamepadIntentKind.NextSection),
                (GamepadButton.Start, BattleEditorGamepadIntentKind.Transport)
            })
            {
                yield return Send(new GamepadState());
                intents.Clear();
                yield return Send(new GamepadState().WithButton(button));
                yield return Send(new GamepadState().WithButton(button));
                Assert.That(intents.Count, Is.EqualTo(1));
                Assert.That(intents[0].Kind, Is.EqualTo(kind));
            }
        }

        private IEnumerator TapInWindow(GamepadState state)
        {
            // Neutral and release happen through the same real input stream;
            // a held direction must not carry into a newly opened submenu.
            yield return Send(new GamepadState());
            yield return Send(state);
        }

        [UnityTest]
        public IEnumerator OffscreenWindowNavigatesAiMagicListsAndRecordsOneTargetedCommand()
        {
            CreateOffscreenWindow();
            var editor = (BattleEditorSession)typeof(BattleEditorWindow).GetField("_session", Members).GetValue(_window);
            BattleSession battle = BattleHudTests.Session(BattleData.Variant);
            typeof(BattleEditorSession).GetProperty(nameof(BattleEditorSession.Battle)).SetValue(editor, battle);
            var hud = (BattleHudView)typeof(BattleEditorWindow).GetField("_hud", Members).GetValue(_window);
            hud.TakeNavigationInput(battle);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Commands));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("attack"));
            int initialInputs = battle.Inputs.Count;
            int aiProfile = battle.GetAiProfile("A");

            yield return TapInWindow(new GamepadState().WithButton(GamepadButton.DpadUp).WithButton(GamepadButton.DpadLeft));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("ai"));
            yield return Send(new GamepadState().WithButton(GamepadButton.DpadUp).WithButton(GamepadButton.DpadLeft).WithButton(GamepadButton.South));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Ai));
            Assert.That(hud.Presenter.AiActor, Is.EqualTo("A"));
            yield return TapInWindow(new GamepadState().WithButton(GamepadButton.RightShoulder));
            Assert.That(hud.Presenter.AiActor, Is.EqualTo("B"));
            yield return TapInWindow(new GamepadState().WithButton(GamepadButton.LeftShoulder));
            Assert.That(hud.Presenter.AiActor, Is.EqualTo("A"));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("flag:0"));
            for (int row = 0; row < 5; row++)
                yield return TapInWindow(new GamepadState().WithButton(GamepadButton.DpadDown));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("flag:5"));
            yield return TapInWindow(new GamepadState().WithButton(GamepadButton.South));
            Assert.That(hud.Presenter.AiFlags("A"), Is.Not.EqualTo(aiProfile), "A toggles the selected draft option.");
            Assert.That(battle.GetAiProfile("A"), Is.EqualTo(aiProfile));
            yield return TapInWindow(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Commands));
            Assert.That(battle.GetAiProfile("A"), Is.EqualTo(aiProfile), "B discards the AI draft without consuming the waiting actor's turn.");
            Assert.That(battle.Inputs.Count, Is.EqualTo(initialInputs));

            yield return TapInWindow(new GamepadState().WithButton(GamepadButton.DpadLeft));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("magic"));
            yield return Send(new GamepadState().WithButton(GamepadButton.DpadLeft).WithButton(GamepadButton.South));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Magic));
            Assert.That(hud.Presenter.Category, Is.Zero);
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("ability:301"));
            yield return TapInWindow(new GamepadState().WithButton(GamepadButton.RightShoulder));
            Assert.That(hud.Presenter.Category, Is.EqualTo(1));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("ability:302"));
            yield return TapInWindow(new GamepadState().WithButton(GamepadButton.LeftShoulder));
            Assert.That(hud.Presenter.Category, Is.Zero);
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("ability:301"));
            yield return TapInWindow(new GamepadState().WithButton(GamepadButton.DpadDown));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("ability:301"), "The one-row category wraps on down navigation.");
            yield return TapInWindow(new GamepadState().WithButton(GamepadButton.DpadUp));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("ability:301"), "The same list also supports up navigation.");
            yield return TapInWindow(new GamepadState().WithButton(GamepadButton.South));
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Targets));
            string firstTarget = hud.Presenter.Target;
            yield return TapInWindow(new GamepadState().WithButton(GamepadButton.DpadRight));
            string chosenTarget = hud.Presenter.Target;
            Assert.That(chosenTarget, Is.Not.EqualTo(firstTarget));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("target:" + chosenTarget));
            Assert.That(battle.Inputs.Count, Is.EqualTo(initialInputs), "Browsing menus, changing categories and choosing a target remain read-only.");
            yield return TapInWindow(new GamepadState().WithButton(GamepadButton.South));
            Assert.That(battle.Inputs.Count, Is.EqualTo(initialInputs + 1));
            BattleInput command = battle.Inputs[battle.Inputs.Count - 1];
            Assert.That(command.Kind, Is.EqualTo(BattleInputKind.Command));
            Assert.That(command.Actor, Is.EqualTo("A"));
            Assert.That(command.Action, Is.EqualTo(BattleAction.Skill));
            Assert.That(command.AbilityId, Is.EqualTo(301));
            Assert.That(command.Target, Is.EqualTo(chosenTarget));
            yield return Send(new GamepadState().WithButton(GamepadButton.South));
            Assert.That(battle.Inputs.Count, Is.EqualTo(initialInputs + 1), "Holding A across the controller change must not submit a second command.");
        }

        private IEnumerator SendBatchInWindow(params GamepadState[] states)
        {
            _awaitedDevice = _gamepad;
            InputSystem.onEvent -= ObserveEvent;
            InputSystem.onEvent += ObserveEvent;
            int before = _receivedEvents;
            bool queued = false;
            void QueueBatch()
            {
                if (queued || InputState.currentUpdateType != InputUpdateType.Editor)
                    return;
                foreach (GamepadState state in states)
                    InputSystem.QueueStateEvent(_gamepad, state);
                queued = true;
            }
            InputSystem.onBeforeUpdate += QueueBatch;
            try
            {
                double deadline = EditorApplication.timeSinceStartup + 5;
                while (_receivedEvents - before < states.Length && EditorApplication.timeSinceStartup < deadline)
                    yield return null;
                Assert.That(queued, Is.True);
                Assert.That(_receivedEvents - before, Is.EqualTo(states.Length), "Every state in the ordered batch must reach the engine's native Editor event stream.");
                Assert.That(_receivedUpdate, Is.EqualTo(InputUpdateType.Editor));
                Assert.That(_eventHandled, Is.False);
            }
            finally
            {
                InputSystem.onBeforeUpdate -= QueueBatch;
            }
        }

        [UnityTest]
        public IEnumerator OffscreenWindowKeepsOppositeConfirmAndDirectionOrdersInTheSameUpdate()
        {
            CreateOffscreenWindow();
            var editor = (BattleEditorSession)typeof(BattleEditorWindow).GetField("_session", Members).GetValue(_window);
            var hud = (BattleHudView)typeof(BattleEditorWindow).GetField("_hud", Members).GetValue(_window);
            BattleSession BindBattle()
            {
                BattleSession battle = BattleHudTests.Session(BattleData.Variant);
                typeof(BattleEditorSession).GetProperty(nameof(BattleEditorSession.Battle)).SetValue(editor, battle);
                hud.TakeNavigationInput(battle);
                Assert.That(hud.Presenter.FocusedId, Is.EqualTo("attack"));
                return battle;
            }
            var confirm = new GamepadState().WithButton(GamepadButton.South);
            var cancel = new GamepadState().WithButton(GamepadButton.East);
            var down = new GamepadState().WithButton(GamepadButton.DpadDown);
            var neutral = new GamepadState();
            BattleSession first = BindBattle();
            int inputs = first.Inputs.Count;
            yield return SendBatchInWindow(confirm, neutral, down, neutral);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Targets), "A opens Attack before the later Down chooses a target.");
            // This fixture uses native slot order: Fire (E3) precedes Wind (E2).
            Assert.That(hud.Presenter.Target, Is.EqualTo("E3"));
            Assert.That(first.Inputs.Count, Is.EqualTo(inputs));
            BattleSession reverse = BindBattle();
            inputs = reverse.Inputs.Count;
            yield return SendBatchInWindow(down, neutral, confirm, neutral);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Targets), "Releasing Down returns the command wheel to Attack before the later A.");
            Assert.That(reverse.Inputs.Count, Is.EqualTo(inputs));
            BindBattle();
            yield return SendBatchInWindow(confirm, neutral, cancel, neutral);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Commands), "Separate A then B must both execute in order.");
            BindBattle();
            yield return SendBatchInWindow(cancel, neutral, confirm, neutral);
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Targets), "Separate B then A cannot collapse to B alone.");
            BattleSession doubleConfirm = BindBattle();
            inputs = doubleConfirm.Inputs.Count;
            yield return SendBatchInWindow(confirm, neutral, confirm, neutral);
            Assert.That(doubleConfirm.Inputs.Count, Is.EqualTo(inputs + 1), "Two full A gestures select Attack and then submit its default target.");
            Assert.That(doubleConfirm.Inputs[inputs].Action, Is.EqualTo(BattleAction.Attack));
        }

        [UnityTest]
        public IEnumerator NativeResetRebasesPartialDeltasBeforeFreshNavigation()
        {
            var intents = new List<BattleEditorGamepadIntent>();
            _input = new BattleEditorGamepadInput(() => true, CaptureActions(intents), () => 0);
            foreach (bool hard in new[] { false, true })
                foreach (bool waitForUpdate in new[] { false, true })
                {
                    yield return Send(new GamepadState());
                    intents.Clear();
                    yield return Send(new GamepadState().WithButton(GamepadButton.DpadRight));
                    Assert.That(intents.Count, Is.EqualTo(1));
                    Assert.That(intents[0].Direction, Is.EqualTo(BattleHudDirection.Right));
                    InputSystem.ResetDevice(_gamepad, hard);
                    if (waitForUpdate)
                        yield return EditorInputUpdate();
                    Assert.That(_gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.zero));
                    yield return SendDelta(_gamepad.leftStick.y, 1);
                    Assert.That(intents.Count, Is.EqualTo(2), "A partial delta after reset must use the real neutral baseline rather than cached DpadRight.");
                    Assert.That(intents[1].Direction, Is.EqualTo(BattleHudDirection.Up));
                    Assert.That(intents[1].IsRepeat, Is.False);
                }
        }

        [UnityTest]
        public IEnumerator ControllersKeepNativeOrderAcrossButtonBarriers()
        {
            var intents = new List<BattleEditorGamepadIntent>();
            _input = new BattleEditorGamepadInput(() => true, CaptureActions(intents), () => 0);
            _otherGamepad = InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(_otherGamepad, new GamepadState().WithButton(GamepadButton.South));
            InputSystem.QueueStateEvent(_otherGamepad, new GamepadState());
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.DpadDown));
            InputSystem.QueueStateEvent(_gamepad, new GamepadState());
            yield return EditorInputUpdate();
            Assert.That(intents.Count, Is.EqualTo(2));
            Assert.That(intents[0].Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Confirm));
            Assert.That(intents[1].Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Navigate));
            Assert.That(intents[1].Direction, Is.EqualTo(BattleHudDirection.Down));
            intents.Clear();
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.DpadUp));
            InputSystem.QueueStateEvent(_otherGamepad, new GamepadState().WithButton(GamepadButton.South));
            InputSystem.QueueStateEvent(_otherGamepad, new GamepadState());
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.DpadUp).WithButton(GamepadButton.DpadRight));
            InputSystem.QueueStateEvent(_gamepad, new GamepadState());
            yield return EditorInputUpdate();
            Assert.That(intents.Count, Is.EqualTo(3));
            Assert.That(intents[0].Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Navigate));
            Assert.That(intents[0].Direction, Is.EqualTo(BattleHudDirection.Up));
            Assert.That(intents[1].Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Confirm));
            Assert.That(intents[2].Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Navigate));
            Assert.That(intents[2].Direction, Is.EqualTo(BattleHudDirection.UpRight));
        }

        [UnityTest]
        public IEnumerator NativeNeutralConfirmAndHeldConfirmUseTheirOwnEventSnapshotsWithoutAWindow()
        {
            BattleSession battle = BattleHudTests.Session();
            var hud = new BattleHudView();
            hud.TakeNavigationInput(battle);
            typeof(BattleHudView).GetField("_inputEnabled", Members).SetValue(hud, true);
            _input = new BattleEditorGamepadInput(() => true, input =>
            {
                BattleHudPage before = hud.Presenter.Page;
                hud.TakeGamepadInput(battle);
                hud.SendGamepadIntent(input);
                if (before != hud.Presenter.Page)
                    _input.ResetNavigation();
            }, () => 0);
            yield return Send(new GamepadState().WithButton(GamepadButton.DpadUp));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("skills"));
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.South));
            InputSystem.QueueStateEvent(_gamepad, new GamepadState());
            yield return EditorInputUpdate();
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Targets));
            Assert.That(hud.Presenter.Command.Action, Is.EqualTo(BattleAction.Attack));
            hud.Presenter.Back();
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.DpadUp).WithButton(GamepadButton.South));
            InputSystem.QueueStateEvent(_gamepad, new GamepadState());
            yield return EditorInputUpdate();
            Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Skills), "The later release cannot rewrite the earlier held-direction Confirm.");
        }

        [UnityTest]
        public IEnumerator NativeResetDisableAndRemovalReturnTheOwnedWheelToCenterWithoutAWindow()
        {
            BattleSession battle = BattleHudTests.Session();
            var hud = new BattleHudView();
            hud.TakeNavigationInput(battle);
            typeof(BattleHudView).GetField("_inputEnabled", Members).SetValue(hud, true);
            _input = new BattleEditorGamepadInput(() => true, input =>
            {
                hud.TakeGamepadInput(battle);
                hud.SendGamepadIntent(input);
            }, () => 0);
            yield return Send(new GamepadState().WithButton(GamepadButton.DpadUp));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("skills"));
            InputSystem.ResetDevice(_gamepad);
            yield return EditorInputUpdate();
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("attack"));
            yield return SendDelta(_gamepad.leftStick.x, 1);
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("defend"));
            InputSystem.DisableDevice(_gamepad);
            yield return EditorInputUpdate();
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("attack"));
            InputSystem.EnableDevice(_gamepad);
            yield return Send(new GamepadState());
            yield return Send(new GamepadState().WithButton(GamepadButton.DpadLeft));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("magic"));
            InputSystem.RemoveDevice(_gamepad);
            yield return EditorInputUpdate();
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("attack"));
        }

        [UnityTest]
        public IEnumerator NativeKeyboardTakeoverSuppressesTheOldGamepadReleaseWithoutAWindow()
        {
            BattleSession battle = BattleHudTests.Session();
            var hud = new BattleHudView();
            hud.TakeGamepadInput(battle);
            typeof(BattleHudView).GetField("_inputEnabled", Members).SetValue(hud, true);
            _input = new BattleEditorGamepadInput(() => true, input =>
            {
                hud.TakeGamepadInput(battle);
                hud.SendGamepadIntent(input);
            }, () => 0);
            hud.KeyboardTookControl += _input.Suspend;
            yield return Send(new GamepadState().WithButton(GamepadButton.DpadUp));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("skills"));
            hud.TakeNavigationInput();
            hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Left));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("magic"));
            yield return Send(new GamepadState());
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("magic"));
            yield return Send(new GamepadState().WithButton(GamepadButton.DpadRight));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("defend"));
        }

        [UnityTest]
        public IEnumerator NativeResetAndRemovalKeepTheControllerThatOwnsTheWheelAtThatSequenceWithoutAWindow()
        {
            foreach (bool remove in new[] { false, true })
                for (int order = 0; order < 4; order++)
                {
                    _input?.Dispose();
                    if (_gamepad.added)
                        InputSystem.RemoveDevice(_gamepad);
                    if (_otherGamepad != null && _otherGamepad.added)
                        InputSystem.RemoveDevice(_otherGamepad);
                    _gamepad = InputSystem.AddDevice<Gamepad>();
                    _otherGamepad = InputSystem.AddDevice<Gamepad>();
                    BattleSession battle = BattleHudTests.Session();
                    var hud = new BattleHudView();
                    hud.TakeGamepadInput(battle);
                    typeof(BattleHudView).GetField("_inputEnabled", Members).SetValue(hud, true);
                    bool changeAfterState = false, changed = false;
                    void Change(Gamepad device)
                    {
                        changed = true;
                        if (remove)
                            InputSystem.RemoveDevice(device);
                        else
                            InputSystem.ResetDevice(device);
                    }
                    void BeforeAdapterFlush()
                    {
                        if (InputState.currentUpdateType == InputUpdateType.Editor && changeAfterState && !changed)
                            Change(order >= 2 ? _otherGamepad : _gamepad);
                    }
                    void AfterState(InputEventPtr input, InputDevice device)
                    {
                        if (device == _otherGamepad && (input.IsA<StateEvent>() || input.IsA<DeltaStateEvent>()))
                            changeAfterState = order != 0;
                    }
                    // Device state has been applied when onAfterUpdate starts.
                    // Register before the adapter so the reset is ordered after
                    // B's state event and before the adapter's deferred flush.
                    InputSystem.onAfterUpdate += BeforeAdapterFlush;
                    _input = new BattleEditorGamepadInput(() => true, input =>
                    {
                        hud.TakeGamepadInput(battle);
                        hud.SendGamepadIntent(input);
                    }, () => 0);
                    InputSystem.onEvent += AfterState;
                    try
                    {
                        yield return Send(new GamepadState().WithButton(GamepadButton.DpadUp));
                        Assert.That(hud.Presenter.FocusedId, Is.EqualTo("skills"));
                        int before = battle.Inputs.Count;
                        if (order == 0)
                            Change(_gamepad);
                        GamepadState next = new GamepadState().WithButton(GamepadButton.DpadRight);
                        if (order == 3)
                            next = next.WithButton(GamepadButton.South);
                        yield return Send(next, _otherGamepad);
                        Assert.That(changed, Is.True);
                        Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Commands));
                        Assert.That(hud.Presenter.FocusedId, Is.EqualTo(order >= 2 ? "attack" : "defend"),
                            "Reset applies only to the owner at its place in the native stream, including a newly acquired owner resetting itself.");
                        Assert.That(battle.Inputs.Count, Is.EqualTo(before), "A reset preserves accepted pointing order without reviving a queued Confirm.");
                    }
                    finally
                    {
                        InputSystem.onEvent -= AfterState;
                        InputSystem.onAfterUpdate -= BeforeAdapterFlush;
                    }
                }
        }

        [UnityTest]
        public IEnumerator FormerOwnerRepeatsAndReleasesKeepTheCurrentWheelDirection()
        {
            BattleSession battle = BattleHudTests.Session();
            var hud = new BattleHudView();
            hud.TakeGamepadInput(battle);
            _otherGamepad = InputSystem.AddDevice<Gamepad>();
            double now = 0;
            _input = new BattleEditorGamepadInput(() => true, input =>
            {
                hud.TakeGamepadInput(battle);
                hud.SendGamepadIntent(input);
            }, () => now);

            yield return Send(new GamepadState().WithButton(GamepadButton.DpadUp));
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("skills"));
            now = .1;
            GamepadState right = new GamepadState().WithButton(GamepadButton.DpadRight);
            yield return Send(right, _otherGamepad);
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("defend"));
            now = .36;
            yield return Send(right, _otherGamepad);
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("defend"), "A's repeat must not move B's wheel.");
            yield return Send(new GamepadState());
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("defend"), "A's release must not clear B's direction.");
            yield return Send(new GamepadState(), _otherGamepad);
            Assert.That(hud.Presenter.FocusedId, Is.EqualTo("attack"));
        }
    }
}
