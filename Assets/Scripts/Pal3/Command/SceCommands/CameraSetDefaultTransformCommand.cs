// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(37, "调用默认镜头参数，" +
                    "参数：镜头参数ID")]
    public class CameraSetDefaultTransformCommand : ICommand
    {
        public CameraSetDefaultTransformCommand(int option)
        {
            Option = option;
        }

        public int Option { get; }
    }
}