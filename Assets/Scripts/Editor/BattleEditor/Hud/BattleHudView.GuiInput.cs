// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using Battle.Presentation;
    using UnityEditor;
    using UnityEngine;

    public sealed partial class BattleHudView
    {
        private int _guiPointerControl, _guiCapturedControl;

        private bool OwnsGuiPointer => _guiCapturedControl != 0 && GUIUtility.hotControl == _guiCapturedControl;

        private bool ForeignGuiPointer => GUIUtility.hotControl != 0 && !OwnsGuiPointer;

        private void PrepareGuiInput()
        {
            // Request this at the same place on every GUI pass, independently
            // of the command page or the controls drawn by that page.
            _guiPointerControl = GUIUtility.GetControlID(0x42617448, FocusType.Passive);
            if (_guiCapturedControl != 0 && !OwnsGuiPointer)
                CancelInput();
            // A native scrollbar can claim the mouse after the HUD processed
            // the press. Its drag and release bypass the HUD input handler.
            if (ForeignGuiPointer && _touchCandidatePointer == 0 && !_presenter.CapturedPointerId.HasValue)
                ResetTouchGesture();
        }

        private void CaptureGuiPointer()
        {
            if (_guiPointerControl != 0 && GUIUtility.hotControl == 0)
            {
                _guiCapturedControl = _guiPointerControl;
                GUIUtility.hotControl = _guiCapturedControl;
            }
        }

        private void ReleaseGuiPointer()
        {
            if (OwnsGuiPointer)
                GUIUtility.hotControl = 0;
            _guiCapturedControl = 0;
        }

        internal static bool CanReadBattleKey(Event input) => GUIUtility.keyboardControl == 0 &&
            !EditorGUIUtility.editingTextField && IsBattleKey(input);

        internal static void TakeGuiKeyboardFocus()
        {
            GUIUtility.keyboardControl = 0;
            EditorGUIUtility.editingTextField = false;
        }

        private void HandleGuiInput(Event input, Rect content)
        {
            if (_inputEnabled && !ForeignGuiPointer && CanReadBattleKey(input) && KeyboardIntent(input.keyCode) is { } keyIntent)
            {
                TakeNavigationInput();
                Dispatch(keyIntent);
                input.Use();
            }
            if (!ForeignGuiPointer && input.button == 0 && input.type is EventType.MouseDown or EventType.MouseMove or EventType.MouseDrag or EventType.MouseUp)
            {
                EventType type = input.type;
                bool captured = OwnsGuiPointer;
                try
                {
                    BattleHudIntentKind phase = type == EventType.MouseDown ? BattleHudIntentKind.PointerDown :
                        type == EventType.MouseUp ? BattleHudIntentKind.PointerUp : BattleHudIntentKind.PointerMove;
                    if (SendPointer(phase, 0, input.mousePosition))
                    {
                        if (type == EventType.MouseDown)
                        {
                            if (content.Contains(input.mousePosition))
                                TakeGuiKeyboardFocus();
                            CaptureGuiPointer();
                        }
                        input.Use();
                    }
                    else if (captured && type != EventType.MouseMove)
                        input.Use();
                }
                finally
                {
                    if (type == EventType.MouseUp)
                        ReleaseGuiPointer();
                }
            }
            if (_inputEnabled && !ForeignGuiPointer && _presenter.Controller != null && _presenter.Page != BattleHudPage.Commands &&
                input.type == EventType.MouseDown && input.button == 1 && _viewport.SafeRect.Contains(input.mousePosition) &&
                Dispatch(new BattleHudIntent(BattleHudIntentKind.Back)))
            {
                TakeGuiKeyboardFocus();
                input.Use();
            }
        }

        internal void CompleteGuiInput(Event input, Rect content)
        {
            if (ForeignGuiPointer || !OwnsGuiPointer && !content.Contains(input.mousePosition))
                return;
            switch (input.type)
            {
                case EventType.MouseDown when input.button == 0:
                    TakeGuiKeyboardFocus();
                    CaptureGuiPointer();
                    input.Use();
                    break;
                case EventType.MouseUp when input.button == 0:
                    ReleaseGuiPointer();
                    input.Use();
                    break;
                case EventType.MouseDrag when input.button == 0:
                case EventType.ScrollWheel:
                    input.Use();
                    break;
            }
        }
    }
}
