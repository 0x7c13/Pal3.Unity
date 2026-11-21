// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;

    // Signed original powers. Target eligibility, critical hits, guarding and HP writes belong to the session.
    public sealed class BattleEffectRules
    {
        private readonly bool _isPal3A;

        public BattleEffectRules(string variant)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown combat variant.", nameof(variant));
            _isPal3A = variant == "PAL3A";
        }

        public int MagicPower(int raw, BattleParticipant source, BattleParticipant target, int elementMask,
            BattleRandom random, int enhancementMask = 0, int weatherElement = 0)
        {
            RequireParticipants(source, target, random);
            if ((elementMask & ~31) != 0 || (enhancementMask & ~31) != 0 || weatherElement < 0 || weatherElement > 5)
                throw new ArgumentOutOfRangeException(nameof(elementMask));
            if (raw == 0)
                return 0;
            // Both coefficient floats are exact multiples of 2^-28. Keep the
            // product exact until the native truncation, since double can round
            // across an integer boundary before _ftol retains the low 32 bits.
            const long denominator = 1L << 28;
            long factor = source.Level * (long)(0.05f * denominator) +
                (source.Side == BattleSide.Ally ? denominator : (long)(0.1f * denominator));
            long integralPower = raw * (factor / denominator) + raw * (factor % denominator) / denominator;
            int power = random.Warp(BattleBasicRules.SignedAbs(unchecked((int)integralPower)), 0.1f);
            if (raw < 0)
                power = -power;

            GetElements(elementMask, out int first, out int second);
            if (source.Side == BattleSide.Ally)
            {
                int position = (int)source.Slot + 1;
                int divisor = _isPal3A ? 4 : 2;
                if (Matches(first, position) || Matches(second, position))
                    power += power / divisor;
                if (Matches(GreaterElement(first), position))
                    power -= power / divisor;
                if (Matches(GreaterElement(second), position))
                    power -= power / divisor;
            }
            // PAL3A Magic Formula (0x4EAB42) queries enhancement at 0x504471
            // and the current weather element at 0x504440 after position adjustment.
            if (_isPal3A)
            {
                if (HasElement(enhancementMask, first) || HasElement(enhancementMask, second))
                    power += BattleBasicRules.Round(power * 0.3f);
                if (Matches(first, weatherElement))
                    power += power / -2;
                if (Matches(second, weatherElement))
                    power += power / -2;
            }
            return power;
        }

        public int TechniquePower(int raw, BattleParticipant source, BattleParticipant target, bool multi,
            int abilityId, int primaryActorLevel, BattleRandom random, out BattleValueMode mode)
        {
            RequireParticipants(source, target, random);
            mode = !multi && target.Side == BattleSide.Ally ? BattleValueMode.Percentage : BattleValueMode.Absolute;
            if (raw == 0)
                return 0;
            int power = target.Side == BattleSide.Enemy ? random.Warp(raw, 0.1f) : raw;
            // The discarded original power still consumes its Warp calls before this ID override.
            if (abilityId == (_isPal3A ? 4106 : 4102))
                power = 40 + 2 * primaryActorLevel;
            return power;
        }

        public int TechniqueSuccessPercent(int sourceLuck, int targetLuck, int equipmentBonus, BattleRandom random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            int result = 80 + (sourceLuck - targetLuck) * 10;
            result += BattleBasicRules.Round(Percent(result, equipmentBonus));
            return random.Warp(result, 0.2f);
        }

        public int PhysicalPower(BattleParticipant source, BattleParticipant target,
            BattleActorDefinition sourceDefinition, BattleActorDefinition targetDefinition,
            BattleTargetRange range, int damageDivisor, bool madness, BattleRandom random)
        {
            RequireParticipants(source, target, random);
            if (damageDivisor < 1)
                throw new ArgumentOutOfRangeException(nameof(damageDivisor));
            // The native expression spills to float immediately before f2i.
            float raw = (float)((1.0 + source.Level * (double)0.005f) * (source.Attributes[3] - target.Attributes[4]));
            int power = random.Warp(-BattleBasicRules.Round(raw), 0.1f);
            if (range is BattleTargetRange.OpponentRow or BattleTargetRange.OpponentColumn)
                power = power * (random.Warp(100, 0.05f) - 45) / 100;

            int elementDifference = PhysicalElementDifference(sourceDefinition, targetDefinition);
            power = BattleBasicRules.Round((float)(power * (1.0 - elementDifference * (double)0.05f)));
            if (source.Side == BattleSide.Ally && damageDivisor > 1)
                power /= damageDivisor;
            if (madness)
                power *= 2;
            return power >= 0 ? 1 + random.Next() % 5 : power;
        }

        public int CriticalPower(int signedPower, int equipmentBonus)
        {
            // PAL3A adds round(power * .5f); PAL3 multiplies by 1.3f.
            int power = _isPal3A ? signedPower + BattleBasicRules.Round(signedPower * 0.5f) : BattleBasicRules.Round(signedPower * 1.3f);
            return power + BattleBasicRules.Round(power * (float)equipmentBonus / 100);
        }

        public static int ReduceResourceLoss(int signedDelta, int savingPercentage)
            => signedDelta < 0 ? signedDelta - BattleBasicRules.Round(Percent(signedDelta, savingPercentage)) : signedDelta;

        // ProcFleeWait in both released executables writes BYTE(20 + 8 * speed).
        // The older PAL3 source computes a different value through CallDefaultSpeed.
        public int FleeGaugePerStep(int effectiveSpeed) => unchecked((byte)(20 + 8 * effectiveSpeed));

        public int FleeSuccessPercent(int level, int luck, IReadOnlyList<int> opponentLevels, IReadOnlyList<int> opponentLuck)
        {
            if (opponentLevels == null || opponentLuck == null)
                throw new ArgumentNullException(opponentLevels == null ? nameof(opponentLevels) : nameof(opponentLuck));
            if (opponentLevels.Count == 0 || opponentLevels.Count != opponentLuck.Count)
                throw new ArgumentException("Flee requires matching nonempty opponent attribute lists.");
            int totalLevel = 0;
            int lowestLuck = 0xffffff;
            for (int index = 0; index < opponentLevels.Count; index++)
            {
                totalLevel = unchecked(totalLevel + opponentLevels[index]);
                lowestLuck = Math.Min(lowestLuck, opponentLuck[index]);
            }
            int averageLevel = totalLevel / opponentLevels.Count;
            return unchecked(30 + (level - averageLevel) * (luck - lowestLuck) * 10);
        }

        // Original element order is water, fire, wind, thunder, earth.
        public static int GreaterElement(int element) => element switch
        {
            1 => 5,
            2 => 1,
            3 => 4,
            4 => 2,
            5 => 3,
            _ => 0,
        };

        public static int PunyElement(int element) => element switch
        {
            1 => 2,
            2 => 4,
            3 => 5,
            4 => 3,
            5 => 1,
            _ => 0,
        };

        public static int PhysicalElementDifference(BattleActorDefinition source, BattleActorDefinition target)
        {
            if (source == null || target == null)
                return 0;
            bool targetHasElement = false;
            int attackElement = 0;
            for (int element = 1; element <= 5; element++)
            {
                targetHasElement |= PackedElement(target, element) != 0;
                if (attackElement == 0 && ValueAt(source.PhysicalAttackElementValues, element) != 0)
                    attackElement = element;
            }
            if (!targetHasElement || attackElement == 0)
                return 0;
            // Both native EXEs preserve the logical-AND bug: compare whole packed DWORDs to zero.
            return (PackedElement(target, GreaterElement(attackElement)) != 0 ? 1 : 0)
                - (PackedElement(target, PunyElement(attackElement)) != 0 ? 1 : 0);
        }

        private static uint PackedElement(BattleActorDefinition actor, int element)
            => (uint)ValueAt(actor.PhysicalAttackElementValues, element) << 16 | ValueAt(actor.PhysicalDefenseElementValues, element);

        private static ushort ValueAt(IReadOnlyList<int> values, int element)
            => values != null && element >= 1 && element <= values.Count ? unchecked((ushort)values[element - 1]) : (ushort)0;

        private static float Percent(int value, int percent) => (float)(value * (double)percent * (double)0.01f);

        private static bool HasElement(int mask, int element) => element > 0 && element <= 5 && (mask & (1 << (element - 1))) != 0;

        private static bool Matches(int element, int position) => element > 0 && element <= 5 && element == position;

        private static void GetElements(int mask, out int first, out int second)
        {
            first = second = 0;
            for (int element = 1; element <= 5; element++)
            {
                if (!HasElement(mask, element))
                    continue;
                if (first == 0)
                    first = element;
                else
                    second = element;
            }
        }

        private static void RequireParticipants(BattleParticipant source, BattleParticipant target, BattleRandom random)
        {
            if (source == null || target == null || random == null)
                throw new ArgumentNullException(source == null ? nameof(source) : target == null ? nameof(target) : nameof(random));
        }
    }
}
