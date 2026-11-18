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
        public IReadOnlyList<BattleCostEstimate> GetCostEstimate(BattleCommand command)
        {
            if (command.Actor == null || !_byId.TryGetValue(command.Actor, out Actor actor))
                return Array.Empty<BattleCostEstimate>();
            BattleAbilityDefinition ability = Ability(command);
            if (ability == null || ability.IsItem)
                return Array.Empty<BattleCostEstimate>();
            return Array.AsReadOnly(ability.Costs.Select(cost =>
            {
                int amount = CostAmount(actor, ability, cost);
                int required = cost.Resource is 1 or 2 ? (cost.Resource == (ability.IsMagic ? 2 : 1) ? amount : 0) : amount;
                required = SpecialRequiredCost(ability, cost, required);
                int saving = cost.Resource == 1 ? EquipmentBonus(actor, item => item.SpSaving) : cost.Resource == 2 ? EquipmentBonus(actor, item => item.MpSaving) : 0;
                int payable = -BattleEffectRules.ReduceResourceLoss(-amount, saving);
                return new BattleCostEstimate(cost.Resource, required, payable, ResourceValue(actor, cost.Resource));
            }).ToArray());
        }

        private int CostAmount(Actor actor, BattleAbilityDefinition ability, BattleCost cost)
        {
            if (cost.Resource == 0 && BattleSpecialRules.UsesAbsoluteHpCost(Definitions.Variant, ability.Id))
                return Math.Max(0, cost.Value);
            if (cost.Resource == 1 && Definitions.Variant == "PAL3A" && ability.Id == 4308)
                return actor.Attributes[1];
            // GP/MP consume mode bytes are metadata; original FightSkillCost reads their absolute integers.
            if (cost.Resource is 1 or 2)
                return Math.Max(0, cost.Value);
            if (cost.Resource == 16)
                return 1;
            if (cost.Mode == BattleValueMode.Percentage)
                return (int)((long)ResourceValue(actor, cost.Resource) * cost.Value / 100);
            return Math.Max(0, cost.Value);
        }

        private int ResourceValue(Actor actor, int resource) => resource switch
        {
            0 => actor.Hp,
            1 => actor.Sp,
            2 => actor.Mp,
            6 => actor.Experience,
            7 => actor.Attributes[5],
            8 => actor.Attributes[6],
            10 => _money,
            16 => _inventory.GetValueOrDefault(BattlePal3ARules.GuInventoryItem),
            _ => 0,
        };

        private static int EquipmentBonus(Actor actor, Func<BattleEquipmentDefinition, int> select) =>
            actor.Definition?.Equipment.Where(item => item.Slot != 0).Sum(select) ?? 0;

        private int ItemRecoveryBonus(Actor actor) => actor.Definition?.Equipment.Where(item => item.Slot != 0)
            .Sum(item => item.SpecialEffect is 21 or 22 ? 10 : 0) ?? 0;

        private void PaySkillCost(Actor actor, BattleAbilityDefinition ability, long actionId, int lastTargetReturn)
        {
            foreach (BattleCost cost in ability.Costs.OrderBy(value => value.Resource is 1 or 2 ? value.Resource : 100 + value.Resource))
            {
                int amount = cost.Resource == 10 && ability.Handler == BattleSpecialHandler.MoneyRecovery ? lastTargetReturn : CostAmount(actor, ability, cost);
                if (cost.Resource == 0 && Definitions.Variant == "PAL3")
                    continue; // Released PAL3 leaves special HP costs unimplemented; PAL3A actually charges them.
                if (cost.Resource == 16)
                {
                    if (_inventory.GetValueOrDefault(BattlePal3ARules.GuInventoryItem) > 0)
                    {
                        ChangeItem(BattlePal3ARules.GuInventoryItem, -1, actor.Initial.InstanceId, "fixed original Gu skill cost " + ability.Id);
                        QueueNumber(actor, 16, -1, 3.5f, false);
                    }
                }
                else if (cost.Resource == 0 && BattleSpecialRules.UsesAbsoluteHpCost(Definitions.Variant, ability.Id))
                    ChangeHp(actor, -amount, null, actionId, "skill cost " + ability.Id, showAction: false);
                else
                    ChangeResource(actor, cost.Resource, -amount, actor, actionId, "skill cost " + ability.Id);
            }
            if (ability.IsMagic && actor.Initial.Side == BattleSide.Ally)
            {
                BattleSkill skill = AiSkill(actor, ability.Id);
                int maximum = Definitions.Variant == "PAL3A" ? ushort.MaxValue : byte.MaxValue;
                skill.Uses = Math.Min(maximum, skill.Uses + 1);
            }
        }

        private void ResolveActionMarker(Actor source, ActionRun run)
        {
            if (ResolveElementWheelMarker(source, run))
                return;
            if (ResolvePal3AAbilityMarker(source, run))
                return;
            if (ResolveMagicSwordMarker(source, run))
                return;
            if (run.Ability == null)
            {
                ResolvePhysicalMarker(source, run);
                CheckResult();
                return;
            }
            if (run.AbilityResolved)
                return;
            run.AbilityResolved = true;
            BattleAbilityDefinition ability = run.Ability;
            ResolveAiSceneMagic(source, ability);
            int lastTargetReturn = 0;
            foreach (string targetId in run.Targets)
            {
                Actor target = _byId[targetId];
                if (target.Escaped)
                    continue;
                bool multi = ability.Range is not (BattleTargetRange.OwnSingle or BattleTargetRange.OpponentSingle);
                if (!TechniqueSucceeds(source, target, run))
                    continue;
                if (!IsAtHome(target) && !ability.Revives && (ability.IsMagic || !multi))
                {
                    Add(BattleEventKind.Miss, source.Initial.InstanceId, targetId, run.Id, "target away from home");
                    QueueNumber(target, 0, 0, 1f, false, miss: true);
                    continue;
                }
                if (ability.IsMagic && Has(target, BattleState.Barrier))
                {
                    Add(BattleEventKind.Miss, source.Initial.InstanceId, targetId, run.Id, "界 rejects all magic effects");
                    continue;
                }
                if (ResolveSpecialBeforeStates(source, target, run))
                {
                    lastTargetReturn = 0;
                    continue;
                }
                // Ordinary spells and techniques change statuses before resource values. Resurrection depends on this order.
                ApplyAbilityStates(source, target, run);
                int oldHp = target.Hp;
                int lastReturnedDelta = 0;
                bool hpRecovered = false, mpRecovered = false;
                foreach (BattleAttributeEffect effect in ability.Attributes.OrderBy(value => value.Attribute))
                {
                    if (effect.Value == 0)
                        continue;
                    int power = effect.Value;
                    BattleValueMode mode = effect.Mode;
                    if (ability.IsMagic)
                        power = _effectRules.MagicPower(power, RuleParticipant(source), RuleParticipant(target), ability.ElementMask, _random,
                            enhancementMask: Definitions.Variant == "PAL3A" && FiveSpiritElement > 0 ? 1 << (FiveSpiritElement - 1) : 0,
                            weatherElement: Definitions.Variant == "PAL3A" ? BattleEffectRules.GreaterElement(FiveSpiritElement) : 0);
                    if (ability.IsMagic)
                        power = AdjustPal3AMagicPower(source, power);
                    else if (!ability.IsItem)
                        power = _effectRules.TechniquePower(power, RuleParticipant(source), RuleParticipant(target), multi, ability.Id,
                            _actors.FirstOrDefault(actor => actor.Definition?.Identity == 0)?.Level ?? source.Level, _random, out mode);
                    else if (power > 0 && source.Initial.Side == BattleSide.Ally)
                        power += BattleBasicRules.Round(power * (float)ItemRecoveryBonus(source) / 100);
                    if (effect.Attribute > 2)
                        throw new NotSupportedException("Combat attributes outside HP/SP/MP need an explicit effect handler.");
                    int resource = effect.Attribute;
                    if (mode == BattleValueMode.IncreaseMax)
                    {
                        if (resource != 1)
                        {
                            int before = target.Attributes[resource];
                            target.Attributes[resource] = Math.Max(0, unchecked(before + power));
                            AddResource(source, target, run.Id, resource, before, target.Attributes[resource], power, "maximum increased");
                        }
                        lastReturnedDelta = 0;
                        continue;
                    }
                    int requested = mode == BattleValueMode.RecoverToMax ? target.Attributes[resource] - ResourceValue(target, resource) :
                        mode == BattleValueMode.Percentage ? (int)((long)target.Attributes[resource] * power / 100) : power;
                    lastReturnedDelta = ChangeResource(target, resource, requested, source, run.Id, ability.Name,
                        showNumber: Definitions.Variant == "PAL3A" && resource == 2);
                    hpRecovered |= resource == 0 && lastReturnedDelta > 0;
                    mpRecovered |= resource == 2 && lastReturnedDelta > 0;
                }
                PlaySound(BattleEffectAudioRules.Recovery(hpRecovered, mpRecovered));
                lastTargetReturn = lastReturnedDelta;
                ResolveSpecialAfterValues(source, target, run, oldHp, lastReturnedDelta);
                if (lastReturnedDelta < 0 && target.Run?.Casting == true)
                {
                    Cancel(target, "ability hit interrupted casting");
                    target.Ready = false;
                    target.Gauge = ability.IsMagic ? FullGauge - FullGauge * ability.SpellRank / 8 : 0;
                }
                ResolveSpecialAfterTarget(source, target, run);
                TryObtainOriginalCorpse(source, target, run);
            }
            // All targets resolve before costs and the single proficiency use; a last lethal hit must not skip payment.
            if (!ability.IsItem && (!ability.IsMagic || !_bossRules.SkipsMagicCost(source.Initial.ActorId)))
                PaySkillCost(source, ability, run.Id, lastTargetReturn);
            HandleOriginalPostMagic(source, run);
            CheckResult();
        }

        private void ResolvePhysicalMarker(Actor source, ActionRun run)
        {
            GainMagicSwordOnPhysicalWork(source, run.Id);
            int sourceLuck = EffectiveAttributes(source)[6];
            bool critical = _random.Probability((int)(Math.Sqrt(sourceLuck / 2) * 10));
            BattleTargetRange range = ActionRange(source, null);
            foreach (string targetId in run.Targets)
            {
                Actor target = _byId[targetId];
                if (!target.Alive || target.Escaped)
                    continue;
                if (!IsAtHome(target) || Has(target, BattleState.Evade))
                {
                    Add(BattleEventKind.Miss, source.Initial.InstanceId, targetId, run.Id, "away target or 避");
                    QueueNumber(target, 0, 0, 1f, false, miss: !Has(target, BattleState.Evade));
                    continue;
                }
                bool dodged = target.Initial.Side == BattleSide.Ally && _random.Probability(Math.Max(0, (EffectiveAttributes(target)[5] - EffectiveAttributes(source)[5]) * 5));
                if (Has(target, BattleState.Reflection) || Has(target, BattleState.Paralysis) || Has(target, BattleState.Sleep))
                    dodged = false;
                int damage = Has(target, BattleState.Invisible) ? 0 : _effectRules.PhysicalPower(RuleParticipant(source), RuleParticipant(target), source.Definition,
                    target.Definition, range, _catalog.DamageDivisor(source.Initial.InstanceId), Has(source, BattleState.Madness), _random);
                if (dodged || damage >= 0)
                {
                    _events.Add(new BattleEvent
                    {
                        Tick = Tick,
                        Kind = BattleEventKind.Miss,
                        Source = source.Initial.InstanceId,
                        Target = targetId,
                        ActionId = run.Id,
                        Critical = critical,
                        Dodged = dodged,
                        RandomState = _random.State,
                        Detail = dodged ? "dodged" : "nonnegative native damage"
                    });
                    QueueNumber(target, 0, 0, 1f, false, miss: true, dodgeBody: true);
                    continue;
                }
                if (critical)
                    damage = _effectRules.CriticalPower(damage, EquipmentBonus(source, item => item.CriticalBonus));
                bool reflected = Has(target, BattleState.Reflection) ||
                    _random.Probability(4) && target.Definition.Equipment.Any(item => item.PhysicalReflectionPassive);
                if (reflected)
                    ChangeHp(source, damage, target, run.Id, "physical reflection", false, critical);
                else
                {
                    bool protectedHit = TryGetPhysicalProtector(source, target, out Actor guardian);
                    Actor receiver = protectedHit ? guardian : target;
                    bool fleeing = IsFleeWaiting(receiver);
                    if (!protectedHit && target.Run?.Casting == true)
                    {
                        if (IsFleeWaiting(target))
                            InterruptFleeWaiting(target, "physical request interrupted the second slot before HP payment", FullGauge / 2);
                        else
                        {
                            Cancel(target, "physical request interrupted the second slot before HP payment");
                            target.Gauge = FullGauge / 2;
                            target.Ready = false;
                        }
                    }
                    if (critical && !protectedHit && _effects != null)
                        StartEffect(new BattleEffectRequest(470, target.Initial.InstanceId, new[] { target.Initial.InstanceId }, target.Position, unchecked((uint)_scenario.Seed)));
                    int actualHp = ChangeHp(receiver, damage, source, run.Id, "physical", true, critical);
                    if (protectedHit)
                    {
                        ChangeLove(target, 1);
                        if (critical && _effects != null)
                            StartEffect(new BattleEffectRequest(470, receiver.Initial.InstanceId, new[] { receiver.Initial.InstanceId }, receiver.Position, unchecked((uint)_scenario.Seed)));
                    }
                    if (fleeing && actualHp < 0)
                        receiver.Gauge = 0;
                    if (target.Initial.Side == BattleSide.Ally)
                        ChangeResource(target, 1, _catalog.SpImpact(source.Initial.InstanceId), source, run.Id, "physical SP impact");
                    if (source.Initial.Side == BattleSide.Enemy)
                    {
                        int probability = 5 + EffectiveAttributes(source)[6] - EffectiveAttributes(receiver)[6];
                        if (_random.Probability(probability))
                        {
                            int mode = source.Definition.NormalAttackMode;
                            BattleState? state = BattlePhysicalAttackModes.AddedState(mode);
                            if (state.HasValue)
                                ApplyState(receiver, state.Value, false, source: source, actionId: run.Id);
                            else if (mode == 912)
                            {
                                foreach (int resource in new[] { 2, 1 })
                                {
                                    int transfer = Math.Min(10, ResourceValue(receiver, resource));
                                    ChangeResource(receiver, resource, -transfer, source, run.Id, "normal attack resource transfer", showNumber: true);
                                    ChangeResource(source, resource, transfer, source, run.Id, "normal attack resource transfer", showNumber: true);
                                }
                            }
                            else if (mode == 915)
                                ApplyRandomPoison(source, receiver, run.Id);
                            else
                                ResolvePhysicalSpecial(source, receiver, run, mode);
                        }
                    }
                    foreach (BattleEquipmentDefinition weapon in source.Definition.Equipment.Where(item => item.Slot == 0))
                        foreach (BattleStatusEffect state in weapon.States.OrderBy(value => value.State))
                            if (state.State < BattleState.Dying && _random.Probability(state.Value))
                            {
                                ApplyState(receiver, state.State, false, source: source, actionId: run.Id);
                                break;
                            }
                    ResolveAttackSteal(source, receiver, run);
                }
            }
        }

        private int ChangeResource(Actor actor, int resource, int requested, Actor source, long actionId, string reason, bool showNumber = false)
        {
            if (resource == 0)
                return ChangeHp(actor, requested, source, actionId, reason);
            int before = ResourceValue(actor, resource);
            int delta = requested;
            if (resource is 1 or 2)
            {
                if (!actor.Alive || resource == 2 && Pal3AIgnoresMpChange(actor))
                    return 0;
                int saving = resource == 1 ? EquipmentBonus(actor, item => item.SpSaving) : EquipmentBonus(actor, item => item.MpSaving);
                delta = BattleEffectRules.ReduceResourceLoss(delta, saving);
                if (resource == 2)
                    TrackPal3AMpLoss(actor, delta);
                int after = (int)Math.Clamp((long)before + delta, 0, actor.Attributes[resource]);
                if (resource == 1)
                    actor.Sp = after;
                else
                    actor.Mp = after;
            }
            else if (resource == 10)
                _money = (int)Math.Clamp((long)_money + delta, 0, int.MaxValue);
            else if (resource == 7 || resource == 8)
            {
                actor.Attributes[resource == 7 ? 5 : 6] = (int)Math.Clamp((long)before + delta, 0, 1000000);
                if (resource == 7 && actor.Run?.Casting == true)
                    actor.CastSpeed = _rules.GaugePerStep(RuleParticipant(actor));
            }
            else
                throw new NotSupportedException("Unsupported resource " + resource);
            int actual = ResourceValue(actor, resource) - before;
            AddResource(source, actor, actionId, resource, before, ResourceValue(actor, resource), requested, reason);
            // Only a successfully registered displayed GP/MP number clamps the native return value.
            bool displayed = resource is 1 or 2 && requested != 0 && showNumber && QueueNumber(actor, resource, actual, 2.5f, false);
            return resource is 1 or 2 ? displayed ? actual : delta : actual;
        }

        private int ChangeHp(Actor target, int requested, Actor source, long actionId, string reason, bool showAction = true, bool critical = false, bool ignoreInvisible = false)
        {
            int before = target.Hp;
            if (target.Escaped || !target.Alive || !ignoreInvisible && Has(target, BattleState.Invisible))
                return 0;
            int delta = requested;
            if (delta < -1 && target.Guard)
                delta /= 2;
            if (delta < 0 && Definitions?.Variant == "PAL3" && target.Definition.MoneyShield)
            {
                delta = ApplyMoneyShield(target, delta, source, actionId);
                if (delta == 0)
                    return 0;
            }
            TrackPal3AHpLoss(target, delta);
            target.Hp = (int)Math.Clamp((long)target.Hp + delta, 0, target.Attributes[0]);
            if (requested < 0 && Has(target, BattleState.Sleep))
                ApplyState(target, BattleState.Sleep, true, source: source, actionId: actionId);
            ApplyOriginalMustFailHp(target);
            HandlePal3AFormHpFloor(target, actionId);
            if (HandleOriginalBossLethal(target, actionId))
                return -before;
            int actual = target.Hp - before;
            if (target.Hp > 0)
                target.Reviving = false;
            _events.Add(new BattleEvent
            {
                Tick = Tick,
                Kind = actual < 0 ? BattleEventKind.Hit : BattleEventKind.ResourceChanged,
                Source = source?.Initial.InstanceId,
                Target = target.Initial.InstanceId,
                ActionId = actionId,
                HpBefore = before,
                HpAfter = target.Hp,
                RequestedDelta = requested,
                ActualDelta = actual,
                Critical = critical,
                Resource = 0,
                ValueBefore = before,
                ValueAfter = target.Hp,
                RandomState = _random.State,
                Detail = reason
            });
            if (requested < 0 && IsFleeWaiting(target))
                InterruptFleeWaiting(target, "negative HP request interrupted escape waiting");
            bool displayed = target.ModelAlive && QueueNumber(target, 0, delta > 0 ? actual : delta,
                delta > 0 ? 2.5f : 2f, delta < 0 && showAction, recovery: delta > 0);
            if (target.Hp == 0)
            {
                target.Reviving = false;
                target.Killer = source?.Initial.InstanceId;
                target.DeathTick = SimulationTick;
                RequestRoleEnd(target);
                target.PoseStarted = (int)SimulationTick;
                Add(BattleEventKind.Death, source?.Initial.InstanceId, target.Initial.InstanceId, actionId);
            }
            return delta > 0 && !displayed ? delta : delta <= 0 ? -Math.Abs(actual) : actual;
        }

        private void ApplyAbilityStates(Actor source, Actor target, ActionRun run)
        {
            foreach (BattleStatusEffect state in run.Ability.States)
                ApplyState(target, state.State, state.Remove, source: source, actionId: run.Id);
            // Native cure cues describe the requested operation, even when the
            // recipient had none of these ailments. Expiry and waking are silent.
            foreach (string sound in BattleEffectAudioRules.ClearedAilments(run.Ability.States))
                PlaySound(sound);
        }

        // The second gauge can fill during ATB advancement before the next
        // system pass transfers MagicWait to the spell's stage.
        private static bool CanInterruptCast(Actor actor) => actor.Run?.Casting == true ||
            actor.CastGauge == FullCastGauge && actor.Run is
            { Kind: BattleAction.Skill, SystemStarted: true, StageHandle: null, Ability: { IsMagic: true }, ElementWheel: false };

        private bool ApplyState(Actor target, BattleState state, bool remove, int? duration = null, bool permanent = false, Actor source = null, long actionId = 0, bool sound = true)
        {
            if (Has(target, BattleState.Invisible) && state != BattleState.Invisible)
                return false;
            if (state == BattleState.Death)
            {
                if (remove && !target.Alive)
                {
                    // Native resurrection restores life without deleting the
                    // stage thread or its body queue. Existing end requests stay.
                    target.States.Clear();
                    target.Reviving = true;
                    target.Alive = true;
                    target.ModelAlive = true;
                    target.Ready = false;
                    target.Gauge = FullGauge * 2 / 3;
                    target.CastGauge = 0;
                    target.PoseStarted = (int)SimulationTick;
                    QueuePhysicalRevivalReturn(target);
                    Add(BattleEventKind.StatusRemoved, source?.Initial.InstanceId, target.Initial.InstanceId, actionId, "Death; resurrection enabled before HP recovery");
                    if (sound)
                        PlaySound("wd004");
                    return true;
                }
                if (!remove && target.Alive)
                    ChangeHp(target, -target.Hp * (target.Guard ? 2 : 1), source, actionId, "death status");
                return false;
            }
            if (!target.Alive && !permanent)
            {
                // Native removal cues still play for dead roles (including the
                // victory cleanup); their stage-only undo animation is skipped.
                if (remove && sound)
                    PlaySound(BattleEffectAudioRules.State(state, true));
                return false;
            }
            if (remove)
            {
                if (sound)
                    PlaySound(BattleEffectAudioRules.State(state, true));
                if (!target.States.Remove(state))
                    return false;
                Add(BattleEventKind.StatusRemoved, source?.Initial.InstanceId, target.Initial.InstanceId, actionId, state.ToString());
            }
            else
            {
                string denied = _statusRules.CanAdd(state, target.States.Keys, target.Definition.ImmuneStates);
                if (denied != null)
                {
                    Add(BattleEventKind.Miss, source?.Initial.InstanceId, target.Initial.InstanceId, actionId, denied);
                    return false;
                }
                BattleStatusInstance instance = _statusRules.Create(state, target.Attributes[0], duration, permanent, _random, target.Initial.Side == BattleSide.Ally);
                foreach (BattleState conflict in _statusRules.ReplacedBy(state))
                    if (target.States.Remove(conflict))
                        Add(BattleEventKind.StatusRemoved, source?.Initial.InstanceId, target.Initial.InstanceId, actionId, conflict + " replaced");
                target.States.Add(state, instance);
                Add(BattleEventKind.StatusAdded, source?.Initial.InstanceId, target.Initial.InstanceId, actionId, state + (permanent ? " permanent" : " " + instance.RemainingRounds));
                if (sound)
                    PlaySound(BattleEffectAudioRules.State(state, false));
            }
            if (state is BattleState.SpeedIncrease or BattleState.SpeedDecrease && target.Run?.Casting == true)
                target.CastSpeed = _rules.GaugePerStep(RuleParticipant(target));
            bool losesControl = !remove && state is BattleState.Paralysis or BattleState.Sleep or BattleState.Chaos or BattleState.Madness;
            if (losesControl && (IsFleeWaiting(target) || IsFleeStageRun(target.Run)))
            {
                if (!_fleeAlwaysSucceeds)
                {
                    if (IsFleeWaiting(target))
                        InterruptFleeWaiting(target, state + " interrupted escape waiting");
                    else
                    {
                        target.Gauge = target.CastGauge = 0;
                        target.Ready = false;
                    }
                }
                return true;
            }
            // HandleUncontrolableState resets only the second gauge. ProcControl
            // handles sleep and paralysis when the first gauge reaches its command mark.
            if (!remove && CanInterruptCast(target) && (losesControl && !_fleeAlwaysSucceeds ||
                state == BattleState.Seal && target.Run.Kind == BattleAction.Skill ||
                state == BattleState.Forbidden && target.Run.Kind == BattleAction.Flee && !_fleeAlwaysSucceeds))
            {
                if (IsFleeWaiting(target))
                    InterruptFleeWaiting(target, state + " interrupted escape waiting");
                else
                {
                    Cancel(target, state + " interrupted action");
                    target.Gauge = target.CastGauge = 0;
                    target.Ready = false;
                }
            }
            return true;
        }

        private int ChangeItem(int itemId, int amount, string actor, string detail)
        {
            int before = _inventory.GetValueOrDefault(itemId);
            long sum = (long)before + amount;
            // Existing entries cap additions at 99. A larger authored stack keeps its current quantity.
            int after = Definitions != null && before > 0 && amount > 0
                ? (int)Math.Min(Math.Max(99, before), sum) : checked((int)sum);
            if (after < 0)
                throw new InvalidOperationException("Inventory cannot become negative.");
            _inventory[itemId] = after;
            if (before <= 0 && after > 0)
                _inventoryOrder.Add(itemId);
            else if (before > 0 && after == 0)
                _inventoryOrder.Remove(itemId);
            int actual = after - before;
            _events.Add(new BattleEvent
            {
                Tick = Tick,
                Kind = BattleEventKind.ItemChanged,
                Source = actor,
                AbilityId = itemId,
                ValueBefore = before,
                ValueAfter = after,
                RequestedDelta = amount,
                ActualDelta = actual,
                RandomState = _random.State,
                Detail = detail
            });
            return actual;
        }

        private void AddResource(Actor source, Actor target, long actionId, int resource, int before, int after, int requested, string detail)
            => _events.Add(new BattleEvent
            {
                Tick = Tick,
                Kind = BattleEventKind.ResourceChanged,
                Source = source?.Initial.InstanceId,
                Target = target.Initial.InstanceId,
                ActionId = actionId,
                Resource = resource,
                ValueBefore = before,
                ValueAfter = after,
                RequestedDelta = requested,
                ActualDelta = after - before,
                RandomState = _random.State,
                Detail = detail
            });

    }
}
