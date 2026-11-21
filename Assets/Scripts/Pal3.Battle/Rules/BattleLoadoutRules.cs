// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    public static class BattleLoadoutRules
    {
        public static int DefaultWeapon(string variant, int identity)
        {
            if (identity is < 0 or > 4)
                throw new ArgumentOutOfRangeException(nameof(identity));
            return variant switch
            {
                "PAL3" => BattleMagicSwordRules.WeaponId,
                "PAL3A" => identity switch { 0 => 82110, 1 => 82209, 2 => 82310, 3 => 82402, _ => 82508 },
                _ => throw new ArgumentException("Unknown original game variant.", nameof(variant))
            };
        }

        public static int PlayerIdentity(string variant, int actorId) => variant switch
        {
            "PAL3" => actorId switch { 101 => 0, 104 => 1, 106 => 2, 107 => 3, 102 => 4, _ => -1 },
            "PAL3A" => actorId is >= 101 and <= 105 ? actorId - 101 : -1,
            _ => throw new ArgumentException("Unknown original game variant.", nameof(variant))
        };

        public static bool IsNativeDefaultWeapon(string variant, BattleParticipant actor)
        {
            if (variant is not ("PAL3" or "PAL3A") || actor == null || !actor.UsesNativeDefaultWeapon)
                return false;
            int identity = PlayerIdentity(variant, actor.ActorId);
            return actor.Side == BattleSide.Ally && identity >= 0 &&
                actor.Equipment is { Length: > 0 } && actor.Equipment[0] == DefaultWeapon(variant, identity);
        }

        // Preparation is deterministic and returns a copy. The authoring draft
        // and the original database remain untouched, including across replay.
        public static BattleParticipant Prepare(string variant, BattleParticipant actor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            BattleParticipant copy = actor.Copy();
            int identity = PlayerIdentity(variant, actor.ActorId);
            if (copy.Side != BattleSide.Ally || identity < 0 || copy.Equipment == null ||
                copy.Equipment.Length > 0 && copy.Equipment[0] != 0)
                return copy;
            if (copy.Equipment.Length == 0)
                copy.Equipment = new int[6];
            copy.Equipment[0] = DefaultWeapon(variant, identity);
            copy.UsesNativeDefaultWeapon = true;
            if (variant == "PAL3" && identity == 0)
                copy.MagicSword ??= new BattleMagicSword();
            return copy;
        }
    }
}
