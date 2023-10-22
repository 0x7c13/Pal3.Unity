// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;

    public sealed class CameraSetFieldOfViewCommand : ICommand
    {
        public CameraSetFieldOfViewCommand(float fieldOfView)
        {
            FieldOfView = fieldOfView;
        }

        public float FieldOfView { get; }
    }
}