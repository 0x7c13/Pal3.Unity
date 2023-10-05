// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(50, "取出好感值，" +
                    "参数：用户变量，角色ID")]
    public class ScriptGetFavorCommand : ICommand
    {
        public ScriptGetFavorCommand(int variable, int actorId)
        {
            Variable = variable;
            ActorId = actorId;
        }

        public int Variable { get; }
        public int ActorId { get; }
    }
}