// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(50, "取出好感值并赋值给变量，" +
                    "参数：用户变量，角色ID", 0b0001)]
    public class ScriptVarSetActorFavorCommand : ICommand
    {
        public ScriptVarSetActorFavorCommand(ushort variable, int actorId)
        {
            Variable = variable;
            ActorId = actorId;
        }

        public ushort Variable { get; }
        public int ActorId { get; }
    }
}