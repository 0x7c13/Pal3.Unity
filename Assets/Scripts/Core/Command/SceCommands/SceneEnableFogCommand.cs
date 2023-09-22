// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(162, "设置并开启场景内的雾气显示，" +
                    "参数：起始距离（原GameBox引擎下的距离单位），结束距离（原GameBox引擎下的距离单位），强度值，" +
                    "蓝色(0f-255f)，绿色(0f-255f)，红色(0f-255f)，透明度(0f-1f)")]
    public class SceneEnableFogCommand : ICommand
    {
        public SceneEnableFogCommand(
            float startDistance,
            float endDistance,
            float intensity,
            float blue,
            float green,
            float red,
            float alpha)
        {
            StartDistance = startDistance;
            EndDistance = endDistance;
            Intensity = intensity;
            Blue = blue;
            Green = green;
            Red = red;
            Alpha = alpha;
        }

        public float StartDistance { get; }
        public float EndDistance { get; }
        public float Intensity { get; }
        public float Blue { get; }
        public float Green { get; }
        public float Red { get; }
        public float Alpha { get; }
    }
    #endif
}