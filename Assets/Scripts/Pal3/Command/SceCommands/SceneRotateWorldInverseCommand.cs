// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(128, "反方向旋转整个场景，" +
                    "参数：原GameBox引擎下的一个三维坐标（X，Y，Z）")]
    public class SceneRotateWorldInverseCommand : ICommand
    {
        public SceneRotateWorldInverseCommand(int gameBoxXPosition, int gameBoxYPosition, int gameBoxZPosition)
        {
            GameBoxXPosition = gameBoxXPosition;
            GameBoxYPosition = gameBoxYPosition;
            GameBoxZPosition = gameBoxZPosition;
        }

        public int GameBoxXPosition { get; }
        public int GameBoxYPosition { get; }
        public int GameBoxZPosition { get; }
    }
}