// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    // Preserve the full learned-skill/save layout without exposing a state writer.
    public sealed class BattleSkillSnapshot
    {
        public int Id
        {
            get;
        }

        public int Level
        {
            get;
        }

        public int Uses
        {
            get;
        }

        public bool Mastered
        {
            get;
        }

        public int NativeMagicGroup
        {
            get;
        }

        public int NativeMagicSlot
        {
            get;
        }

        public BattleSkillSnapshot(BattleSkill skill)
        {
            if (skill == null)
                throw new ArgumentNullException(nameof(skill));
            Id = skill.Id;
            Level = skill.Level;
            Uses = skill.Uses;
            Mastered = skill.Mastered;
            NativeMagicGroup = skill.NativeMagicGroup;
            NativeMagicSlot = skill.NativeMagicSlot;
        }
    }
}
