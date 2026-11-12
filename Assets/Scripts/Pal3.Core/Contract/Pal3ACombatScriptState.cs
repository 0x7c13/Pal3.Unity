// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Core.Contract
{
    using System;
    using Enums;

    // Original 176/177 are persistent abilities; 192/196 are next-battle flags.
    public sealed class Pal3ACombatScriptState
    {
        public bool NanGongHuangAbilitiesEnabled { get; set; }

        public bool WangPengXuTransformEnabled { get; set; }

        public bool NextNanGongHuangTransform { get; set; }

        public bool NextWangPengXuTransform { get; set; }

        public bool PreserveLosses { get; set; }

        public int LostHp { get; private set; }

        public int LostMp { get; private set; }

        public int Flags => (NanGongHuangAbilitiesEnabled ? 1 : 0) | (WangPengXuTransformEnabled ? 2 : 0) |
            (NextNanGongHuangTransform ? 4 : 0) | (NextWangPengXuTransform ? 8 : 0) | (PreserveLosses ? 16 : 0);
        public void Restore(int flags, int lostHp, int lostMp)
        {
            NanGongHuangAbilitiesEnabled = (flags & 1) != 0;
            WangPengXuTransformEnabled = (flags & 2) != 0;
            NextNanGongHuangTransform = (flags & 4) != 0;
            NextWangPengXuTransform = (flags & 8) != 0;
            PreserveLosses = (flags & 16) != 0;
            LostHp = Math.Max(0, lostHp);
            LostMp = Math.Max(0, lostMp);
        }

        public (bool wolf, bool pengXu, int hpBonus, int mpBonus) BeginBattle(bool hasLiaoRiSecondForm)
        {
            var result = (NextNanGongHuangTransform, NextWangPengXuTransform,
                PreserveLosses && hasLiaoRiSecondForm ? LostHp : 0,
                PreserveLosses && hasLiaoRiSecondForm ? LostMp : 0);
            NextNanGongHuangTransform = NextWangPengXuTransform = false;
            if (!PreserveLosses)
                LostHp = LostMp = 0;
            if (hasLiaoRiSecondForm)
                PreserveLosses = false;
            return result;
        }

        public void RecordAttributeChange(int playerActorId, ActorAttributeType attribute, int delta)
        {
            if (playerActorId < 0 || playerActorId >= 5 || delta >= 0)
                return;
            int loss = (int)Math.Min(int.MaxValue, -(long)delta);
            if (attribute == ActorAttributeType.Hp)
                LostHp = (int)Math.Min(int.MaxValue, (long)LostHp + loss);
            if (attribute == ActorAttributeType.Mp)
                LostMp = (int)Math.Min(int.MaxValue, (long)LostMp + loss);
        }

        public void Reset() => Restore(0, 0, 0);
    }
}
#endif
