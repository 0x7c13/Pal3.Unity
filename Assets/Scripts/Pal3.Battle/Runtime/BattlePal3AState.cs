// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // Core element order is water, fire, wind, thunder, earth. The native wheel
    // UI swaps wind/thunder; that presentation detail never changes this contract.
    [Serializable]
    public sealed class BattleSpiritWheel
    {
        public int[] Charges = new int[5];

        public BattleSpiritWheel Copy() => new() { Charges = Charges?.ToArray() };

        public BattleSpiritWheelSnapshot Capture() => new(Charges);
    }

    [Serializable]
    public sealed class BattlePal3AState
    {
        public BattleSpiritWheel SpiritWheel = new();
        public int SpiritVisualSeed = 1;
        public bool NangongStartsTransformed;
        public bool PengxuStartsTransformed;
        public bool PreserveLiaoRiLosses;
        public int HpLoss;
        public int MpLoss;

        public BattlePal3AState Copy() => new()
        {
            SpiritWheel = SpiritWheel?.Copy(),
            SpiritVisualSeed = SpiritVisualSeed,
            NangongStartsTransformed = NangongStartsTransformed,
            PengxuStartsTransformed = PengxuStartsTransformed,
            PreserveLiaoRiLosses = PreserveLiaoRiLosses,
            HpLoss = HpLoss,
            MpLoss = MpLoss
        };

        public void Validate()
        {
            if (SpiritWheel == null)
                throw new ArgumentException("PAL3A needs an explicit spirit wheel snapshot.");
            _ = SpiritWheel.Capture();
            if (PreserveLiaoRiLosses && (HpLoss < 0 || MpLoss < 0))
                throw new ArgumentException("Captured PAL3A HP and MP losses must be nonnegative signed int32 values.");
        }
    }

    public sealed class BattleSpiritWheelSnapshot
    {
        public IReadOnlyList<int> Charges
        {
            get;
        }

        public int Tier => Charges.Min() / 3;

        public int NangongRounds => Tier * 3;

        public BattleSpiritWheelSnapshot(IEnumerable<int> charges)
        {
            int[] values = charges?.ToArray();
            if (values is not { Length: 5 } || values.Any(value => value < 0 || value > 9))
                throw new ArgumentException("The original spirit wheel has five int32 charges between zero and nine.", nameof(charges));
            Charges = Array.AsReadOnly(values);
        }
    }

    public enum BattlePal3AFormKind
    {
        Human,
        NangongFireBeast,
        PengxuFairyBeast,
    }

    public sealed class BattlePal3AFormSnapshot
    {
        public BattlePal3AFormKind Kind
        {
            get;
        }

        public int Generation
        {
            get;
        }

        public int Rounds
        {
            get;
        }

        public int RoundLimit
        {
            get;
        }

        public bool Pending
        {
            get;
        }

        public bool LethalReturnPending
        {
            get;
        }

        public string Model => Kind switch
        {
            BattlePal3AFormKind.NangongFireBeast => "107",
            BattlePal3AFormKind.PengxuFairyBeast => "106",
            _ => ""
        };

        internal BattlePal3AFormSnapshot(BattlePal3AFormKind kind, int generation, int rounds, int roundLimit,
            bool pending, bool lethalReturnPending)
        {
            Kind = kind;
            Generation = generation;
            Rounds = rounds;
            RoundLimit = roundLimit;
            Pending = pending;
            LethalReturnPending = lethalReturnPending;
        }
    }

    public sealed class BattlePal3AStateSnapshot
    {
        public BattleSpiritWheelSnapshot SpiritWheel
        {
            get;
        }

        public uint SpiritVisualRandomState
        {
            get;
        }

        public bool PreserveLiaoRiLosses
        {
            get;
        }

        public int HpLoss
        {
            get;
        }

        public int MpLoss
        {
            get;
        }

        internal BattlePal3AStateSnapshot(BattleSpiritWheelSnapshot spiritWheel, bool preserveLiaoRiLosses, int hpLoss, int mpLoss, uint spiritVisualRandomState)
        {
            SpiritWheel = spiritWheel;
            SpiritVisualRandomState = spiritVisualRandomState;
            PreserveLiaoRiLosses = preserveLiaoRiLosses;
            HpLoss = hpLoss;
            MpLoss = mpLoss;
        }
    }

    // Captured immutable resource profiles are selected by the adapter at the
    // native model swap, while actor identity, native slot and AI remain intact.
    public interface IBattlePal3AFormCatalog
    {
        BattlePhysicalAnimation FormChangeAnimation(string actorId, bool transformed);

        void ActivatePal3AForm(string actorId, BattlePal3AFormKind kind);
    }
}
