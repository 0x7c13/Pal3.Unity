// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Dev.Presenters
{
    using Core.DataReader.Lgt;
    using Engine.Core.Implementation;

    /// <summary>
    /// LightNode holder component to present LightNode in the Unity inspector.
    /// </summary>
    public sealed class LightSourceInfoPresenter : GameEntityScript
    {
        [UnityEngine.SerializeField] public LightNode lightNode;
    }
}