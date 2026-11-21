// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    public sealed class BattleRandom
    {
        public string Variant
        {
            get;
        }

        public uint State
        {
            get; private set;
        }

        public BattleRandom(int seed, string variant = "PAL3")
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown combat variant.", nameof(variant));
            Variant = variant;
            State = unchecked((uint)seed);
        }

        public int Next()
        {
            State = unchecked(State * 214013 + 2531011);
            return (int)(State >> 16) & 0x7fff;
        }

        // PAL3 inlines every positive roll. PAL3A's shared helper returns
        // immediately for certain success; preserving the stream affects later actions.
        public bool Probability(int percent) => percent > 0 && (Variant == "PAL3A" && percent >= 100 ||
            Next() % 1024 < BattleBasicRules.Round(1024f / 100 * percent));

        public int Warp(int value, float fraction)
        {
            int width = BattleBasicRules.SignedAbs(BattleBasicRules.Round(value * fraction));
            if (width == 0)
                return value;
            int offset = Next() % width;
            return Next() % 1024 >= 512 ? value + offset : value - offset;
        }
    }

    public readonly struct PhysicalHit
    {
        public int Delta
        {
            get;
        }

        public bool Critical
        {
            get;
        }

        public bool Dodged
        {
            get;
        }

        public PhysicalHit(int delta, bool critical, bool dodged)
        {
            Delta = delta;
            Critical = critical;
            Dodged = dodged;
        }
    }

    public interface IBattleRules
    {
        int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random);

        int GaugePerStep(BattleParticipant actor);

        PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1);
    }

    // Verified common PAL3/PAL3A neutral-element physical rules. Equipment/abnormal states are stage 3.
    public sealed class BattleBasicRules : IBattleRules
    {
        // Native int32 abs keeps INT_MIN negative; widening it changes both
        // the signed capture comparison and Warp's division/RNG behavior.
        internal static int SignedAbs(int value) => value < 0 ? unchecked(-value) : value;

        public static int Round(float value)
        {
            // Native util::f2i uses x87 FISTP with masked invalid exceptions.
            // Its integer-indefinite result is INT_MIN for NaN, infinity and
            // finite values whose rounded result is outside signed int32.
            double rounded = Math.Round(value, MidpointRounding.ToEven);
            return rounded >= int.MinValue && rounded <= int.MaxValue ? (int)rounded : int.MinValue;
        }

        public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random)
        {
            if (encounter != BattleEncounter.Normal)
                return (encounter == BattleEncounter.AllyAmbush) == (actor.Side == BattleSide.Ally) ? BattleSession.FullGauge : 0;
            // The native expression divides two integers before multiplying by speed.
            return Math.Min(BattleSession.FullGauge, 638 * actor.Attributes[5] + random.Next() % 638);
        }

        public int GaugePerStep(BattleParticipant actor)
        {
            // Native storage is BYTE; int += float truncates at every update. Our reference update is 20ms.
            return unchecked((byte)DefaultGaugeSpeed(actor));
        }

        private static int DefaultGaugeSpeed(BattleParticipant actor) => 20 + 5 * actor.Attributes[5];

        // Published MagicWait multiplies CallDefaultSpeed's full int result
        // before clamping and storing the second-slot byte.
        internal int MagicCastGaugePerStep(BattleParticipant actor, int proficiency) =>
            Math.Min(byte.MaxValue, DefaultGaugeSpeed(actor) * proficiency * 2);

        public int NormalDamage(BattleParticipant source, BattleParticipant target, BattleRandom random, int damageDivisor = 1)
        {
            if (damageDivisor < 1)
                throw new ArgumentOutOfRangeException(nameof(damageDivisor));
            int delta = -Round((1 + source.Level / 200f) * (source.Attributes[3] - target.Attributes[4]));
            delta = random.Warp(delta, 0.1f);
            if (source.Side == BattleSide.Ally)
                delta /= damageDivisor;
            // Native fallback is positive, so AttackDamage treats it as a miss, not minimum HP damage.
            return delta >= 0 ? 1 + random.Next() % 5 : delta;
        }

        public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1)
        {
            int criticalPercent = (int)(Math.Sqrt(source.Attributes[6] / 2) * 10);
            bool critical = random.Probability(criticalPercent);
            if (!targetAtHome)
                return new PhysicalHit(0, critical, false);
            bool dodge = target.Side == BattleSide.Ally && random.Probability(Math.Max(0, (target.Attributes[5] - source.Attributes[5]) * 5));
            int delta = NormalDamage(source, target, random, damageDivisor);
            if (dodge || delta >= 0)
                return new PhysicalHit(0, critical, dodge);
            if (critical)
                delta = Round(delta * 1.3f);
            if (guarding && delta < -1)
                delta /= 2;
            return new PhysicalHit(delta, critical, false);
        }
    }
}
