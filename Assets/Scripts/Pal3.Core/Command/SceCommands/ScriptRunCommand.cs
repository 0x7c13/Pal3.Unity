// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(16, "调用另一段脚本，" +
                    "参数：脚本ID")]
    public class ScriptRunCommand : ICommand
    {
        public ScriptRunCommand(int scriptId)
        {
            ScriptId = scriptId;
        }

        public int ScriptId { get; }
    }
}