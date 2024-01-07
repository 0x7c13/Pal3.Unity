// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(141, "设置全局镜头（屏幕）特效，" +
                     "参数：特效模式（-1清除所有特效，0水下效果，1黑白色阶）")]
    public sealed class EffectSetScreenEffectCommand : ICommand
    {
        public EffectSetScreenEffectCommand(int mode)
        {
            Mode = mode;
        }

        public int Mode { get; }
    }
}