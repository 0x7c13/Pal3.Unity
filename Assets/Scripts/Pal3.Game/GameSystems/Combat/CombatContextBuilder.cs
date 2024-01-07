// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Combat
{
    using Core.Contract.Enums;
    using Script.Waiter;

    public sealed class CombatContextBuilder
    {
        private CombatContext _context = new();

        public CombatContextBuilder WithScriptWaiter(WaitUntilCanceled scriptWaiter)
        {
            _context.ScriptWaiter = scriptWaiter;
            return this;
        }

        public CombatContextBuilder WithCombatSceneName(string combatSceneName)
        {
            _context.CombatSceneName = combatSceneName;
            return this;
        }

        public CombatContextBuilder WithCombatSceneElementType(ElementType combatSceneElementType)
        {
            _context.CombatSceneElementType = combatSceneElementType;
            return this;
        }

        public CombatContextBuilder WithIsScriptTriggeredCombat(bool isScriptTriggeredCombat)
        {
            _context.IsScriptTriggeredCombat = isScriptTriggeredCombat;
            return this;
        }

        public CombatContextBuilder WithMeetType(MeetType meetType)
        {
            _context.MeetType = meetType;
            return this;
        }

        public CombatContextBuilder WithCombatMusicName(string combatMusicName)
        {
            _context.CombatMusicName = combatMusicName;
            return this;
        }

        public CombatContextBuilder WithMaxRound(int maxRound)
        {
            _context.MaxRound = maxRound;
            return this;
        }

        public CombatContextBuilder WithUnbeatable(bool isUnbeatable)
        {
            _context.IsUnbeatable = isUnbeatable;
            return this;
        }

        public CombatContextBuilder WithNoGameOverWhenLose(bool isNoGameOverWhenLose)
        {
            _context.IsNoGameOverWhenLose = isNoGameOverWhenLose;
            return this;
        }

        public CombatContextBuilder WithEnemyIds(
            uint enemy1Id,
            uint enemy2Id,
            uint enemy3Id,
            uint enemy4Id,
            uint enemy5Id,
            uint enemy6Id)
        {
            _context.EnemyIds = new []
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
            _context = new CombatContext();
        }

        public CombatContext CurrentContext => _context;

        public CombatContext Build() => _context;
    }
}