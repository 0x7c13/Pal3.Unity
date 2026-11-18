// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    public sealed partial class BattleSession
    {
        public string CanSetProtection(string controller, string target)
        {
            if (_catalog is not IBattlePhysicalTimings)
                return "This catalog has no original protection body tasks.";
            if (WaitingActor != controller || ActiveFormationController != null)
                return "Choose the current ready controller.";
            return ValidatePhysicalProtection(controller, target);
        }

        public bool SetProtection(string controller, string target)
        {
            if (CanSetProtection(controller, target) != null || !ApplyPhysicalProtection(controller, target))
                return false;
            _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.SetProtection, Actor = controller, Target = target });
            Add(BattleEventKind.ProtectionChanged, controller, target, detail: target == null ? "protection cleared" : "protection selected");
            return true;
        }
    }
}
