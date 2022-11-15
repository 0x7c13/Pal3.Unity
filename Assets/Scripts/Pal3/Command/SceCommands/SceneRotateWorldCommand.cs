// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [AvailableInConsole]
    [SceCommand(117, "旋转整个场景，" +
                    "参数：原GameBox引擎下的一个三维坐标（X，Y，Z）")]
    public class SceneRotateWorldCommand : ICommand
    {
        public SceneRotateWorldCommand(int gameBoxXPosition, int gameBoxYPosition, int gameBoxZPosition)
        {
            GameBoxXPosition = gameBoxXPosition;
            GameBoxYPosition = gameBoxYPosition;
            GameBoxZPosition = gameBoxZPosition;
        }

        public int GameBoxXPosition { get; }
        public int GameBoxYPosition { get; }
        public int GameBoxZPosition { get; }
    }
    #elif PAL3A
    [AvailableInConsole]
    [SceCommand(117, "旋转整个场景，" +
                     "参数：X，Y，Z")]
    public class SceneRotateWorldCommand : ICommand
    {
        public SceneRotateWorldCommand(int unknown1, int unknown2, int gameBoxXPosition, int gameBoxYPosition, int gameBoxZPosition)
        {
            Unknown1 = unknown1;
            Unknown2 = unknown2;
            GameBoxXPosition = gameBoxXPosition;
            GameBoxYPosition = gameBoxYPosition;
            GameBoxZPosition = gameBoxZPosition;
        }

        public int Unknown1 { get; }
        public int Unknown2 { get; }
        public int GameBoxXPosition { get; }
        public int GameBoxYPosition { get; }
        public int GameBoxZPosition { get; }
    }
    #endif
}