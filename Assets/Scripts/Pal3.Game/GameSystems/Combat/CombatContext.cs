// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Combat
{
    using Core.Contract.Enums;
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
        public WaitUntilCanceled ScriptWaiter { get; set; } = null; // null means no script waiter
        public string CombatSceneName { get; set; } = string.Empty;
        public ElementType CombatSceneElementType { get; set; } = ElementType.None;
        public bool IsScriptTriggeredCombat { get; set; } = false;
        public MeetType MeetType { get; set; } = MeetType.RunningIntoEachOther;
        public string CombatMusicName { get; set; } = string.Empty;
        public int MaxRound { get; set; } = -1; // -1 means no limit
        public bool IsUnbeatable { get; set; } = false;
        public bool IsNoGameOverWhenLose { get; set; } = false;
        public uint[] EnemyIds { get; set; } = new uint[6];

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