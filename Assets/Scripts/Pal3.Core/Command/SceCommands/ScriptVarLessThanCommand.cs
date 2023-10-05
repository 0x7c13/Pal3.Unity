// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(7, "A<B?, A Less than B?")]
    public class ScriptVarLessThanCommand : ICommand
    {
        public ScriptVarLessThanCommand(int variable, int value)
        {
            Variable = variable;
            Value = value;
        }

        public int Variable { get; }
        public int Value { get; }
    }
}