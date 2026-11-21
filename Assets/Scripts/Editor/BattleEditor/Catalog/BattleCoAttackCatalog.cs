// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Pal3.Battle;
    using Pal3.Core.DataReader.Dat;

    public sealed partial class BattleAnimationCatalog : IBattleCoAttackTimings
    {
        private readonly Dictionary<int, BattleComboStageProfile> _coAttacks = new();

        public BattleMovementPoint CoAttackHome(string actor, BattleSlot slot) =>
            NativePosition(_data.FormationPosition(_participants[actor].Side, slot, 0));

        public BattleComboStageProfile CoAttack(string actor, BattleComboDefinition combo)
        {
            if (combo == null || !_participants.ContainsKey(actor))
                throw new ArgumentException("The original combo needs a captured source actor and definition.");
            if (_coAttacks.TryGetValue(combo.Id, out BattleComboStageProfile cached))
                return cached;
            if (!_linkers.SkillIdToEffectGroupMap.TryGetValue(combo.Id, out EffectGroupInfo[] groups) || groups.Length != 2)
                throw new NotSupportedException($"Combo {combo.Id} has no original two-group effect linker.");
            // All shipped combo linkers place their groups in scene coordinates.
            // Reject unsupported authored placements before the source gauge changes.
            if (groups.Any(group => group.IsCenteredAroundCaster || group.IsCenteredAroundReceiver || group.IsCasterActor || group.IsReceiverActor))
                throw new NotSupportedException($"Combo {combo.Id} has an unverified actor-relative effect placement.");
            var enemyCenter = NativePosition(_data.FormationPosition(BattleSide.Enemy, BattleSlot.Center, 0));
            static BattleMovementPoint Origin(EffectGroupInfo group) =>
                new(group.EffectGameBoxPosition.X, group.EffectGameBoxPosition.Y, group.EffectGameBoxPosition.Z);
            var profile = new BattleComboStageProfile(groups[0].EffGroupId, groups[1].EffGroupId,
                groups[0].WaitDurationInSeconds, groups[1].WaitDurationInSeconds, groups[0].WorkTimeInSeconds,
                Origin(groups[0]), Origin(groups[1]), enemyCenter);
            _coAttacks.Add(combo.Id, profile);
            return profile;
        }
    }
}
