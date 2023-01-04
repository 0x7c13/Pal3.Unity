// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    using UnityEngine.InputSystem;

    public class ActiveInputDeviceChangedNotification : ICommand
    {
        public ActiveInputDeviceChangedNotification(InputDevice device)
        {
            Device = device;
        }

        public InputDevice Device { get; }
    }
}