// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    // Original purification UI order differs from the GDB elemental attribute order.
    public enum BattleSwordPurificationElement
    {
        Water, Fire, Thunder, Wind, Earth, None
    }

    // Captured cultivation data and battle energy. The editor supplies these values;
    // battle rules neither consult PlayerMgr nor reconstruct cultivation history.
    public sealed class BattleMagicSwordSnapshot
    {
        public int AttackBonus
        {
            get;
        }

        public BattleSwordPurificationElement SelectedElement
        {
            get;
        }

        public int PurificationCount
        {
            get;
        }

        public int SkillId
        {
            get;
        }

        public uint ChargeCoefficient
        {
            get;
        }

        public float Energy
        {
            get;
        }

        public BattleMagicSwordSnapshot(int attackBonus, BattleSwordPurificationElement selectedElement,
            int purificationCount, int skillId, uint chargeCoefficient, float energy)
        {
            if (attackBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(attackBonus));
            if (selectedElement < BattleSwordPurificationElement.Water || selectedElement > BattleSwordPurificationElement.None)
                throw new ArgumentOutOfRangeException(nameof(selectedElement));
            if (purificationCount < 0)
                throw new ArgumentOutOfRangeException(nameof(purificationCount));
            if (skillId < 0)
                throw new ArgumentOutOfRangeException(nameof(skillId));
            if (float.IsNaN(energy) || float.IsInfinity(energy) || energy < 0 || energy > 200)
                throw new ArgumentOutOfRangeException(nameof(energy));
            AttackBonus = attackBonus;
            SelectedElement = selectedElement;
            PurificationCount = purificationCount;
            SkillId = skillId;
            ChargeCoefficient = chargeCoefficient;
            Energy = energy;
        }

        public BattleMagicSwordSnapshot WithEnergy(float energy, uint? chargeCoefficient = null) =>
            new(AttackBonus, SelectedElement, PurificationCount, SkillId, chargeCoefficient ?? ChargeCoefficient, energy);
    }

    // PAL3 released EXE rules. Session code owns energy writes, action validation,
    // target/status order and events; cultivation menus are outside battle scope.
    public static class BattleMagicSwordRules
    {
        public const int WeaponId = 6603;
        public const float MaximumEnergy = 200;
        public const float ReadyThreshold = 199.9f;

        public static bool IsSupportedSkill(int id) => id >= 2201 && id <= 2224;

        public static int SelectedAttribute(BattleSwordPurificationElement element) => element switch
        {
            BattleSwordPurificationElement.Water => 7,
            BattleSwordPurificationElement.Fire => 8,
            BattleSwordPurificationElement.Thunder => 10,
            BattleSwordPurificationElement.Wind => 9,
            BattleSwordPurificationElement.Earth => 11,
            BattleSwordPurificationElement.None => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(element))
        };

        public static int EquipmentAttributeBonus(BattleMagicSwordSnapshot snapshot, int attribute)
        {
            RequireSnapshot(snapshot);
            if (attribute < 0 || attribute > 11)
                throw new ArgumentOutOfRangeException(nameof(attribute));
            if (attribute == 3)
                return snapshot.AttackBonus;
            return attribute == SelectedAttribute(snapshot.SelectedElement) ? 1 : 0;
        }

        public static bool IsReady(BattleMagicSwordSnapshot snapshot, int identity, bool equipped)
        {
            RequireSnapshot(snapshot);
            return identity == 0 && equipped && snapshot.SkillId != 0 && snapshot.Energy > ReadyThreshold;
        }

        // Invoke for every ordinary physical work/work1 marker before hit resolution,
        // including markers that subsequently miss. A zero coefficient is repaired there.
        public static BattleMagicSwordSnapshot GainOnPhysicalWork(BattleMagicSwordSnapshot snapshot, int identity, bool equipped)
        {
            RequireSnapshot(snapshot);
            if (identity != 0 || !equipped || snapshot.SkillId == 0)
                return snapshot;
            uint coefficient = snapshot.ChargeCoefficient == 0 ? 1 : snapshot.ChargeCoefficient;
            float energy = Math.Min(MaximumEnergy, (float)(snapshot.Energy + (double)coefficient));
            return snapshot.WithEnergy(energy, coefficient);
        }

        // Native action start consumes the entire gauge before any target is affected.
        public static BattleMagicSwordSnapshot ConsumeEnergy(BattleMagicSwordSnapshot snapshot)
        {
            RequireSnapshot(snapshot);
            return snapshot.WithEnergy(0);
        }

        public static int SkillPower(BattleMagicSwordSnapshot snapshot, int skillId, short rawPower,
            BattleSide targetSide, out BattleValueMode mode)
        {
            RequireSnapshot(snapshot);
            if (!IsSupportedSkill(skillId))
                throw new NotSupportedException("Unknown PAL3 magic sword skill.");
            if (targetSide is not (BattleSide.Ally or BattleSide.Enemy))
                throw new ArgumentOutOfRangeException(nameof(targetSide));
            mode = targetSide == BattleSide.Ally ? BattleValueMode.Percentage : BattleValueMode.Absolute;
            // No magic-level, elemental, Warp or technique success calculation belongs here.
            return skillId == 2224 ? BattleNativeArithmetic.TruncateToLowInt32(rawPower * (1.0 + 0.5 * snapshot.PurificationCount)) : rawPower;
        }

        private static void RequireSnapshot(BattleMagicSwordSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
        }
    }
}
