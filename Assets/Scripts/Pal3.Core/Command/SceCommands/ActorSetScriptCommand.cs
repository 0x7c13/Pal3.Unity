// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(29, "设置角色的对话脚本，" +
                    "参数：角色ID，脚本ID")]
    public sealed class ActorSetScriptCommand : ICommand
    {
        public ActorSetScriptCommand(int actorId, int scriptId)
        {
            ActorId = actorId;
            ScriptId = scriptId;
        }

        [SceActorId] public int ActorId { get; set; }
        public int ScriptId { get; }
    }
}