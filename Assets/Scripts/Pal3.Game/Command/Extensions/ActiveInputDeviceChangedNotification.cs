// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;
    using UnityEngine.InputSystem;

    public sealed class ActiveInputDeviceChangedNotification : ICommand
    {
        public ActiveInputDeviceChangedNotification(InputDevice device)
        {
            Device = device;
        }

        public InputDevice Device { get; }
    }
}