// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public sealed class BattleResult
    {
        public int SettlementEventStart
        {
            get; internal set;
        }

        public IReadOnlyList<BattleActorSnapshot> PreSettlementActors
        {
            get; internal set;
        }

        public bool GameOver
        {
            get; internal set;
        }

        public BattleSessionStatus Status
        {
            get; internal set;
        }

        public BattleSide? Winner
        {
            get; internal set;
        }

        public string Reason
        {
            get; internal set;
        }

        public long Tick
        {
            get; internal set;
        }

        public IReadOnlyList<BattleActorSnapshot> Actors
        {
            get; internal set;
        }

        public IReadOnlyList<BattleItemSnapshot> TreasureUses
        {
            get; internal set;
        }

        public IReadOnlyList<BattleStealPoolSnapshot> StealPools
        {
            get; internal set;
        }

        public IReadOnlyDictionary<string, BattleMagicSwordSnapshot> MagicSwords
        {
            get; internal set;
        }

        public int MoneyBefore
        {
            get; internal set;
        }

        public int MoneyAfter
        {
            get; internal set;
        }

        public BattlePal3AStateSnapshot Pal3A
        {
            get; internal set;
        }

        public bool Escaped
        {
            get; internal set;
        }

        public IReadOnlyList<BattleActorReward> Rewards
        {
            get; internal set;
        }

        public IReadOnlyList<BattleItemSnapshot> Items
        {
            get; internal set;
        }

        public IReadOnlyList<BattleItemSnapshot> Loot
        {
            get; internal set;
        }
    }
}
