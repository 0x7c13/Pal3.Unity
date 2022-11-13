// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(29, "设置角色的对话脚本，" +
                    "参数：角色ID，脚本ID")]
    public class ActorSetScriptCommand : ICommand
    {
        public ActorSetScriptCommand(int actorId, int scriptId)
        {
            ActorId = actorId;
            ScriptId = scriptId;
        }

        public int ActorId { get; }
        public int ScriptId { get; }
    }
}