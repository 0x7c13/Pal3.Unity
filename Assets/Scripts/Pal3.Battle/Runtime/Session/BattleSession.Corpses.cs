// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System.Linq;

    public sealed partial class BattleSession
    {
        private void TryObtainOriginalCorpse(Actor source, Actor target, ActionRun run)
        {
            BattleAbilityDefinition ability = run.Ability;
            BattleActorDefinition definition = target.Definition;
            if (ability.IsItem || NativeActorIndex(target) is < 5 or >= 11 || target.Hp > 0 ||
                definition.CorpseItem == 0 || definition.CorpseSkill != ability.Id ||
                _actors.Any(actor => NativeActorIndex(actor) < 5 && actor.Slot == BattleSlot.Center))
                return;
            // The lineup test considers every valid ally, including dead or
            // fled allies. The roll occurs here, before payment and proficiency.
            if (!_random.Probability(definition.CorpsePercent))
                return;
            int before = target.CorpseItem;
            target.CorpseItem = definition.CorpseItem;
            _events.Add(new BattleEvent
            {
                Tick = Tick,
                Kind = BattleEventKind.CorpseObtained,
                Source = source.Initial.InstanceId,
                Target = target.Initial.InstanceId,
                ActionId = run.Id,
                AbilityId = definition.CorpseItem,
                ValueBefore = before,
                ValueAfter = target.CorpseItem,
                RandomState = _random.State,
                Detail = "original corpse recorded on the native enemy slot"
            });
        }
    }
}
