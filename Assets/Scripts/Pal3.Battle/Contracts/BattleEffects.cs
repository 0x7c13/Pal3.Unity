// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public enum BattleValueMode
    {
        Absolute, Percentage, RecoverToMax, IncreaseMax
    }

    public enum BattleTargetRange
    {
        None, OwnSingle, OwnAll, OpponentSingle, OpponentAll, OpponentRow, OpponentColumn
    }

    public enum BattleState
    {
        PoisonWind, PoisonThunder, PoisonWater, PoisonFire, PoisonEarth,
        AttackIncrease, AttackDecrease, DefenseIncrease, DefenseDecrease, LuckIncrease, LuckDecrease, SpeedIncrease, SpeedDecrease,
        Paralysis, Seal, Forbidden, Sleep, Chaos, Madness, Reflection, Evade, Barrier, Invisible, Dying, Death,
        PoisonResist, PoisonAny, EvilResist, DemonResist, GodsEye, ExpIncrease,
    }

    public readonly struct BattleAttributeEffect
    {
        public int Attribute
        {
            get;
        }

        public BattleValueMode Mode
        {
            get;
        }

        public int Value
        {
            get;
        }

        public BattleAttributeEffect(int attribute, BattleValueMode mode, int value)
        {
            if (attribute < 0 || attribute > 11 || !Enum.IsDefined(typeof(BattleValueMode), mode))
                throw new ArgumentException("Invalid attribute effect.");
            Attribute = attribute;
            Mode = mode;
            Value = value;
        }
    }

    public readonly struct BattleStatusEffect
    {
        public BattleState State
        {
            get;
        }

        public bool Remove
        {
            get;
        }

        public int Value
        {
            get;
        }

        public BattleStatusEffect(BattleState state, bool remove, int value = 0)
        {
            if (!Enum.IsDefined(typeof(BattleState), state))
                throw new ArgumentOutOfRangeException(nameof(state));
            State = state;
            Remove = remove;
            Value = value;
        }
    }

    public readonly struct BattleCost
    {
        // 0 HP, 1 SP, 2 MP, 10 money, 16 inventory Gu item 6113: original resource numbers are preserved at this boundary.
        public int Resource
        {
            get;
        }

        public BattleValueMode Mode
        {
            get;
        }

        public int Value
        {
            get;
        }

        public BattleCost(int resource, BattleValueMode mode, int value)
        {
            Resource = resource;
            Mode = mode;
            Value = value;
        }
    }

    public readonly struct BattleCostEstimate
    {
        public int Resource
        {
            get;
        }

        public int Required
        {
            get;
        }

        public int Payable
        {
            get;
        }

        public int Available
        {
            get;
        }

        public BattleCostEstimate(int resource, int required, int payable, int available)
        {
            Resource = resource;
            Required = required;
            Payable = payable;
            Available = available;
        }
    }
}
