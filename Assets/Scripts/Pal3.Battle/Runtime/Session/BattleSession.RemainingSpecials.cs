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
        private readonly Dictionary<string, int> _absorbHpWormRounds = new(StringComparer.Ordinal);

        public int? AbsorbHpWormRound(string actor) => actor != null && _absorbHpWormRounds.TryGetValue(actor, out int round) ? round : null;

        private void RandomizeOriginalFormation(Actor source, long actionId)
        {
            Actor[] allies = _actors.Where(actor => actor.Initial.Side == BattleSide.Ally).OrderBy(NativeActorIndex).ToArray();
            // Valid roles include dead and fled participants. The published gate
            // tests scene position and stage tasks, without a living-role filter.
            if (allies.Any(actor => !IsAtHome(actor) || _stage.CountOwner(actor.Initial.InstanceId) > 0))
                return;
            var geometry = (IBattleStageGeometry)_catalog;
            string controller = _formationController;
            _formationController = null;
            _formationInitialSlots = null;
            if (controller != null)
                Add(BattleEventKind.FormationClosed, controller, actionId: actionId, detail: "random formation forced control release; no gauge cost");
            IReadOnlyList<BattleSlot> lineup = BattleSpecialRules.RandomFormation(_random);
            foreach (Actor actor in allies)
            {
                BattleSlot before = actor.Slot;
                actor.Slot = lineup[NativeActorIndex(actor)];
                actor.Home = geometry.FormationHome(actor.Initial.InstanceId, actor.Slot);
                SetActorPosition(actor, actor.Home);
                Add(BattleEventKind.FormationChanged, source.Initial.InstanceId, actor.Initial.InstanceId, actionId,
                    before + " -> " + actor.Slot + "; original random lineup changed position/home immediately");
            }
        }

        private void ApplyOriginalAbsorbHpWorm(Actor target) => _absorbHpWormRounds[target.Initial.InstanceId] = 0;

        private void ClearOriginalAbsorbHpWorm(Actor target) => _absorbHpWormRounds.Remove(target.Initial.InstanceId);

        private void AdvanceOriginalAbsorbHpWormOnRound(Actor target)
        {
            if (_scenario.Variant != "PAL3" || !_absorbHpWormRounds.TryGetValue(target.Initial.InstanceId, out int before))
                return;
            int round = before + 1;
            if (round >= 4)
                ClearOriginalAbsorbHpWorm(target);
            else
                _absorbHpWormRounds[target.Initial.InstanceId] = round;
            int request = BattleSpecialRules.AbsorbHpWormRequest(target.Attributes[0], round);
            Actor recipient = ActorAtNativeIndex(3);
            ChangeHp(target, request, recipient, 0, "original absorb HP worm round " + round);
            // Native always asks identity 3 for the full opposite request,
            // independently of the actual target loss or the casting identity.
            if (recipient != null)
                ChangeHp(recipient, -request, null, 0, "original absorb HP worm recovery");
        }
    }
}
