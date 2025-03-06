// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Ini
{
    public struct CombatCameraConfig
    {
        public float GameBoxPositionX;
        public float GameBoxPositionY;
        public float GameBoxPositionZ;

        public float Yaw;
        public float Pitch;
        public float Roll;
    }

    public sealed class CombatCameraConfigFile
    {
        public CombatCameraConfig[] DefaultCamConfigs { get; }

        public CombatCameraConfigFile(CombatCameraConfig[] defaultCamConfigs)
        {
            DefaultCamConfigs = defaultCamConfigs;
        }
    }
}