// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    public sealed partial class BattleSession
    {
        // MainPanel 0x220 is a separate released command, not ordinary magic.
        // Its GDB record still supplies the element and effect linker.
        public string CanSubmitElementWheel(string controller, int skillId) =>
            ValidateCommand(new BattleCommand(controller, BattleAction.Skill, null, skillId), false, true);

        public bool SubmitElementWheel(string controller, int skillId) =>
            SubmitCommand(new BattleCommand(controller, BattleAction.Skill, null, skillId), false, true);

        private string ValidateElementWheelController(Actor actor, BattleCommand command, BattleAbilityDefinition ability)
        {
            if (Definitions?.Variant != "PAL3A" || command.Action != BattleAction.Skill || command.Target != null ||
                command.AbilityId is < 264 or > 268 || ability == null || ability.Range != BattleTargetRange.None)
                return "Choose a PAL3A five-spirit wheel element.";
            if (actor.Initial.Side != BattleSide.Ally || NativeActorIndex(actor) != 0 || WaitingActor != command.Actor)
                return "The spirit wheel needs Nangong Huang as the current ready controller.";
            if (_pal3AForms.TryGetValue(command.Actor, out Pal3AFormState form) && form.Pending)
                return "The original form transition is still running.";
            if (_spiritWheel.Charges[ElementWheelCoreElement(command.AbilityId)] != 9)
                return "This spirit wheel element needs nine charges.";
            if (_catalog is IBattleAbilityEffects && _catalog is not IBattleElementWheelTimings)
                return "The original spirit wheel presentation is missing.";
            return null;
        }

        private static int ElementWheelCoreElement(int skillId) => skillId switch
        {
            264 => 0,
            265 => 1,
            266 => 3,
            267 => 2,
            268 => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(skillId))
        };

        private AttackTiming ElementWheelTiming(string actor, BattleAbilityDefinition ability) =>
            _catalog is IBattleElementWheelTimings wheel ? wheel.ElementWheel(actor, ability) :
                (_catalog as IBattleAbilityTimings)?.Ability(actor, ability);

        private bool ResolveElementWheelMarker(Actor source, ActionRun run)
        {
            if (!run.ElementWheel)
                return false;
            if (!run.AbilityResolved)
            {
                run.AbilityResolved = true;
                ResolveAiSceneMagic(source, run.Ability);
                int element = ElementWheelCoreElement(run.Ability.Id);
                _spiritWheel = BattlePal3ARules.Gain(_spiritWheel, element, -9);
                Add(BattleEventKind.ControlChanged, source.Initial.InstanceId, actionId: run.Id,
                    detail: "spirit wheel element " + element + " consumed");
            }
            return true;
        }
    }
}
