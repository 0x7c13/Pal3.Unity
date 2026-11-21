// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    public enum BattleDamageNumberColorMode
    {
        Original,
        Yellow,
        White
    }

    public static class BattleDamageNumberColors
    {
        public static uint Resolve(uint nativeColor, BattleDamageNumberColorMode mode)
        {
            // The native projection uniquely assigns this color to HP damage
            // and its zero result. Keep all other number kinds unchanged.
            if (nativeColor != 0x3232ffu)
                return nativeColor;
            return mode switch
            {
                BattleDamageNumberColorMode.Yellow => 0xffff00u,
                BattleDamageNumberColorMode.White => 0xffffffu,
                _ => nativeColor
            };
        }
    }
}
