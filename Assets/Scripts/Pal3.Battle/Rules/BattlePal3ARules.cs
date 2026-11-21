// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class BattlePal3ARules
    {
        public const int SpiritCaptureSkill = 4102;
        public const int PengxuTransformSkill = 4308;
        public const int GuInventoryItem = 6113;
        public const int LiaoRiCarryActor = 722;

        public static bool IsDedicatedAbility(int id) => id is SpiritCaptureSkill or PengxuTransformSkill;

        public static BattleSpiritWheelSnapshot Gain(BattleSpiritWheelSnapshot wheel, int element, int count)
        {
            if (wheel == null || element < 0 || element >= 5)
                throw new ArgumentException("Invalid spirit wheel operation.");
            int[] charges = wheel.Charges.ToArray();
            charges[element] = Math.Clamp(unchecked(charges[element] + count), 0, 9);
            return new BattleSpiritWheelSnapshot(charges);
        }

        public static int CaptureForecast(int level, int effectiveAttack, int effectiveDefense,
            int physicalElementDifference, bool rowOrColumn, BattleRandom random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            // ForecastDamage(type=7), not a real attack: no minimum damage roll,
            // madness multiplier, critical check, guarding or weapon divisor.
            float raw = (float)((effectiveAttack - effectiveDefense) * (1.0 + level * (double)0.005f));
            int value = random.Warp(-BattleBasicRules.Round(raw), .1f);
            if (rowOrColumn)
                value = value * (random.Warp(100, .05f) - 45) / 100;
            float scaled = (float)(value * (1.0 - physicalElementDifference * (double).05f));
            return BattleBasicRules.Round(scaled);
        }

        public static bool CanCapture(int hp, bool valid, bool alive, bool boss, int forecast)
            => valid && alive && !boss && hp <= BattleBasicRules.SignedAbs(forecast);

        public static int CaptureCount(IEnumerable<int> originalElementValues) => checked(originalElementValues.Sum());

        public static int AddLoss(int accumulated, int negativeRequest) => negativeRequest < 0
            ? unchecked(accumulated - negativeRequest) : accumulated;

        public static int CarryAttribute(int ordinary, int accumulated) => unchecked(ordinary + accumulated);

        public static int NangongAttribute(int ordinary, bool transformed) => transformed ? unchecked(ordinary * 2) : ordinary;

        public static bool ExpiresOnRound(int completedRounds, int roundLimit) => completedRounds >= roundLimit - 1;

        public static int PengxuSpellPower(int originalPower, bool transformed) => transformed
            ? BattleBasicRules.Round((float)(originalPower * 1.5f)) : originalPower;
    }
}
