// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Script.Waiter
{
    using Command;
    using Command.Extensions;
    using Core.Command;
    using UnityEngine;

    public sealed class WaitUntilScriptFinished : CustomYieldInstruction,
        ICommandExecutor<ScriptFinishedRunningNotification>
    {
        public override bool keepWaiting => !_isFinished;

        private readonly PalScriptType _scriptType;
        private readonly uint _scriptId;
        private bool _isFinished;

        public WaitUntilScriptFinished(PalScriptType scriptType, uint scriptId)
        {
            _scriptType = scriptType;
            _scriptId = scriptId;
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Execute(ScriptFinishedRunningNotification command)
        {
            if (_scriptType == command.ScriptType && _scriptId == command.ScriptId)
            {
                _isFinished = true;
                CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            }
        }
    }
}