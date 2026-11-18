// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using K = BattleAiAttemptKind;

    public sealed partial class BattleSession
    {
        private BattleAi _ai;
        private SessionAiPort _aiPort;
        private readonly Dictionary<int, BattleAiCommand> _aiLastCommands = new();
        private readonly Dictionary<(string Actor, int Ability), BattleSkill> _aiSkillAliases = new();
        private BattleCommand? _aiSubmitting;
        private BattleAiCommand _aiRepeating;
        private bool _aiPrevalidated;
        private int _fiveSpiritElement, _fiveSpiritForce, _fiveSpiritRoundActor, _fiveSpiritRounds;

        public int FiveSpiritElement => _fiveSpiritElement;

        public int FiveSpiritForce => _fiveSpiritForce;

        private void InitializeAi()
        {
            if (Definitions == null || _ai != null)
                return;
            _aiPort = new SessionAiPort(this);
            _ai = new BattleAi(Definitions.Variant == "PAL3A", _random, _aiPort);
            for (int index = 5; index < 11; index++)
            {
                _ai.Roles[index].Profile.SetEnabled(true);
                _ai.Roles[index].Profile.Negative = true;
            }
            _automatic[(int)BattleSide.Enemy] = true;
            _fiveSpiritElement = _scenario.FiveSpiritElement;
            _fiveSpiritForce = _scenario.FiveSpiritForce;
            ResetAiSceneAnchor(true);
            RefreshAiSnapshot();
        }

        private void SetOriginalAutomatic(BattleSide side, bool enabled)
        {
            if (Definitions == null)
                return;
            InitializeAi();
            if (side == BattleSide.Ally)
                _ai.SetAutomatic(enabled);
            else
            {
                for (int index = 5; index < 11; index++)
                    _ai.Roles[index].Profile.SetEnabled(enabled);
            }
            _automatic[(int)side] = enabled;
        }

        private bool IsAiEnabled(Actor actor)
        {
            if (Definitions == null)
                return _automatic[(int)actor.Initial.Side];
            InitializeAi();
            return _ai.Roles[NativeActorIndex(actor)].Profile.Enabled;
        }

        public int GetAiProfile(string actorId)
        {
            InitializeAi();
            if (_ai == null || actorId == null || !_byId.TryGetValue(actorId, out Actor actor))
                throw new ArgumentException("A captured battle actor is required.", nameof(actorId));
            BattleAiProfile profile = _ai.Roles[NativeActorIndex(actor)].Profile;
            return (profile.Attack ? 1 : 0) | (profile.Magic ? 2 : 0) | (profile.Skill ? 4 : 0) | (profile.Item ? 8 : 0) |
                (profile.Negative ? 16 : 0) | (profile.Enabled ? 32 : 0);
        }

        public void SetAiProfile(string actorId, int flags)
        {
            if (flags < 0 || flags > 63)
                throw new ArgumentOutOfRangeException(nameof(flags));
            int current = GetAiProfile(actorId);
            if (Status != BattleSessionStatus.Running || current == flags)
                return;
            ApplyAiProfile(_byId[actorId], flags);
            _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.AiProfile, Actor = actorId, AiFlags = flags });
            Add(BattleEventKind.ControlChanged, actorId, detail: "original AI flags " + flags);
        }

        private void ApplyAiProfile(Actor actor, int flags)
        {
            BattleAiProfile profile = _ai.Roles[NativeActorIndex(actor)].Profile;
            profile.Attack = (flags & 1) != 0;
            profile.Magic = (flags & 2) != 0;
            profile.Skill = (flags & 4) != 0;
            profile.Item = (flags & 8) != 0;
            profile.Negative = (flags & 16) != 0;
            profile.Enabled = (flags & 32) != 0;
        }

        public void SetAiAutoOnly(bool enabled)
        {
            InitializeAi();
            if (_ai == null || Status != BattleSessionStatus.Running || _ai.Automatic == enabled)
                return;
            _ai.SetAutomatic(enabled, true);
            _automatic[(int)BattleSide.Ally] = enabled;
            _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.AiAutoOnly, Automatic = enabled });
            Add(BattleEventKind.ControlChanged, detail: "original system auto only " + enabled);
        }

        public bool RequestAiNormal(string actorId)
        {
            if (!CanRequestAiInput(actorId, out Actor actor))
                return false;
            int index = NativeActorIndex(actor);
            if (index == 2 && _pal3AForms.TryGetValue(actorId, out Pal3AFormState form) &&
                (form.Pending || form.Kind == BattlePal3AFormKind.PengxuFairyBeast))
                return false;
            RefreshAiSnapshot();
            _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.AiNormal, Actor = actorId });
            return _ai.PlayerNormal(index, true);
        }

        public bool RequestAiRepeat(string actorId)
        {
            if (!CanRequestAiInput(actorId, out Actor actor))
                return false;
            // PAL3A checks Pengxu and then Nangong before repeating for the active controller.
            if (Definitions.Variant == "PAL3A" && (!CanRepeatWithTransformedRole(2, actorId) || !CanRepeatWithTransformedRole(0, actorId)))
                return false;
            int index = NativeActorIndex(actor);
            _aiLastCommands.TryGetValue(index, out BattleAiCommand command);
            RefreshAiSnapshot();
            _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.AiRepeat, Actor = actorId });
            return _ai.Repeat(index, command ?? new BattleAiCommand { Source = index, Target = -1 });
        }

        private bool CanRepeatWithTransformedRole(int index, string controller)
        {
            if (!IsPal3ATransformed(ActorAtNativeIndex(index)?.Initial.InstanceId))
                return true;
            bool fairyBeast = index == 2;
            if (!_aiLastCommands.TryGetValue(index, out BattleAiCommand command))
                return !fairyBeast;
            bool forbidden = fairyBeast ? command.Kind is 0x103 or 0x104 or 0x106 : command.Kind is 0x104 or 0x105 or 0x106;
            if (!forbidden)
                return true;
            _aiLastCommands.Remove(index);
            _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.AiRepeat, Actor = controller });
            return false;
        }

        public bool InspectMagicMenuAbility(string actorId, int abilityId)
        {
            if (Definitions?.Variant != "PAL3A" || _fiveSpiritElement == 0 ||
                !CanRequestAiInput(actorId, out Actor actor) || !_aiLastCommands.ContainsKey(NativeActorIndex(actor)) ||
                !Definitions.Skills.TryGetValue(abilityId, out BattleAbilityDefinition ability))
                return false;
            var spell = new BattleCommand(actorId, BattleAction.Skill, abilityId: abilityId);
            ValidateAbility(actor, spell, ability, out bool forbiddenByField);
            if (!forbiddenByField || !_aiLastCommands.Remove(NativeActorIndex(actor)))
                return false;
            // The original spell list invokes the same ability check as the AI.
            // Record the state change without making menu rendering mutate battle state.
            _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.MagicMenuFieldDenial, Actor = actorId, AbilityId = abilityId });
            return true;
        }

        private bool CanRequestAiInput(string actorId, out Actor actor)
        {
            InitializeAi();
            actor = null;
            return _ai != null && actorId != null && WaitingActor == actorId && _byId.TryGetValue(actorId, out actor) &&
                Status == BattleSessionStatus.Running && !_resolvingOutcome && ActiveFormationController == null &&
                actor.Initial.Side == BattleSide.Ally && actor.Alive && !actor.Escaped && actor.Ready && actor.Run == null &&
                ActionBodyIdle(actor) && !IsUncontrollable(actor);
        }

        private void PrepareAiActions()
        {
            foreach (Actor actor in _actors)
            {
                ActionRun run = actor.Run;
                if (run?.Ability?.IsMagic != true || run.ElementWheel || run.CreatedTick >= SimulationTick || !Has(actor, BattleState.Seal) ||
                    run.SystemStarted && !run.Casting || IsCoAttackRun(run))
                    continue;
                // Native MagicWait substitutes a combination before testing the seal state.
                if (!run.SystemStarted && TryReplaceWithCoAttack(actor, run))
                    continue;
                Cancel(actor, "封 interrupted original magic wait");
                actor.Gauge = actor.CastGauge = 0;
                actor.Ready = false;
            }
        }

        private void RunAi()
        {
            InitializeAi();
            if (_ai == null || _resolvingOutcome || ActiveFormationController != null)
                return;
            RefreshAiSnapshot();
            _ai.UpdateDemands();
            for (int index = 0; index < 11; index++)
            {
                Actor actor = ActorAtNativeIndex(index);
                if (actor == null || !actor.Alive || actor.Escaped || !actor.Ready || actor.Run != null ||
                    !ActionBodyIdle(actor))
                    continue;
                if (Has(actor, BattleState.Paralysis) || Has(actor, BattleState.Sleep))
                {
                    AdvanceFiveSpiritFieldTurn(actor);
                    actor.Gauge = 0;
                    actor.Ready = false;
                    continue;
                }
                if (!_ai.Roles[index].Profile.Enabled && !Has(actor, BattleState.Chaos) && !Has(actor, BattleState.Madness))
                {
                    if (index < 5)
                        return; // ProcControl activates the first manual player before scanning later roles.
                    continue;
                }
                if (index >= 5)
                    _ai.HandleEnemy(index);
                else
                    _ai.HandleFriend(index);
            }
        }

        private void RefreshAiSnapshot()
        {
            for (int index = 0; index < 11; index++)
            {
                Actor actor = ActorAtNativeIndex(index);
                BattleAiProfile profile = _ai.Roles[index].Profile;
                if (actor == null)
                {
                    _ai.Roles[index] = new BattleAiRole { Profile = profile };
                    continue;
                }
                int[] bad = actor.States.Keys.Where(AiBadState).Select(state => (int)state + 1).OrderBy(state => state).ToArray();
                _ai.Roles[index] = new BattleAiRole
                {
                    Valid = true,
                    Alive = actor.Alive,
                    Fled = actor.Escaped,
                    Fleeing = IsFleeing(actor),
                    Boss = actor.Initial.Side == BattleSide.Enemy && actor.Definition.Boss,
                    EnemyFriend = actor.Initial.InstanceId == _scenario.EnemyAiIgnoredActor || Definitions.Variant == "PAL3" && index == 3 &&
                        _actors.Any(enemy => enemy.Initial.Side == BattleSide.Enemy && enemy.Initial.ActorId == 708),
                    Identity = actor.Initial.ActorId,
                    Hp = actor.Hp,
                    MaxHp = actor.Attributes[0],
                    Mp = actor.Mp,
                    MaxMp = actor.Attributes[2],
                    Chaos = Has(actor, BattleState.Chaos),
                    Madness = Has(actor, BattleState.Madness),
                    Seal = Has(actor, BattleState.Seal),
                    BadState = bad.Length > 0,
                    // The released getter checks role zero's alive flag before scanning any target.
                    WorstBadState = ActorAtNativeIndex(0)?.Alive == true ? bad.FirstOrDefault() : 0,
                    Profile = profile,
                    Skills = CaptureAiSkills(actor)
                };
            }
            _ai.Items = _inventoryOrder.Where(Definitions.Items.ContainsKey).Select(id =>
                CaptureAiSkill(Definitions.Items[id], null, _inventory[id])).ToArray();
        }

        private static bool AiBadState(BattleState state) => state <= BattleState.PoisonResist && state is not
            (BattleState.AttackIncrease or BattleState.DefenseIncrease or BattleState.LuckIncrease or BattleState.SpeedIncrease or
            BattleState.Reflection or BattleState.Evade or BattleState.Barrier or BattleState.Invisible or BattleState.PoisonResist);

        private BattleAiSkill[] CaptureAiSkills(Actor actor)
        {
            if (actor.Initial.Side == BattleSide.Enemy)
                return actor.Skills.Take(4).Select(skill => Definitions.Skills.TryGetValue(skill.Id, out BattleAbilityDefinition ability)
                    ? CaptureAiSkill(ability, skill) : new BattleAiSkill { Id = skill.Id, Level = skill.Level }).ToArray();
            var learned = actor.Skills.ToDictionary(skill => skill.Id);
            BattleAbilityDefinition[] magic = Definitions.SkillsInOriginalOrder.Where(ability => ability.IsMagic && learned.ContainsKey(ability.Id) && AiMagicGroup(ability.ElementMask) >= 0)
                .OrderBy(ability => AiMagicGroup(ability.ElementMask)).ThenBy(ability => ability.Id).ToArray();
            var magicIds = new HashSet<int>(magic.Select(ability => ability.Id));
            BattleAbilityDefinition[] nativeMagic = BattleMagicSlots.Capture(Definitions.Variant, Definitions, actor.Definition.Identity, actor.Skills)
                .Where(slot => magicIds.Contains(slot.SkillId)).Select(slot => slot.Definition).ToArray();
            var nativeIds = new HashSet<int>(nativeMagic.Select(ability => ability.Id));
            int nativeIndex = 0;
            // Native selectors scan captured group/slot order. Keep combat-only
            // and synthetic abilities in their existing positions outside that catalogue.
            for (int index = 0; index < magic.Length; index++)
                if (nativeIds.Contains(magic[index].Id))
                    magic[index] = nativeMagic[nativeIndex++];
            IEnumerable<BattleAbilityDefinition> techniques = Definitions.SkillsInOriginalOrder.Where(ability => !ability.IsMagic && !ability.IsItem &&
                (ability.SkillTypeId == 0 || ability.SkillTypeId < 0) && learned.ContainsKey(ability.Id));
            return magic.Concat(techniques).Select(ability => CaptureAiSkill(ability, learned[ability.Id])).ToArray();
        }

        private static int AiMagicGroup(int mask)
        {
            if (mask == 0)
                return -1;
            if ((mask & (mask - 1)) != 0)
                return 5;
            return mask switch
            {
                1 => 0,
                2 => 1,
                8 => 2,
                4 => 3,
                16 => 4,
                _ => -1
            };
        }

        private static BattleAiSkill CaptureAiSkill(BattleAbilityDefinition ability, BattleSkill learned, int count = 1)
        {
            BattleAttributeEffect hp = ability.Attributes.FirstOrDefault(effect => effect.Attribute == 0);
            var states = new int[32];
            foreach (BattleStatusEffect effect in ability.States)
                states[(int)effect.State] = effect.Remove ? 2 : 1;
            return new BattleAiSkill
            {
                Id = ability.Id,
                Type = ability.IsItem ? ability.ItemType : ability.SkillTypeId >= 0 ? ability.SkillTypeId : ability.IsMagic ? hp.Value < 0 ? 2 : hp.Value > 0 ? 3 : 1 : 0,
                Range = (int)ability.Range,
                Power = hp.Value,
                PowerMode = (int)hp.Mode,
                ElementMask = ability.ElementMask,
                Level = learned?.Level ?? 1,
                Count = count,
                Mastered = learned?.Mastered ?? true,
                IsMagic = ability.IsMagic,
                IsItem = ability.IsItem,
                NullCureOperation = ability.IsItem ? ability.AiNullCureOperation : ability.SuccessRate,
                StateOperations = states
            };
        }

        private int AiAverage(BattleSide side, bool maximum = false, bool missing = false)
        {
            Actor[] members = _actors.Where(actor => actor.Initial.Side == side && actor.Alive).ToArray();
            if (members.Length == 0)
                return missing ? 99999 : 0;
            return members.Sum(actor => missing ? actor.Attributes[0] - actor.Hp : maximum ? actor.Attributes[0] : actor.Hp) / members.Length;
        }

        private bool AiPrevalidatedAbility(Actor actor, BattleAbilityDefinition ability) => _aiPrevalidated &&
            _aiSubmitting is BattleCommand command && command.Actor == actor.Initial.InstanceId && command.AbilityId == ability.Id;

        private BattleSkill AiSkill(Actor actor, int abilityId) => actor.Skills.FirstOrDefault(skill => skill.Id == abilityId) ??
            (_aiSkillAliases.TryGetValue((actor.Initial.InstanceId, abilityId), out BattleSkill alias) ? alias : null);

        private bool TryAiTargetCandidates(BattleCommand command, out IReadOnlyList<BattleActorSnapshot> candidates)
        {
            candidates = null;
            if (_aiSubmitting is not BattleCommand active || active.Actor != command.Actor || active.Action != command.Action ||
                active.AbilityId != command.AbilityId || active.Target != command.Target)
                return false;
            BattleAbilityDefinition ability = Ability(command);
            Actor source = _byId[command.Actor];
            BattleTargetRange range = ActionRange(source, ability);
            if (range == BattleTargetRange.None && Definitions.Variant == "PAL3A" && command.AbilityId is >= 264 and <= 268)
            {
                candidates = Array.AsReadOnly(new[] { Snapshot(_byId[command.Actor]) });
                return true;
            }
            if (range == BattleTargetRange.None)
                return false;
            if (ability != null && range is BattleTargetRange.OwnAll or BattleTargetRange.OpponentAll)
            {
                candidates = Array.AsReadOnly(FightSkillRange(source, range).Select(Snapshot).ToArray());
                return true;
            }
            if (command.Target == null || !_byId.TryGetValue(command.Target, out Actor anchor))
                return false;
            // AI has already chosen its primary role, including same-side targets under chaos.
            // GetNormalAttackRange expands around that role without the manual selection filters.
            candidates = range is BattleTargetRange.OwnSingle or BattleTargetRange.OpponentSingle
                ? Array.AsReadOnly(new[] { Snapshot(anchor) })
                : Array.AsReadOnly(_actors.OrderBy(NativeActorIndex).Where(actor => actor.Initial.Side == anchor.Initial.Side &&
                    actor.Alive && !actor.Escaped && !IsFleeing(actor)).Select(Snapshot).ToArray());
            return true;
        }

        private bool TryAiCapturedTargets(BattleCommand command, out string[] targets)
        {
            targets = null;
            if (_aiRepeating == null || _aiSubmitting is not BattleCommand active || active.Actor != command.Actor ||
                active.Action != command.Action || active.AbilityId != command.AbilityId || active.Target != command.Target)
                return false;
            if (_aiRepeating.MultipleTargets)
                targets = Enumerable.Range(0, 11).Where(index => _aiRepeating.TargetMask[index]).Select(ActorAtNativeIndex)
                    .Where(actor => actor != null).Select(actor => actor.Initial.InstanceId).ToArray();
            else
                targets = command.Target == null ? Array.Empty<string>() : new[] { command.Target };
            return true;
        }

        private long SubmitAiCommand(BattleCommand command, bool prevalidated)
        {
            BattleCommand? previous = _aiSubmitting;
            bool oldPrevalidated = _aiPrevalidated;
            _aiSubmitting = command;
            _aiPrevalidated = prevalidated;
            if (prevalidated && command.Action == BattleAction.Skill && !_byId[command.Actor].Skills.Any(skill => skill.Id == command.AbilityId))
                _aiSkillAliases[(command.Actor, command.AbilityId)] = new BattleSkill
                {
                    Id = command.AbilityId,
                    Mastered = true,
                    Level = command.AbilityId is 207 or 215 or 224 or 232 or 240 ? 1 : 4
                };
            try
            {
                if (!SubmitCommand(command, true))
                    return -1;
                AdvanceFiveSpiritFieldTurn(_byId[command.Actor]);
                return _byId[command.Actor].Run.Id;
            }
            finally
            {
                _aiSubmitting = previous;
                _aiPrevalidated = oldPrevalidated;
            }
        }

        private void RememberAiCommand(BattleCommand command)
        {
            if (Definitions == null || !_byId.TryGetValue(command.Actor, out Actor actor) || actor.Initial.Side != BattleSide.Ally)
                return;
            InitializeAi();
            int index = NativeActorIndex(actor);
            _ai.NewRole[index] = false;
            // ProcControl dispatches the sword thread without replacing LastRoleCmd.
            if (IsMagicSwordAbility(Ability(command)))
                return;
            int kind = command.Action switch
            {
                BattleAction.Attack => 0x103,
                BattleAction.Skill => Ability(command)?.IsMagic == true ? 0x105 : 0x104,
                BattleAction.Item => 0x106,
                BattleAction.Defend => 0x109,
                BattleAction.Flee => 0x10b,
                _ => 0
            };
            var stored = new BattleAiCommand
            {
                Source = index,
                Kind = kind,
                AbilityId = command.AbilityId,
                Target = command.Target != null && _byId.TryGetValue(command.Target, out Actor target) ? NativeActorIndex(target) : -1
            };
            if (command.Action is BattleAction.Attack or BattleAction.Skill or BattleAction.Item)
            {
                BattleTargetRange range = ActionRange(actor, Ability(command));
                stored.MultipleTargets = range is not (BattleTargetRange.OwnSingle or BattleTargetRange.OpponentSingle);
                foreach (string targetId in actor.Run.Targets)
                    stored.TargetMask[NativeActorIndex(_byId[targetId])] = true;
                if (stored.MultipleTargets)
                    stored.Target = -1;
            }
            _aiLastCommands[index] = stored;
        }

        private void ResolveAiSceneMagic(Actor source, BattleAbilityDefinition ability)
        {
            if (Definitions.Variant != "PAL3A" || ability.Id < 264 || ability.Id > 268)
                return;
            int group = AiMagicGroup(ability.ElementMask);
            if (group < 0 || group >= 5)
                throw new NotSupportedException("A five-spirit field spell needs one original element.");
            _fiveSpiritElement = ability.ElementMask switch
            {
                1 => 1,
                2 => 2,
                4 => 3,
                8 => 4,
                16 => 5,
                _ => 0
            };
            _fiveSpiritForce = source.Initial.Side == BattleSide.Ally ? 1 : 2;
            ResetAiSceneAnchor(true);
            Add(BattleEventKind.ControlChanged, source.Initial.InstanceId, source.Run?.Target, source.Run?.Id ?? 0,
                "five-spirit field " + _fiveSpiritElement + ":" + _fiveSpiritForce);
        }

        private void ResetAiSceneAnchor(bool resetRounds)
        {
            _fiveSpiritRoundActor = Enumerable.Range(0, 5).FirstOrDefault(index => ActorAtNativeIndex(index)?.Alive == true);
            if (resetRounds)
                _fiveSpiritRounds = 0;
        }

        private void AdvanceFiveSpiritFieldTurn(Actor actor)
        {
            if (Definitions?.Variant != "PAL3A" || _fiveSpiritElement == 0 || actor.Initial.Side != BattleSide.Ally)
                return;
            if (ActorAtNativeIndex(_fiveSpiritRoundActor)?.Alive != true)
                ResetAiSceneAnchor(false);
            if (NativeActorIndex(actor) != _fiveSpiritRoundActor || ++_fiveSpiritRounds < 5)
                return;
            _fiveSpiritElement = _fiveSpiritForce = 0;
            Add(BattleEventKind.ControlChanged, actor.Initial.InstanceId, detail: "five-spirit field expired");
        }

        private sealed class SessionAiPort : BattleAiPort
        {
            private readonly BattleSession _session;

            public SessionAiPort(BattleSession session) => _session = session;

            public override int SceneFiveSpiritMode => _session._fiveSpiritElement;

            public override int SceneFiveSpiritOwner => _session._fiveSpiritForce;

            public override bool CanAttackBack(int source, int target) => _session.CanAttackBack(_session.ActorAtNativeIndex(source), _session.ActorAtNativeIndex(target));

            public override bool IsActionActive(long actionId) => _session._actors.Any(actor => actor.Run?.Id == actionId);

            public override bool CanCast(int source, int skillId)
            {
                Actor actor = _session.ActorAtNativeIndex(source);
                if (actor == null)
                    return false;
                bool item = _session.Definitions.Items.TryGetValue(skillId, out BattleAbilityDefinition ability);
                if (!item && !_session.Definitions.Skills.TryGetValue(skillId, out ability))
                    return false;
                var command = new BattleCommand(actor.Initial.InstanceId, item ? BattleAction.Item : BattleAction.Skill, abilityId: skillId);
                string denial = _session.ValidateAbility(actor, command, ability, out bool forbiddenByField);
                if (forbiddenByField)
                    _session._aiLastCommands.Remove(source);
                return denial == null;
            }

            public override long TryAttempt(BattleAiAttempt attempt)
            {
                BattleAi ai = _session._ai;
                Actor source = _session.ActorAtNativeIndex(attempt.Source);
                Actor target = attempt.Target >= 0 ? _session.ActorAtNativeIndex(attempt.Target) : null;
                if (source == null || !ai.CapabilityEnabled(attempt.Source, attempt.Kind))
                    return -1;
                K kind = attempt.Kind;
                if (kind == K.SpecAI)
                {
                    if (attempt.AbilityId != 7709 || !BattleAi.IsSpecialBossReady(ai.Roles[attempt.Source]) || _session._effects == null)
                        return -1;
                    long special = Submit(source, null, 7709, false, true);
                    if (special >= 0)
                        _session.StartEffect(new BattleEffectRequest(559, source.Initial.InstanceId, source.Run.Targets, source.Position, unchecked((uint)_session._scenario.Seed)));
                    return special;
                }
                if (!source.Alive)
                    return -1;
                if (kind == K.Normal)
                {
                    if (target == null || !ai.CanNormalAttack(attempt.Source, attempt.Target, Has(target, BattleState.Invisible), Has(target, BattleState.Evade), Has(target, BattleState.Reflection), _session.IsAtHome(target)))
                        return -1;
                    return _session.SubmitAiCommand(new BattleCommand(source.Initial.InstanceId, BattleAction.Attack, target.Initial.InstanceId), false);
                }
                if (kind == K.BossPhysical)
                    return -1;
                bool group = kind is K.RecoverBigMagic or K.AttackBigMagic or K.RecoverBigSkill or K.AttackBigSkill or K.RecoverBigItem or K.AttackBigItem or K.FiveSpiritBigMagic;
                bool item = kind is K.RecoverItem or K.RecoverBigItem or K.AttackItem or K.AttackBigItem or K.ReviveItem or K.CureItem;
                bool revive = kind is K.ReviveMagic or K.ReviveSkill or K.ReviveItem;
                bool selected = kind is K.SelectedMiscMagic or K.FiveSpiritMagic or K.FiveSpiritBigMagic;
                if (!group && !selected && (target == null || (revive ? target.Alive : !target.Alive)))
                    return -1;
                bool pal3A = _session.Definitions.Variant == "PAL3A";
                if (pal3A && kind is K.RecoverMagic or K.AttackMagic or K.CureMagic && (target.Escaped || _session.IsFleeing(target)))
                    return -1;
                BattleSide opposite = source.Initial.Side == BattleSide.Ally ? BattleSide.Enemy : BattleSide.Ally;
                int weight = 0;
                int abilityId;
                if (kind == K.SelectedMiscMagic)
                {
                    if (_session.AiAverage(opposite) == 0)
                        return -1;
                    abilityId = attempt.AbilityId;
                }
                else if (kind is K.FiveSpiritMagic or K.FiveSpiritBigMagic)
                {
                    weight = group ? _session.AiAverage(opposite) : 0;
                    if (group && weight == 0)
                        return -1;
                    abilityId = ai.SelectFiveSpiritMagic(attempt.Source, group, weight);
                }
                else if (kind == K.RandomMiscMagic)
                {
                    if (target.Hp <= 0)
                        return -1;
                    abilityId = ai.SelectMiscMagic(attempt.Source, false);
                }
                else
                {
                    if (kind is K.RecoverMagic or K.RecoverSkill or K.RecoverItem)
                        weight = target.Attributes[0] - target.Hp;
                    else if (kind is K.RecoverBigMagic or K.RecoverBigSkill or K.RecoverBigItem)
                        weight = _session.AiAverage(source.Initial.Side, missing: true);
                    else if (revive)
                        weight = source.Attributes[0];
                    else if (kind is K.AttackMagic or K.AttackSkill or K.AttackItem)
                    {
                        if (target.Hp <= 0)
                            return -1;
                        weight = kind == K.AttackMagic ? target.Hp : _session._random.Next() % target.Hp;
                    }
                    else if (kind == K.AttackBigSkill)
                    {
                        int maximum = _session.AiAverage(opposite, maximum: true);
                        if (maximum <= 0)
                            return -1;
                        weight = _session._random.Next() % maximum;
                    }
                    else if (kind is K.AttackBigMagic or K.AttackBigItem)
                    {
                        weight = _session.AiAverage(opposite);
                        if (kind == K.AttackBigMagic && weight == 0)
                            return -1;
                    }
                    abilityId = kind is K.RecoverMagic or K.RecoverBigMagic or K.AttackMagic or K.AttackBigMagic
                        ? ai.SelectMagic(attempt.Source, kind is K.AttackMagic or K.AttackBigMagic, group, weight)
                        : ai.SelectSupport(attempt.Source, NormalizeSupportKind(kind), group, weight, attempt.Demand?.State ?? 0);
                }
                if (abilityId == 0)
                    return -1;
                if (kind == K.AttackMagic)
                    abilityId = ai.ConvertSingleAttackMagic(abilityId);
                if (pal3A && kind is K.AttackSkill or K.AttackBigSkill)
                    _session.ResumeOriginalAtb();
                return Submit(source, target, abilityId, item, true);
            }

            private static K NormalizeSupportKind(K kind) => kind switch
            {
                K.RecoverBigSkill => K.RecoverSkill,
                K.AttackBigSkill => K.AttackSkill,
                K.RecoverBigItem => K.RecoverItem,
                K.AttackBigItem => K.AttackItem,
                _ => kind
            };

            private long Submit(Actor source, Actor target, int abilityId, bool item, bool prevalidated)
                => _session.SubmitAiCommand(new BattleCommand(source.Initial.InstanceId, item ? BattleAction.Item : BattleAction.Skill, target?.Initial.InstanceId, abilityId), prevalidated);

            public override void RecordPlayerNormal(int source, int target)
                => _session._aiLastCommands[source] = new BattleAiCommand { Kind = 0x103, Source = source, Target = target };

            public override bool CanRepeat(int source, BattleAiCommand command)
            {
                if (command.Kind == 0x10f || !TryCommand(command, out BattleCommand translated))
                    return false;
                string denial = _session.ValidateAbility(_session.ActorAtNativeIndex(source), translated, _session.Ability(translated),
                    out bool forbiddenByField);
                if (forbiddenByField)
                    _session._aiLastCommands.Remove(source);
                return denial == null;
            }

            public override bool AllRepeatTargetsDead(BattleAiCommand command) => _session._ai.RepeatTargetsDead(command);

            public override bool IsRepeatAttack(BattleAiCommand command)
            {
                bool item = command.Kind == 0x106;
                var definitions = item ? _session.Definitions.Items : _session.Definitions.Skills;
                if (!definitions.TryGetValue(command.AbilityId, out BattleAbilityDefinition ability))
                    return false;
                if (_session.Definitions.Variant == "PAL3A")
                    return item ? ability.ItemType == 2 : ability.Range > BattleTargetRange.OwnAll;
                BattleAttributeEffect hp = ability.Attributes.FirstOrDefault(effect => effect.Attribute == 0);
                return item ? ability.ItemType == 2 && hp.Mode is BattleValueMode.Absolute or BattleValueMode.Percentage && hp.Value < 0
                    : command.Kind == 0x105 ? ability.SkillTypeId == 2 : hp.Value < 0;
            }

            public override int PickRepeatTarget(int source, bool enemy, bool filterBackRole)
                => _session._ai.SelectRepeatTarget(source, enemy, filterBackRole);

            public override void ExpandRepeatRange(BattleAiCommand command)
            {
                if (!TryCommand(command, out BattleCommand translated) || !_session._byId.TryGetValue(translated.Actor, out Actor source))
                    return;
                BattleAbilityDefinition ability = _session.Ability(translated);
                BattleTargetRange range = _session.ActionRange(source, ability);
                command.MultipleTargets = range is not (BattleTargetRange.OwnSingle or BattleTargetRange.OpponentSingle);
                if (!command.MultipleTargets)
                    return;
                Actor anchor = _session.ActorAtNativeIndex(command.Target);
                IEnumerable<Actor> targets = command.Kind is 0x104 or 0x105 or 0x106
                    ? _session.FightSkillRange(source, range)
                    : _session._actors.Where(actor => actor.Initial.Side == anchor?.Initial.Side &&
                        actor.Alive && !actor.Escaped && !_session.IsFleeing(actor));
                if (range is BattleTargetRange.OpponentRow or BattleTargetRange.OpponentColumn)
                {
                    if (anchor == null)
                        return;
                    targets = targets.Where(actor => BattleTargetRules.Aligned(anchor.Initial.Side, anchor.Slot, actor.Slot,
                        range == BattleTargetRange.OpponentRow, _session._scenario.SceneElement));
                }
                foreach (Actor target in targets)
                    command.TargetMask[NativeActorIndex(target)] = true;
                command.Target = -1;
            }

            public override long SubmitRepeat(BattleAiCommand command)
            {
                if (!TryCommand(command, out BattleCommand translated))
                    return -1;
                BattleAiCommand previous = _session._aiRepeating;
                _session._aiRepeating = command;
                try
                {
                    return _session.SubmitAiCommand(translated, command.Kind is 0x104 or 0x105 or 0x106);
                }
                finally
                {
                    _session._aiRepeating = previous;
                }
            }

            private bool TryCommand(BattleAiCommand command, out BattleCommand translated)
            {
                translated = default;
                Actor source = _session.ActorAtNativeIndex(command.Source);
                if (source == null || command.Kind is not (0x103 or 0x104 or 0x105 or 0x106 or 0x109 or 0x10b))
                    return false;
                BattleAction action = command.Kind switch
                {
                    0x103 => BattleAction.Attack,
                    0x106 => BattleAction.Item,
                    0x109 => BattleAction.Defend,
                    0x10b => BattleAction.Flee,
                    _ => BattleAction.Skill
                };
                Actor target = _session.ActorAtNativeIndex(command.Target);
                int abilityId = action is BattleAction.Skill or BattleAction.Item ? command.AbilityId : 0;
                translated = new BattleCommand(source.Initial.InstanceId, action, target?.Initial.InstanceId, abilityId);
                if (command.MultipleTargets && target == null &&
                    (action == BattleAction.Attack || _session.GetActionTargetRange(translated) is BattleTargetRange.OpponentRow or BattleTargetRange.OpponentColumn))
                {
                    // Native group repeats store only a mask. Supply a legal anchor to the
                    // command API; the physical stage and damage both use the recorded mask.
                    target = _session._actors.OrderBy(NativeActorIndex).FirstOrDefault(actor => command.TargetMask[NativeActorIndex(actor)] &&
                        actor.Alive && !actor.Escaped && !_session.IsFleeing(actor));
                    translated = new BattleCommand(source.Initial.InstanceId, action, target?.Initial.InstanceId, abilityId);
                }
                return true;
            }
        }
    }
}
