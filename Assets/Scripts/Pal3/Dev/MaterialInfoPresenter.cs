// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Dev
{
    using Core.GameBox;
    using UnityEngine;

    /// <summary>
    /// BlendFlag and GameBoxMaterial holder component to present MaterialInfo in the Unity inspector.
    /// </summary>
    public class MaterialInfoPresenter : MonoBehaviour
    {
        [SerializeField] public GameBoxBlendFlag blendFlag;
        [SerializeField] public GameBoxMaterial material;
    }
}