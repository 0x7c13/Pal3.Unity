// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Linq;
    using Battle;
    using Core.Contract.Enums;

    // Editor-only initial state: unlock legal personal skills without changing
    // growth, costs, equipment, form restrictions or the original-data factory.
    public static class BattleTestLoadoutBuilder
    {
        // A test inventory belongs to a newly authored scenario, never to a
        // loaded preset or the game's persistent inventory.
        public static BattleScenario CreateScenario(BattleData data)
        {
            var scenario = new BattleScenario { Variant = BattleData.Variant, DataFingerprint = data?.Fingerprint ?? "" };
            if (data == null)
                return scenario;
            foreach (BattleAbilityDefinition item in data.Database.GameItemInfos.Values.Where(item => item.Id > 0)
                .Select(BattleDefinitionCompiler.Item).Where(item => item.BattleUsable && item.Supported).OrderBy(item => item.Id))
            {
                scenario.Items.Add(new BattleItem { Id = item.Id, Count = 99 });
                if (item.IsTreasure)
                    scenario.TreasureUses.Add(new BattleItem { Id = item.Id, Count = 3 });
            }
            return scenario;
        }

        public static BattleParticipant Create(BattleData data, int actorId, BattleSide side, BattleSlot slot)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            BattleParticipant actor = data.CreateParticipant(actorId, side, slot);
            int identity = BattleData.PlayerIdentity(actorId);
            if (side != BattleSide.Ally || identity < 0 || data.Database.CombatActorInfos[actorId].Type != CombatActorType.MainActor)
                return actor;
            actor.Skills = data.Database.SkillOrder.Where(id => id > 0).Select(id => data.Database.SkillInfos[id])
                .Where(skill => (int)skill.Type is >= 0 and <= 3 && skill.ApplicableActors.Contains((PlayerActorId)identity))
                .Select(skill => new BattleSkill { Id = checked((int)skill.Id), Level = 4, Mastered = true }).ToList();
            // Weapon magic is selected through the captured magic sword. The
            // PAL3A five-spirit wheel and Nangong form use their own commands.
            return actor;
        }
    }
}
