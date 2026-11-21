// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class BattleResultStatistics
    {
        // Both shipped UI callbacks publish float task time, retain the
        // gravity product through x87 and truncate the final MoveWindow pixel.
        public static int CardY(int column, double seconds)
        {
            if (column < 0 || column > 4 || !double.IsFinite(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            float now = (float)seconds;
            float start = column * .08f;
            if (now <= start)
                return -256;
            double elapsed = (double)now - start;
            return (int)Math.Min(0, elapsed * elapsed * 1500d - 256d);
        }

        public static IReadOnlyList<int> MagicUseCounts(string variant, BattleDefinitions definitions, int identity, IReadOnlyList<BattleSkillSnapshot> skills)
        {
            IReadOnlyList<BattleMagicSlot> slots = BattleMagicSlots.Capture(variant, definitions, identity, skills);
            return Array.AsReadOnly(Enumerable.Range(0, 6).Select(group => slots.Where(slot => slot.Group == group)
                .Sum(slot => skills.FirstOrDefault(skill => skill.Id == slot.SkillId)?.Uses ?? 0)).ToArray());
        }

        // Native reward accumulation can wrap to a negative signed int32 gain.
        // Animate that committed value without changing the settlement result.
        public static int Experience(int gained, double seconds)
        {
            if (!double.IsFinite(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            float ratio = Math.Clamp((float)seconds / 2f, 0, 1);
            return BattleBasicRules.Round(gained * ratio);
        }
    }
}
