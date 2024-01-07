// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Dev.Presenters
{
    using Core.Primitives;
    using Engine.Core.Implementation;

    /// <summary>
    /// BlendFlag and GameBoxMaterial holder component to present MaterialInfo in the Unity inspector.
    /// </summary>
    public sealed class MaterialInfoPresenter : GameEntityScript
    {
        [UnityEngine.SerializeField] public GameBoxBlendFlag blendFlag;
        [UnityEngine.SerializeField] public GameBoxMaterial material;
    }
}