// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(144, "在角色位置播放特效，" +
                     "参数：角色ID")]
    public class EffectAttachToActorCommand : ICommand
    {
        public EffectAttachToActorCommand(int actorId)
        {
            ActorId = actorId;
        }

        public int ActorId { get; }
    }
}