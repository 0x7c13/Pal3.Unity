// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Ini
{
    using Primitives;

    public struct FiveElementsFormationConfig
    {
        public GameBoxVector3 CenterGameBoxPosition;
        public float GameBoxRadius;
    }

    public sealed class CombatConfigFile
    {
        public GameBoxVector3[] ActorGameBoxPositions { get; }

        public FiveElementsFormationConfig AllyFormationConfig { get; }

        public FiveElementsFormationConfig EnemyFormationConfig { get; }

        public int[] LevelExperienceTable { get; }

        public CombatConfigFile(GameBoxVector3[] actorGameBoxPositions,
            FiveElementsFormationConfig allyFormationConfig,
            FiveElementsFormationConfig enemyFormationConfig,
            int[] levelExperienceTable)
        {
            ActorGameBoxPositions = actorGameBoxPositions;
            AllyFormationConfig = allyFormationConfig;
            EnemyFormationConfig = enemyFormationConfig;
            LevelExperienceTable = levelExperienceTable;
        }
    }
}