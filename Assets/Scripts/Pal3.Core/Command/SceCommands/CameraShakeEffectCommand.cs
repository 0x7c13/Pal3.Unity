// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(118, "摇晃镜头（模拟地震）效果，" +
                     "参数：持续时间，振幅")]
    public sealed class CameraShakeEffectCommand : ICommand
    {
        public CameraShakeEffectCommand(float duration, float amplitude)
        {
            Duration = duration;
            Amplitude = amplitude;
        }

        public float Duration { get; }
        public float Amplitude { get; }
    }
}