// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(141, "设置全局镜头（屏幕）特效，" +
                     "参数：特效模式（-1清除所有特效，0水下效果，1黑白色阶）")]
    public class EffectSetScreenEffectCommand : ICommand
    {
        public EffectSetScreenEffectCommand(int mode)
        {
            Mode = mode;
        }

        public int Mode { get; }
    }
}