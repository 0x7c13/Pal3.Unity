// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using Core.DataReader.Scn;
    using UnityEngine;

    /// <summary>
    /// ScnNpcInfo holder component to present ScnNpcInfo in the Unity inspector
    /// </summary>
    public class NpcInfoPresenter : MonoBehaviour
    {
        [SerializeField] public ScnNpcInfo NpcInfo;
    }
}