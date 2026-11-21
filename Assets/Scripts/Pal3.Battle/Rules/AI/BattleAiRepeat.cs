// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    public sealed partial class BattleAi
    {
        public int SelectRepeatTarget(int source, bool enemy, bool filterBackRole)
        {
            int best = -1;
            int minimum = int.MaxValue;
            for (int target = enemy ? 5 : 0; target < (enemy ? 11 : 5); target++)
            {
                BattleAiRole actor = Roles[target];
                if (!actor.Valid || !actor.Alive || actor.Fled || actor.Fleeing)
                    continue;
                if (filterBackRole && target == 6 && !_port.CanAttackBack(source, target))
                    continue;
                if (actor.Hp < minimum)
                {
                    minimum = actor.Hp;
                    best = target;
                }
            }
            return best;
        }

        public bool RepeatTargetsDead(BattleAiCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (command.Target >= 0 && command.Target < 11)
            {
                BattleAiRole target = Roles[command.Target];
                return target.Hp <= 0 || target.Fled || _pal3A && target.Fleeing;
            }
            for (int target = 0; target < 11; target++)
            {
                BattleAiRole actor = Roles[target];
                if (command.TargetMask[target] && actor.Valid && actor.Hp > 0 && !actor.Fled && (!_pal3A || !actor.Fleeing))
                    return false;
            }
            return true;
        }

        public bool Repeat(int source, BattleAiCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (command.Kind is 0x109 or 0x10b)
                return _port.SubmitRepeat(command) >= 0;
            bool normal = command.Kind == 0x103;
            bool extra = _pal3A && command.Kind == 0x10f;
            bool ability = command.Kind is 0x104 or 0x105 or 0x106 || extra;
            if (!normal && (!ability || !_port.CanRepeat(source, command)))
                return PlayerNormal(source, false);
            // Preserve the call before the NewRole check: the native function evaluates both this way.
            if (_port.AllRepeatTargetsDead(command) || NewRole[source])
            {
                Array.Clear(command.TargetMask, 0, command.TargetMask.Length);
                bool enemy = normal || extra || _port.IsRepeatAttack(command);
                command.Target = _port.PickRepeatTarget(source, enemy, _pal3A && enemy && command.Kind != 0x105);
                if (_pal3A && command.Target < 0)
                    return false;
                command.MultipleTargets = false;
                if (!extra)
                    _port.ExpandRepeatRange(command);
            }
            return _port.SubmitRepeat(command) >= 0;
        }
    }
}
