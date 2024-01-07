// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Dev.Presenters
{
    using Core.DataReader.Scn;
    using Engine.Core.Implementation;

    /// <summary>
    /// ScnObjectInfo holder component to present ScnObjectInfo in the Unity inspector.
    /// </summary>
    public sealed class SceneObjectInfoPresenter : GameEntityScript
    {
        [UnityEngine.SerializeField] public ScnObjectInfo sceneObjectInfo;
    }
}