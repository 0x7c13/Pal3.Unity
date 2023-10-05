// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;
    using UnityEngine;

    [AvailableInConsole]
    public class CameraSetInitialStateOnNextSceneLoadCommand : ICommand
    {
        public CameraSetInitialStateOnNextSceneLoadCommand(Vector3 initRotationInEulerAngles, int initTransformOption)
        {
            InitRotationInEulerAngles = initRotationInEulerAngles;
            InitTransformOption = initTransformOption;
        }

        public Vector3 InitRotationInEulerAngles { get; }
        public int InitTransformOption { get; }
    }
}