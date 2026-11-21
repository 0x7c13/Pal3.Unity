// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using UnityEngine;

    public sealed partial class BattleHudView
    {
        private void DrawSceneField()
        {
            if (!_layout.IsPal3A || _battle.FiveSpiritElement is < 1 or > 5)
                return;
            int resource = _battle.FiveSpiritElement switch
            {
                3 => 3,
                4 => 2,
                _ => _battle.FiveSpiritElement - 1
            };
            // The released UI creates six overlapping widgets with allied
            // defaults. Field updates hide all six, replace the selected
            // element's image for force 1/2, then show that widget only.
            string force = _battle.FiveSpiritForce == 2 ? "e" : "w";
            Sprite($"control/{force}_sx{resource}", new Rect(_viewport.CanvasSize.x - 56, 81, 45, 60));
        }
    }
}
