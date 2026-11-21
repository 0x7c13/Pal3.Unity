// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed partial class BattleSession
    {
        private bool AutomaticMatches(BattleSide side, bool enabled)
        {
            if (Definitions == null)
                return true;
            InitializeAi();
            int first = side == BattleSide.Ally ? 0 : 5;
            int count = side == BattleSide.Ally ? 5 : 6;
            return Enumerable.Range(first, count).All(index =>
            {
                BattleAiProfile profile = _ai.Roles[index].Profile;
                return profile.Enabled == enabled && profile.Attack == enabled && profile.Magic == enabled &&
                    profile.Skill == enabled && profile.Item == enabled;
            });
        }

        private void AssignNativeActorIndices()
        {
            Actor[] allies = _actors.Where(actor => actor.Initial.Side == BattleSide.Ally).ToArray();
            var reserved = new HashSet<int>();
            // Known original identities keep their exact native slots. Custom
            // fixtures may use other actors; assign them only the unreserved
            // player slots, independently of resource identity and formation.
            foreach (var group in allies.Where(actor => actor.Definition?.Identity is >= 0 and < 5)
                .GroupBy(actor => actor.Definition.Identity).Where(group => group.Count() == 1))
            {
                group.Single().NativeIndex = group.Key;
                reserved.Add(group.Key);
            }
            foreach (Actor actor in allies.Where(actor => actor.NativeIndex < 0))
            {
                actor.NativeIndex = Enumerable.Range(0, 5).First(index => !reserved.Contains(index));
                reserved.Add(actor.NativeIndex);
            }
            foreach (Actor actor in _actors.Where(actor => actor.Initial.Side == BattleSide.Enemy))
                actor.NativeIndex = 5 + (int)actor.Initial.Slot;
        }
    }
}
