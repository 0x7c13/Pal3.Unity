// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(163, "检测场景中物品（宝箱）是否被完全使用过并赋值给变量，" +
                    "参数：场景名，物件ID，变量名")]
    public sealed class ScriptVarSetObjectUsageStatusCommand : ICommand
    {
        public ScriptVarSetObjectUsageStatusCommand(
            string sceneName,
            int objectId,
            ushort variable)
        {
            SceneName = sceneName;
            ObjectId = objectId;
            Variable = variable;
        }

        public string SceneName { get; }
        public int ObjectId { get; }
        [SceUserVariable] public ushort Variable { get; }
    }
    #endif
}