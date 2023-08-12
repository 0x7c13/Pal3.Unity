// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Dat
{
    using UnityEngine;

    public struct EffectGroupInfo
    {
        public int EffGroupId;                   // 特效组Id
        public bool IsCenteredAroundCaster;      // 以发动方中心
        public bool IsCenteredAroundReceiver;    // 以接收方中心
        public bool IsCasterActor;               // 发动方是人物
        public bool IsReceiverActor;             // 接收方是人物
        public Vector3 Position;                 // 特效位置
        public float WaitDurationInSeconds;      // 等待时间
        public float EffectDurationInSeconds;    // 特效时间
    }

    public struct EffectLinker
    {
        public uint SkillId;
        public EffectGroupInfo[] EffectGroupInfos;
    }

    public sealed class EffectLinkerFile
    {
        public EffectLinkerFile(EffectLinker[] effectLinkers)
        {
            EffectLinkers = effectLinkers;
        }

        public EffectLinker[] EffectLinkers { get; }
    }
}