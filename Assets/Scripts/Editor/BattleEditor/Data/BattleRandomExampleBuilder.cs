// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Battle;
    using Core.Contract.Constants;
    using Core.Contract.Enums;

    // Uses a separate authoring RNG: generating examples never consumes battle RNG.
    public sealed class BattleRandomExampleBuilder
    {
        private readonly BattleData _data;
        private readonly int[] _allies;
        private readonly IGrouping<int, int>[] _enemyLevels;
        private readonly BattleRandomEquipmentBuilder _equipment;

        public BattleRandomExampleBuilder(BattleData data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _equipment = new BattleRandomEquipmentBuilder(data);
            _allies = ActorConstants.MainActorCombatActorIdMap.Values.OrderBy(id => id)
                .Where(id => Available(id, BattleSide.Ally)).ToArray();
            _enemyLevels = data.Database.CombatActorInfos.OrderBy(pair => pair.Key)
                .Where(pair => pair.Key > 0 && pair.Value.Type != CombatActorType.MainActor &&
                    pair.Value.Level is >= 1 and <= 100 && pair.Value.AttributeValues[ActorAttributeType.Hp] > 0 &&
                    pair.Value.SpecialActionId == 0 && data.Database.SkillInfos.ContainsKey((int)pair.Value.NormalAttackModeId))
                .Select(pair => pair.Key).Where(id => Available(id, BattleSide.Enemy))
                .GroupBy(id => data.Database.CombatActorInfos[id].Level).ToArray();
            if (_allies.Length == 0 || _enemyLevels.Length == 0 || data.Scenes.Length == 0)
                throw new InvalidOperationException("No complete original-data roster or battle scene is available for random examples.");
        }

        private bool Available(int id, BattleSide side)
        {
            if (!_data.Database.CombatActorInfos.ContainsKey(id))
                return false;
            BattleParticipant actor = _data.CreateParticipant(id, side, BattleSlot.Water);
            try
            {
                return _data.TryActionPath(actor, _data.IdleAction(actor), out _);
            }
            catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException or InvalidDataException or
                NotSupportedException or InvalidOperationException or FormatException)
            {
                // An incomplete resource set can still supply other test participants.
                return false;
            }
        }

        public BattleScenario Create(Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            BattleScenario example = BattleTestLoadoutBuilder.CreateScenario(_data);
            example.Scene = _data.Scenes[random.Next(_data.Scenes.Length)];
            example.Seed = random.Next();
            example.SceneElement = (int)_data.SceneMap.CombatSceneElementTypeInfo.GetValueOrDefault(example.Scene);
            IGrouping<int, int> level = _enemyLevels[random.Next(_enemyLevels.Length)];
            int[] enemies = level.ToArray();
            int[] allies = Shuffle(_allies, random);
            BattleSlot[] slots = Shuffle((BattleSlot[])Enum.GetValues(typeof(BattleSlot)), random);
            int allyCount = Math.Min(4, allies.Length);
            for (int index = 0; index < allyCount; index++)
            {
                BattleParticipant actor = _data.CreateTestParticipant(allies[index], BattleSide.Ally, slots[index]);
                actor.Level = Math.Min(100, level.Key + 3);
                actor.AttributeSource = BattleAttributeSource.LevelGrowth;
                // The reader indexes by actual level; the original final level uses INT_MAX.
                actor.Experience = actor.Level < _data.Layout.LevelExperienceTable.Length ? _data.Layout.LevelExperienceTable[actor.Level] : int.MaxValue;
                _equipment.Equip(actor, random);
                example.Participants.Add(actor);
            }
            slots = Shuffle(slots, random);
            int enemyCount = random.Next(1, 7);
            for (int index = 0; index < enemyCount; index++)
                example.Participants.Add(_data.CreateParticipant(enemies[random.Next(enemies.Length)], BattleSide.Enemy, slots[index]));

            // Compile growth and equipment through the same adapter as a real battle.
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, _data.Capture(example));
            foreach (BattleParticipant actor in example.Participants.Where(actor => actor.Side == BattleSide.Ally))
            {
                BattleActorDefinition definition = definitions.Actors[actor.InstanceId];
                actor.Attributes = BattleAttributeCalculator.Calculate(actor, definition, example.Variant);
                actor.Vitals = actor.Attributes.Take(3).ToArray();
            }
            return example;
        }

        private static T[] Shuffle<T>(IEnumerable<T> source, Random random)
        {
            T[] values = source.ToArray();
            for (int index = values.Length - 1; index > 0; index--)
            {
                int other = random.Next(index + 1);
                (values[index], values[other]) = (values[other], values[index]);
            }
            return values;
        }
    }
}
