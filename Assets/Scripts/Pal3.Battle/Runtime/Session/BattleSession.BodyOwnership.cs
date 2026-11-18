// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;

    public sealed partial class BattleSession
    {
        // IsIdle describes the role's task queue. Normal Grab separately reserves
        // that queue until Release, even after an ability's body has finished.
        private readonly Dictionary<string, long> _bodyStageOwners = new(StringComparer.Ordinal);

        private bool OwnsStageBody(Actor actor, BattleStageHandle handle) =>
            _bodyStageOwners.TryGetValue(actor.Initial.InstanceId, out long owner) && owner == handle.Id;

        private bool TryGrabStageBody(Actor actor, BattleStageHandle handle)
        {
            if (_bodyStageOwners.TryGetValue(actor.Initial.InstanceId, out long owner) && owner != handle.Id)
                return false;
            _bodyStageOwners[actor.Initial.InstanceId] = handle.Id;
            return true;
        }

        private void ReleaseStageBody(Actor actor, BattleStageHandle handle)
        {
            if (!OwnsStageBody(actor, handle))
                return;
            ClearPhysicalBody(actor);
            _bodyStageOwners.Remove(actor.Initial.InstanceId);
        }
    }
}
