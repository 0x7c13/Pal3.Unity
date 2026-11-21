// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    internal static class BattleNativeArithmetic
    {
        // Released CRT _ftol truncates to signed int64; callers retain EAX.
        // Invalid conversion returns int64 indefinite, whose low int32 is zero.
        // This differs from util::f2i's rounded int32 FISTP conversion.
        internal static int TruncateToLowInt32(double value)
        {
            double truncated = Math.Truncate(value);
            long converted = truncated >= long.MinValue && truncated < (double)long.MaxValue ? (long)truncated : long.MinValue;
            return unchecked((int)converted);
        }
    }
}
