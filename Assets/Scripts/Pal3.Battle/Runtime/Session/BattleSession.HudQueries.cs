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
        public bool HasActiveState(string actorId, BattleState state) => actorId != null && _byId.TryGetValue(actorId, out Actor actor) &&
            actor.States.TryGetValue(state, out BattleStatusInstance status) && status.LatentRounds == 0;

        public BattleTargetRange GetActionTargetRange(BattleCommand command)
        {
            if (command.Actor == null || !_byId.TryGetValue(command.Actor, out Actor source) ||
                command.Action is not (BattleAction.Attack or BattleAction.Skill or BattleAction.Item))
                return BattleTargetRange.None;
            BattleAbilityDefinition ability = Ability(command);
            if (command.Action is BattleAction.Skill or BattleAction.Item && ability == null)
                return BattleTargetRange.None;
            return ActionRange(source, ability);
        }

        public string GetDefaultTarget(BattleCommand command)
        {
            var candidates = GetTargetCandidates(command);
            if (candidates.Count == 0)
                return null;
            BattleTargetRange range = GetActionTargetRange(command);
            if (range is BattleTargetRange.OwnSingle or BattleTargetRange.OwnAll)
            {
                BattleAbilityDefinition ability = Ability(command);
                if (ability != null)
                {
                    var cure = candidates.FirstOrDefault(actor => ability.States.Any(effect => effect.Remove && AiBadState(effect.State) &&
                        (effect.State == BattleState.Death ? !actor.Alive : Has(_byId[actor.Id], effect.State))));
                    if (cure != null)
                        return cure.Id;
                    if (ability.IsItem && ability.ItemType == 1 && RecoversResource(ability, 2) && !RecoversResource(ability, 0))
                    {
                        string lowestMp = LowestResourceTarget(candidates, mp: true);
                        if (lowestMp != null)
                            return lowestMp;
                    }
                }
            }
            else if (candidates[0].Side == BattleSide.Enemy)
            {
                int preferred = -1;
                if (range == BattleTargetRange.OpponentSingle)
                    preferred = FirstEnemyToReachCommand();
                else if (range is BattleTargetRange.OpponentRow or BattleTargetRange.OpponentColumn)
                    preferred = BattleTargetRules.BestEnemyLine(range == BattleTargetRange.OpponentRow, index =>
                    {
                        Actor actor = ActorAtNativeIndex(index);
                        return actor != null && actor.Hp > 0 && !actor.Escaped && !IsFleeing(actor) &&
                            (_scenario.Variant == "PAL3A" || !Has(actor, BattleState.Invisible));
                    });
                var selected = candidates.FirstOrDefault(actor => actor.NativeIndex == preferred);
                if (selected != null)
                    return selected.Id;
            }
            return LowestResourceTarget(candidates, mp: false) ?? candidates[0].Id;
        }

        private static bool RecoversResource(BattleAbilityDefinition ability, int resource) => ability.Attributes.Any(effect =>
            effect.Attribute == resource && (effect.Mode == BattleValueMode.RecoverToMax ||
                effect.Mode is BattleValueMode.Absolute or BattleValueMode.Percentage && effect.Value > 0));

        private static string LowestResourceTarget(IReadOnlyList<BattleActorSnapshot> candidates, bool mp)
        {
            float lowest = 1f;
            string best = null;
            foreach (BattleActorSnapshot actor in candidates)
            {
                if (!actor.Alive)
                    continue;
                // The original compares its x87 division to the previous single-precision minimum.
                double ratio = mp ? actor.Mp / (double)actor.MaxMp : actor.Hp / (double)actor.MaxHp;
                if (!(mp ? ratio >= lowest : ratio > lowest))
                {
                    lowest = (float)ratio;
                    best = actor.Id;
                }
            }
            return best;
        }

        private int FirstEnemyToReachCommand()
        {
            float shortest = 100000000f;
            int best = -1;
            foreach (Actor actor in _actors.Where(actor => actor.Initial.Side == BattleSide.Enemy).OrderBy(NativeActorIndex))
            {
                if (actor.Hp <= 0 || actor.Escaped || IsFleeing(actor))
                    continue;
                bool casting = actor.Run?.SystemStarted == true && CanInterruptCast(actor);
                int speed = casting ? actor.CastSpeed : _rules.GaugePerStep(RuleParticipant(actor));
                int firstGauge = actor.Run is { SystemStarted: false } ? FullGauge : actor.Gauge;
                double time = casting ? (FullCastGauge - actor.CastGauge) / (50.0 * speed) :
                    (FullGauge - firstGauge) / (50.0 * speed) + 10000.0;
                if (!(time >= shortest))
                {
                    shortest = (float)time;
                    best = NativeActorIndex(actor);
                }
            }
            return best;
        }

        // This is a potential area preview. Protection, random hit routing, and
        // original AI repetition are resolved only when the command executes.
        public IReadOnlyList<BattleActorSnapshot> GetPotentialAffectedTargets(BattleCommand command)
        {
            BattleTargetRange range = GetActionTargetRange(command);
            if (range == BattleTargetRange.None)
                return Array.Empty<BattleActorSnapshot>();
            IReadOnlyList<BattleActorSnapshot> candidates = GetTargetCandidates(command);
            if (range is BattleTargetRange.OwnAll or BattleTargetRange.OpponentAll)
                return candidates;
            BattleActorSnapshot anchor = candidates.FirstOrDefault(actor => actor.Id == command.Target);
            if (anchor == null)
                return Array.Empty<BattleActorSnapshot>();
            if (range is BattleTargetRange.OpponentRow or BattleTargetRange.OpponentColumn)
                return Array.AsReadOnly(candidates.Where(actor => BattleTargetRules.Aligned(anchor.Side,
                    anchor.Slot, actor.Slot, range == BattleTargetRange.OpponentRow, _scenario.SceneElement)).ToArray());
            return Array.AsReadOnly(new[] { anchor });
        }
    }
}
