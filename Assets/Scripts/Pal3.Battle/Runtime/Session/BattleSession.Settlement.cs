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
        private BattleResult BuildResult(BattleSessionStatus status, BattleSide? winner, string reason)
        {
            int settlementEventStart = _events.Count;
            IReadOnlyList<BattleActorSnapshot> preSettlementActors = Actors;
            FlushPal3ASpiritOrbs();
            IReadOnlyList<BattleActorReward> rewards = Array.Empty<BattleActorReward>();
            IReadOnlyList<BattleItemSnapshot> loot = Array.Empty<BattleItemSnapshot>();
            if (Definitions != null && status == BattleSessionStatus.Completed && (winner == BattleSide.Ally || _partyEscaped))
            {
                if (!_partyEscaped)
                    foreach (Actor actor in _actors.Where(actor => actor.Initial.Side == BattleSide.Ally))
                        foreach (BattleState state in new[] { BattleState.Paralysis, BattleState.Sleep, BattleState.Invisible })
                            ApplyState(actor, state, true);
                rewards = SettleExperienceAndSkills();
                loot = SettleLootAndMoney();
                foreach (Actor actor in _actors.Where(actor => actor.Initial.Side == BattleSide.Ally && actor.Hp == 0 &&
                    (!_partyEscaped || _scenario.Variant == "PAL3A")))
                {
                    actor.Hp = Math.Min(1, actor.Attributes[0]);
                    ChangeLove(actor, -2);
                }
            }
            return new BattleResult
            {
                SettlementEventStart = settlementEventStart,
                PreSettlementActors = preSettlementActors,
                Status = status,
                GameOver = status == BattleSessionStatus.Completed && winner == BattleSide.Enemy && _scenario.GameOverOnDefeat,
                Winner = winner,
                Reason = reason,
                Tick = Tick,
                Actors = Actors,
                MoneyBefore = _scenario.Money,
                MoneyAfter = _money,
                Pal3A = _scenario.Variant == "PAL3A" ? Pal3AState : null,
                Escaped = _partyEscaped,
                Rewards = rewards,
                Items = Inventory,
                TreasureUses = TreasureUses,
                MagicSwords = MagicSwords,
                StealPools = StealPools,
                Loot = loot
            };
        }

        private IReadOnlyList<BattleActorReward> SettleExperienceAndSkills()
        {
            Actor[] dead = _actors.Where(actor => actor.Initial.Side == BattleSide.Enemy && actor.Hp <= 0 && !actor.Escaped).OrderBy(NativeActorIndex).ToArray();
            Actor[] survivors = _actors.Where(actor => actor.Initial.Side == BattleSide.Ally && actor.Hp > 0 && !actor.Escaped).ToArray();
            var gains = _actors.Where(actor => actor.Initial.Side == BattleSide.Ally).ToDictionary(actor => actor.Initial.InstanceId, actor => 0);
            // PAL3A's flee path runs only the commit pass. Unlike PAL3, it
            // never allocates new experience from enemies killed this battle.
            if (survivors.Length > 0 && (!_partyEscaped || Definitions.Variant == "PAL3"))
            {
                int shared = dead.Aggregate(0, (sum, actor) => unchecked(sum + Math.Max(0, actor.Definition.ExperienceReward))) / survivors.Length;
                foreach (Actor actor in survivors)
                    gains[actor.Initial.InstanceId] = shared;
            }
            if (!_partyEscaped)
            {
                Actor last = dead.Where(actor => actor.DeathTick > 0).OrderByDescending(actor => actor.DeathTick).FirstOrDefault();
                if (last?.Killer != null && gains.ContainsKey(last.Killer))
                {
                    int finishingExperience = dead.Where(actor => actor.DeathTick >= last.DeathTick && actor.Killer == last.Killer)
                        .Aggregate(0, (sum, actor) => unchecked(sum + Math.Max(0, actor.Definition.ExperienceReward)));
                    gains[last.Killer] += finishingExperience * 3 / 10;
                }
            }
            var reports = new List<BattleActorReward>();
            foreach (Actor actor in _actors.Where(actor => actor.Initial.Side == BattleSide.Ally).OrderBy(NativeActorIndex))
            {
                int beforeLevel = actor.Level, beforeExperience = actor.Experience;
                int[] beforeAttributes = actor.Attributes.ToArray();
                int gained = 0;
                if (actor.Hp > 0 && !actor.Escaped)
                {
                    int bonus = actor.Definition.Equipment.Where(item => item.Slot != 0).Sum(item => item.SpecialEffect switch { 20 => 5, 21 => 15, 24 => 20, _ => 0 });
                    gained = BattleBasicRules.Round(gains[actor.Initial.InstanceId] * (float)(100 + bonus) / 100);
                    // The released commit adds signed int32 values before CalLevel.
                    // A wrapped runtime result is distinct from validated initial EXP.
                    actor.Experience = unchecked(actor.Experience + gained);
                    // CalLevel compares its DWORD table with unsigned JA, including
                    // wrapped negative EXP. Only a higher resulting level commits growth.
                    if (actor.Definition.ExperienceThresholds.Count == 101)
                        while (actor.Level < 100 && unchecked((uint)actor.Experience) >= unchecked((uint)actor.Definition.ExperienceThresholds[actor.Level + 1]))
                            actor.Level++;
                    if (actor.Level > beforeLevel)
                    {
                        if (!string.IsNullOrEmpty(actor.Definition.GrowthUnsupportedReason) || actor.Definition.AttributesByLevel.Count <= actor.Level)
                            throw new NotSupportedException("Level-up has no verified original growth definition for " + actor.Initial.InstanceId);
                        for (int attribute = 0; attribute <= 4; attribute++)
                            actor.Attributes[attribute] = unchecked(actor.Attributes[attribute] + actor.Definition.AttributesByLevel[actor.Level][attribute] - actor.Definition.AttributesByLevel[beforeLevel][attribute]);
                        actor.Hp = Math.Max(0, actor.Attributes[0]);
                        actor.Sp = Math.Max(0, actor.Attributes[1]);
                        actor.Mp = Math.Max(0, actor.Attributes[2]);
                        Add(BattleEventKind.LevelUp, actor.Initial.InstanceId, detail: beforeLevel + " → " + actor.Level);
                        foreach (BattleAbilityDefinition technique in Definitions.Skills.Values.Where(ability => !ability.IsMagic && ability.SkillTypeId == 0 &&
                            ability.ApplicableIdentities.Contains(actor.Definition.Identity) && ability.LearningLevel > 0 && ability.LearningLevel <= actor.Level).OrderBy(ability => ability.Id))
                            LearnSkill(actor, technique.Id);
                    }
                    SettleSkillGrowth(actor);
                }
                reports.Add(new BattleActorReward
                {
                    Actor = actor.Initial.InstanceId,
                    ExperienceGained = gained,
                    LevelBefore = beforeLevel,
                    LevelAfter = actor.Level,
                    ExperienceBefore = beforeExperience,
                    ExperienceAfter = actor.Experience,
                    AttributesBefore = Array.AsReadOnly(beforeAttributes),
                    AttributesAfter = Array.AsReadOnly(actor.Attributes.ToArray())
                });
            }
            return reports.AsReadOnly();
        }

        private bool LearnSkill(Actor actor, int id)
        {
            if (!Definitions.Skills.TryGetValue(id, out BattleAbilityDefinition definition) || !definition.ApplicableIdentities.Contains(actor.Definition.Identity))
                return false;
            BattleSkill existing = actor.Skills.FirstOrDefault(skill => skill.Id == id);
            if (existing?.Mastered == true)
                return false;
            if (existing == null)
            {
                existing = new BattleSkill { Id = id };
                actor.Skills.Add(existing);
            }
            existing.Mastered = true;
            existing.Level = 1;
            existing.Uses = 0;
            _events.Add(new BattleEvent
            {
                Tick = Tick,
                Kind = BattleEventKind.SkillLearned,
                Source = actor.Initial.InstanceId,
                AbilityId = id,
                RandomState = _random.State,
                Detail = definition.Name
            });
            return true;
        }

        private void SettleSkillGrowth(Actor actor)
        {
            IReadOnlyList<BattleMagicSlot> slots = BattleMagicSlots.Capture(Definitions.Variant, Definitions, actor.Definition.Identity, actor.Skills);
            // Only newly learned next-chain magic restarts the published pass.
            // Composite learning alone does not rescan a previously visited slot.
            int limit = Definitions.Skills.Count + 1;
            for (int pass = 0; pass < limit; pass++)
            {
                foreach (BattleMagicSlot slot in slots)
                {
                    BattleSkill skill = actor.Skills.FirstOrDefault(candidate => candidate.Id == slot.SkillId && candidate.Mastered);
                    if (skill == null)
                        continue;
                    BattleAbilityDefinition definition = slot.Definition;
                    int before = skill.Level;
                    for (int tier = Math.Min(4, definition.UsesBeforeLevelUp.Count); tier > skill.Level; tier--)
                        if (skill.Uses >= definition.UsesBeforeLevelUp[tier - 1])
                        {
                            skill.Level = tier;
                            break;
                        }
                    if (skill.Level != before)
                        _events.Add(new BattleEvent
                        {
                            Tick = Tick,
                            Kind = BattleEventKind.SkillImproved,
                            Source = actor.Initial.InstanceId,
                            AbilityId = skill.Id,
                            ValueBefore = before,
                            ValueAfter = skill.Level,
                            RandomState = _random.State,
                            Detail = definition.Name
                        });
                }
                foreach (BattleMagicSlot slot in slots)
                {
                    BattleSkill skill = actor.Skills.FirstOrDefault(candidate => candidate.Id == slot.SkillId && candidate.Mastered);
                    if (skill == null)
                        continue;
                    foreach (BattleSkillUnlock unlock in slot.Definition.Unlocks)
                    {
                        BattleSkill other = actor.Skills.FirstOrDefault(candidate => candidate.Id == unlock.OtherSkillId && candidate.Mastered);
                        if (skill.Level >= unlock.SelfTier && actor.Level >= unlock.ActorLevel && other != null && other.Level >= unlock.OtherTier &&
                            slots.Any(candidate => candidate.SkillId == unlock.OtherSkillId) && slots.Any(candidate => candidate.SkillId == unlock.SkillId))
                            LearnSkill(actor, unlock.SkillId);
                    }
                }
                bool nextLearned = false;
                foreach (BattleMagicSlot slot in slots)
                {
                    BattleSkill skill = actor.Skills.FirstOrDefault(candidate => candidate.Id == slot.SkillId && candidate.Mastered);
                    BattleAbilityDefinition definition = slot.Definition;
                    if (skill != null && definition.NextSkillId > 0 && skill.Level >= definition.NextSkillRequiredProficiency &&
                        actor.Level >= definition.LearningLevel && slots.Any(candidate => candidate.SkillId == definition.NextSkillId))
                        nextLearned |= LearnSkill(actor, definition.NextSkillId);
                }
                if (!nextLearned)
                    return;
            }
            throw new InvalidOperationException("Skill growth graph did not converge.");
        }

        private IReadOnlyList<BattleItemSnapshot> SettleLootAndMoney()
        {
            Actor[] dead = _actors.Where(actor => actor.Initial.Side == BattleSide.Enemy && actor.Hp <= 0 && !actor.Escaped).OrderBy(NativeActorIndex).ToArray();
            int baseMoney = dead.Aggregate(0, (sum, actor) => unchecked(sum + Math.Max(0, actor.Definition.MoneyReward)));
            int bonus = _actors.Where(actor => actor.Initial.Side == BattleSide.Ally).Sum(actor =>
                actor.Definition.Equipment.Count(item => item.Slot != 0 && item.SpecialEffect == 21) * 5);
            // Published reward accumulators and arithmetic use signed int32 wrap.
            int reward = unchecked(baseMoney + (int)(baseMoney * (float)bonus / 100));
            int before = _money;
            _money = (int)Math.Clamp((long)_money + reward, 0, int.MaxValue);
            _events.Add(new BattleEvent
            {
                Tick = Tick,
                Kind = BattleEventKind.ResourceChanged,
                Resource = 10,
                ValueBefore = before,
                ValueAfter = _money,
                RequestedDelta = reward,
                ActualDelta = _money - before,
                RandomState = _random.State,
                Detail = "ordinary combat money reward"
            });
            var drops = new Dictionary<int, int>();
            void Receive(int item, int count, string reason)
            {
                if (item <= 0 || count <= 0)
                    return;
                int received = ChangeItem(item, count, null, reason);
                if (received > 0)
                    drops[item] = drops.GetValueOrDefault(item) + received;
            }
            foreach (Actor actor in dead)
            {
                Receive(actor.Definition.LootItem, actor.Definition.LootCount, "ordinary combat loot; no drop probability roll");
                Receive(actor.CorpseItem, 1, "original corpse loot; success was rolled at the ability marker");
            }
            return Array.AsReadOnly(drops.OrderBy(pair => pair.Key).Select(pair => new BattleItemSnapshot(pair.Key, pair.Value)).ToArray());
        }
    }
}
