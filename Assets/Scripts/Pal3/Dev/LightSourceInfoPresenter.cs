﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Dev
{
    using Core.DataReader.Lgt;
    using UnityEngine;

    /// <summary>
    /// LightNode holder component to present LightNode in the Unity inspector.
    /// </summary>
    public class LightSourceInfoPresenter : MonoBehaviour
    {
        [SerializeField] public LightNode LightNode;
    }
}