// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    using System;

    [SceCommand(29, "设置角色的对话脚本，" +
                    "参数：角色ID，脚本ID")]
    public class ActorSetScriptCommand : ICommand
    {
        public ActorSetScriptCommand(int actorId, int scriptId)
        {
            if (actorId == -1)
            {
                throw new Exception("ActorId should no be -1 for SceCommand 29");
            }

            ActorId = actorId;
            ScriptId = scriptId;
        }

        public int ActorId { get; }
        public int ScriptId { get; }
    }
}