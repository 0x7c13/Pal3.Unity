// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(16, "调用另一段脚本，" +
                    "参数：脚本ID")]
    public sealed class ScriptExecuteCommand : ICommand
    {
        public ScriptExecuteCommand(int scriptId)
        {
            ScriptId = scriptId;
        }

        public int ScriptId { get; }
    }
}