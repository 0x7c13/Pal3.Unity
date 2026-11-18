// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using Core.Contract.Enums;

    // Original items have no equipment-level field. This editor-only tier is
    // a relative ranking of actual attribute bonuses at level 50, per character
    // and slot. It is authoring convenience, not an original equip restriction.
    public sealed class BattleRandomEquipmentBuilder
    {
        private readonly BattleData _data;
        private readonly Dictionary<(int actor, int slot), int[]> _tiers = new();

        public BattleRandomEquipmentBuilder(BattleData data) => _data = data ?? throw new ArgumentNullException(nameof(data));

        public void Equip(BattleParticipant actor, Random random)
        {
            if (actor == null || random == null)
                throw new ArgumentNullException(actor == null ? nameof(actor) : nameof(random));
            if (actor.Side != BattleSide.Ally || BattleData.PlayerIdentity(actor.ActorId) < 0)
                return;
            actor.Equipment = new int[6];
            actor.UsesNativeDefaultWeapon = false;
            actor.MagicSword = null;
            for (int slot = 0; slot < actor.Equipment.Length; slot++)
            {
                int[] tiers = Tiers(actor.ActorId, slot);
                if (tiers.Length == 0)
                    continue;
                int center = (int)Math.Round((Math.Clamp(actor.Level, 1, 100) - 1) / 99d * (tiers.Length - 1));
                int radius = tiers.Length < 4 ? 0 : Math.Max(1, tiers.Length / 10);
                int first = Math.Max(0, center - radius), last = Math.Min(tiers.Length - 1, center + radius);
                int[] choices = tiers.Skip(first).Take(last - first + 1)
                    .Where(id => slot != 5 || id != actor.Equipment[4]).ToArray();
                actor.Equipment[slot] = choices.Length > 0 ? choices[random.Next(choices.Length)] : tiers[center];
            }
            // Statuses are deliberately untouched. Weapon effects and armor
            // passives are compiled and settled by the normal battle rules.
        }

        private int[] Tiers(int actorId, int slot)
        {
            if (_tiers.TryGetValue((actorId, slot), out int[] cached))
                return cached;
            int identity = BattleData.PlayerIdentity(actorId);
            int type = slot switch
            {
                0 => 11,
                1 => 8,
                2 => 7,
                3 => 9,
                _ => 10
            };
            BattleParticipant probe = _data.CreateParticipant(actorId, BattleSide.Ally, BattleSlot.Water);
            probe.Level = 50;
            probe.AttributeSource = BattleAttributeSource.LevelGrowth;
            probe.Equipment = new int[6];
            BattleActorDefinition bare = BattleDefinitionCompiler.Actor(_data, probe, BattleData.Variant);
            int[] baseline = BattleAttributeCalculator.Calculate(probe, bare, BattleData.Variant);
            var ranked = new List<(int id, double score, int price)>();
            foreach (var item in _data.Database.GameItemInfos.Values.Where(item => (int)item.Type == type &&
                item.Id != BattleMagicSwordRules.WeaponId && item.ApplicableActors.Contains((PlayerActorId)identity)))
            {
                probe.Equipment[slot] = checked((int)item.Id);
                BattleActorDefinition definition = BattleDefinitionCompiler.Actor(_data, probe, BattleData.Variant);
                if (definition.Equipment.Any(equipment => equipment.UnsupportedReason.Length > 0))
                    continue;
                int[] equipped = BattleAttributeCalculator.Calculate(probe, definition, BattleData.Variant);
                double score = Enumerable.Range(0, 12).Sum(index => (equipped[index] - baseline[index]) / (double)Math.Max(1, baseline[index]));
                ranked.Add((checked((int)item.Id), score, item.Price));
            }
            return _tiers[(actorId, slot)] = ranked.OrderBy(item => item.score).ThenBy(item => item.price).ThenBy(item => item.id).Select(item => item.id).ToArray();
        }
    }
}
