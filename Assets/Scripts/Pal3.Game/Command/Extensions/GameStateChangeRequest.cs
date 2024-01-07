// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;
    using State;

    public sealed class GameStateChangeRequest : ICommand
    {
        public GameStateChangeRequest(GameState newState)
        {
            NewState = newState;
        }

        public GameState NewState { get; }
    }
}