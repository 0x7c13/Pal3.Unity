// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BattleStatusInstance
    {
        public BattleState State
        {
            get;
        }

        public int LatentRounds
        {
            get; internal set;
        }

        public int RemainingRounds
        {
            get; internal set;
        }

        public bool Permanent
        {
            get;
        }

        public float EarthDamage
        {
            get; internal set;
        }

        internal bool EarthDamageInitialized
        {
            get; set;
        }

        internal BattleStatusInstance(BattleState state, int latentRounds, int remainingRounds, bool permanent)
        {
            State = state;
            LatentRounds = latentRounds;
            RemainingRounds = remainingRounds;
            Permanent = permanent;
        }
    }

    // Native SetSpecState / HandleSpecStateBout. The session owns HP writes and cancellation.
    public sealed class BattleStatusRules
    {
        public static bool SupportsInitialState(BattleState state) => state is >= BattleState.PoisonWind and <= BattleState.PoisonResist;

        private readonly bool _isPal3A;

        public BattleStatusRules(string variant)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown combat variant.", nameof(variant));
            _isPal3A = variant == "PAL3A";
        }

        public BattleStatusInstance Create(BattleState state, int maxHp, int? durationOverride = null,
            bool permanent = false, BattleRandom random = null, bool isPlayer = true)
        {
            if (maxHp < 0)
                throw new ArgumentOutOfRangeException(nameof(maxHp));
            if (durationOverride < 1)
                throw new ArgumentOutOfRangeException(nameof(durationOverride));
            int latent = 0;
            int duration;
            switch (state)
            {
                case BattleState.PoisonWind:
                    latent = 3;
                    duration = 5;
                    break;
                case BattleState.PoisonThunder:
                    latent = 3;
                    duration = 2;
                    break;
                case BattleState.PoisonWater:
                case BattleState.AttackIncrease:
                case BattleState.AttackDecrease:
                case BattleState.DefenseIncrease:
                case BattleState.DefenseDecrease:
                case BattleState.LuckIncrease:
                case BattleState.LuckDecrease:
                case BattleState.SpeedIncrease:
                case BattleState.SpeedDecrease:
                case BattleState.Invisible:
                    duration = 5;
                    break;
                case BattleState.PoisonFire:
                case BattleState.PoisonEarth:
                case BattleState.Seal:
                case BattleState.Forbidden:
                case BattleState.Chaos:
                case BattleState.Madness:
                    duration = short.MaxValue;
                    break;
                case BattleState.Paralysis:
                case BattleState.Evade:
                case BattleState.Barrier:
                    duration = 4;
                    break;
                case BattleState.Sleep:
                    if (!durationOverride.HasValue && random == null)
                        throw new ArgumentException("Sleep duration requires the session random source.", nameof(random));
                    duration = durationOverride ?? random.Next() % 7 + 2;
                    break;
                case BattleState.Reflection:
                    duration = isPlayer ? 3 : 5;
                    break;
                case BattleState.Dying:
                case BattleState.Death:
                case BattleState.PoisonResist:
                    duration = short.MaxValue;
                    permanent = true;
                    break;
                default:
                    throw new NotSupportedException($"State {state} has no verified ordinary combat rule.");
            }
            return new BattleStatusInstance(state, latent, durationOverride ?? duration, permanent);
        }

        public int ModifyAttribute(int index, int value, IEnumerable<BattleState> states)
        {
            if (states == null)
                throw new ArgumentNullException(nameof(states));
            BattleState increase;
            BattleState decrease;
            switch (index)
            {
                case 3:
                    increase = BattleState.AttackIncrease;
                    decrease = BattleState.AttackDecrease;
                    break;
                case 4:
                    increase = BattleState.DefenseIncrease;
                    decrease = BattleState.DefenseDecrease;
                    break;
                case 5:
                    increase = BattleState.SpeedIncrease;
                    decrease = BattleState.SpeedDecrease;
                    break;
                case 6:
                    increase = BattleState.LuckIncrease;
                    decrease = BattleState.LuckDecrease;
                    break;
                default:
                    return value;
            }
            float fraction = _isPal3A && index is 3 or 4 ? 0.08f : 0.25f;
            var active = states as ICollection<BattleState> ?? states.ToArray();
            if (active.Contains(increase))
                return value + BattleBasicRules.Round(value * fraction);
            if (active.Contains(decrease))
                return value + BattleBasicRules.Round(value * -fraction);
            return value;
        }

        public int TickRound(BattleStatusInstance instance, int maxHp, int currentHp)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            if (maxHp < 0 || currentHp < 0)
                throw new ArgumentOutOfRangeException(nameof(maxHp));
            if (!instance.Permanent && instance.RemainingRounds <= 0)
                return 0;
            if (instance.LatentRounds > 0)
            {
                instance.LatentRounds--;
                return 0;
            }
            int delta = 0;
            switch (instance.State)
            {
                case BattleState.PoisonWind:
                    delta = maxHp * 3 / -4;
                    break;
                case BattleState.PoisonThunder:
                    delta = maxHp / -2;
                    break;
                case BattleState.PoisonWater:
                    delta = currentHp / -2;
                    break;
                case BattleState.PoisonFire:
                    delta = maxHp * 5 / -100;
                    break;
                case BattleState.PoisonEarth:
                    if (!instance.EarthDamageInitialized)
                    {
                        instance.EarthDamage = maxHp * -0.02f;
                        instance.EarthDamageInitialized = true;
                    }
                    delta = BattleBasicRules.Round(instance.EarthDamage);
                    instance.EarthDamage *= 1.5f;
                    break;
            }
            if (!instance.Permanent)
                instance.RemainingRounds = Math.Max(0, instance.RemainingRounds - 1);
            return delta;
        }

        public IEnumerable<BattleState> ReplacedBy(BattleState state)
        {
            if (state >= BattleState.Paralysis && state <= BattleState.Invisible)
            {
                for (var conflict = BattleState.Paralysis; conflict <= BattleState.Invisible; conflict++)
                    yield return conflict;
            }
            else if (state >= BattleState.AttackIncrease && state <= BattleState.SpeedDecrease)
            {
                int pair = ((int)state - (int)BattleState.AttackIncrease) / 2;
                yield return BattleState.AttackIncrease + pair * 2;
                yield return BattleState.AttackIncrease + pair * 2 + 1;
            }
            else
            {
                yield return state;
            }
        }

        public string CanAdd(BattleState state, IEnumerable<BattleState> existing, IEnumerable<BattleState> immune)
        {
            if (existing == null || immune == null)
                throw new ArgumentNullException(existing == null ? nameof(existing) : nameof(immune));
            var active = existing as ICollection<BattleState> ?? existing.ToArray();
            if (state != BattleState.Invisible && active.Contains(BattleState.Invisible))
                return "隐状态阻止其他状态变化。";
            if (IsPoison(state))
            {
                if (active.Any(IsPoison))
                    return "已有五毒，不能叠加或覆盖。";
                if (active.Contains(BattleState.PoisonResist) || immune.Contains(BattleState.PoisonResist))
                    return "辟毒免疫五毒。";
            }
            return immune.Contains(state) ? "目标免疫此状态。" : null;
        }

        private static bool IsPoison(BattleState state) => state >= BattleState.PoisonWind && state <= BattleState.PoisonEarth;
    }
}
