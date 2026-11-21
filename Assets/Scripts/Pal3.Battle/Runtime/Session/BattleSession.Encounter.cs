// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public sealed partial class BattleSession
    {
        private BattleEncounterRules _encounterRules;
        private BattleBossRules _bossRules;
        private IBattleMonsterFactory _monsterFactory;
        private int _bossRounds;
        private string _activeEncounterScene;
        private readonly Dictionary<string, int> _monsterGenerations = new(StringComparer.Ordinal);

        public int BossRounds => _bossRounds;

        public string ActiveScene => _activeEncounterScene ?? _scenario.Scene;

        public int MonsterGeneration(string instanceId) => _monsterGenerations.GetValueOrDefault(instanceId);

        public BattleParticipant CaptureParticipant(string instanceId) => _byId.TryGetValue(instanceId, out Actor actor)
            ? actor.Initial.Copy() : throw new ArgumentException("Unknown battle actor.", nameof(instanceId));

        private void InitializeOriginalEncounter()
        {
            _encounterRules = new BattleEncounterRules(_scenario.Variant);
            _bossRules = new BattleBossRules(_scenario.Variant);
            _monsterFactory = _catalog as IBattleMonsterFactory;
        }

        private bool BeginOriginalRound(Actor actor) => BattleEncounterRules.BeginRound(NativeActorIndex(actor), actor.Rounds,
            _scenario.RoundLimit, ref actor.ProcessedRounds, ref _bossRounds);

        private bool RoundLimitReached => BattleEncounterRules.RoundLimitReached(_bossRounds, _scenario.RoundLimit);

        private BattleEncounterOutcome EncounterOutcome(IReadOnlyCollection<int> unfinishedNativeTasks) => _encounterRules.CheckOutcome(
            _actors.Any(actor => actor.Initial.Side == BattleSide.Ally && actor.Alive),
            _actors.Any(actor => actor.Initial.Side == BattleSide.Enemy && actor.Alive && !actor.Escaped),
            _bossRounds, _scenario.RoundLimit, _scenario.MustFail, unfinishedNativeTasks);

        private void ApplyOriginalMustFailHp(Actor actor) => actor.Hp = BattleEncounterRules.HpAfterMustFail(
            _scenario.MustFail, NativeActorIndex(actor), actor.Hp, actor.Attributes[0]);

        private void HandleOriginalBossRound(Actor actor)
        {
            if (Definitions == null)
                return;
            foreach (BattleState state in _bossRules.RoundStates(actor.Initial.ActorId, actor.ProcessedRounds))
                ApplyState(actor, state, false);
            int slot = _bossRules.SplitSlot(actor.Initial.ActorId, actor.ProcessedRounds, CaptureEnemySlots(), _random);
            if (slot >= 0)
                CreateOriginalMonster(slot, actor.Initial.ActorId, 0);
        }

        private bool HandleOriginalBossLethal(Actor actor, long actionId)
        {
            if (Definitions == null || actor.Hp > 0)
                return false;
            int replacement = _bossRules.DeathReplacement(actor.Initial.ActorId);
            if (replacement == 0)
                return false;
            RequestRoleEnd(actor);
            CreateOriginalMonster(NativeActorIndex(actor), replacement, actionId);
            _activeEncounterScene = "z39";
            return true;
        }

        private void HandleOriginalPostMagic(Actor actor, ActionRun run)
        {
            if (Definitions == null || run.Ability?.IsMagic != true || run.Ability.IsItem ||
                !_bossRules.SelfDestructsAfterMagic(actor.Initial.ActorId, run.Ability.Id) || actor.Escaped ||
                actor.Run?.Kind == BattleAction.Flee)
                return;
            if (_effects != null)
            {
                StartEffect(new BattleEffectRequest(324, actor.Initial.InstanceId, new[] { actor.Initial.InstanceId }, actor.Position, unchecked((uint)_scenario.Seed)));
                StartEffect(new BattleEffectRequest(325, actor.Initial.InstanceId, new[] { actor.Initial.InstanceId }, actor.Position, unchecked((uint)_scenario.Seed)));
            }
            ChangeHp(actor, -actor.Hp, null, run.Id, "original fireball self-destruction after magic cost", false);
        }

        private void ResolveOriginalSummon(Actor source, long actionId)
        {
            int alive = _actors.Count(actor => actor.Initial.Side == BattleSide.Enemy && actor.Alive && !actor.Escaped);
            foreach (BattleMonsterRequest request in _bossRules.Summon(NativeActorIndex(source), alive, CaptureEnemySlots(), _random))
                CreateOriginalMonster(request.NativeIndex, request.ActorId, actionId);
        }

        private BattleNativeEnemySlot[] CaptureEnemySlots() => Enumerable.Range(5, 6).Select(index =>
        {
            Actor actor = ActorAtNativeIndex(index);
            return actor == null ? default : new BattleNativeEnemySlot(true, actor.Alive, actor.Escaped);
        }).ToArray();

        private void CreateOriginalMonster(int nativeIndex, int actorId, long actionId)
        {
            if (_monsterFactory == null)
                throw new NotSupportedException("This encounter needs a captured native-slot monster factory.");
            Actor actor = ActorAtNativeIndex(nativeIndex);
            string instanceId = actor?.Initial.InstanceId ?? "__native_enemy_" + nativeIndex;
            if (actor == null)
            {
                string prefix = instanceId;
                int suffix = 0;
                while (_byId.ContainsKey(instanceId))
                    instanceId = prefix + "_" + ++suffix;
            }
            BattleMonsterCreation creation = _monsterFactory.Prepare(nativeIndex, actorId, instanceId)
                ?? throw new InvalidOperationException("The monster factory did not prepare a candidate.");
            BattleParticipant participant = creation.Participant;
            if (creation.NativeIndex != nativeIndex || participant.ActorId != actorId || participant.InstanceId != instanceId ||
                participant.Attributes.Length != 12 || participant.Vitals.Length != 3 || participant.Attributes.Any(value => value < 0 || value > 1000000) ||
                participant.Vitals.Where((value, resource) => value != participant.Attributes[resource]).Any())
                throw new InvalidOperationException("The prepared monster does not match the native creation request.");
            _monsterFactory.Commit(creation);
            if (actor == null)
            {
                actor = new Actor { NativeIndex = nativeIndex };
                _actors.Add(actor);
                _byId.Add(instanceId, actor);
            }
            else
            {
                Cancel(actor, "native monster slot replaced");
                ClearPhysicalBody(actor);
                _physicalBodies.Remove(instanceId);
                // Native StageRole.Init resets the role's grab as well as its
                // body queue. Independent stages retain their slot reference,
                // but cannot keep a lease on the replacement role's body.
                _bodyStageOwners.Remove(instanceId);
                _fallbackFleeBodies.Remove(instanceId);
            }
            // Native CreateMonster zeroes this slot's role state. Stage jobs
            // referring to the slot remain; stable IDs make queued targets bind
            // to its new occupant just as original native-index commands do.
            actor.Initial = participant;
            ClearOriginalAbsorbHpWorm(actor);
            actor.Definition = creation.Definition;
            actor.Slot = participant.Slot;
            actor.Home = (_catalog as IBattleStageGeometry)?.InitialHome(instanceId) ?? default;
            actor.Position = actor.Home;
            actor.FacingDegrees = BattleFacingRules.Starting(BattleSide.Enemy, _scenario.Encounter);
            actor.Movement = null;
            actor.MovementQueue.Clear();
            actor.Hp = participant.Vitals[0];
            actor.Sp = participant.Vitals[1];
            actor.Mp = participant.Vitals[2];
            actor.Gauge = actor.CastGauge = actor.CastSpeed = actor.Rounds = actor.ProcessedRounds = actor.HurtUntil = 0;
            actor.PoseStarted = checked((int)SimulationTick);
            actor.Level = participant.Level;
            actor.Experience = participant.Experience;
            actor.Love = participant.Love;
            actor.PresentationTick = 0;
            actor.Attributes = participant.Attributes.ToArray();
            ApplyPal3ALiaoRiCarry(actor);
            actor.EnemyFleeing = false;
            actor.Escaped = actor.Dodging = actor.Reviving = actor.Ready = actor.Guard = false;
            actor.Alive = actor.ModelAlive = true;
            actor.Killer = null;
            actor.DeathTick = -1;
            actor.CorpseItem = 0;
            actor.Skills.Clear();
            actor.Skills.AddRange(participant.Skills.Select(skill => skill.Copy()));
            actor.States.Clear();
            foreach (BattleState state in actor.Definition.PermanentStates.OrderBy(state => state))
                ApplyState(actor, state, false, null, true, sound: false);
            _stealPools[instanceId] = new StealPool
            {
                Money = actor.Definition.StealableMoney,
                ItemId = actor.Definition.StealableItemId,
                ItemCount = actor.Definition.StealableItemCount
            };
            _bodyAnimations.Remove(instanceId);
            _bodyPassRates.Remove(instanceId);
            _externalEffectActors.Remove(instanceId);
            foreach (CoAttackStageRun stage in _coAttackStages.Values)
                stage.HiddenActors.Remove(instanceId);
            _spiritCapturedActors.Remove(instanceId);
            foreach (var key in _aiSkillAliases.Keys.Where(key => key.Actor == instanceId).ToArray())
                _aiSkillAliases.Remove(key);
            _monsterGenerations[instanceId] = _monsterGenerations.GetValueOrDefault(instanceId) + 1;
            if (_ai != null)
            {
                _ai.Roles[nativeIndex].Profile.SetEnabled(true);
                _ai.Roles[nativeIndex].Profile.Negative = true;
                for (int index = 0; index < _ai.NewRole.Length; index++)
                    _ai.NewRole[index] = true;
            }
            Add(BattleEventKind.ResourceChanged, target: instanceId, actionId: actionId,
                detail: "native enemy slot " + nativeIndex + " created actor " + actorId);
            if (_effects != null)
                StartEffect(new BattleEffectRequest(321, instanceId, new[] { instanceId }, actor.Home, unchecked((uint)_scenario.Seed)));
        }
    }
}
