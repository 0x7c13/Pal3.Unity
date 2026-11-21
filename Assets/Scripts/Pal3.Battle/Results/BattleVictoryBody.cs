// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System.Collections.Generic;

    // A single Win task uses the same result role controller as character
    // level-up tasks. Completion leaves action49 until the next free pass.
    public sealed class BattleVictoryBody
    {
        private readonly BattleResultRoleBody _role;

        public BattleBodyAnimationBinding Binding => _role.Binding;

        public bool Completed => _role.VictoryCompleted;

        public float PlaybackRate => _role.PlaybackRate;

        public float BladeSeconds => _role.BladeSeconds;

        public BattleVictoryBody(string variant, BattleBodyAnimationBinding initial,
            BattlePhysicalAnimation win, BattlePhysicalAnimation idle)
        {
            _role = new BattleResultRoleBody(variant, initial, new Dictionary<int, BattlePhysicalAnimation> { [49] = win, [32] = idle },
                new BattleResultBodyState(100, 100), true);
        }

        public void Step() => _role.Step();
    }
}
