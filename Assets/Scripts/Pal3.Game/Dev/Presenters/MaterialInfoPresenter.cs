// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Dev.Presenters
{
    using Core.Primitives;
    using Engine.Abstraction;
    using UnityEngine;

    /// <summary>
    /// BlendFlag and GameBoxMaterial holder component to present MaterialInfo in the Unity inspector.
    /// </summary>
    public sealed class MaterialInfoPresenter : GameEntityBase
    {
        [SerializeField] public GameBoxBlendFlag blendFlag;
        [SerializeField] public GameBoxMaterial material;
    }
}