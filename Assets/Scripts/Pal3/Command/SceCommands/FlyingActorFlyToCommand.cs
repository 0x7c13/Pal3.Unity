// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [SceCommand(89, "让花盈飞到某空间点" +
                    "参数：X，Y，Z")]
    #elif PAL3A
    [SceCommand(89, "让桃子飞到某空间点" +
                    "参数：X，Y，Z")]
    #endif
    public class FlyingActorFlyToCommand : ICommand
    {
        public FlyingActorFlyToCommand(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
    }
}