// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Ini
{
    using UnityEngine;

    public struct FiveElementsFormationConfig
    {
        public Vector3 CenterGameBoxPosition;
        public float GameBoxRadius;
    }

    public sealed class CombatConfigFile
    {
        public Vector3[] ActorGameBoxPositions { get; }

        public FiveElementsFormationConfig AllyFormationConfig { get; }

        public FiveElementsFormationConfig EnemyFormationConfig { get; }

        public int[] LevelExperienceTable { get; }

        public CombatConfigFile(Vector3[] actorGameBoxPositions,
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