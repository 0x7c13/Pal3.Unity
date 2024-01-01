// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;

    [AvailableInConsole]
    public sealed class CameraSetInitialStateOnNextSceneLoadCommand : ICommand
    {
        public CameraSetInitialStateOnNextSceneLoadCommand(float xEulerAngle,
            float yEulerAngle,
            float zEulerAngle,
            int initTransformOption)
        {
            XEulerAngle = xEulerAngle;
            YEulerAngle = yEulerAngle;
            ZEulerAngle = zEulerAngle;
            InitTransformOption = initTransformOption;
        }

        public float XEulerAngle { get; }
        public float YEulerAngle { get; }
        public float ZEulerAngle { get; }
        public int InitTransformOption { get; }
    }
}