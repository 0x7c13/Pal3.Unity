// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Script
{
    using Core.Command;
    using Engine.Utilities;
    using Patcher;

    public sealed class PalScriptCommandPreprocessor
    {
        private readonly IPalScriptPatcher _patcher;

        public PalScriptCommandPreprocessor(IPalScriptPatcher patcher)
        {
            _patcher = Requires.IsNotNull(patcher, nameof(patcher));
        }

        public void Process(ref ICommand command,
            PalScriptType scriptType,
            uint scriptId,
            string scriptDescription,
            long positionInScript,
            int codepage)
        {
            if (_patcher.TryPatchCommandInScript(scriptType,
                    scriptId,
                    scriptDescription,
                    positionInScript,
                    codepage,
                    command,
                    out ICommand fixedCommand))
            {
                command = fixedCommand;
            }
        }
    }
}