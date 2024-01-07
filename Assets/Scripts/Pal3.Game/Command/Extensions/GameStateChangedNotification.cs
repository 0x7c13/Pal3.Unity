// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;
    using State;

    public sealed class GameStateChangedNotification : ICommand
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