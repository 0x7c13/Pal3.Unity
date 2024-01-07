// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    #if PAL3A
    using Core.Command;

    public sealed class ThreePhaseSwitchStateChangedNotification : ICommand
    {
        public ThreePhaseSwitchStateChangedNotification(
            int objectId,
            int previousState,
            int currentState,
            bool isBridgeMovingAlongYAxis)
        {
            ObjectId = objectId;
            PreviousState = previousState;
            CurrentState = currentState;
            IsBridgeMovingAlongYAxis = isBridgeMovingAlongYAxis;
        }

        public int ObjectId { get; }
        public int PreviousState { get; }
        public int CurrentState { get; }
        public bool IsBridgeMovingAlongYAxis { get; }
    }
    #endif
}