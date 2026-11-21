// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public enum BattleInputKind
    {
        Command, ControlChange, CancelAction, Abort, Fault,
        BeginFormation, SwapFormation, EndFormation, SetProtection,
        AiNormal, AiRepeat, AiProfile, AiAutoOnly, CoAttackPresentationSkip, AiControl, ElementWheel, ModalPresentationSkip, MagicMenuFieldDenial
    }

    // Every external input, including a controller change, is recorded at the exact logical boundary.
    [Serializable]
    public sealed class BattleInput
    {
        public long Tick;
        public string Actor;
        public string Target;
        public BattleAction Action;
        public int AbilityId;
        public int AiFlags;
        public BattleInputKind Kind;
        public BattleSlot FormationSlot;
        public BattleSide Side;
        public bool Automatic;
        public bool FromTestAi;
        public string Reason;

        internal bool Matches(BattleInput other) => other != null &&
            Tick == other.Tick && Kind == other.Kind && Actor == other.Actor && Target == other.Target &&
            Action == other.Action && AbilityId == other.AbilityId && AiFlags == other.AiFlags && FormationSlot == other.FormationSlot &&
            Side == other.Side && Automatic == other.Automatic && FromTestAi == other.FromTestAi && Reason == other.Reason;

        internal BattleInput Copy() => (BattleInput)MemberwiseClone();
    }
}
