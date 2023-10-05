// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Script.Patcher
{
    using System.Collections.Generic;
    using Core.Command;

    public interface IPalScriptPatcher
    {
        public Dictionary<string, ICommand> PatchedCommands { get; }

        bool TryPatchCommandInScript(PalScriptType scriptType,
            uint scriptId,
            string scriptDescription,
            long positionInScript,
            int codepage,
            ICommand command,
            out ICommand fixedCommand)
        {
            string cmdHashKey = $"{codepage}_{scriptType}_{scriptId}_{scriptDescription}" +
                                $"_{positionInScript}_{command.GetType()}";

            if (PatchedCommands.TryGetValue(cmdHashKey, out ICommand patchedCommand))
            {
                fixedCommand = patchedCommand;
                return true;
            }

            fixedCommand = command;
            return false;
        }
    }
}