// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    using Script.Waiter;

    public class ScriptRunnerAddWaiterRequest : ICommand
    {
        public ScriptRunnerAddWaiterRequest(IScriptRunnerWaiter waiter)
        {
            Waiter = waiter;
        }

        public IScriptRunnerWaiter Waiter { get; }
    }
}