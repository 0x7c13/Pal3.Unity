// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed partial class BattleSession
    {
        private readonly List<(long Id, string Actor, BattleMovementPoint Position, long Tick)> _hitFlashes = new();
        private long _nextHitFlash;

        public IReadOnlyList<BattleHitFlashSnapshot> HitFlashes => Array.AsReadOnly(_hitFlashes
            .Select(flash => new BattleHitFlashSnapshot(flash.Id, flash.Actor, flash.Position,
                BattleHitFlashSnapshot.FrameAt(SimulationTick - flash.Tick)))
            .Where(flash => flash.Frame is >= 0 and < BattleHitFlashSnapshot.FrameCount).ToArray());

        private void ShowOriginalHitFlash(Actor actor)
        {
            // ProcBlock emits on enemy roles even when Stable retains the old
            // animation. ProcDodge and party blocks never create this quad.
            if (actor.Initial.Side != BattleSide.Enemy)
                return;
            _hitFlashes.RemoveAll(flash => BattleHitFlashSnapshot.FrameAt(SimulationTick - flash.Tick) >= BattleHitFlashSnapshot.FrameCount);
            if (_hitFlashes.Count >= 2047)
                return; // CEffect::CreateOnePartical reserves the final quad slot.
            _hitFlashes.Add((++_nextHitFlash, actor.Initial.InstanceId,
                new BattleMovementPoint(actor.Position.X, 20, actor.Position.Z), SimulationTick));
        }
    }
}
