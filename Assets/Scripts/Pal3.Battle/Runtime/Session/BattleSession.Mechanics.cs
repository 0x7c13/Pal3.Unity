// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed partial class BattleSession
    {
        private BattleScenario _scenario;
        private BattleStatusRules _statusRules;
        private BattleEffectRules _effectRules;
        private int _money;
        private bool _partyEscaped;
        private readonly Dictionary<int, int> _inventory = new();

        public BattleDefinitions Definitions
        {
            get; private set;
        }

        public int Money => _money;

        public IReadOnlyList<BattleItemSnapshot> Inventory => Array.AsReadOnly(_inventory.OrderBy(pair => pair.Key)
            .Where(pair => pair.Value > 0).Select(pair => new BattleItemSnapshot(pair.Key, pair.Value)).ToArray());

        private void InitializeMechanics(BattleScenario scenario, BattleDefinitions definitions)
        {
            _scenario = scenario;
            Definitions = definitions;
            _money = scenario.Money;
            InitializePal3AState(scenario);
            InitializeSpecialResources(scenario);
            foreach (BattleItem item in scenario.Items)
                _inventory.Add(item.Id, item.Count);
            if (definitions == null)
                return;
            if (definitions.Variant != scenario.Variant || definitions.Fingerprint != scenario.DataFingerprint)
                throw new ArgumentException("Combat definitions do not match the captured scenario variant/data.");
            _statusRules = new BattleStatusRules(definitions.Variant);
            _effectRules = new BattleEffectRules(definitions.Variant);
        }

        private void InitializeActorMechanics(Actor actor)
        {
            actor.Skills.AddRange(actor.Initial.Skills.Select(skill => skill.Copy()));
            if (Definitions == null)
            {
                InitializeActorMagicSword(actor);
                return;
            }
            if (!Definitions.Actors.TryGetValue(actor.Initial.InstanceId, out BattleActorDefinition definition))
                throw new ArgumentException("Missing combat actor definition: " + actor.Initial.InstanceId);
            actor.Definition = definition;
            InitializeActorMagicSword(actor);
            InitializeActorSpecials(actor);
            if (!string.IsNullOrEmpty(definition.UnsupportedReason))
                throw new NotSupportedException($"Actor {actor.Initial.ActorId}: {definition.UnsupportedReason}");
            foreach (BattleEquipmentDefinition item in definition.Equipment)
                if (!string.IsNullOrEmpty(item.UnsupportedReason))
                    throw new NotSupportedException($"Equipment {item.Id}: {item.UnsupportedReason}");
            actor.Attributes = BattleAttributeCalculator.Calculate(actor.Initial, definition, Definitions.Variant);
            if (actor.Attributes.Length != 12 || actor.Attributes.Take(3).Any(value => value < 0) || actor.Attributes.Any(value => value > 1000000))
                throw new ArgumentException("Invalid derived attributes on " + actor.Initial.InstanceId);
            if (actor.Hp > actor.Attributes[0] || actor.Sp > actor.Attributes[1] || actor.Mp > actor.Attributes[2])
                throw new ArgumentException("Initial vitals exceed the derived maxima on " + actor.Initial.InstanceId);
            foreach (BattleState state in definition.PermanentStates.OrderBy(state => state))
                ApplyState(actor, state, false, null, true, sound: false);
            foreach (BattleState state in definition.InitialStates.OrderBy(state => state))
                ApplyState(actor, state, false, sound: false);
            foreach (BattleStatus initial in actor.Initial.Statuses.OrderBy(state => state.Id))
            {
                if (Enum.IsDefined(typeof(BattleState), initial.Id))
                {
                    actor.States.Remove((BattleState)initial.Id);
                    ApplyState(actor, (BattleState)initial.Id, false, initial.Rounds, sound: false);
                }
                else
                    throw new NotSupportedException("Unknown initial combat state " + initial.Id);
            }
        }

        private static bool Has(Actor actor, BattleState state) => actor.States.ContainsKey(state);

        private static bool IsUncontrollable(Actor actor) => Has(actor, BattleState.Paralysis) || Has(actor, BattleState.Sleep) ||
            Has(actor, BattleState.Chaos) || Has(actor, BattleState.Madness);

        private int[] EffectiveAttributes(Actor actor)
        {
            int[] attributes = actor.Attributes.ToArray();
            if (_statusRules != null)
                for (int index = 3; index <= 6; index++)
                    attributes[index] = _statusRules.ModifyAttribute(index, attributes[index], actor.States.Keys);
            AdjustPal3AEffectiveAttributes(actor, attributes);
            return attributes;
        }

        private BattleParticipant RuleParticipant(Actor actor)
        {
            if (Definitions == null)
                return actor.Initial;
            return new BattleParticipant
            {
                InstanceId = actor.Initial.InstanceId,
                ActorId = actor.Initial.ActorId,
                Side = actor.Initial.Side,
                Slot = actor.Slot,
                Level = actor.Level,
                Attributes = EffectiveAttributes(actor)
            };
        }

        private IReadOnlyList<BattleStatusSnapshot> StatusSnapshots(Actor actor) => Array.AsReadOnly(actor.States.Values.OrderBy(value => value.State)
            .Select(value => new BattleStatusSnapshot
            {
                State = value.State,
                LatentRounds = value.LatentRounds,
                RemainingRounds = value.RemainingRounds,
                Permanent = value.Permanent
            }).ToArray());

        public string CanSubmit(BattleCommand command) => ValidateCommand(command, false);

        private string ValidateCommand(BattleCommand command, bool fromController, bool elementWheel = false)
        {
            if (Status != BattleSessionStatus.Running)
                return "The battle has ended.";
            if (IsBattleInputSuspended)
                return "The battle movie is playing or its frame is resuming.";
            if (_resolvingOutcome)
                return "The encounter outcome is resolving.";
            if (ActiveFormationController != null)
                return "Finish formation control before submitting an action.";
            if (!Enum.IsDefined(typeof(BattleAction), command.Action))
                return "Unsupported command.";
            if (command.Action is not (BattleAction.Skill or BattleAction.Item) && command.AbilityId != 0)
                return "This action cannot carry a skill or item ID.";
            if (command.Actor == null || !_byId.TryGetValue(command.Actor, out Actor actor) || !actor.Alive || actor.Escaped || actor.EnemyFleeing ||
                !actor.Ready || Definitions != null && actor.ProcessedRounds < actor.Rounds || actor.Run != null || !ActionBodyIdle(actor))
                return "Actor is not ready.";
            if (Has(actor, BattleState.Paralysis) || Has(actor, BattleState.Sleep))
                return "定／眠期间不能行动。";
            if (!fromController && (Has(actor, BattleState.Chaos) || Has(actor, BattleState.Madness)))
                return "乱／狂期间由原版异常状态 AI 接管。";
            if (!fromController && !IsAiEnabled(actor) && WaitingActor != command.Actor)
                return "An earlier actor is waiting for input.";
            BattleAbilityDefinition ability = Ability(command);
            string formDenied = elementWheel ? ValidateElementWheelController(actor, command, ability) :
                ValidatePal3AFormCommand(actor, command, ability, fromController);
            if (formDenied != null)
                return formDenied;
            if (command.Action is BattleAction.Defend or BattleAction.Transform)
                return null;
            if (command.Action == BattleAction.Flee)
                return Definitions == null ? "The basic fixture has no escape rules." : actor.Initial.Side != BattleSide.Ally ? "Only the player party can request escape." : Has(actor, BattleState.Forbidden) ? "禁期间不能等待逃跑。" : null;
            if (command.Action is BattleAction.Skill or BattleAction.Item)
            {
                string denied = ValidateAbility(actor, command, ability, out _, elementWheel);
                if (denied != null)
                    return denied;
            }
            else if (command.Action != BattleAction.Attack)
                return "Unsupported command.";
            if (elementWheel)
                return null;
            IReadOnlyList<BattleActorSnapshot> candidates = CommandTargetCandidates(command, fromController);
            BattleTargetRange range = ActionRange(actor, ability);
            bool manualGroup = !fromController && !IsMagicSwordAbility(ability) &&
                range is BattleTargetRange.OwnAll or BattleTargetRange.OpponentAll;
            if (candidates.Count == 0 || manualGroup && !candidates.Any(target => target.Alive))
                return "There is no legal target.";
            if (manualGroup && command.Target != null && !candidates.Any(target => target.Id == command.Target))
                return "Choose a legal target for this action.";
            if (command.Action == BattleAction.Attack || range is BattleTargetRange.OwnSingle or BattleTargetRange.OpponentSingle or BattleTargetRange.OpponentRow or BattleTargetRange.OpponentColumn)
            {
                if (command.Target == null || !candidates.Any(target => target.Id == command.Target))
                    return "Choose a legal target for this action.";
            }
            return null;
        }

        private string ValidateAbility(Actor actor, BattleCommand command, BattleAbilityDefinition ability,
            out bool forbiddenByField, bool elementWheel = false)
        {
            forbiddenByField = false;
            if (Definitions == null || ability == null)
                return "Unknown skill or item.";
            if (!ability.Supported)
                return ability.UnsupportedReason;
            if (_scenario.Variant == "PAL3A" && ability.Id == BattlePal3ARules.SpiritCaptureSkill && _effects is not IBattleSpiritCaptureLayout)
                return "Spirit capture needs the original screen-space wheel layout.";
            if (ability.Attributes.Any(effect => effect.Attribute > 2 && effect.Value != 0))
                return "This attribute effect needs its original dedicated battle handler.";
            if (!Enum.IsDefined(typeof(BattleTargetRange), ability.Range) || ability.States.Any(effect => effect.State == BattleState.Dying && !effect.Remove || effect.State > BattleState.Death))
                return "This ability contains an unverified target range or derived/passive state operation.";
            if (ability.Costs.Any(cost => cost.Value < 0 || !Enum.IsDefined(typeof(BattleValueMode), cost.Mode) || cost.Resource is not (0 or 1 or 2 or 7 or 8 or 10 or 16)))
                return "This ability has an unsupported resource cost.";
            string specialDenied = ValidateSpecialCommand(ability);
            if (specialDenied != null)
                return specialDenied;
            if (!ability.BattleUsable)
                return "This item cannot be used in combat.";
            if (command.Action == BattleAction.Item)
            {
                if (actor.Initial.Side != BattleSide.Ally)
                    return "Original enemies cannot use inventory items.";
                if (Has(actor, BattleState.Forbidden))
                    return "禁期间不能使用物品。";
                int reserved = ability.IsTreasure ? 0 : _actors.Count(other => other.Run is { Kind: BattleAction.Item, ItemConsumed: false } &&
                    other.Run.Ability.Id == ability.Id);
                if (!_inventory.TryGetValue(ability.Id, out int count) || count <= reserved)
                    return "No item remains in this battle inventory.";
            }
            else if (IsMagicSwordAbility(ability))
            {
                string denied = ValidateMagicSwordCommand(actor, ability);
                if (denied != null)
                    return denied;
                if (_effects == null || _catalog is not IBattleMagicSwordTimings)
                    return "The magic sword needs its original effect completion director.";
            }
            else if (!elementWheel && !AiPrevalidatedAbility(actor, ability))
            {
                BattleSkill learned = actor.Skills.FirstOrDefault(skill => skill.Id == ability.Id);
                if (learned == null || actor.Initial.Side == BattleSide.Ally && !learned.Mastered)
                    return "This actor has not mastered the skill.";
                if (ability.ApplicableIdentities.Count > 0 && actor.Initial.Side == BattleSide.Ally && !ability.ApplicableIdentities.Contains(actor.Definition.Identity))
                    return "This skill belongs to another character.";
                if (learned.Level < 1 || learned.Level > 4)
                    return "The original skill proficiency must be 1–4.";
                if (ability.IsMagic && Has(actor, BattleState.Seal))
                    return "封期间不能施放仙术。";
            }
            foreach (BattleCostEstimate cost in elementWheel || AiPrevalidatedAbility(actor, ability)
                ? Array.Empty<BattleCostEstimate>() : GetCostEstimate(command))
                if (cost.Required > cost.Available)
                    return $"Resource {cost.Resource}: need {cost.Required}, available {cost.Available}.";
            if (!elementWheel && command.Action == BattleAction.Skill && ability.IsMagic &&
                _ai?.IsSceneMagicForbidden(NativeActorIndex(actor), ability.Id, ability.ElementMask) == true)
            {
                forbiddenByField = true;
                return "敌方五灵阵法封禁了此系仙术。";
            }
            try
            {
                string presentationDenied = ValidateAbilityPresentation(actor, ability, elementWheel);
                if (presentationDenied != null)
                    return presentationDenied;
                AttackTiming timing = elementWheel ? ElementWheelTiming(command.Actor, ability) :
                    (_catalog as IBattleAbilityTimings)?.Ability(command.Actor, ability);
                if (timing == null)
                    return "The ability has no verified action timeline.";
                if (timing.HitTicks.Count != 1)
                    return "This ability requires a dedicated repeated-effect timeline.";
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException or System.IO.IOException)
            {
                return exception.Message;
            }
            return null;
        }

        private BattleAbilityDefinition Ability(BattleCommand command)
        {
            if (Definitions == null || command.Action is not (BattleAction.Skill or BattleAction.Item))
                return null;
            var table = command.Action == BattleAction.Item ? Definitions.Items : Definitions.Skills;
            return table.TryGetValue(command.AbilityId, out BattleAbilityDefinition ability) ? ability : null;
        }

        private BattleTargetRange RuntimeNormalAttackRange(Actor source)
        {
            // The original fire beast swap clears all three weapon range flags.
            if (_scenario.Variant == "PAL3A" && NativeActorIndex(source) == 0 && IsPal3ATransformed(source.Initial.InstanceId))
                return BattleTargetRange.OpponentSingle;
            return source.Definition?.NormalAttackRange ?? BattleTargetRange.OpponentSingle;
        }

        private bool CanAttackBack(Actor source, Actor target)
        {
            if (source == null || target == null)
                return false;
            if (RuntimeNormalAttackRange(source) is BattleTargetRange.OpponentColumn or BattleTargetRange.OpponentAll)
                return true;
            if (source.Initial.Side == BattleSide.Enemy && target.Initial.Side == BattleSide.Ally && (int)target.Slot + 1 == BattleEffectRules.GreaterElement(_scenario.SceneElement))
                return !_actors.Any(actor => actor.Initial.Side == BattleSide.Ally && actor.Slot == BattleSlot.Center && actor.Hp > 0 &&
                    !actor.Escaped && !IsFleeing(actor));
            if (source.Initial.Side == BattleSide.Ally && NativeActorIndex(target) == 6)
            {
                Actor center = ActorAtNativeIndex(10);
                return center == null || center.Hp <= 0 || center.Escaped || IsFleeing(center);
            }
            return true;
        }

        private BattleTargetRange ActionRange(Actor source, BattleAbilityDefinition ability)
        {
            if (ability != null)
                return ability.Range;
            return RuntimeNormalAttackRange(source);
        }

        public IReadOnlyList<BattleActorSnapshot> GetTargetCandidates(BattleCommand command)
        {
            if (command.Actor == null || !_byId.TryGetValue(command.Actor, out Actor source))
                return Array.Empty<BattleActorSnapshot>();
            BattleTargetRange range = GetActionTargetRange(command);
            if (range == BattleTargetRange.None)
                return Array.Empty<BattleActorSnapshot>();
            BattleAbilityDefinition ability = Ability(command);
            bool own = range is BattleTargetRange.OwnSingle or BattleTargetRange.OwnAll;
            if (IsMagicSwordAbility(ability))
                return Array.AsReadOnly(FightSkillRange(source, range).Select(Snapshot).ToArray());

            // ProcSelectRole copies these selected slots into the command at confirmation.
            return Array.AsReadOnly(_actors.OrderBy(NativeActorIndex).Where(target =>
                (target.Initial.Side == source.Initial.Side) == own && !target.Escaped && !IsFleeing(target) &&
                (target.Alive || own && ability?.Revives == true) &&
                CanManuallySelectTarget(target, own, ability) &&
                (Definitions == null || command.Action != BattleAction.Attack || CanAttackBack(source, target)) &&
                (_scenario.Variant != "PAL3A" || ability?.Id != BattlePal3ARules.PengxuTransformSkill || target == source))
                .Select(Snapshot).ToArray());
        }

        // GetFightSkillRange constructs group masks for AI spells, items and PAL3 weapon skills.
        private IEnumerable<Actor> FightSkillRange(Actor source, BattleTargetRange range) =>
            _actors.OrderBy(NativeActorIndex).Where(target => !target.Escaped &&
                (target.Initial.Side == source.Initial.Side) == (range == BattleTargetRange.OwnAll) &&
                (_scenario.Variant == "PAL3" || target.Hp > 0));

        private IReadOnlyList<BattleActorSnapshot> CommandTargetCandidates(BattleCommand command, bool fromController) =>
            fromController && TryAiTargetCandidates(command, out IReadOnlyList<BattleActorSnapshot> targets)
                ? targets : GetTargetCandidates(command);

        private string[] CaptureTargets(Actor source, BattleCommand command, BattleAbilityDefinition ability, bool fromController)
        {
            if (IsMagicSwordAbility(ability))
                return Array.Empty<string>(); // ProcWeaponSkill builds its own mask on the first system pass.
            if (fromController && TryAiCapturedTargets(command, out string[] recorded))
                return recorded;
            BattleTargetRange range = ActionRange(source, ability);
            var candidates = CommandTargetCandidates(command, fromController);
            if (range is BattleTargetRange.OwnSingle or BattleTargetRange.OpponentSingle)
                return new[] { command.Target };
            if (range is BattleTargetRange.OpponentRow or BattleTargetRange.OpponentColumn)
            {
                Actor anchor = _byId[command.Target];
                return candidates.Where(target => BattleTargetRules.Aligned(anchor.Initial.Side, anchor.Slot,
                    _byId[target.Id].Slot, range == BattleTargetRange.OpponentRow, _scenario.SceneElement)).Select(target => target.Id).ToArray();
            }
            return candidates.Select(target => target.Id).ToArray();
        }

        private bool SubmitCommand(BattleCommand command, bool fromController, bool elementWheel = false)
        {
            if (ValidateCommand(command, fromController, elementWheel) != null)
                return false;
            Actor actor = _byId[command.Actor];
            BattleAbilityDefinition ability = Ability(command);
            BattleAttackPlan attackPlan = command.Action == BattleAction.Attack ? PlanAttack(actor, _byId[command.Target]) : null;
            AttackTiming timing = elementWheel ? ElementWheelTiming(command.Actor, ability) :
                attackPlan?.Timing ?? (ability != null ? ((IBattleAbilityTimings)_catalog).Ability(command.Actor, ability) : null);
            int guardTicks = _catalog.GuardEntry(command.Actor);
            string[] targets = !elementWheel && command.Action is BattleAction.Attack or BattleAction.Skill or BattleAction.Item ? CaptureTargets(actor, command, ability, fromController) : Array.Empty<string>();
            bool casting = !elementWheel && ability?.IsMagic == true || command.Action == BattleAction.Flee;
            actor.Ready = false;
            actor.Guard = false;
            actor.Gauge = casting ? FullGauge : 0;
            actor.CastGauge = 0;
            actor.CastSpeed = 0;
            if (command.Action == BattleAction.Flee)
                actor.CastSpeed = _effectRules.FleeGaugePerStep(EffectiveAttributes(actor)[5]);
            else if (casting)
            {
                int proficiency = AiSkill(actor, ability.Id).Level;
                if (proficiency >= 4)
                    casting = false;
                else
                {
                    BattleParticipant current = RuleParticipant(actor);
                    actor.CastSpeed = _rules is BattleBasicRules native ?
                        native.MagicCastGaugePerStep(current, proficiency) :
                        Math.Min(byte.MaxValue, _rules.GaugePerStep(current) * proficiency * 2);
                }
                if (!casting)
                {
                    actor.Gauge = 0;
                    actor.CastGauge = FullCastGauge;
                }
            }
            actor.Run = new ActionRun
            {
                Id = ++_nextAction,
                CreatedTick = SimulationTick,
                TimelineOriginTick = SimulationTick,
                Kind = command.Action,
                Target = command.Target,
                Targets = targets,
                Ability = ability,
                Timing = timing,
                AttackPlan = attackPlan,
                LastAttackSample = attackPlan?.Origin ?? actor.Position,
                GuardTicks = guardTicks,
                Casting = casting,
                ElementWheel = elementWheel
            };
            _inputs.Add(new BattleInput
            {
                Tick = Tick,
                Kind = elementWheel ? BattleInputKind.ElementWheel : BattleInputKind.Command,
                Actor = command.Actor,
                Action = command.Action,
                Target = command.Target,
                AbilityId = command.AbilityId,
                FromTestAi = fromController
            });
            Add(BattleEventKind.Command, command.Actor, actor.Run.Target, actor.Run.Id, detail: command.Action + (ability == null ? "" : " " + ability.Id));
            if (!fromController)
            {
                AdvanceFiveSpiritFieldTurn(actor);
                if (!elementWheel && command.Action != BattleAction.Transform)
                    RememberAiCommand(command);
            }
            return true;
        }

        private void AdvanceGauges()
        {
            if (IsCoAttackAtbPaused || _fleeAtbPaused)
                return;
            foreach (Actor actor in _actors.Where(actor => actor.Alive && !actor.Escaped && !actor.EnemyFleeing))
            {
                if (actor.Run?.Casting == true)
                {
                    actor.CastGauge = Math.Min(FullCastGauge, actor.CastGauge + actor.CastSpeed);
                    if (actor.CastGauge >= FullCastGauge)
                    {
                        actor.Run.Casting = false;
                        actor.Gauge = 0;
                        actor.Run.Elapsed = 0;
                        actor.Run.TimelineOriginTick = SimulationTick;

                    }
                }
                else if (!actor.Ready && (actor.Run == null || actor.Run.Kind == BattleAction.Defend || IsInterruptedFleeRun(actor.Run)))
                {
                    actor.Gauge = Math.Min(FullGauge, actor.Gauge + _rules.GaugePerStep(RuleParticipant(actor)));
                    MakeReady(actor);
                }
            }
        }

        private Actor ChooseTestTarget(Actor actor) => _actors.FirstOrDefault(target => target.Alive && !target.Escaped && target.Initial.Side != actor.Initial.Side && IsAtHome(target) &&
            (target.Run?.Kind != BattleAction.Attack || target.Run.Timing.TravelTicks == 0));

        private void ProcessRounds()
        {
            if (Definitions != null && (_resolvingOutcome || !_actors.Any(actor => actor.Initial.Side == BattleSide.Enemy && actor.Alive && !actor.Escaped)))
            {
                CheckResult();
                return;
            }
            foreach (Actor actor in _actors.OrderBy(NativeActorIndex).Where(actor => actor.Run == null && ActionBodyIdle(actor)).ToArray())
            {
                if (RoundLimitReached)
                    break;
                if (BeginOriginalRound(actor))
                {
                    if (Definitions != null)
                    {
                        foreach (BattleStatusInstance state in actor.States.Values.OrderBy(value => value.State).ToArray())
                        {
                            int delta = _statusRules.TickRound(state, actor.Attributes[0], actor.Hp);
                            if (delta != 0)
                                ChangeHp(actor, delta, null, 0, "poison " + state.State, false);
                            if (!state.Permanent && state.RemainingRounds == 0)
                                ApplyState(actor, state.State, true);
                            if (actor.Hp <= 0)
                                break;
                        }
                        AdvanceOriginalAbsorbHpWormOnRound(actor);
                        AdvancePal3AFormsOnRound(actor);
                        if (actor.Hp > 0)
                        {
                            for (int resource = 0; resource < 3; resource++)
                            {
                                int rate = actor.Definition.Equipment.Where(item => item.Slot != 0).Sum(item => item.RecoveryPerRound.Count > resource ? item.RecoveryPerRound[resource] : 0);
                                if (rate > 0 && ResourceValue(actor, resource) < actor.Attributes[resource])
                                    ChangeResource(actor, resource, (int)(actor.Attributes[resource] * (float)rate / 100), null, 0, "equipment per-round recovery");
                            }
                            if (actor.Initial.Side == BattleSide.Enemy && _random.Probability(actor.Definition.EscapePercent))
                            {
                                StartEnemyFlee(actor);
                            }
                        }
                        HandleOriginalBossRound(actor);
                    }
                }
                if (!actor.Alive || actor.Escaped || actor.EnemyFleeing)
                    continue;
                if (actor.Ready && (Has(actor, BattleState.Paralysis) || Has(actor, BattleState.Sleep)))
                {
                    AdvanceFiveSpiritFieldTurn(actor);
                    actor.Ready = false;
                    actor.Gauge = 0;
                    Add(BattleEventKind.Cancelled, actor.Initial.InstanceId, detail: "定／眠 skips a ready turn");
                }
                if (actor.Ready && !IsUncontrollable(actor) && !IsAiEnabled(actor))
                    break;
            }
            CheckResult();
        }
    }
}
