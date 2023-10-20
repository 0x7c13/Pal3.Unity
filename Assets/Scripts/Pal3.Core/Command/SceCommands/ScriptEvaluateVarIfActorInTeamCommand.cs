// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(137, "检测角色是否在队伍里并与临时变量计算结果，" +
                     "参数：角色ID")]
    public class ScriptEvaluateVarIfActorInTeamCommand : ICommand
    {
        public ScriptEvaluateVarIfActorInTeamCommand(int actorId)
        {
            ActorId = actorId;
        }

        public int ActorId { get; }
    }
}