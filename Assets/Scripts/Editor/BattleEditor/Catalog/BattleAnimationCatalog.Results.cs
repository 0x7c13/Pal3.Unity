// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Battle;
    using Core.Contract.Constants;
    using Core.Contract.Enums;

    public sealed partial class BattleAnimationCatalog
    {
        public IReadOnlyDictionary<int, BattlePhysicalAnimation> ResultBodyAnimations(BattleParticipant participant)
        {
            var result = new Dictionary<int, BattlePhysicalAnimation>
            {
                [32] = CompilePhysicalAnimation(participant, _data.IdleAction(participant)),
                [42] = CompilePhysicalAnimation(participant, ActorConstants.ActionToNameMap[ActorActionType.Defence]),
                [45] = CompilePhysicalAnimation(participant, ActorConstants.ActionToNameMap[ActorActionType.Freeze]),
                [46] = CompilePhysicalAnimation(participant, ActorConstants.ActionToNameMap[ActorActionType.Dying]),
                [47] = CompilePhysicalAnimation(participant, ActorConstants.ActionToNameMap[ActorActionType.Dead]),
                [49] = CompilePhysicalAnimation(participant, ActorConstants.ActionToNameMap[ActorActionType.Win])
            };
#if PAL3A
            int identity = PhysicalIdentity(participant);
            int weapon = participant.Equipment.Length > 0 && participant.Equipment[0] != 0 ? (int)_data.Database.GameItemInfos[participant.Equipment[0]].WeaponType : 0;
            bool alternateLevelUp = identity == 1 && weapon == 3 || identity == 4 && weapon == 8;
            result[59] = CompilePhysicalAnimation(participant, alternateLevelUp ? "z162" : ActorConstants.ActionToNameMap[ActorActionType.LevelUp]);
#endif
            return new ReadOnlyDictionary<int, BattlePhysicalAnimation>(result);
        }
    }
}
