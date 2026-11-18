// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    public sealed partial class BattleSession
    {
        private void ChangeLove(Actor actor, int requested)
        {
            int before = actor.Love;
            BattleRelationChange change = BattleRelationRules.Change(_scenario.Variant, NativeActorIndex(actor), before, requested,
                ActorAtNativeIndex(0) != null);
            if (!change.Applied)
                return;
            actor.Love = change.Value;
            if (change.Sound != null)
                PlaySound(change.Sound);
            _events.Add(new BattleEvent
            {
                Tick = Tick,
                Kind = BattleEventKind.RelationChanged,
                Target = actor.Initial.InstanceId,
                RequestedDelta = requested,
                ActualDelta = unchecked(change.Value - before),
                ValueBefore = before,
                ValueAfter = change.Value,
                Detail = change.Sound
            });
        }
    }
}
