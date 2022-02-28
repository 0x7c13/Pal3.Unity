// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Core.DataReader.Scn;
    using UnityEngine;

    /// <summary>
    /// ScnObjectInfo holder component to present ScnObjectInfo in the Unity inspector
    /// </summary>
    public class SceneObjectInfoPresenter : MonoBehaviour
    {
        [SerializeField] public ScnObjectInfo SceneObjectInfo;
    }
}