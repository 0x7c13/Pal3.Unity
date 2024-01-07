// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command
{
    /// <summary>
    /// ICommandExecutor interface
    /// </summary>
    /// <typeparam name="TCommand">Command type</typeparam>
    public interface ICommandExecutor<in TCommand>
    {
        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="command">TCommand instance</param>
        public void Execute(TCommand command);
    }
}
