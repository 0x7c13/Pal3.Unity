// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // UIStaticCB's reference count belongs to presentation. A native Trail
    // suppresses Name/Flee/ambush callers permanently, while items keep showing.
    // This projection cannot shorten a domain wait or consume a random number.
    public sealed class BattleStageMessageProjection
    {
        private readonly HashSet<long> _seen = new();
        private readonly Dictionary<long, BattleStageMessageSnapshot> _visible = new();
        private readonly BattleStageMessageSnapshot[] _last = new BattleStageMessageSnapshot[2];

        public IReadOnlyList<BattleStageMessageSnapshot> Messages => Array.AsReadOnly(_last
            .Where(message => message != null && _visible.Values.Any(reference => reference.Side == message.Side)).ToArray());

        public int VisibleReferenceCount(BattleSide side) => _visible.Values.Count(message => message.Side == side);

        public void Synchronize(IReadOnlyList<BattleStageMessageSnapshot> references, bool trail)
        {
            if (references == null)
                throw new ArgumentNullException(nameof(references));
            var active = new HashSet<long>(references.Select(message => message.ReferenceId));
            _seen.RemoveWhere(reference => !active.Contains(reference));
            foreach (long reference in _visible.Keys.ToArray())
                if (!active.Contains(reference) || trail && !_visible[reference].AllowTrail)
                    _visible.Remove(reference);
            foreach (BattleStageMessageSnapshot message in references)
            {
                if (!_seen.Add(message.ReferenceId) || trail && !message.AllowTrail)
                    continue;
                _visible.Add(message.ReferenceId, message);
                _last[(int)message.Side] = message;
            }
        }

        public void Clear()
        {
            _seen.Clear();
            _visible.Clear();
            Array.Clear(_last, 0, _last.Length);
        }
    }
}
