// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Battle;

    public sealed partial class BattleAnimationCatalog : IBattleMonsterFactory
    {
        private readonly ConditionalWeakTable<BattleMonsterCreation, BattleAnimationCatalog> _preparedMonsters = new();

        public BattleMonsterCreation Prepare(int nativeIndex, int actorId, string instanceId)
        {
            if (nativeIndex < 5 || nativeIndex > 10 || string.IsNullOrWhiteSpace(instanceId))
                throw new ArgumentException("A native enemy slot and stable instance ID are required.");
            BattleParticipant participant = _data.CreateParticipant(actorId, BattleSide.Enemy, (BattleSlot)(nativeIndex - 5));
            participant.InstanceId = instanceId;
            participant.Statuses.Clear();
            BattleActorDefinition original = BattleDefinitionCompiler.Actor(_data, participant, BattleData.Variant);
            // Released CreateMonster zeroes the role and installs poison
            // immunity only; it does not replay LoadRoleData's initial states.
            var definition = new BattleActorDefinition(new BattleActorData
            {
                InstanceId = instanceId,
                Identity = original.Identity,
                Boss = original.Boss,
                Equipment = original.Equipment.ToArray(),
                ImmuneStates = original.ImmuneStates.ToArray(),
                PermanentStates = original.PermanentStates.Where(state => state == BattleState.PoisonResist).ToArray(),
                PhysicalAttackElements = original.PhysicalAttackElements,
                PhysicalDefenseElements = original.PhysicalDefenseElements,
                PhysicalAttackElementValues = original.PhysicalAttackElementValues.ToArray(),
                PhysicalDefenseElementValues = original.PhysicalDefenseElementValues.ToArray(),
                NormalAttackMode = original.NormalAttackMode,
                NormalAttackRange = original.NormalAttackRange,
                ExperienceReward = original.ExperienceReward,
                MoneyReward = original.MoneyReward,
                LootItem = original.LootItem,
                LootCount = original.LootCount,
                CorpseItem = original.CorpseItem,
                CorpseSkill = original.CorpseSkill,
                CorpsePercent = original.CorpsePercent,
                EscapePercent = original.EscapePercent,
                StealableMoney = original.StealableMoney,
                StealableItemId = original.StealableItemId,
                StealableItemCount = original.StealableItemCount,
                GrowthUnsupportedReason = original.GrowthUnsupportedReason,
                UnsupportedReason = original.UnsupportedReason,
            });
            var creation = new BattleMonsterCreation(nativeIndex, participant, definition);
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                DataFingerprint = _data.Fingerprint,
                Scene = "prepared-native-monster",
                SceneElement = _sceneElement,
                Participants = _participants.Values.Where(actor => actor.InstanceId != instanceId).Select(actor => actor.Copy()).ToList()
            };
            scenario.Participants.Add(participant);
            var setup = new BattleSetup(scenario);
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, setup);
            var prepared = new BattleAnimationCatalog(_data, setup, definitions, _linkers);
            // Force both movement/body and ability metadata to be captured
            // before a live slot is changed or any source action is cancelled.
            _ = prepared.Physical(instanceId);
            foreach (BattleSkill skill in participant.Skills)
                if (definitions.Skills.TryGetValue(skill.Id, out BattleAbilityDefinition ability) && ability.SkillTypeId != 4 &&
                    string.IsNullOrEmpty(ability.UnsupportedReason) && skill.Id != (BattleData.Variant == "PAL3" ? 7711 : 7714))
                {
                    _ = prepared.Ability(instanceId, ability);
                    _ = prepared.AbilityStage(instanceId, ability);
                }
            _preparedMonsters.Add(creation, prepared);
            return creation;
        }

        public void Commit(BattleMonsterCreation creation)
        {
            if (creation == null || !_preparedMonsters.TryGetValue(creation, out BattleAnimationCatalog prepared))
                throw new ArgumentException("The monster was not prepared by this catalog.", nameof(creation));
            _preparedMonsters.Remove(creation);
            string instanceId = creation.Participant.InstanceId;
            _participants[instanceId] = prepared._participants[instanceId];
            _actors[instanceId] = prepared._actors[instanceId];
            _physicalProfiles[instanceId] = prepared._physicalProfiles[instanceId];
            foreach (var key in _abilities.Keys.Where(key => key.Item1 == instanceId).ToArray())
                _abilities.Remove(key);
            foreach (var key in _abilityStages.Keys.Where(key => key.Item1 == instanceId).ToArray())
                _abilityStages.Remove(key);
            foreach (var pair in prepared._abilities.Where(pair => pair.Key.Item1 == instanceId))
                _abilities[pair.Key] = pair.Value;
            foreach (var pair in prepared._abilityStages.Where(pair => pair.Key.Item1 == instanceId))
                _abilityStages[pair.Key] = pair.Value;
            InvalidateInitialAttacks(instanceId);
            foreach (var pair in prepared._initialAttacks.Where(pair => pair.Key.Source == instanceId || pair.Key.Target == instanceId))
                _initialAttacks.Add(pair.Key, pair.Value);
            Diagnostics.AddRange(prepared.Diagnostics);
        }
    }
}
