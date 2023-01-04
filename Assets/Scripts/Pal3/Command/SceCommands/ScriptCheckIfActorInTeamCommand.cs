// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(137, "检测角色是否在队伍里并设置给变量，" +
                     "参数：角色ID")]
    public class ScriptCheckIfActorInTeamCommand : ICommand
    {
        public ScriptCheckIfActorInTeamCommand(int actorId)
        {
            ActorId = actorId;
        }

        public int ActorId { get; }
    }
}