// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(181, "用两点设置相机水平朝向约束")]
    public sealed class CameraSetHorizontalDirectionCommand : ICommand
    {
        public CameraSetHorizontalDirectionCommand(float x1, float z1, float x2, float z2)
        {
            X1 = x1;
            Z1 = z1;
            X2 = x2;
            Z2 = z2;
        }

        public float X1 { get; }

        public float Z1 { get; }

        public float X2 { get; }

        public float Z2 { get; }
    }
}
#endif
