// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Linq;

    public static class BattleAttributeCalculator
    {
        public static int[] Calculate(BattleParticipant initial, BattleActorDefinition definition, string variant)
        {
            if (initial == null || definition == null)
                throw new ArgumentNullException(initial == null ? nameof(initial) : nameof(definition));
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown combat variant.", nameof(variant));
            if (initial.Attributes == null || initial.Attributes.Length != 12)
                throw new ArgumentException("An actor needs twelve attributes.", nameof(initial));
            if (initial.AttributeSource == BattleAttributeSource.ExplicitEffective)
                return initial.Attributes.ToArray();
            int[] values;
            if (initial.AttributeSource == BattleAttributeSource.BaseWithEquipment)
                values = initial.Attributes.ToArray();
            else if (initial.AttributeSource == BattleAttributeSource.LevelGrowth)
            {
                if (!string.IsNullOrEmpty(definition.GrowthUnsupportedReason))
                    throw new NotSupportedException(definition.GrowthUnsupportedReason);
                if (initial.Level < 1 || initial.Level >= definition.AttributesByLevel.Count ||
                    definition.AttributesByLevel[initial.Level].Count != 12)
                    throw new NotSupportedException("Original level attributes are unavailable for this actor.");
                values = definition.AttributesByLevel[initial.Level].ToArray();
            }
            else
                throw new ArgumentException("Unknown attribute source.", nameof(initial));

            // Native CalMaxRV: weapon, cloth, hat, shoes, two ornaments. Percentage
            // vital effects belong to per-round recovery and do not change maxima.
            foreach (BattleEquipmentDefinition item in definition.Equipment.OrderBy(item => EquipmentOrder(item.Slot)))
            {
                if (item.Slot == 0 && item.Id == BattleMagicSwordRules.WeaponId)
                {
                    bool nativeDefault = BattleLoadoutRules.IsNativeDefaultWeapon(variant, initial);
                    BattleMagicSwordSnapshot cultivation = definition.Identity == 0 ? definition.MagicSword : item.MagicSwordCultivation;
                    if (variant != "PAL3" || (definition.Identity != 0 && !nativeDefault) || cultivation == null)
                        throw new NotSupportedException("Weapon 6603 requires captured PAL3 cultivation attributes and a valid owner or native fallback.");
                    // CalMaxRV keys cultivation attributes by item ID, whereas
                    // combat energy and weapon skills belong only to Jingtian.
                    for (int attribute = 0; attribute < 12; attribute++)
                        values[attribute] = checked(values[attribute] + BattleMagicSwordRules.EquipmentAttributeBonus(cultivation, attribute));
                }
                foreach (BattleAttributeEffect effect in item.Attributes)
                {
                    if (effect.Mode != BattleValueMode.Absolute || effect.Attribute >= 7 && (variant == "PAL3A" || item.Slot == 0))
                        continue;
                    values[effect.Attribute] = checked(values[effect.Attribute] + effect.Value);
                }
                for (int element = 0; element < 5; element++)
                {
                    if ((item.ElementMask & (1 << element)) != 0)
                        values[element + 7] = checked(values[element + 7] + 1);
                }
            }
            return values;
        }

        private static int EquipmentOrder(int slot) => slot switch { 0 => 0, 2 => 1, 1 => 2, _ => slot };
    }
}
