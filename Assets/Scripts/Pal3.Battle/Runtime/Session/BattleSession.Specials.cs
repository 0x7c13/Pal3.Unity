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
        private sealed class StealPool
        {
            public int Money, ItemId, ItemCount;
        }

        private readonly Dictionary<string, StealPool> _stealPools = new(StringComparer.Ordinal);
        private readonly Dictionary<int, int> _treasureUses = new();
        private readonly List<int> _inventoryOrder = new();

        public IReadOnlyList<BattleItemSnapshot> TreasureUses => Array.AsReadOnly(_treasureUses.OrderBy(pair => pair.Key)
            .Select(pair => new BattleItemSnapshot(pair.Key, pair.Value)).ToArray());

        public IReadOnlyList<BattleStealPoolSnapshot> StealPools => Array.AsReadOnly(_stealPools.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new BattleStealPoolSnapshot(pair.Key, pair.Value.Money, pair.Value.ItemId, pair.Value.ItemCount)).ToArray());

        public IReadOnlyList<BattleItemSnapshot> InventoryEntries => Array.AsReadOnly(_inventoryOrder
            .Select(id => new BattleItemSnapshot(id, _inventory[id])).ToArray());

        private void InitializeSpecialResources(BattleScenario scenario)
        {
            _inventoryOrder.AddRange(scenario.Items.Select(item => item.Id));
            foreach (BattleItem item in scenario.TreasureUses)
                _treasureUses.Add(item.Id, item.Count);
        }

        private void InitializeActorSpecials(Actor actor)
        {
            _stealPools.Add(actor.Initial.InstanceId, new StealPool
            {
                Money = actor.Definition.StealableMoney,
                ItemId = actor.Definition.StealableItemId,
                ItemCount = actor.Definition.StealableItemCount
            });
        }

        private void InitializeStartupSpecials()
        {
            if (Definitions == null || Definitions.Variant != "PAL3")
                return;
            // Native InitAllObj initializes every gauge before this identity-ordered equipment roll.
            foreach (Actor actor in _actors.Where(actor => actor.Initial.Side == BattleSide.Ally)
                .OrderBy(actor => actor.Definition.Identity).ThenBy(actor => actor.Initial.InstanceId, StringComparer.Ordinal))
                if (actor.Definition.Equipment.Any(item => item.StartupInvisibility) && _random.Probability(10))
                    ApplyState(actor, BattleState.Invisible, false);
        }

        private bool CanManuallySelectTarget(Actor target, bool own, BattleAbilityDefinition ability)
        {
            if (own)
                return !IsPal3ATransformed(target.Initial.InstanceId);
            // PAL3 CanBeSelect exempts Wan Yi Gu (4401) from the Hermit check.
            return _scenario.Variant != "PAL3" || ability?.Id == 4401 || !Has(target, BattleState.Invisible);
        }

        private string ValidateSpecialCommand(BattleAbilityDefinition ability)
        {
            if (ability.Handler == BattleSpecialHandler.RandomFormation && _catalog is not IBattleStageGeometry)
                return "Random formation needs the original scene's formation positions.";
            if (!ability.IsTreasure)
                return null;
            if (!BattleSpecialRules.IsTreasure(Definitions.Variant, ability.Id))
                return "This treasure has no original usage counter in the selected variant.";
            int reserved = _actors.Count(actor => actor.Run is { Kind: BattleAction.Item, ItemConsumed: false } &&
                actor.Run.Ability.Id == ability.Id);
            return _treasureUses.GetValueOrDefault(ability.Id) > reserved ? null : "No independent treasure use remains.";
        }

        private int PrimaryActorLevel() => _actors.FirstOrDefault(actor => actor.Definition?.Identity == 0)?.Level ?? 0;

        private int SpecialRequiredCost(BattleAbilityDefinition ability, BattleCost cost, int ordinaryRequired)
        {
            return cost.Resource != 10 ? ordinaryRequired : BattleSpecialRules.RequiredMoney(Definitions.Variant, ability.Handler, ordinaryRequired,
                _actors.Count(actor => actor.Initial.Side == BattleSide.Ally && actor.Hp > 0 && !actor.Escaped), PrimaryActorLevel());
        }

        private void ConsumeItemAtStart(Actor actor, BattleAbilityDefinition ability)
        {
            if (!ability.IsTreasure)
            {
                ChangeItem(ability.Id, -1, actor.Initial.InstanceId, "item consumed when its action starts");
                return;
            }
            int before = _treasureUses.GetValueOrDefault(ability.Id);
            if (before <= 0)
                throw new InvalidOperationException("A treasure counter cannot become negative.");
            _treasureUses[ability.Id] = before - 1;
            _events.Add(new BattleEvent
            {
                Tick = Tick,
                Kind = BattleEventKind.TreasureUseChanged,
                Source = actor.Initial.InstanceId,
                AbilityId = ability.Id,
                ValueBefore = before,
                ValueAfter = before - 1,
                RequestedDelta = -1,
                ActualDelta = -1,
                RandomState = _random.State,
                Detail = "independent treasure use consumed at action start; inventory retained"
            });
        }

        private bool ResolveSpecialBeforeStates(Actor source, Actor target, ActionRun run)
        {
            switch (run.Ability.Handler)
            {
                case BattleSpecialHandler.ForceFlee:
                    ResolveForceFlee(source, target);
                    break;
                case BattleSpecialHandler.RandomPoison:
                    ApplyRandomPoison(source, target, run.Id);
                    break;
                case BattleSpecialHandler.Summon:
                    ResolveOriginalSummon(source, run.Id);
                    break;
                case BattleSpecialHandler.RandomFormation:
                    RandomizeOriginalFormation(source, run.Id);
                    break;
                case BattleSpecialHandler.PoisonDragonGall:
                    if (!target.States.Keys.Any(IsPoisonState))
                    {
                        ChangeHp(target, -99999999, source, run.Id, "poison dragon gall used on an unpoisoned target");
                        return true; // Native ItemAffect returns zero before ordinary states, values and interrupt handling.
                    }
                    break;
            }
            return false;
        }

        private void ResolveSpecialAfterValues(Actor source, Actor target, ActionRun run, int oldHp, int returned)
        {
            switch (run.Ability.Handler)
            {
                case BattleSpecialHandler.HalfActualHpAbsorb:
                    ChangeHp(source, BattleSpecialRules.HalfActualHp(oldHp, target.Hp), source, run.Id, "half actual HP absorption");
                    break;
                case BattleSpecialHandler.ReturnedMpAbsorb:
                    ChangeResource(source, 2, BattleSpecialRules.AbsorbedMp(returned), source, run.Id, "native returned MP absorption");
                    break;
                case BattleSpecialHandler.AbsorbHpWorm:
                    ApplyOriginalAbsorbHpWorm(target);
                    break;
            }
        }

        private void ResolveSpecialAfterTarget(Actor source, Actor target, ActionRun run)
        {
            if (run.Ability.Handler == BattleSpecialHandler.Steal)
                StealFrom(source, target, run.Id);
        }

        private static bool IsPoisonState(BattleState state) => state >= BattleState.PoisonWind && state <= BattleState.PoisonEarth;

        private void ApplyRandomPoison(Actor source, Actor target, long actionId)
        {
            if (Has(target, BattleState.PoisonResist) || target.Definition.ImmuneStates.Contains(BattleState.PoisonResist) || target.States.Keys.Any(IsPoisonState))
                return;
            ApplyState(target, (BattleState)(_random.Next() % 5), false, source: source, actionId: actionId);
        }

        private void StealFrom(Actor source, Actor target, long actionId)
        {
            StealPool pool = _stealPools[target.Initial.InstanceId];
            int money = BattleSpecialRules.StealAmount(pool.Money, _random);
            if (money > 0)
            {
                ChangeStealPool(source, target, actionId, pool, true, money);
                ChangeResource(source, 10, money, source, actionId, "stolen money");
            }
            // Original dispatch draws money first, then independently draws an item quantity.
            int count = pool.ItemId > 0 ? BattleSpecialRules.StealAmount(pool.ItemCount, _random) : 0;
            if (count > 0)
            {
                ChangeStealPool(source, target, actionId, pool, false, count);
                PlaySound("wa006");
                ChangeItem(pool.ItemId, count, source.Initial.InstanceId, "stolen item");
            }
            if (money == 0 && count == 0)
                Add(BattleEventKind.Miss, source.Initial.InstanceId, target.Initial.InstanceId, actionId, "nothing remains in the independent stealable pool");
        }

        private void ChangeStealPool(Actor source, Actor target, long actionId, StealPool pool, bool money, int amount)
        {
            int before = money ? pool.Money : pool.ItemCount;
            if (amount < 0 || amount > before)
                throw new InvalidOperationException("Invalid stealable-pool change.");
            if (money)
                pool.Money -= amount;
            else
                pool.ItemCount -= amount;
            _events.Add(new BattleEvent
            {
                Tick = Tick,
                Kind = BattleEventKind.StealPoolChanged,
                Source = source.Initial.InstanceId,
                Target = target.Initial.InstanceId,
                ActionId = actionId,
                Resource = money ? 10 : -1,
                AbilityId = money ? 0 : pool.ItemId,
                ValueBefore = before,
                ValueAfter = before - amount,
                RequestedDelta = -amount,
                ActualDelta = -amount,
                RandomState = _random.State,
                Detail = money ? "stealable money" : "stealable item quantity"
            });
        }

        private void ResolvePhysicalSpecial(Actor source, Actor target, ActionRun run, int mode)
        {
            if (mode == 929)
                return; // Published Special30 succeeds without effects, service calls or RNG.
            if (mode == 913)
                return; // Published Special27 is a verified no-op for legal nonnegative money, without RNG.
            if (mode != 914 || target.Initial.Side != BattleSide.Ally)
                return;
            int[] eligible = _inventoryOrder.Where(id => Definitions.Items.TryGetValue(id, out BattleAbilityDefinition item) && item.ItemType == 1 &&
                (Definitions.Variant == "PAL3" || item.Revives || item.Attributes.Any(effect => effect.Attribute < 3 &&
                    (effect.Mode == BattleValueMode.RecoverToMax || effect.Value > 0)))).ToArray();
            if (eligible.Length == 0)
                return;
            int chosen = eligible[_random.Next() % eligible.Length];
            ChangeItem(chosen, Definitions.Variant == "PAL3" ? -_inventory[chosen] : -1, source.Initial.InstanceId, "enemy recovery-item theft");
        }

        private void ResolveAttackSteal(Actor source, Actor target, ActionRun run)
        {
            if (_random.Probability(EquipmentBonus(source, item => item.AttackStealPercent)))
                StealFrom(source, target, run.Id);
        }

        private int ApplyMoneyShield(Actor target, int signedRequest, Actor source, long actionId)
        {
            int remaining = BattleSpecialRules.ShieldedHpRequest(signedRequest, _money, out int payment);
            ChangeResource(target, 10, -payment, source, actionId, "money shield paid HP request");
            return remaining;
        }
    }
}
