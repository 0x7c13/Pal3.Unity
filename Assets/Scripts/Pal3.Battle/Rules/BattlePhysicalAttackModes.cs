// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    // Published InitRoleState/AttackSpecAffect switches, not the GDB skill range
    // or state descriptions. Modes 930-934 belong to PAL3A.
    public static class BattlePhysicalAttackModes
    {
        public static BattleTargetRange Range(int modeId)
        {
            ValidateMode(modeId);
            return modeId switch
            {
                916 or 917 or 921 or 922 or 926 or 927 or 933 => BattleTargetRange.OpponentRow,
                918 or 919 or 920 or 923 or 928 or 930 or 931 or 932 => BattleTargetRange.OpponentColumn,
                _ => BattleTargetRange.OpponentSingle
            };
        }

        public static BattleState? AddedState(int modeId)
        {
            ValidateMode(modeId);
            return modeId switch
            {
                901 or 933 => BattleState.PoisonWater,
                902 or 918 or 926 => BattleState.PoisonFire,
                903 or 932 => BattleState.PoisonWind,
                904 => BattleState.PoisonThunder,
                905 or 920 or 927 => BattleState.PoisonEarth,
                906 => BattleState.Paralysis,
                907 or 919 or 921 => BattleState.Seal,
                908 or 928 => BattleState.Forbidden,
                909 => BattleState.Sleep,
                910 or 916 => BattleState.Chaos,
                911 or 917 => BattleState.Madness,
                // PAL3A's 930/931/934 do not add the states written in the GDB.
                _ => null
            };
        }

        public static BattleTargetRange WeaponRange(string variant, int weaponType, int identity)
        {
            ValidateVariant(variant);
            if (identity < 0 || identity > 4)
                throw new ArgumentOutOfRangeException(nameof(identity), "A weapon range needs the original player identity.");
            if (weaponType == 0)
                throw new NotSupportedException("Weapon type 0 consumes original random initialization and needs a session-owned range snapshot.");
            if (variant == "PAL3")
            {
                if (weaponType < 1 || weaponType > 7)
                    throw new NotSupportedException("Unknown PAL3 weapon type " + weaponType + ".");
                // The published game overrides Jingtian's range after the type switch.
                if (identity == 0)
                    return BattleTargetRange.OpponentColumn;
                return weaponType switch
                {
                    1 or 3 => BattleTargetRange.OpponentColumn,
                    4 or 7 => BattleTargetRange.OpponentRow,
                    _ => BattleTargetRange.OpponentSingle
                };
            }
            if (weaponType < 1 || weaponType > 10)
                throw new NotSupportedException("Unknown PAL3A weapon type " + weaponType + ".");
            return weaponType switch
            {
                2 or 10 => BattleTargetRange.OpponentRow,
                4 or 9 => BattleTargetRange.OpponentColumn,
                _ => BattleTargetRange.OpponentSingle
            };
        }

        public static string UnsupportedReason(string variant, int modeId)
        {
            ValidateVariant(variant);
            // Stock non-combat spirits use 0; PAL3A actor 7398 uses 7714.
            // InitRoleState leaves all range flags clear and AttackSpecAffect
            // takes its default branch. The latter is not an AI spell alias
            // when stored in the actor's normal-attack field.
            if (modeId == 0 || variant == "PAL3A" && modeId == 7714)
                return "";
            if (modeId < 901 || modeId > (variant == "PAL3" ? 929 : 934))
                return "Unknown original physical attack mode " + modeId + ".";
            // Published special handler 30 succeeds without changing state.
            return "";
        }

        private static void ValidateMode(int modeId)
        {
            if (modeId != 0 && modeId != 7714 && (modeId < 901 || modeId > 934))
                throw new NotSupportedException("Unknown original physical attack mode " + modeId + ".");
        }

        private static void ValidateVariant(string variant)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown combat variant.", nameof(variant));
        }
    }
}
