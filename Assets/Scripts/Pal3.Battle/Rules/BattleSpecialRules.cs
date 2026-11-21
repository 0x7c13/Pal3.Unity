// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;

    public enum BattleSpecialHandler
    {
        None, HalfActualHpAbsorb, ReturnedMpAbsorb, Steal, MoneyRecovery, MoneyThrow, RandomPoison, PoisonDragonGall, Summon,
        GenericOnly, RandomFormation, AbsorbHpWorm, ForceFlee
    }

    public sealed class BattleStealPoolSnapshot
    {
        public string Actor
        {
            get;
        }

        public int Money
        {
            get;
        }

        public int ItemId
        {
            get;
        }

        public int ItemCount
        {
            get;
        }

        internal BattleStealPoolSnapshot(string actor, int money, int itemId, int itemCount)
        {
            Actor = actor;
            Money = money;
            ItemId = itemId;
            ItemCount = itemCount;
        }
    }

    // Published dispatch and arithmetic only. The session owns every resource and state write.
    public static class BattleSpecialRules
    {
        public static BattleSpecialHandler AbilityHandler(string variant, int id, bool item, bool magic, int rawSpecial)
        {
            ValidateVariant(variant);
            if (item)
                return id == 6230 ? BattleSpecialHandler.PoisonDragonGall : rawSpecial switch
                {
                    1 => BattleSpecialHandler.RandomPoison,
                    2 => BattleSpecialHandler.ForceFlee,
                    29 => BattleSpecialHandler.Summon,
                    _ => BattleSpecialHandler.None
                };
            if (magic)
                return rawSpecial switch
                {
                    1 => BattleSpecialHandler.RandomPoison,
                    25 => BattleSpecialHandler.RandomFormation,
                    29 => BattleSpecialHandler.Summon,
                    18 when id == 258 => BattleSpecialHandler.GenericOnly,
                    0 when variant == "PAL3A" && id == 205 => BattleSpecialHandler.GenericOnly,
                    35 when variant == "PAL3A" && id == 273 => BattleSpecialHandler.GenericOnly,
                    _ => BattleSpecialHandler.None
                };
            if (id == 4101)
                return BattleSpecialHandler.Steal;
            if (id == (variant == "PAL3" ? 4201 : 4301))
                return BattleSpecialHandler.HalfActualHpAbsorb;
            if (id == (variant == "PAL3" ? 4304 : 4504))
                return BattleSpecialHandler.ReturnedMpAbsorb;
            if (id == (variant == "PAL3" ? 4102 : 4106))
                return BattleSpecialHandler.MoneyRecovery;
            if (id == (variant == "PAL3" ? 4305 : 4206))
                return BattleSpecialHandler.MoneyThrow;
            if (variant == "PAL3" && id == 4402)
                return BattleSpecialHandler.AbsorbHpWorm;
            if (variant == "PAL3A" && id == 4505 && rawSpecial == 37)
                return BattleSpecialHandler.GenericOnly;
            return BattleSpecialHandler.None;
        }

        public static bool SupportsSuccessRate(string variant, int id, int rate)
        {
            ValidateVariant(variant);
            return rate is 0 or 1 or 100 || variant == "PAL3A" && id is 4503 or 4505 && rate == 50;
        }

        public static bool UsesLiteralSuccessRate(string variant, int id, int rate)
        {
            ValidateVariant(variant);
            return variant == "PAL3A" && id is 4503 or 4505 && rate == 50;
        }

        public static bool RejectsBossBeforeSuccessRoll(string variant, int id)
        {
            ValidateVariant(variant);
            return variant == "PAL3A" && id == 4505;
        }

        public static bool UsesAbsoluteHpCost(string variant, int id)
        {
            ValidateVariant(variant);
            return variant == "PAL3A" && id == 4507;
        }

        public static bool SupportsEquipmentSpecial(string variant, int id, int slot, int rawSpecial)
        {
            ValidateVariant(variant);
            if (rawSpecial is 0 or 20 or 21 or 22 or 23 or 24)
                return true;
            // Released non-weapon initialization ignores these markers. Their
            // ordinary GDB attributes still apply; no named passive is inferred.
            return slot != 0 && (variant == "PAL3" && id == 84006 && rawSpecial == 32 ||
                variant == "PAL3A" && (id == 8148 && rawSpecial == 33 || id == 8151 && rawSpecial == 34));
        }

        public static IReadOnlyList<BattleSlot> RandomFormation(BattleRandom random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            var lineup = new[] { BattleSlot.Water, BattleSlot.Fire, BattleSlot.Wind, BattleSlot.Thunder, BattleSlot.Earth, BattleSlot.Center };
            for (int swap = 0; swap < 32; swap++)
            {
                int first = random.Next() % 6;
                int second = random.Next() % 6;
                (lineup[first], lineup[second]) = (lineup[second], lineup[first]);
            }
            return Array.AsReadOnly(lineup);
        }

        public static int AbsorbHpWormRequest(int maximumHp, int round)
        {
            if (maximumHp < 0 || round < 1 || round > byte.MaxValue)
                throw new ArgumentOutOfRangeException(maximumHp < 0 ? nameof(maximumHp) : nameof(round));
            // Released x87 multiplies the integer by the stored float constant
            // before truncation; keep that precision instead of a float product.
            float fraction = round switch
            {
                1 => .05f,
                2 => .1f,
                3 => .15f,
                _ => 0f
            };
            return (int)(-maximumHp * (double)fraction);
        }

        public static bool IsTreasure(string variant, int id)
        {
            ValidateVariant(variant);
            return id >= 6119 && id <= (variant == "PAL3" ? 6125 : 6124);
        }

        public static int HalfActualHp(int before, int after) => Math.Abs((before - after) / 2);

        public static int AbsorbedMp(int nativeReturnedValue) => BattleBasicRules.SignedAbs(nativeReturnedValue);

        public static int RequiredMoney(string variant, BattleSpecialHandler handler, int rawCost, int livingAllies, int primaryLevel)
        {
            ValidateVariant(variant);
            return handler switch
            {
                BattleSpecialHandler.MoneyRecovery => Math.Max(rawCost, checked(livingAllies * (40 + 2 * primaryLevel))),
                BattleSpecialHandler.MoneyThrow => Math.Max(rawCost, variant == "PAL3" ? 1000 : 3000),
                _ => rawCost
            };
        }

        public static int StealAmount(int remaining, BattleRandom random)
        {
            if (remaining < 0)
                throw new ArgumentOutOfRangeException(nameof(remaining));
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            return remaining == 0 ? 0 : random.Next() % remaining + 1;
        }

        public static int ShieldedHpRequest(int signedRequest, int money, out int payment)
        {
            if (money < 0)
                throw new ArgumentOutOfRangeException(nameof(money));
            payment = signedRequest >= 0 ? 0 : (int)Math.Min(money, -(long)signedRequest);
            return signedRequest + payment;
        }

        private static void ValidateVariant(string variant)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown combat variant.", nameof(variant));
        }
    }
}
