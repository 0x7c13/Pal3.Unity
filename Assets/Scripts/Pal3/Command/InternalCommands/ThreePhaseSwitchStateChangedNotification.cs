// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    #if PAL3A
    public class ThreePhaseSwitchStateChangedNotification : ICommand
    {
        public ThreePhaseSwitchStateChangedNotification(
            int objectId,
            int previousState,
            int currentState)
        {
            ObjectId = objectId;
            PreviousState = previousState;
            CurrentState = currentState;
        }

        public int ObjectId { get; }
        public int PreviousState { get; }
        public int CurrentState { get; }
    }
    #endif
}