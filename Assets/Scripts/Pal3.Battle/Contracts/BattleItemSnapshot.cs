// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    // Inventory results carry values, never the mutable scenario authoring object.
    public sealed class BattleItemSnapshot
    {
        public int Id
        {
            get;
        }

        public int Count
        {
            get;
        }

        public BattleItemSnapshot(int id, int count)
        {
            Id = id;
            Count = count;
        }

        public BattleItemSnapshot(BattleItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            Id = item.Id;
            Count = item.Count;
        }
    }
}
