// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    using State;

    public class GameStateChangedNotification : ICommand
    {
        public GameStateChangedNotification(GameState previousState, GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }

        public GameState PreviousState { get; }
        public GameState NewState { get; }
    }
}