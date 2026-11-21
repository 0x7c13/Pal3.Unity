// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using Battle.Presentation;

    public sealed partial class BattleHudView
    {
        private void PlayMenuSound(string sound)
        {
            if (_hasInputFocus && _preview?.Audio is { PlaybackEnabled: true, Muted: false } audio)
            {
                audio.Play(sound);
            }
        }

        private void PlayInputSounds(BattleHudIntent intent, BattleHudPage previousPage, string previousController,
            string previousFocus, int previousCategory, bool accepted)
        {
            if (!accepted && _presenter.ActivatedControl != null && _presenter.Message != null)
            {
                PlayMenuSound("wa004");
                return;
            }
            bool activate = accepted && intent.Kind is BattleHudIntentKind.Confirm or BattleHudIntentKind.PointerUp;
            string control = intent.ControlId ?? previousFocus;
            bool categoryChanged = previousPage is BattleHudPage.Magic or BattleHudPage.Items && previousCategory != _presenter.Category;
            if (activate)
            {
                if (control?.StartsWith("ability:", StringComparison.Ordinal) == true)
                    PlayMenuSound("wa002");
                else if (previousPage is BattleHudPage.Commands or BattleHudPage.Formation or BattleHudPage.Ai ||
                    control?.StartsWith("category:", StringComparison.Ordinal) == true)
                    PlayMenuSound("wa001");
            }
            else if (accepted && categoryChanged)
                PlayMenuSound("wa001");
            // World target selection has no window-pop sound. Returning to a
            // list or the command wheel opens that native panel once again.
            if (_presenter.Controller != null &&
                (_presenter.Page != previousPage || _presenter.Controller != previousController) &&
                _presenter.Page != BattleHudPage.Targets)
                PlayMenuSound("wa003");
        }
    }
}
