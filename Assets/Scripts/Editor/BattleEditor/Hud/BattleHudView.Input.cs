// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Presentation;
    using UnityEngine;

    public sealed partial class BattleHudView
    {
        private readonly List<(string Id, Rect Rect)> _hitControls = new();
        private readonly HashSet<string> _interactiveControls = new(StringComparer.Ordinal);
        private readonly Dictionary<(string Id, Rect Rect), SpriteHitRegion> _spriteHitRegions = new();
        private readonly Dictionary<Texture2D, SpritePixels> _spritePixels = new();
        private Action _repaint;

        private sealed class SpritePixels
        {
            public readonly Color32[] Pixels;
            public readonly int Width;
            public readonly int Height;

            public SpritePixels(Texture2D texture)
            {
                Pixels = texture.GetPixels32();
                Width = texture.width;
                Height = texture.height;
            }

            // UIButtonEX/UIButtonEXA use an alpha threshold of six for their
            // visible surface; the older eighteen threshold is commented out.
            public bool Opaque(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height && Pixels[y * Width + x].a >= 6;
        }

        private readonly struct SpriteHitRegion
        {
            private readonly SpritePixels _pixels;
            private readonly Rect _rect;
            private readonly Vector2 _nativeSize;

            public SpriteHitRegion(SpritePixels pixels, Rect rect, Vector2 nativeSize)
            {
                _pixels = pixels;
                _rect = rect;
                _nativeSize = nativeSize;
            }

            public bool Contains(Vector2 point)
            {
                if (!_rect.Contains(point))
                    return false;
                Vector2 local = point - _rect.position;
                return _pixels.Opaque(Mathf.FloorToInt(local.x * _nativeSize.x / _rect.width),
                    Mathf.CeilToInt(_nativeSize.y) - 1 - Mathf.FloorToInt(local.y * _nativeSize.y / _rect.height));
            }

            public float DistanceSquared(Vector2 point, float padding)
            {
                Vector2 scale = new(_rect.width / _nativeSize.x, _rect.height / _nativeSize.y);
                Vector2 local = point - _rect.position;
                int left = Math.Max(0, Mathf.FloorToInt((local.x - padding) / scale.x));
                int right = Math.Min(Mathf.CeilToInt(_nativeSize.x) - 1, Mathf.FloorToInt((local.x + padding) / scale.x));
                int top = Math.Max(0, Mathf.FloorToInt((local.y - padding) / scale.y));
                int bottom = Math.Min(Mathf.CeilToInt(_nativeSize.y) - 1, Mathf.FloorToInt((local.y + padding) / scale.y));
                float distance = float.PositiveInfinity;
                for (int y = top; y <= bottom; y++)
                    for (int x = left; x <= right; x++)
                    {
                        // GUI's top edge samples the top of the cropped UV
                        // region, while GetPixels32 starts at texture row zero.
                        if (!_pixels.Opaque(x, Mathf.CeilToInt(_nativeSize.y) - 1 - y))
                            continue;
                        var pixel = new Rect(_rect.position + new Vector2(x * scale.x, y * scale.y), scale);
                        distance = Math.Min(distance, RectangleDistanceSquared(pixel, point));
                    }
                return distance;
            }
        }

        private void ClearHitControls()
        {
            _hitControls.Clear();
            _spriteHitRegions.Clear();
        }

        private static float RectangleDistanceSquared(Rect rect, Vector2 point)
        {
            float x = Math.Max(rect.xMin - point.x, Math.Max(0, point.x - rect.xMax));
            float y = Math.Max(rect.yMin - point.y, Math.Max(0, point.y - rect.yMax));
            return x * x + y * y;
        }

        private void RegisterControl(string id, Rect rect, Texture2D sprite = null)
        {
            if (id == null || Event.current.type != EventType.Repaint)
                return;
            if (sprite != null)
                rect.size = new Vector2(Math.Min(rect.width, sprite.width), Math.Min(rect.height, sprite.height));
            Vector2 nativeSize = rect.size;
            rect = new Rect(_pageOffset + rect.position * _pageScale, rect.size * _pageScale);
            _hitControls.Add((id, rect));
            if (sprite != null)
            {
                if (!_spritePixels.TryGetValue(sprite, out SpritePixels pixels))
                    _spritePixels.Add(sprite, pixels = new SpritePixels(sprite));
                _spriteHitRegions[(id, rect)] = new SpriteHitRegion(pixels, rect, nativeSize);
            }
        }

        private string HitControl(Vector2 point, bool slotsOnly, bool inspectMagicRows = false)
        {
            _interactiveControls.Clear();
            foreach (BattleHudFocus item in _presenter.FocusItems())
                if (item.Enabled || inspectMagicRows && item.Id.StartsWith("ability:", StringComparison.Ordinal))
                    _interactiveControls.Add(item.Id);
            // Inside an ability list, padding must not turn a disabled row or
            // scrollbar press into a neighbouring command.
            bool expanded = _interaction.Touch && !(AbilityMenuOpen && _touchPanel.Contains(_viewport.ToViewport(point)));
            float padding = _interaction.HitPadding / _viewport.Scale;
            float nearest = padding * padding;
            string id = null;
            for (int i = _hitControls.Count - 1; i >= 0; i--)
            {
                var control = _hitControls[i];
                if ((!_interactiveControls.Contains(control.Id) && !control.Id.StartsWith("inspect:", StringComparison.Ordinal)) ||
                    (slotsOnly && !control.Id.StartsWith("slot:", StringComparison.Ordinal)))
                    continue;
                bool sprite = _spriteHitRegions.TryGetValue(control, out SpriteHitRegion region);
                if (sprite ? region.Contains(point) : control.Rect.Contains(point))
                    return control.Id;
                if (!expanded)
                    continue;
                float distance = sprite ? region.DistanceSquared(point, padding) : RectangleDistanceSquared(control.Rect, point);
                if (distance <= nearest && (id == null || distance < nearest))
                {
                    nearest = distance;
                    id = control.Id;
                }
            }
            return id;
        }

        public bool SendPointer(BattleHudIntentKind phase, int pointerId, Vector2 viewportPoint)
        {
            if (ConsumeRetiredPointer(phase, pointerId))
                return true;
            if (!_inputEnabled)
                return false;
            if (phase == BattleHudIntentKind.PointerDown &&
                (_presenter.CapturedPointerId == pointerId || _touchCandidatePointer == pointerId ||
                    _touchWorldPointer == pointerId || _outsidePointer == pointerId))
            {
                // A fresh press proves that the old release was lost. End that
                // gesture without consuming this new press/release pair.
                CancelInput();
                _retiredPointers.Remove(pointerId);
            }
            if (phase == BattleHudIntentKind.PointerDown &&
                (_outsidePointer.HasValue && _outsidePointer != pointerId ||
                    _touchCandidatePointer.HasValue && _touchCandidatePointer != pointerId ||
                    _presenter.CapturedPointerId.HasValue && _presenter.CapturedPointerId != pointerId))
            {
                _retiredPointers.Add(pointerId);
                return true;
            }
            if (phase == BattleHudIntentKind.PointerDown && !_viewport.Viewport.Contains(viewportPoint))
                return false;
            if (phase == BattleHudIntentKind.PointerDown || phase == BattleHudIntentKind.PointerMove &&
                (!_lastPointer.HasValue || (_lastPointer.Value - viewportPoint).sqrMagnitude > .01f))
            {
                if (_navigationInput)
                    PointerTookControl?.Invoke();
                _navigationInput = false;
                _lastPointer = viewportPoint;
            }
            Vector2 point = _viewport.ToCanvas(viewportPoint);
            bool slotsOnly = _presenter.CapturedPointerId == pointerId && _presenter.FormationActor != null && phase != BattleHudIntentKind.PointerDown;
            string id = slotsOnly ? HitControl(point, true) : null;
            string actor = null;
            if (WorldSelectionOpen)
            {
                var actors = WorldSelectionActors();
                actor = _preview.PickActor(viewportPoint, _viewport.Viewport, _battle.Actors.Select(value => value.Id));
                if (actor == null && _interaction.Touch && _viewport.Viewport.Contains(viewportPoint))
                    actor = actors.Where(target => _preview.TryActorSelectionPoint(target.Actor, _viewport.Viewport, out Vector2 center) && Vector2.Distance(center, viewportPoint) <= 28)
                        .OrderBy(target =>
                        {
                            _preview.TryActorSelectionPoint(target.Actor, _viewport.Viewport, out Vector2 center);
                            return Vector2.Distance(center, viewportPoint);
                        }).Select(target => target.Actor).FirstOrDefault();
                id = actor == null ? null : actors.FirstOrDefault(target => target.Actor == actor).Control;
                // The original AI flag panel still owns its native controls.
                if (_presenter.Page == BattleHudPage.Ai)
                    id = HitControl(point, false) ?? (CommandPanelViewport(AiPanelBounds).Contains(viewportPoint) ? null : id);
            }
            if (OutsideMenuGesture(phase, pointerId, viewportPoint, id))
                return true;
            if (TouchGesture(phase, pointerId, viewportPoint))
                return true;
            if (WorldSelectionOpen)
            {
                if (id == null && phase == BattleHudIntentKind.PointerDown)
                {
                    _pointerTarget = null;
                    _touchTargetArmed = null;
                    _touchWorldPreview = null;
                    _touchWorldPointer = null;
                    _presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerCancel, pointerId: pointerId));
                    if (_presenter.Page is BattleHudPage.Targets or BattleHudPage.Protection)
                        Dispatch(new BattleHudIntent(BattleHudIntentKind.Back));
                    _retiredPointers.Add(pointerId);
                    return true;
                }
                if (!_navigationInput || phase == BattleHudIntentKind.PointerDown ||
                    phase == BattleHudIntentKind.PointerMove && (!_lastPointer.HasValue || (_lastPointer.Value - viewportPoint).sqrMagnitude > .01f))
                    _pointerTarget = id == null ? null : actor;
            }
            else
                id ??= HitControl(point, false, inspectMagicRows: phase == BattleHudIntentKind.PointerMove &&
                    _presenter.Page == BattleHudPage.Magic);
            if (_navigationInput && phase == BattleHudIntentKind.PointerMove)
                return false;
            if (_interaction.Touch && WorldSelectionOpen)
            {
                bool worldControl = id != null && (id.StartsWith("target:", StringComparison.Ordinal) || id.StartsWith("protect:", StringComparison.Ordinal) || id.StartsWith("ai-actor:", StringComparison.Ordinal));
                if (phase == BattleHudIntentKind.PointerDown)
                {
                    if (_touchWorldPointer == pointerId)
                    {
                        _presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerCancel, pointerId: pointerId));
                        _touchTargetArmed = null;
                    }
                    _touchWorldPointer = null;
                    _touchWorldPreview = null;
                    if (worldControl)
                    {
                        _touchTargetConfirm = _presenter.Page == BattleHudPage.Targets || _touchTargetArmed == id;
                        _touchTargetArmed = null;
                        _touchWorldPreview = id;
                        _touchWorldPointer = pointerId;
                        _touchWorldStart = viewportPoint;
                        _touchWorldDragged = false;
                    }
                    else
                        _touchTargetArmed = null;
                }
                if (_touchWorldPointer == pointerId && phase is BattleHudIntentKind.PointerMove or BattleHudIntentKind.PointerUp &&
                    (viewportPoint - _touchWorldStart).sqrMagnitude > 64)
                    _touchWorldDragged = true;
                if (_touchWorldPointer == pointerId && phase is BattleHudIntentKind.PointerUp or BattleHudIntentKind.PointerCancel)
                {
                    bool valid = phase == BattleHudIntentKind.PointerUp && !_touchWorldDragged && id == _touchWorldPreview;
                    bool confirm = valid && _touchTargetConfirm;
                    _touchWorldPointer = null;
                    _touchWorldPreview = null;
                    _touchTargetConfirm = false;
                    if (!confirm)
                    {
                        _presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerCancel, pointerId: pointerId));
                        _touchTargetArmed = valid ? id : null;
                        if (valid)
                            _presenter.SetFocus(id);
                        _repaint?.Invoke();
                        return true;
                    }
                }
            }
            if (phase == BattleHudIntentKind.PointerDown && !_presenter.CapturedPointerId.HasValue &&
                id?.StartsWith("inspect:", StringComparison.Ordinal) == true)
            {
                // Inspector selection is an editor tool operation, separate from
                // the presenter's recorded battle commands and focus navigation.
                _selectActor?.Invoke(id[8..]);
                _repaint?.Invoke();
                return true;
            }
            if (phase == BattleHudIntentKind.PointerMove && !_presenter.CapturedPointerId.HasValue)
            {
                bool inspected = _presenter.InspectAbilityRow(id);
                bool focused = id != null && _presenter.SetFocus(id);
                if (inspected)
                    _changed?.Invoke();
                else if (focused)
                    _repaint?.Invoke();
                return inspected || focused;
            }
            bool captured = _presenter.CapturedPointerId == pointerId;
            bool accepted = Dispatch(new BattleHudIntent(phase, id, pointerId));
            return accepted || captured;
        }

        internal static bool IsBattleKey(Event input) => input.type == EventType.KeyDown &&
            (input.modifiers & (EventModifiers.Command | EventModifiers.Control | EventModifiers.Alt)) == 0 &&
            input.keyCode is not (KeyCode.None or KeyCode.LeftShift or KeyCode.RightShift or KeyCode.LeftControl or KeyCode.RightControl or
                KeyCode.LeftAlt or KeyCode.RightAlt or KeyCode.LeftCommand or KeyCode.RightCommand or KeyCode.CapsLock or KeyCode.AltGr);

        public event Action PointerTookControl;

        public event Action KeyboardTookControl;

        private bool _navigationInput;
        private BattleHudDirection? _gamepadPointingDirection;
        private Vector2? _lastPointer;
        private string _touchTargetArmed;
        private string _pointerTarget;
        private bool _touchTargetConfirm;
        private int? _touchWorldPointer;
        private Vector2 _touchWorldStart;
        private string _touchWorldPreview;
        private bool _touchWorldDragged;

        public void TakeNavigationInput(Battle.BattleSession combat = null)
        {
            KeyboardTookControl?.Invoke();
            TakeNavigationInputCore(combat);
        }

        public void TakeGamepadInput(Battle.BattleSession combat = null)
        {
            TakeNavigationInputCore(combat);
        }

        private void TakeNavigationInputCore(Battle.BattleSession combat)
        {
            _gamepadPointingDirection = null;
            CancelInput();
            if (combat != null)
            {
                if (_battle != combat)
                    ClearHitControls();
                _battle = combat;
                Synchronize(combat);
            }
            _navigationInput = true;
            _pointerTarget = null;
            SetInputFocus(true);
        }

        public bool SendIntent(BattleHudIntent intent) => Dispatch(intent);

        public bool SendGamepadIntent(BattleEditorGamepadIntent input)
        {
            BattleHudDirection? pointingDirection = input.PointingDirection;
            _gamepadPointingDirection = pointingDirection;
            BattleHudIntent? intent = input.Kind switch
            {
                BattleEditorGamepadIntentKind.Point when _presenter.Page == BattleHudPage.Commands => new BattleHudIntent(BattleHudIntentKind.Navigate, direction: input.Direction),
                BattleEditorGamepadIntentKind.Navigate => new BattleHudIntent(BattleHudIntentKind.Navigate, direction: input.Direction),
                BattleEditorGamepadIntentKind.Confirm => new BattleHudIntent(BattleHudIntentKind.Confirm),
                BattleEditorGamepadIntentKind.Cancel => new BattleHudIntent(BattleHudIntentKind.Back),
                BattleEditorGamepadIntentKind.PreviousSection => new BattleHudIntent(BattleHudIntentKind.CyclePrevious),
                BattleEditorGamepadIntentKind.NextSection => new BattleHudIntent(BattleHudIntentKind.CycleNext),
                BattleEditorGamepadIntentKind.Primary => new BattleHudIntent(BattleHudIntentKind.Primary),
                BattleEditorGamepadIntentKind.Repeat => new BattleHudIntent(BattleHudIntentKind.Repeat),
                _ => null
            };
            BattleHudPage before = _presenter.Page;
            if (intent?.Kind == BattleHudIntentKind.Confirm && before == BattleHudPage.Commands &&
                pointingDirection.HasValue && (pointingDirection != BattleHudDirection.Neutral || CommandRingFocused()))
                ApplyGamepadPointing();
            bool accepted = intent.HasValue && Dispatch(intent.Value);
            if (before != BattleHudPage.Commands && _presenter.Page == BattleHudPage.Commands)
                ApplyGamepadPointing();
            return accepted;
        }

        private bool CommandRingFocused()
        {
            string focus = _presenter.FocusedId;
            foreach (BattleHudFocus item in _presenter.FocusItems())
                if (item.Id == focus)
                    return item.Column is >= 0 and <= 2 && item.Row is >= 0 and <= 2;
            return false;
        }

        private void ApplyGamepadPointing()
        {
            if (_navigationInput && _hasInputFocus && _gamepadPointingDirection is BattleHudDirection direction &&
                _presenter.Page == BattleHudPage.Commands)
                _presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: direction));
        }

        private bool Dispatch(BattleHudIntent intent)
        {
            if (!_inputEnabled)
                return false;
            if (intent.Kind == BattleHudIntentKind.Back)
                CancelInput();
            if (intent.Kind == BattleHudIntentKind.Navigate &&
                _presenter.Page is BattleHudPage.Targets or BattleHudPage.Protection)
                PublishFocusLayout();
            int before = _battle.RecordedInputCount;
            string message = _presenter.Message;
            BattleHudPage page = _presenter.Page;
            string controller = _presenter.Controller;
            string focused = _presenter.FocusedId;
            int category = _presenter.Category;
            bool accepted = _presenter.Handle(intent);
            PlayInputSounds(intent, page, controller, focused, category, accepted);
            if (_battle.RecordedInputCount != before)
                _changed?.Invoke();
            else if (accepted || message != _presenter.Message || page != _presenter.Page)
                _repaint?.Invoke();
            return accepted || _presenter.ActivatedControl != null || page != _presenter.Page || message != _presenter.Message;
        }

        public static BattleHudIntent? KeyboardIntent(KeyCode key) => key switch
        {
            KeyCode.UpArrow or KeyCode.W => new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Up),
            KeyCode.DownArrow or KeyCode.S => new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Down),
            KeyCode.LeftArrow or KeyCode.A => new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Left),
            KeyCode.RightArrow or KeyCode.D => new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Right),
            KeyCode.Keypad7 or KeyCode.Q => new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.UpLeft),
            KeyCode.Keypad9 or KeyCode.E => new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.UpRight),
            KeyCode.Keypad1 or KeyCode.Z => new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.DownLeft),
            KeyCode.Keypad3 or KeyCode.C => new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.DownRight),
            KeyCode.Keypad5 or KeyCode.X => new BattleHudIntent(BattleHudIntentKind.Primary),
            KeyCode.Tab => new BattleHudIntent(BattleHudIntentKind.CycleNext),
            KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Space => new BattleHudIntent(BattleHudIntentKind.Confirm),
            KeyCode.Escape => new BattleHudIntent(BattleHudIntentKind.Back),
            KeyCode.F => new BattleHudIntent(BattleHudIntentKind.Normal),
            KeyCode.R => new BattleHudIntent(BattleHudIntentKind.Repeat),
            _ => null
        };

        public void SetInputFocus(bool active)
        {
            _hasInputFocus = active;
            if (!active)
                _gamepadPointingDirection = null;
            _inputEnabled = active && _battle?.Status == Battle.BattleSessionStatus.Running && !_battle.IsModalPresentation;
            if (!active)
                CancelInput();
        }

        public void CancelInput()
        {
            ReleaseGuiPointer();
            _pointerTarget = null;
            if (_touchCandidatePointer is int touch)
                _retiredPointers.Add(touch);
            if (_presenter.CapturedPointerId is int captured)
                _retiredPointers.Add(captured);
            ResetTouchGesture();
            ResetOutsideGesture();
            _touchTargetArmed = null;
            _touchTargetConfirm = false;
            _touchWorldPointer = null;
            _touchWorldPreview = null;
            if (_presenter.CapturedPointerId is int pointer)
                _presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerCancel, pointerId: pointer));
        }

        private static string ControlId(string resource) => resource switch
        {
            "control/attack" => "attack",
            "control/skill" => "skills",
            "control/magic" => "magic",
            "control/item" => "items",
            "control/defence" => "defend",
            "control/flee" => "flee",
            "control/lineup" => "formation",
            "control/protect" => "protection",
            "control/ai" => "ai",
            "control/mj" => "sword",
            "control/bs" => "transform",
            "control/AI/attack" => "flag:0",
            "control/AI/magic" => "flag:1",
            "control/AI/skill" => "flag:2",
            "control/AI/item" => "flag:3",
            "control/ItemList/recover" => "category:0",
            "control/ItemList/attack" => "category:1",
            "control/ItemList/treasure" => "category:2",
            "control/MagicList/water" => "category:0",
            "control/MagicList/fire" => "category:1",
            "control/MagicList/thunder" => "category:2",
            "control/MagicList/wind" => "category:3",
            "control/MagicList/earth" => "category:4",
            "control/MagicList/double" => "category:5",
            "control/AI/affirmative" => "flag:4",
            "control/AI/ai" => "flag:5",
            "control/AI/ok" => "end",
            "control/Protect/ok" => "end",
            _ => null
        };

    }
}
