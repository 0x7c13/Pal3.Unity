// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using Battle;
    using Battle.Presentation;
    using UnityEditor;

    public sealed partial class BattleEditorWindow
    {
        private Func<EditorWindow> _gamepadFocusedWindow = () => focusedWindow;
        private (BattleSession Battle, string Controller, BattleHudPage Page, int Category, string FormationActor, string AiActor)? _gamepadContext;

        private bool CanReceiveGamepad() => _session != null && _gamepadFocusedWindow() == this && !DraftPending &&
            !EditorApplication.isPlayingOrWillChangePlaymode && !EditorGUIUtility.editingTextField;

        private (BattleSession Battle, string Controller, BattleHudPage Page, int Category, string FormationActor, string AiActor) CurrentGamepadContext() =>
            (_session.Battle, _session.Battle?.WaitingActor, _hud.Presenter.Page, _hud.Presenter.Category, _hud.Presenter.FormationActor, _hud.Presenter.AiActor);

        private bool GamepadPresentationBlocksHud => _session?.Battle is { } battle &&
            (battle.IsModalPresentation || battle.IsCoAttackPresentationBlockingInput);

        private void ReceiveGamepad(BattleEditorGamepadIntent input)
        {
            if (!CanReceiveGamepad())
                return;
            if (_session.IsReplaying)
            {
                if (input.Kind == BattleEditorGamepadIntentKind.Cancel)
                {
                    _session.CancelReplay();
                    Repaint();
                }
                return;
            }
            if (input.Kind == BattleEditorGamepadIntentKind.Transport)
            {
                // Start never opens an OS dialog or applies an authoring draft.
                if (PrimaryAction() is WorkspaceAction.Start or WorkspaceAction.Pause or WorkspaceAction.Resume or WorkspaceAction.Restart)
                {
                    RunPrimaryAction();
                    _gamepadContext = null;
                    _gamepadInput?.ResetNavigation();
                }
                return;
            }
            if (_session.ResultPresentation is { Completed: false } result)
            {
                if (input.Kind is BattleEditorGamepadIntentKind.Confirm or BattleEditorGamepadIntentKind.Cancel)
                {
                    if (input.Kind == BattleEditorGamepadIntentKind.Cancel && result.StatisticsVisible)
                        result.CloseStatistics();
                    _session.SkipResultStep();
                    _gamepadInput?.ResetNavigation();
                    Repaint();
                }
                return;
            }
            if (GamepadPresentationBlocksHud)
            {
                BattleSession battle = _session.Battle;
                if ((input.Kind is BattleEditorGamepadIntentKind.Confirm or BattleEditorGamepadIntentKind.Cancel) &&
                    (battle.IsModalPresentation ? battle.SkipModalPresentation() : battle.SkipCoAttackPresentation()))
                {
                    _session.SyncBattle();
                    Repaint();
                }
                return;
            }
            if (_session.Battle?.Status != BattleSessionStatus.Running)
                return;
            _hud.BindBattle(_session.Battle, _session.Data, _session.Preview, SyncHudBattle, SelectHudActor, Repaint);
            var before = CurrentGamepadContext();
            bool contextChanged = _gamepadContext.HasValue && _gamepadContext.Value != before;
            if (contextChanged)
                _gamepadInput?.ResetNavigation();
            _gamepadContext = before;
            // A repeat from the previous actor or mouse-selected page must not
            // move the new page. A fresh button edge remains fully usable.
            if (contextChanged && input.IsRepeat)
                return;
            _hud.TakeGamepadInput(_session.Battle);
            before = CurrentGamepadContext();
            _hud.SendGamepadIntent(input);
            var after = CurrentGamepadContext();
            if (after != before)
                _gamepadInput?.ResetNavigation();
            _gamepadContext = after;
            Repaint();
        }
    }
}
