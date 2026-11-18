// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using Battle.Presentation;
    using UnityEngine;

    public sealed partial class BattleHudView
    {
        private int? _outsidePointer;
        private Vector2 _outsideStart;
        private BattleHudPage _outsidePage;
        private bool _outsideDragged;
        private readonly HashSet<int> _retiredPointers = new();

        private bool AbilityMenuOpen => _presenter.Controller != null &&
            _presenter.Page is BattleHudPage.Skills or BattleHudPage.Magic or BattleHudPage.Items;

        private Vector2 CommandPageOrigin(float scale) => new(0, _viewport.CanvasSize.y - 118 - 482 * scale);

        private Rect AbilityMenuViewport()
        {
            Rect panel = BattleHudText.AbilityPanelBounds(_presenter.Page, _layout.IsPal3A);
            Rect list = BattleHudText.ListBounds(_presenter.Page, _layout.IsPal3A);
            list.position += panel.position;
            Rect description = BattleHudText.DescriptionBounds(_presenter.Page, _layout.IsPal3A);
            description.position += panel.position;
            // PAL3A's scrollbar extends four pixels beyond its panel image.
            Rect bounds = Rect.MinMaxRect(Math.Min(panel.xMin, list.xMin), Math.Min(panel.yMin, list.yMin),
                Math.Max(panel.xMax, Math.Max(list.xMax, description.xMax)), Math.Max(panel.yMax, Math.Max(list.yMax, description.yMax)));
            return CommandPanelViewport(bounds);
        }

        private Rect CommandPanelViewport(Rect bounds)
        {
            float scale = _interaction.ScaleFor(_viewport);
            return new Rect(_viewport.ToViewport(CommandPageOrigin(scale) + bounds.position * scale), bounds.size * (scale * _viewport.Scale));
        }

        private bool IsOutsideMenu(Vector2 point, string control)
        {
            if (!_viewport.Viewport.Contains(point))
                return false;
            if (AbilityMenuOpen)
                return !AbilityMenuViewport().Contains(point);
            return _presenter.Page == BattleHudPage.Ai && control == null && !CommandPanelViewport(AiPanelBounds).Contains(point);
        }

        // Capture the whole outside tap so its release cannot activate a
        // control on the parent page or start a camera/actor operation.
        private bool OutsideMenuGesture(BattleHudIntentKind phase, int pointer, Vector2 point, string control)
        {
            if (_outsidePointer.HasValue)
            {
                if (_outsidePointer != pointer)
                {
                    if (phase != BattleHudIntentKind.PointerDown)
                        return false;
                    _retiredPointers.Add(pointer);
                    return true;
                }
                if ((point - _outsideStart).sqrMagnitude > 64)
                    _outsideDragged = true;
                if (phase is BattleHudIntentKind.PointerUp or BattleHudIntentKind.PointerCancel)
                {
                    bool back = phase == BattleHudIntentKind.PointerUp && !_outsideDragged &&
                        _presenter.Page == _outsidePage && IsOutsideMenu(point, control);
                    _outsidePointer = null;
                    _outsideDragged = false;
                    if (back)
                        Dispatch(new BattleHudIntent(BattleHudIntentKind.Back));
                }
                return true;
            }
            if (phase != BattleHudIntentKind.PointerDown || _presenter.CapturedPointerId.HasValue ||
                _touchCandidatePointer.HasValue || !IsOutsideMenu(point, control))
                return false;
            _touchTargetArmed = null;
            _pointerTarget = null;
            _outsidePointer = pointer;
            _outsideStart = point;
            _outsidePage = _presenter.Page;
            _outsideDragged = false;
            return true;
        }

        private bool ConsumeRetiredPointer(BattleHudIntentKind phase, int pointer)
        {
            if (!_retiredPointers.Contains(pointer))
                return false;
            if (phase == BattleHudIntentKind.PointerDown)
            {
                // The previous release may have happened outside this window.
                // A fresh press starts a new gesture even if its ID is reused.
                _retiredPointers.Remove(pointer);
                return false;
            }
            if (phase is BattleHudIntentKind.PointerUp or BattleHudIntentKind.PointerCancel)
                _retiredPointers.Remove(pointer);
            return true;
        }

        private void ResetOutsideGesture()
        {
            if (_outsidePointer is int pointer)
                _retiredPointers.Add(pointer);
            _outsidePointer = null;
            _outsideDragged = false;
        }
    }
}
