// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    public readonly struct BattleWeaponVisibility
    {
        public bool Left
        {
            get;
        }

        public bool Right
        {
            get;
        }

        public BattleWeaponVisibility(bool left, bool right)
        {
            Left = left;
            Right = right;
        }
    }

    public static class BattleWeaponPresentationRules
    {
        // Native logical action indices, not the currently retained MV3 name.
        // The published combat tables and additional A actions are independent
        // of the scene actor enum's NpcRun alias.
        public static BattleWeaponVisibility Visible(string variant, int identity, int action, int weaponType,
            bool hiddenForm = false, bool hasWeapon = true, bool external = false, float leftFlag = 0, float rightFlag = 0)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown original combat variant.", nameof(variant));
            bool left = action == 46;
            bool right = action is >= 29 and <= 49 && action is not (38 or 46 or 48) || variant == "PAL3A" && action is 60 or 61;
            if (variant == "PAL3")
            {
                if (identity == 0 && action is 39 or 49)
                    left = right = false;
                if (identity is 1 or 3)
                    left = right = true;
                if (identity is 2 or 4 && action == 46)
                {
                    left = false;
                    right = true;
                }
                if (external)
                {
                    left = leftFlag >= 0;
                    right = rightFlag >= 0;
                }
            }
            else
            {
                if (identity == 0)
                {
                    ApplyAHelper(identity, action, weaponType, ref left, ref right);
                    if (hiddenForm)
                        left = right = false;
                }
                if (identity == 1)
                {
                    if (weaponType == 3)
                        left = right = true;
                    else if (weaponType == 4)
                    {
                        left = false;
                        right = true;
                    }
                }
                if (identity == 2)
                {
                    if (hiddenForm)
                        left = right = false;
                    else if (weaponType == 6)
                        left = right = true;
                    else if (weaponType == 5)
                    {
                        left = true;
                        right = false;
                    }
                }
                if (identity == 3)
                {
                    if (weaponType == 7)
                    {
                        left = false;
                        right = true;
                    }
                    if (weaponType == 10)
                    {
                        left = true;
                        right = false;
                    }
                    ApplyAHelper(identity, action, weaponType, ref left, ref right);
                }
                if (identity == 4)
                    ApplyAHelper(identity, action, weaponType, ref left, ref right);
                if (external)
                {
                    // A: positive forces on, zero forces off, negative retains
                    // the table/weapon result. PAL3's zero instead forces on.
                    if (leftFlag >= 0)
                        left = leftFlag > 0;
                    if (rightFlag >= 0)
                        right = rightFlag > 0;
                }
            }
            return hasWeapon ? new BattleWeaponVisibility(left, right) : default;
        }

        private static void ApplyAHelper(int identity, int action, int weapon, ref bool left, ref bool right)
        {
            if (identity == 0)
            {
                if (action == 46)
                {
                    left = false;
                    right = true;
                }
                else if (action == 59 || action == 62 && weapon == 2)
                {
                    left = false;
                    right = true;
                }
                else if (action == 62)
                    left = false;
            }
            if (identity == 3)
            {
                if (action == 59 && weapon == 10)
                    left = right = false;
                if (action == 62)
                {
                    if (weapon == 2)
                        right = false;
                    left = true;
                }
            }
            if (identity == 4 && action is 46 or 59)
            {
                left = false;
                right = true;
            }
        }
    }
}
