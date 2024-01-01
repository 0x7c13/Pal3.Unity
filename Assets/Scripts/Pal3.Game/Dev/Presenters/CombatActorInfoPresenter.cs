// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Dev.Presenters
{
    using Core.DataReader.Gdb;
    using Engine.Core.Implementation;

    /// <summary>
    /// CombatActorInfo holder component to present ScnObjectInfo in the Unity inspector.
    /// </summary>
    public sealed class CombatActorInfoPresenter : GameEntityScript
    {
        [UnityEngine.SerializeField] public CombatActorInfo combatActorInfo;
    }
}