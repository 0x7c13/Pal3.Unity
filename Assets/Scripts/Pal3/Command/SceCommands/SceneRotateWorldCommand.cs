// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [SceCommand(117, "旋转整个场景，" +
                    "参数：X，Y，Z")]
    public class SceneRotateWorldCommand : ICommand
    {
        public SceneRotateWorldCommand(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int X { get; }
        public int Y { get; }
        public int Z { get; }
    }
    #elif PAL3A
    [SceCommand(117, "旋转整个场景，" +
                     "参数：X，Y，Z")]
    public class SceneRotateWorldCommand : ICommand
    {
        public SceneRotateWorldCommand(int unknown1, int unknown2, int x, int y, int z)
        {
            Unknown1 = unknown1;
            Unknown2 = unknown2;
            X = x;
            Y = y;
            Z = z;
        }

        public int Unknown1 { get; }
        public int Unknown2 { get; }
        public int X { get; }
        public int Y { get; }
        public int Z { get; }
    }
    #endif
}