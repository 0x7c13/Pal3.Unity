// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(1, "脚本在一定时间后才执行下一条指令（不影响游戏系统和其它脚本的执行），" +
                   "参数：time（秒）")]
    public sealed class ScriptRunnerWaitUntilTimeCommand : ICommand
    {
        public ScriptRunnerWaitUntilTimeCommand(float time)
        {
            Time = time;
        }

        public float Time { get; }
    }
}