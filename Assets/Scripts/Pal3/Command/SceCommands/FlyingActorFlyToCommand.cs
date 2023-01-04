// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    #if PAL3
    [SceCommand(89, "让花盈飞到某空间点" +
                    "参数：原GameBox引擎下的一个三维坐标（X，Y，Z）")]
    #elif PAL3A
    [SceCommand(89, "让桃子飞到某空间点" +
                    "参数：原GameBox引擎下的一个三维坐标（X，Y，Z）")]
    #endif
    public class FlyingActorFlyToCommand : ICommand
    {
        public FlyingActorFlyToCommand(float gameBoxXPosition, float gameBoxYPosition, float gameBoxZPosition)
        {
            GameBoxXPosition = gameBoxXPosition;
            GameBoxYPosition = gameBoxYPosition;
            GameBoxZPosition = gameBoxZPosition;
        }

        public float GameBoxXPosition { get; }
        public float GameBoxYPosition { get; }
        public float GameBoxZPosition { get; }
    }
}