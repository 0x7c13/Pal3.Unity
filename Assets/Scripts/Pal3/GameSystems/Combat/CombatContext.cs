// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.GameSystems.Combat
{
    using Core.Contracts;
    using Script.Waiter;

    public enum MeetType
    {
        RunningIntoEachOther,
        PlayerChasingEnemy,
        EnemyChasingPlayer,
    }

    /// <summary>
    /// Data contract for combat context.
    /// </summary>
    public sealed class CombatContext
    {
        public WaitUntilCanceled ScriptWaiter { get; private set; }
        public string CombatSceneName { get; private set; }
        public ElementType CombatSceneElementType { get; private set; }
        public bool IsScriptTriggeredCombat { get; private set; }
        public MeetType MeetType { get; private set; }
        public string CombatMusicName { get; private set; }
        public int MaxRound { get; private set; }
        public bool IsUnbeatable { get; private set; }
        public bool IsNoGameOverWhenLose { get; private set; }
        public uint[] EnemyIds { get; private set; }

        public CombatContext()
        {
            ResetContext();
        }

        public void SetScriptWaiter(WaitUntilCanceled scriptWaiter)
        {
            ScriptWaiter = scriptWaiter;
        }

        public void SetCombatSceneName(string combatSceneName)
        {
            CombatSceneName = combatSceneName;
        }

        public void SetCombatSceneElementType(ElementType combatSceneElementType)
        {
            CombatSceneElementType = combatSceneElementType;
        }

        public void SetIsScriptTriggeredCombat(bool isScriptTriggeredCombat)
        {
            IsScriptTriggeredCombat = isScriptTriggeredCombat;
        }

        public void SetMeetType(MeetType meetType)
        {
            MeetType = meetType;
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

        public void SetEnemyIds(
            uint enemy1Id,
            uint enemy2Id,
            uint enemy3Id,
            uint enemy4Id,
            uint enemy5Id,
            uint enemy6Id)
        {
            EnemyIds = new []
            {
                enemy1Id,
                enemy2Id,
                enemy3Id,
                enemy4Id,
                enemy5Id,
                enemy6Id
            };
        }

        public void ResetContext()
        {
            ScriptWaiter = null;
            CombatSceneName = string.Empty;
            CombatSceneElementType = ElementType.None;
            CombatMusicName = string.Empty;
            IsScriptTriggeredCombat = false;
            MeetType = MeetType.RunningIntoEachOther;
            MaxRound = -1; // -1 means no limit
            IsUnbeatable = false;
            IsNoGameOverWhenLose = false;
            EnemyIds = new uint[6];
        }

        public override string ToString()
        {
            return $"CombatContext: " +
                   $"CombatScene: {CombatSceneName} " +
                   $"ElementType: {CombatSceneElementType} " +
                   $"CombatMusic: {CombatMusicName} " +
                   $"IsScriptTriggeredCombat: {IsScriptTriggeredCombat} " +
                   $"MeetType: {MeetType} " +
                   $"MaxRound: {MaxRound} " +
                   $"IsUnbeatable: {IsUnbeatable} " +
                   $"IsNoGameOverWhenLose: {IsNoGameOverWhenLose} " +
                   $"Enemies: {string.Join(" ", EnemyIds)}";
        }
    }
}