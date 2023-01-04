// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    using State;

    public class GameStateChangeRequest : ICommand
    {
        public GameStateChangeRequest(GameState newState)
        {
            NewState = newState;
        }
        
        public GameState NewState { get; }
    }
}