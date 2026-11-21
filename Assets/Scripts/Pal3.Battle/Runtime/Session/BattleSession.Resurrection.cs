// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    public sealed partial class BattleSession
    {
        private void QueuePhysicalRevivalReturn(Actor actor)
        {
            if (_catalog is not IBattlePhysicalTimings || IsAtHome(actor))
                return;
            var stage = new PhysicalStageRun
            {
                Session = this,
                Source = actor,
                Profile = PhysicalBody(actor).Profile,
                RevivalReturn = true
            };
            if (_stage.TryAdd(actor.Initial.InstanceId, "revival return", stage, BattleStageScheduler.TimeAt(SimulationTick), out BattleStageHandle handle))
                stage.Handle = handle;
        }

        // CBST_GotoInitPos differs from the ordinary attack's return routing:
        // PAL3 Zixuan flies without rotation, and PAL3A ignores weapon/form.
        private BattlePhysicalBodyKind RevivalReturnKind(Actor actor) => NativeActorIndex(actor) switch
        {
            2 => BattlePhysicalBodyKind.Teleport,
            1 or 3 when _scenario.Variant == "PAL3" => BattlePhysicalBodyKind.Fly,
            _ => BattlePhysicalBodyKind.Run
        };

        private bool AdvanceRevivalReturn(PhysicalStageRun stage, int index)
        {
            if (index != 1)
                return true;
            if (!stage.Executed[index] && PhysicalBodyIdle(stage.Source))
            {
                QueuePhysicalReturn(stage, new[] { RevivalReturnKind(stage.Source), BattlePhysicalBodyKind.Turn });
                stage.Executed[index] = true;
            }
            // The native return callback deliberately ignores RequireToEnd.
            return PhysicalBodyIdle(stage.Source);
        }
    }
}
