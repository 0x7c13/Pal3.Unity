// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Linq;
    using Battle;
    using Battle.Presentation;
    using UnityEngine;

    public sealed partial class BattleHudView
    {
        private bool WorldSelectionOpen => _presenter.Page is BattleHudPage.Targets or BattleHudPage.Protection or BattleHudPage.Ai;

        private (string Control, string Actor)[] WorldSelectionActors() => _presenter.Page switch
        {
            BattleHudPage.Targets => _presenter.Targets.Select(actor => ("target:" + actor.Id, actor.Id)).ToArray(),
            BattleHudPage.Protection => _battle.Actors.Where(actor => actor.Side == BattleSide.Ally && _battle.CanSetProtection(_presenter.Controller, actor.Id) == null)
                .Select(actor => ("protect:" + actor.Id, actor.Id)).ToArray(),
            BattleHudPage.Ai => _battle.Actors.Select(actor => ("ai-actor:" + actor.Id, actor.Id)).ToArray(),
            _ => Array.Empty<(string, string)>()
        };

        private void PublishFocusLayout()
        {
            if (_preview == null || _viewport.Scale <= 0)
                return;
            var world = WorldSelectionActors()
                .Select(entry => (entry.Control, Projected: _preview.TryActorSelectionPoint(entry.Actor, _viewport.Viewport, out Vector2 screen), Screen: screen))
                .Where(entry => entry.Projected)
                .Select(entry =>
                {
                    Vector2 point = _viewport.ToCanvas(entry.Screen);
                    return (Id: entry.Control, X: point.x, Y: point.y);
                });
            _presenter.SetFocusLayout(_hitControls.Select(control => (control.Id, control.Rect.center.x, control.Rect.center.y)).Concat(world));
        }
    }
}
