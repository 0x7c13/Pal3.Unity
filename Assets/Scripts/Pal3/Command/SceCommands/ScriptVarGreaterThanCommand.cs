// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(6, "A>B?, A Greater than B?")]
    public class ScriptVarGreaterThanCommand : ICommand
    {
        public ScriptVarGreaterThanCommand(int variable, int value)
        {
            Variable = variable;
            Value = value;
        }

        public int Variable { get; }
        public int Value { get; }
    }
}