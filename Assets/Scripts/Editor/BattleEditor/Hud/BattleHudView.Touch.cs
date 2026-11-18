// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Linq;
    using Battle.Presentation;
    using UnityEditor;
    using UnityEngine;

    public sealed partial class BattleHudView
    {
        private BattleHudInteractionProfile _interaction;
        private Rect _touchPanel;
        private float _touchScrollLimit;
        private int? _touchCandidatePointer, _touchScrollPointer;
        private Vector2 _touchStart, _touchPrevious;
        private BattleHudPage _touchPressedPage;

        private float _touchPanelScale = 1;

        private void RegisterTouchList(Rect list, float contentHeight)
        {
            _touchPanel = new Rect(_viewport.ToViewport(_pageOffset + list.position * _pageScale), list.size * (_viewport.Scale * _pageScale));
            _touchPanelScale = _viewport.Scale * _pageScale;
            _touchScrollLimit = Math.Max(0, contentHeight - list.height);
        }

        private bool TouchGesture(BattleHudIntentKind phase, int pointerId, Vector2 point)
        {
            if (!_interaction.Touch)
                return false;
            // A scrollbar may have accepted the unclaimed press after this
            // dispatcher. Keep its drag and release with the native owner.
            if (ForeignGuiPointer && !_presenter.CapturedPointerId.HasValue)
            {
                ResetTouchGesture();
                return false;
            }
            if (_touchCandidatePointer.HasValue && _touchCandidatePointer != pointerId)
                return true;
            if (phase == BattleHudIntentKind.PointerDown && !_presenter.CapturedPointerId.HasValue && !_touchCandidatePointer.HasValue && _touchPanel.Contains(point))
            {
                _touchCandidatePointer = pointerId;
                _touchStart = _touchPrevious = point;
                _touchPressedPage = _presenter.Page;
            }
            if (_touchCandidatePointer != pointerId)
                return false;
            if (_touchPressedPage != _presenter.Page)
            {
                ResetTouchGesture();
                return false;
            }
            if (phase == BattleHudIntentKind.PointerMove)
            {
                if (!_touchScrollPointer.HasValue && _touchPressedPage != BattleHudPage.Formation && _touchScrollLimit > 0 && Math.Abs(point.y - _touchStart.y) > 8 && (!_presenter.CapturedPointerId.HasValue || _presenter.CapturedPointerId == pointerId))
                {
                    _presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerCancel, pointerId: pointerId));
                    _touchScrollPointer = pointerId;
                    // Pointer focus is not a navigation request: keep the
                    // next repaint from scrolling back to the pressed row.
                    _scrollFocus = _presenter.FocusedId;
                }
                if (_touchScrollPointer == pointerId)
                {
                    _listScroll.y = Mathf.Clamp(_listScroll.y - (point.y - _touchPrevious.y) / Math.Max(.001f, _touchPanelScale), 0, _touchScrollLimit);
                    _touchPrevious = point;
                    _repaint?.Invoke();
                    return true;
                }
            }
            if (phase is BattleHudIntentKind.PointerUp or BattleHudIntentKind.PointerCancel)
            {
                bool scrolling = _touchScrollPointer == pointerId;
                ResetTouchGesture();
                return scrolling;
            }
            return false;
        }

        private void ResetTouchGesture()
        {
            _touchCandidatePointer = _touchScrollPointer = null;
        }

    }
}
