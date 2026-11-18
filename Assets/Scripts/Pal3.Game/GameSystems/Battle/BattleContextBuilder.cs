// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Battle
{
    using Core.Contract.Enums;
    using Script.Waiter;

    public sealed class BattleContextBuilder
    {
        private BattleContext _context = new();

        public BattleContextBuilder WithScriptWaiter(WaitUntilCanceled scriptWaiter)
        {
            _context.ScriptWaiter = scriptWaiter;
            return this;
        }

        public BattleContextBuilder WithBattleSceneName(string combatSceneName)
        {
            _context.BattleSceneName = combatSceneName;
            return this;
        }

        public BattleContextBuilder WithBattleSceneElementType(ElementType combatSceneElementType)
        {
            _context.BattleSceneElementType = combatSceneElementType;
            return this;
        }

        public BattleContextBuilder WithIsScriptTriggeredBattle(bool isScriptTriggeredCombat)
        {
            _context.IsScriptTriggeredBattle = isScriptTriggeredCombat;
            return this;
        }

        public BattleContextBuilder WithMeetType(MeetType meetType)
        {
            _context.MeetType = meetType;
            return this;
        }

        public BattleContextBuilder WithBattleMusicName(string combatMusicName)
        {
            _context.BattleMusicName = combatMusicName;
            return this;
        }

        public BattleContextBuilder WithMaxRound(int maxRound)
        {
            _context.MaxRound = maxRound;
            return this;
        }

        public BattleContextBuilder WithUnbeatable(bool isUnbeatable)
        {
            _context.IsUnbeatable = isUnbeatable;
            return this;
        }

        public BattleContextBuilder WithNoGameOverWhenLose(bool isNoGameOverWhenLose)
        {
            _context.IsNoGameOverWhenLose = isNoGameOverWhenLose;
            return this;
        }

        public BattleContextBuilder WithEnemyIds(
            uint enemy1Id,
            uint enemy2Id,
            uint enemy3Id,
            uint enemy4Id,
            uint enemy5Id,
            uint enemy6Id)
        {
            _context.EnemyIds = new[]
            {
                enemy1Id,
                enemy2Id,
                enemy3Id,
                enemy4Id,
                enemy5Id,
                enemy6Id
            };
            return this;
        }

        public void ResetContext()
        {
            _context = new BattleContext();
        }

        public BattleContext CurrentContext => _context;

        public BattleContext Build() => _context;
    }
}
