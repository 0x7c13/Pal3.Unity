// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BattleAbilityPresentationSnapshot
    {
        public long ActionId
        {
            get;
        }

        public string Actor
        {
            get;
        }

        public BattleSide Side
        {
            get;
        }

        public int NativeIndex
        {
            get;
        }

        public BattleSlot Slot
        {
            get;
        }

        public int StageSlot
        {
            get;
        }

        public int AbilityId
        {
            get;
        }

        public string Name
        {
            get;
        }

        public bool IsItem
        {
            get;
        }

        public int ElementMask
        {
            get;
        }

        public bool NameVisible
        {
            get;
        }

        public double NameElapsedSeconds
        {
            get;
        }

        public bool LineupVisible
        {
            get;
        }

        public double LineupElapsedSeconds
        {
            get;
        }

        public double LineupDurationSeconds
        {
            get;
        }

        internal BattleAbilityPresentationSnapshot(long actionId, string actor, BattleSide side, int nativeIndex,
            BattleSlot slot, int stageSlot, BattleAbilityDefinition ability, bool nameVisible, double nameElapsed,
            bool lineupVisible = false, double lineupElapsed = 0, double lineupDuration = 0)
        {
            ActionId = actionId;
            Actor = actor;
            Side = side;
            NativeIndex = nativeIndex;
            Slot = slot;
            StageSlot = stageSlot;
            AbilityId = ability.Id;
            Name = ability.Name;
            IsItem = ability.IsItem;
            ElementMask = ability.ElementMask;
            NameVisible = nameVisible;
            NameElapsedSeconds = nameElapsed;
            LineupVisible = lineupVisible;
            LineupElapsedSeconds = lineupElapsed;
            LineupDurationSeconds = lineupDuration;
        }
    }

    public sealed partial class BattleSession
    {
        private sealed class StageMessage
        {
            public BattleSide Side;
            public string Text, LanguageKey;
            public bool AllowTrail;
            public BattleStageHandle? Handle;
            public long ReferenceId;
        }

        private readonly Dictionary<object, StageMessage> _stageMessageOwners = new();
        private readonly StageMessage[] _stageMessages = new StageMessage[2];
        private readonly object _encounterStageMessage = new();
        private bool _encounterStageMessageStarted, _encounterStageMessageFinished;
        private long _nextStageMessage;

        public IReadOnlyList<BattleStageMessageSnapshot> StageMessageReferences => Status == BattleSessionStatus.Running
            ? Array.AsReadOnly(_stageMessageOwners.Values.Where(message => !message.Handle.HasValue || _stage.IsActive(message.Handle.Value))
                .OrderBy(message => message.ReferenceId)
                .Select(message => new BattleStageMessageSnapshot(message.Side, message.Text, message.LanguageKey,
                    message.AllowTrail, 1, message.ReferenceId)).ToArray())
            : Array.Empty<BattleStageMessageSnapshot>();

        // UIStaticCB keeps the last SetText even when that particular caller
        // hides: its window remains visible until all ShowWindow references end.
        public IReadOnlyList<BattleStageMessageSnapshot> StageMessages => Status == BattleSessionStatus.Running
            ? Array.AsReadOnly(_stageMessages.Where(message => message != null)
                .Select(message => new BattleStageMessageSnapshot(message.Side, message.Text, message.LanguageKey,
                    message.AllowTrail, _stageMessageOwners.Values.Count(owner => owner.Side == message.Side &&
                        (!owner.Handle.HasValue || _stage.IsActive(owner.Handle.Value)))))
                .Where(message => message.ReferenceCount > 0).ToArray())
            : Array.Empty<BattleStageMessageSnapshot>();

        private void ShowStageMessage(object owner, BattleSide side, string text = null, string languageKey = null,
            bool allowTrail = false, BattleStageHandle? handle = null)
        {
            if (_stageMessageOwners.ContainsKey(owner))
                return;
            var message = new StageMessage
            {
                Side = side,
                Text = text,
                LanguageKey = languageKey,
                AllowTrail = allowTrail,
                Handle = handle,
                ReferenceId = checked(++_nextStageMessage)
            };
            _stageMessageOwners.Add(owner, message);
            _stageMessages[(int)side] = message;
        }

        private void HideStageMessage(object owner) => _stageMessageOwners.Remove(owner);

        private void UpdateEncounterStageFeedback()
        {
            foreach (object owner in _stageMessageOwners.Where(pair => pair.Value.Handle.HasValue &&
                !_stage.IsActive(pair.Value.Handle.Value)).Select(pair => pair.Key).ToArray())
                HideStageMessage(owner);
            if (_encounterStageMessageFinished || _scenario.Encounter == BattleEncounter.Normal)
                return;
            if (!_encounterStageMessageStarted)
            {
                _encounterStageMessageStarted = true;
                bool ally = _scenario.Encounter == BattleEncounter.AllyAmbush;
                ShowStageMessage(_encounterStageMessage, ally ? BattleSide.Ally : BattleSide.Enemy,
                    languageKey: ally ? "WeSneakAttackEnemy" : "EnemySneakAttackUs");
            }
            if (BattleStageScheduler.TimeAt(SimulationTick) > 3f)
            {
                HideStageMessage(_encounterStageMessage);
                _encounterStageMessageFinished = true;
            }
        }

        private void ClearStageFeedback()
        {
            _stageMessageOwners.Clear();
            Array.Clear(_stageMessages, 0, _stageMessages.Length);
        }

        // These are captured views of already-advanced tasks. Inspection never
        // starts a task, samples a random stream, or moves either battle clock.
        public IReadOnlyList<BattleAbilityPresentationSnapshot> AbilityPresentations =>
            Array.AsReadOnly(CaptureAbilityPresentations().OrderBy(value => value.StageSlot).ToArray());

        private IEnumerable<BattleAbilityPresentationSnapshot> CaptureAbilityPresentations()
        {
            if (Status != BattleSessionStatus.Running)
                yield break;
            double time = BattleStageScheduler.TimeAt(SimulationTick);
            foreach (var pair in _abilityPresentations)
            {
                AbilityPresentationRun stage = pair.Value;
                Actor actor = stage.Owner;
                ActionRun run = actor.Run;
                if (run?.Id != pair.Key)
                    continue;
                AbilityPresentationSubtask name = stage.Steps[(int)BattleAbilityStageStep.Name];
                AbilityPresentationSubtask lineup = stage.Steps[(int)BattleAbilityStageStep.Lineup];
                yield return new BattleAbilityPresentationSnapshot(run.Id, actor.Initial.InstanceId,
                    actor.Initial.Side, NativeActorIndex(actor), actor.Slot, run.StageHandle?.Slot ?? int.MaxValue,
                    run.Ability, name.Status == 1, Math.Max(0, time - name.Started),
                    stage.Profile.Kind == BattleAbilityStageKind.Magic && lineup.Status == 1,
                    Math.Max(0, time - lineup.Started), stage.Profile.LineupSeconds);
            }
            foreach (var pair in _magicSwordStages)
            {
                MagicSwordStageRun stage = pair.Value;
                Actor actor = _byId[stage.Actor];
                ActionRun run = actor.Run;
                if (run?.Id != pair.Key)
                    continue;
                double elapsed = Math.Max(0, time - stage.NameStarted);
                yield return new BattleAbilityPresentationSnapshot(run.Id, actor.Initial.InstanceId,
                    actor.Initial.Side, NativeActorIndex(actor), actor.Slot, run.StageHandle?.Slot ?? int.MaxValue,
                    run.Ability, stage.NameShown && elapsed <= BattleMagicSwordStageProfile.NameDelaySeconds,
                    elapsed);
            }
        }
    }

    public sealed class BattleStageMessageSnapshot
    {
        public BattleSide Side
        {
            get;
        }

        public string Text
        {
            get;
        }

        public string LanguageKey
        {
            get;
        }

        public bool AllowTrail
        {
            get;
        }

        public int ReferenceCount
        {
            get;
        }

        public long ReferenceId
        {
            get;
        }

        internal BattleStageMessageSnapshot(BattleSide side, string text, string languageKey, bool allowTrail, int references, long referenceId = 0)
        {
            Side = side;
            Text = text;
            LanguageKey = languageKey;
            AllowTrail = allowTrail;
            ReferenceCount = references;
            ReferenceId = referenceId;
        }
    }
}
