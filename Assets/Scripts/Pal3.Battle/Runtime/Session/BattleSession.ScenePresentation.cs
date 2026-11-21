// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    public sealed partial class BattleSession
    {
        public BattleEncounter Encounter => _scenario.Encounter;

        public int SceneElement => _scenario.SceneElement;
    }
}
