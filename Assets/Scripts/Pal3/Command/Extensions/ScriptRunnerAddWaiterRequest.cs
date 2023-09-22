// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.Extensions
{
    using Core.Command;
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