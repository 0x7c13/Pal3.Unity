// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(118, "摇晃镜头（模拟地震）效果，" +
                     "参数：持续时间，振幅")]
    public class CameraShakeEffectCommand : ICommand
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