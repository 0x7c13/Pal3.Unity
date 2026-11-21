// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    public readonly struct BattleRelationChange
    {
        public bool Applied
        {
            get;
        }

        public int Value
        {
            get;
        }

        public string Sound
        {
            get;
        }

        internal BattleRelationChange(bool applied, int value, string sound)
        {
            Applied = applied;
            Value = value;
            Sound = sound;
        }
    }

    public static class BattleRelationRules
    {
        public static BattleRelationChange Change(string variant, int nativeIndex, int current, int requested, bool heroValid)
        {
            if (variant is not ("PAL3" or "PAL3A") || nativeIndex is < 0 or > 10)
                throw new ArgumentException("Invalid original relationship writer context.");
            if (!heroValid || nativeIndex == 4 || variant == "PAL3A" && nativeIndex == 3)
                return new BattleRelationChange(false, current, null);
            int value = Math.Clamp(unchecked(current + requested), 0, 999);
            return new BattleRelationChange(true, value, requested > 0 ? "wa010" : requested < 0 ? "wa048" : null);
        }
    }
}
