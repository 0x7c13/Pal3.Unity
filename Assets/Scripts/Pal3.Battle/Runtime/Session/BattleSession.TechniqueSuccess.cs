// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    public sealed partial class BattleSession
    {
        private bool TechniqueSucceeds(Actor source, Actor target, ActionRun run)
        {
            BattleAbilityDefinition ability = run.Ability;
            if (ability.IsItem || ability.IsMagic)
                return true;
            bool rejectedBoss = target.Initial.Side == BattleSide.Enemy && target.Definition.Boss &&
                BattleSpecialRules.RejectsBossBeforeSuccessRoll(Definitions.Variant, ability.Id);
            if (!rejectedBoss)
            {
                if (ability.SuccessRate == 100)
                    return true;
                int chance = BattleSpecialRules.UsesLiteralSuccessRate(Definitions.Variant, ability.Id, ability.SuccessRate)
                    ? ability.SuccessRate
                    : _effectRules.TechniqueSuccessPercent(EffectiveAttributes(source)[6], EffectiveAttributes(target)[6],
                        EquipmentBonus(source, item => item.TechniqueSuccessBonus), _random);
                if (_random.Probability(chance))
                    return true;
            }
            Add(BattleEventKind.Miss, source.Initial.InstanceId, target.Initial.InstanceId, run.Id,
                rejectedBoss ? "original boss restriction rejected the technique before its success roll" : "technique success roll failed");
            QueueNumber(target, 0, 0, 1f, false, miss: true);
            return false;
        }
    }
}
