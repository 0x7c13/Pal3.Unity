// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Combat
{
    using Core.DataReader.Gdb;

    /// <summary>
    /// Data contract for combat context.
    /// </summary>
    public sealed class CombatContext
    {
        public string CombatSceneName { get; private set; }
        public WuLingType? CombatSceneWuLingType { get; private set; }
        public string CombatMusicName { get; private set; }
        public int MaxRound { get; private set; }
        public bool IsUnbeatable { get; private set; }
        public bool IsNoGameOverWhenLose { get; private set; }
        public uint[] MonsterIds { get; private set; }

        public CombatContext()
        {
            ResetContext();
        }

        public void SetCombatSceneName(string combatSceneName)
        {
            CombatSceneName = combatSceneName;
        }

        public void SetCombatSceneWuLingType(WuLingType combatSceneWuLingType)
        {
            CombatSceneWuLingType = combatSceneWuLingType;
        }

        public void SetCombatMusicName(string combatMusicName)
        {
            CombatMusicName = combatMusicName;
        }

        public void SetMaxRound(int maxRound)
        {
            MaxRound = maxRound;
        }

        public void SetUnbeatable(bool isUnbeatable)
        {
            IsUnbeatable = isUnbeatable;
        }

        public void SetNoGameOverWhenLose(bool isNoGameOverWhenLose)
        {
            IsNoGameOverWhenLose = isNoGameOverWhenLose;
        }

        public void SetMonsterIds(
            uint monster1Id,
            uint monster2Id,
            uint monster3Id,
            uint monster4Id,
            uint monster5Id,
            uint monster6Id)
        {
            MonsterIds = new uint[6]
            {
                monster1Id,
                monster2Id,
                monster3Id,
                monster4Id,
                monster5Id,
                monster6Id
            };
        }

        public void ResetContext()
        {
            CombatSceneName = null;
            CombatSceneWuLingType = null;
            CombatMusicName = null;
            MaxRound = -1; // -1 means no limit
            IsUnbeatable = false;
            IsNoGameOverWhenLose = false;
            MonsterIds = new uint[6];
        }

        public override string ToString()
        {
            return $"CombatContext: CombatScene: {CombatSceneName} " +
                   $"WuLingType: {CombatSceneWuLingType} " +
                   $"CombatMusic: {CombatMusicName} " +
                   $"MaxRound: {MaxRound} " +
                   $"IsUnbeatable: {IsUnbeatable} " +
                   $"IsNoGameOverWhenLose: {IsNoGameOverWhenLose} " +
                   $"Monsters: {string.Join(" ", MonsterIds)}";
        }
    }
}