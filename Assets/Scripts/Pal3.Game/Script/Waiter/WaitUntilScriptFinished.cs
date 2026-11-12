// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Script.Waiter
{
    using System;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using UnityEngine;

    public sealed class WaitUntilScriptFinished : CustomYieldInstruction, IDisposable,
        ICommandExecutor<ScriptFinishedRunningNotification>,
        ICommandExecutor<ResetGameStateCommand>
    {
        public override bool keepWaiting => !_isFinished;
        public bool IsCanceled { get; private set; }

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
                CommandExecutorRegistry<ICommand>.Instance.UnRegister<ScriptFinishedRunningNotification>(this);
            }
        }

        public void Execute(ResetGameStateCommand command)
        {
            IsCanceled = true;
            Dispose();
        }

        public void Dispose()
        {
            if (!_isFinished)
            {
                IsCanceled = true;
                _isFinished = true;
            }
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }
    }
}