// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(1, "脚本在一定时间后才执行下一条指令（不影响游戏系统和其它脚本的执行），" +
                   "参数：time（秒）")]
    public class ScriptWaitUntilTimeCommand : ICommand
    {
        public ScriptWaitUntilTimeCommand(float time)
        {
            Time = time;
        }

        public float Time { get; }
    }
}