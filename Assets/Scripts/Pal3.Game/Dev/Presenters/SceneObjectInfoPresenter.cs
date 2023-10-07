// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Dev.Presenters
{
    using Core.DataReader.Scn;
    using Engine.Abstraction;
    using UnityEngine;

    /// <summary>
    /// ScnObjectInfo holder component to present ScnObjectInfo in the Unity inspector.
    /// </summary>
    public sealed class SceneObjectInfoPresenter : GameEntityScript
    {
        [SerializeField] public ScnObjectInfo sceneObjectInfo;
    }
}