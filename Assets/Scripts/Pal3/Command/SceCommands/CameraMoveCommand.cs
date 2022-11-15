// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [AvailableInConsole]
    [SceCommand(34, "镜头当前位置移动（保持当前角度），" +
                    "参数：原GameBox引擎下的一个三维坐标（X，Y，Z），动作时间，插值类型")]
    public class CameraMoveCommand : ICommand
    {
        public CameraMoveCommand(float gameBoxXPosition, float gameBoxYPosition, float gameBoxZPosition, float duration, int mode)
        {
            GameBoxXPosition = gameBoxXPosition;
            GameBoxYPosition = gameBoxYPosition;
            GameBoxZPosition = gameBoxZPosition;
            Duration = duration;
            Mode = mode;
        }

        public float GameBoxXPosition { get; }
        public float GameBoxYPosition { get; }
        public float GameBoxZPosition { get; }
        public float Duration { get; }
        public int Mode { get; }
    }
    #elif PAL3A
    [AvailableInConsole]
    [SceCommand(34, "镜头当前位置移动（保持当前角度），" +
                    "参数：原GameBox引擎下的一个三维坐标（X，Y，Z），动作时间，插值类型，同步（1暂停当前脚本运行，0异步进行动画且继续执行脚本）")]
    public class CameraMoveCommand : ICommand
    {
        public CameraMoveCommand(float gameBoxXPosition, float gameBoxYPosition, float gameBoxZPosition, float duration, int mode, int synchronous)
        {
            GameBoxXPosition = gameBoxXPosition;
            GameBoxYPosition = gameBoxYPosition;
            GameBoxZPosition = gameBoxZPosition;
            Duration = duration;
            Mode = mode;
            Synchronous = synchronous;
        }

        public float GameBoxXPosition { get; }
        public float GameBoxYPosition { get; }
        public float GameBoxZPosition { get; }
        public float Duration { get; }
        public int Mode { get; }
        public int Synchronous { get; }
    }
    #endif
}