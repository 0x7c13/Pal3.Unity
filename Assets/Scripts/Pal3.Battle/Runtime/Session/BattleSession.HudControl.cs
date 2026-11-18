// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    public sealed partial class BattleSession
    {
        // Native panel confirmation is distinct from the free debugger setter.
        // One recorded input owns both the profile write and its gauge cost.
        public bool CommitAiControl(string controller, int flags)
        {
            if (flags is < 0 or > 63)
                throw new ArgumentOutOfRangeException(nameof(flags));
            if (!CanRequestAiInput(controller, out Actor actor) || IsPal3ATransformed(controller))
                return false;
            int before = GetAiProfile(controller);
            if (before == flags)
                return true;
            ApplyAiProfile(actor, flags);
            bool consumes = BattleAiControlRules.ConsumesGauges(before, flags);
            if (consumes)
            {
                // Native SetToStart writes these slots and second-running only;
                // it does not change the role's guard, pose, rounds or resources.
                AdvanceFiveSpiritFieldTurn(actor);
                actor.Gauge = actor.CastGauge = 0;
                actor.Ready = false;
            }
            _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.AiControl, Actor = controller, AiFlags = flags });
            Add(BattleEventKind.ControlChanged, controller, detail: "original AI control flags " + flags +
                (consumes ? "; controller gauges cleared" : "; gauges retained"));
            return true;
        }
    }
}
