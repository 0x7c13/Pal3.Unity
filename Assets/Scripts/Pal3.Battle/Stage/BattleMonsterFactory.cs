// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;

    public sealed class BattleMonsterCreation
    {
        private readonly BattleParticipant _participant;

        public int NativeIndex
        {
            get;
        }

        public BattleParticipant Participant => _participant.Copy();

        public BattleActorDefinition Definition
        {
            get;
        }

        public BattleMonsterCreation(int nativeIndex, BattleParticipant participant, BattleActorDefinition definition)
        {
            if (nativeIndex < 5 || nativeIndex > 10 || participant == null || definition == null ||
                participant.Side != BattleSide.Enemy || (int)participant.Slot != nativeIndex - 5 ||
                participant.InstanceId != definition.InstanceId || !string.IsNullOrEmpty(definition.UnsupportedReason))
                throw new ArgumentException("A supported captured monster in its original enemy slot is required.");
            NativeIndex = nativeIndex;
            _participant = participant.Copy();
            Definition = definition;
        }
    }

    // Prepare can load and validate resources without changing a live catalog.
    // Commit publishes those prepared bindings only after the session validates
    // the candidate. Native slots keep stable instance IDs across replacement.
    public interface IBattleMonsterFactory
    {
        BattleMonsterCreation Prepare(int nativeIndex, int actorId, string instanceId);

        void Commit(BattleMonsterCreation creation);
    }
}
