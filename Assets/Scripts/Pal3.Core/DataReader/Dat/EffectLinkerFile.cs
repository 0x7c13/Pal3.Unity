// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Dat
{
    using System.Collections.Generic;
    using System.IO;
    using Primitives;

    public struct EffectGroupInfo
    {
        public int EffGroupId;                          // 特效组Id
        public bool IsCenteredAroundCaster;             // 以发动方中心
        public bool IsCenteredAroundReceiver;           // 以接收方中心
        public bool IsCasterActor;                      // 发动方是人物
        public bool IsReceiverActor;                    // 接收方是人物
        public GameBoxVector3 EffectGameBoxPosition;    // 特效位置
        public float WaitDurationInSeconds;             // 等待时间
        public float EffectDurationInSeconds;           // 特效时间
    }

    public struct EffectLinker
    {
        public uint SkillId;
        public EffectGroupInfo[] EffectGroupInfos;
    }

    public sealed class EffectLinkerFile
    {
        public Dictionary<int, EffectGroupInfo[]> SkillIdToEffectGroupMap { get; }

        public EffectLinkerFile(EffectLinker[] effectLinkers)
        {
            SkillIdToEffectGroupMap = new Dictionary<int, EffectGroupInfo[]>();

            foreach (EffectLinker effectLinker in effectLinkers)
            {
                if (effectLinker.SkillId == 0) continue; // Skip empty effect linker

                if (SkillIdToEffectGroupMap.ContainsKey((int) effectLinker.SkillId))
                {
                    throw new InvalidDataException("Duplicate skill id found!"); // Should never happen
                }
                else
                {
                    SkillIdToEffectGroupMap[(int) effectLinker.SkillId] = effectLinker.EffectGroupInfos;
                }
            }
        }
    }
}