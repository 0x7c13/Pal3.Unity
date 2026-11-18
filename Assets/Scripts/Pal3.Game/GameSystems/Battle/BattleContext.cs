// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Battle
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
    public sealed class BattleContext
    {
        public WaitUntilCanceled ScriptWaiter { get; set; } = null; // null means no script waiter
        public string BattleSceneName { get; set; } = string.Empty;
        public ElementType BattleSceneElementType { get; set; } = ElementType.None;
        public bool IsScriptTriggeredBattle { get; set; } = false;
        public MeetType MeetType { get; set; } = MeetType.RunningIntoEachOther;
        public string BattleMusicName { get; set; } = string.Empty;
        public int MaxRound { get; set; } = -1; // -1 means no limit
        public bool IsUnbeatable { get; set; } = false;
        public bool IsNoGameOverWhenLose { get; set; } = false;
        public uint[] EnemyIds { get; set; } = new uint[6];

#if PAL3A
        public bool NanGongHuangAbilitiesEnabled
        {
            get; set;
        }

        public bool WangPengXuTransformEnabled
        {
            get; set;
        }

        public bool NanGongHuangStartsTransformed
        {
            get; set;
        }

        public bool WangPengXuStartsTransformed
        {
            get; set;
        }

        public int LiaoRiHpBonus
        {
            get; set;
        }

        public int LiaoRiMpBonus
        {
            get; set;
        }
#endif

        public override string ToString()
        {
            return $"BattleContext: " +
                   $"BattleScene: {BattleSceneName} " +
                   $"ElementType: {BattleSceneElementType} " +
                   $"BattleMusic: {BattleMusicName} " +
                   $"IsScriptTriggeredBattle: {IsScriptTriggeredBattle} " +
                   $"MeetType: {MeetType} " +
                   $"MaxRound: {MaxRound} " +
                   $"IsUnbeatable: {IsUnbeatable} " +
                   $"IsNoGameOverWhenLose: {IsNoGameOverWhenLose} " +
                   $"Enemies: {string.Join(" ", EnemyIds)}";
        }
    }
}
