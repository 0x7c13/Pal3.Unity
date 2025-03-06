// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Dat
{
    public sealed class EffectLinkerFileReader : IFileReader<EffectLinkerFile>
    {
        private const int NUM_OF_EFFECT_LINKERS = 384;

        public EffectLinkerFile Read(IBinaryReader reader, int codepage)
        {
            EffectLinker[] effectDefinitions = new EffectLinker[NUM_OF_EFFECT_LINKERS];

            for (int i = 0; i < NUM_OF_EFFECT_LINKERS; i++)
            {
                effectDefinitions[i] = ReadEffectLinker(reader);
            }

            return new EffectLinkerFile(effectDefinitions);
        }

        private static EffectLinker ReadEffectLinker(IBinaryReader reader)
        {
            uint skillId = reader.ReadUInt32();

            EffectGroupInfo[] effectGroupInfos = new EffectGroupInfo[2];

            for (int i = 0; i < 2; i++)
            {
                effectGroupInfos[i] = ReadEffectGroupInfo(reader);
            }

            return new EffectLinker()
            {
                SkillId = skillId,
                EffectGroupInfos = effectGroupInfos,
            };
        }

        private static EffectGroupInfo ReadEffectGroupInfo(IBinaryReader reader)
        {
            return new EffectGroupInfo()
            {
                EffGroupId = reader.ReadInt32(),
                IsCenteredAroundCaster = reader.ReadBoolean(),
                IsCenteredAroundReceiver = reader.ReadBoolean(),
                IsCasterActor = reader.ReadBoolean(),
                IsReceiverActor = reader.ReadBoolean(),
                EffectGameBoxPosition = reader.ReadGameBoxVector3(),
                WaitDurationInSeconds = reader.ReadSingle(),
                EffectDurationInSeconds = reader.ReadSingle(),
            };
        }
    }
}