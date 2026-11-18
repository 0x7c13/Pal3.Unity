// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BattleEquipmentData
    {
        public int Id;
        public int Slot;
        public int WeaponType;
        public BattleMagicSwordSnapshot MagicSwordCultivation;
        public bool StartupInvisibility;
        public int AttackStealPercent;
        public string Name = "";
        public BattleAttributeEffect[] Attributes = Array.Empty<BattleAttributeEffect>();
        public BattleStatusEffect[] States = Array.Empty<BattleStatusEffect>();
        public int ElementMask;
        public int SpSaving;
        public int MpSaving;
        public int CriticalBonus;
        public bool PhysicalReflectionPassive;
        public int TechniqueSuccessBonus;
        public int SpecialEffect;
        public string UnsupportedReason = "";
        public int[] RecoveryPerRound = new int[3];
    }

    public sealed class BattleEquipmentDefinition
    {
        // Attribute input for a native fallback; this does not grant energy or skills.
        public BattleMagicSwordSnapshot MagicSwordCultivation
        {
            get;
        }

        public int Id
        {
            get;
        }

        public int Slot
        {
            get;
        }

        public int WeaponType
        {
            get;
        }

        public bool StartupInvisibility
        {
            get;
        }

        public int AttackStealPercent
        {
            get;
        }

        public string Name
        {
            get;
        }

        public IReadOnlyList<BattleAttributeEffect> Attributes
        {
            get;
        }

        public IReadOnlyList<BattleStatusEffect> States
        {
            get;
        }

        public int ElementMask
        {
            get;
        }

        public int SpSaving
        {
            get;
        }

        public int MpSaving
        {
            get;
        }

        public int CriticalBonus
        {
            get;
        }

        public bool PhysicalReflectionPassive
        {
            get;
        }

        public int TechniqueSuccessBonus
        {
            get;
        }

        public int SpecialEffect
        {
            get;
        }

        public string UnsupportedReason
        {
            get;
        }

        public IReadOnlyList<int> RecoveryPerRound
        {
            get;
        }

        public BattleEquipmentDefinition(BattleEquipmentData data)
        {
            if (data == null || data.Id <= 0 || data.Slot < 0 || data.Slot > 5)
                throw new ArgumentException("Invalid equipped item.");
            Id = data.Id;
            Slot = data.Slot;
            WeaponType = data.WeaponType;
            MagicSwordCultivation = data.MagicSwordCultivation;
            StartupInvisibility = data.StartupInvisibility;
            AttackStealPercent = data.AttackStealPercent;
            Name = data.Name ?? "";
            Attributes = BattleAbilityDefinition.Capture(data.Attributes);
            States = BattleAbilityDefinition.Capture(data.States);
            ElementMask = data.ElementMask;
            SpSaving = data.SpSaving;
            MpSaving = data.MpSaving;
            CriticalBonus = data.CriticalBonus;
            PhysicalReflectionPassive = data.PhysicalReflectionPassive;
            TechniqueSuccessBonus = data.TechniqueSuccessBonus;
            SpecialEffect = data.SpecialEffect;
            UnsupportedReason = data.UnsupportedReason ?? "";
            RecoveryPerRound = BattleAbilityDefinition.Capture(data.RecoveryPerRound);
        }
    }
}
