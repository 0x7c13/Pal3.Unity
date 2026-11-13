// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Script
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Enums;
    using Core.DataReader;
    using Core.DataReader.Sce;
    using Engine.Logging;
    using Engine.Services;
    using GameSystems.Inventory;
    using GameSystems.Team;
    using Newtonsoft.Json;
    using Patcher;
    using Waiter;

    public sealed class PalScriptRunner : IDisposable,
        ICommandExecutor<ScriptRunnerChangeExecutionModeCommand>,
        ICommandExecutor<ScriptRunnerSetOperatorCommand>,
        ICommandExecutor<ScriptRunnerGotoCommand>,
        ICommandExecutor<ScriptRunnerGotoIfNotCommand>,
        ICommandExecutor<ScriptRunnerWaitUntilTimeCommand>,
        ICommandExecutor<ScriptRunnerAddWaiterRequest>,
        ICommandExecutor<ScriptEvaluateVarIsGreaterThanCommand>,
        ICommandExecutor<ScriptEvaluateVarIsGreaterThanOrEqualToCommand>,
        ICommandExecutor<ScriptEvaluateVarIsGreaterThanOrEqualToAnotherVarCommand>,
        ICommandExecutor<ScriptEvaluateVarIsEqualToCommand>,
        ICommandExecutor<ScriptEvaluateVarIsNotEqualToCommand>,
        ICommandExecutor<ScriptEvaluateVarIsLessThanCommand>,
        ICommandExecutor<ScriptEvaluateVarIsLessThanOrEqualToCommand>,
        ICommandExecutor<ScriptEvaluateVarIsInRangeCommand>,
        ICommandExecutor<ScriptEvaluateVarIfPlayerHaveItemCommand>,
        ICommandExecutor<ScriptEvaluateVarIfActorInTeamCommand>
    {
        public event EventHandler<ICommand> OnCommandExecutionRequested;

        public uint ScriptId { get; }
        public PalScriptType ScriptType { get; }
        public string ScriptDescription { get; }

        public bool IsCompleted => _isDisposed || _isCompleted;
        internal IUserVariableStore<ushort, int> Variables => _userVariableStore;

        private readonly ISceCommandParser _sceCommandParser;
        private readonly IPalScriptPatcher _scriptPatcher;
        private readonly IUserVariableStore<ushort, int> _userVariableStore;

        private readonly int _codepage;
        private readonly IBinaryReader _scriptDataReader;
        private ScriptExecutionMode _executionMode;

        // Stack operator and value are used to evaluate logical expressions
        // over multiple commands and branches in the script in assembly fashion.
        private ScriptOperatorType _operatorType = ScriptOperatorType.Assign;
        private bool _tempVariable = false;

        private readonly List<IScriptRunnerWaiter> _waiters = new ();
        private PalScriptRunner _calledScript;
        private bool _isCompleted;
        private bool _isExecuting;
        private bool _isDisposed;

        public static PalScriptRunner Create(SceFile sceFile,
            PalScriptType scriptType,
            uint scriptId,
            IUserVariableStore<ushort, int> userVariableStore,
            ISceCommandParser sceCommandParser,
            IPalScriptPatcher scriptPatcher)
        {
            if (!sceFile.ScriptBlocks.TryGetValue(scriptId, out SceScriptBlock sceScriptBlock))
            {
                throw new ArgumentException($"Invalid script id: {scriptId}");
            }

            EngineLogger.Log($"Create script runner: [{sceScriptBlock.Id} {sceScriptBlock.Description}]");

            return new PalScriptRunner(scriptType,
                scriptId,
                sceScriptBlock,
                sceFile.Codepage,
                userVariableStore,
                sceCommandParser,
                scriptPatcher);
        }

        private PalScriptRunner(PalScriptType scriptType,
            uint scriptId,
            SceScriptBlock scriptBlock,
            int codepage,
            IUserVariableStore<ushort, int> userVariableStore,
            ISceCommandParser sceCommandParser,
            IPalScriptPatcher scriptPatcher,
            ScriptExecutionMode executionMode = ScriptExecutionMode.Asynchronous)
        {
            ScriptType = scriptType;
            ScriptId = scriptId;
            ScriptDescription = scriptBlock.Description;

            _userVariableStore = new ScriptVariableStore(userVariableStore, scriptBlock.UserVariables);
            _codepage = codepage;
            _sceCommandParser = sceCommandParser;
            _scriptPatcher = scriptPatcher;
            _executionMode = executionMode;

            _scriptDataReader = new SafeBinaryReader(scriptBlock.ScriptData);

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public bool Update(float deltaTime)
        {
            if (IsCompleted) return false;

            if (_waiters.Count > 0)
            {
                UpdateWaiters(deltaTime);
            }

            if (_calledScript != null)
            {
                if (!_calledScript.IsCompleted) return true;
                _calledScript = null;
            }

            if (_waiters.Count > 0) return true;

            bool canExecute;
            do { canExecute = Execute(); }
            while (canExecute && _waiters.Count == 0 && _calledScript == null);

            if (!canExecute) _isCompleted = true;
            return canExecute;
        }

        private void UpdateWaiters(float deltaTime)
        {
            // All waiters in one command group advance concurrently. In particular,
            // a dialogue/action waiter must not delay the start of a timer below it.
            for (int i = _waiters.Count - 1; i >= 0; i--)
            {
                if (!_waiters[i].ShouldWait(deltaTime)) _waiters.RemoveAt(i);
            }
        }

        internal void WaitForCalledScript(PalScriptRunner script)
        {
            if (_isExecuting && !_isDisposed) _calledScript = script;
        }

        private bool Execute()
        {
            if (IsCompleted) return false;

            if (_scriptDataReader.Position >= _scriptDataReader.Length)
            {
                return false;
            }

            _isExecuting = true;

            try
            {
                while (!_isDisposed && _scriptDataReader.Position < _scriptDataReader.Length)
                {
                    ExecuteNextCommand();
                    if (_executionMode == ScriptExecutionMode.Asynchronous || _calledScript != null) break;
                }
            }
            finally
            {
                _isExecuting = false;
            }

            return !_isDisposed;
        }

        private void ExecuteNextCommand()
        {
            long cmdPosition = _scriptDataReader.Position;

            ICommand command = _sceCommandParser.ParseNextCommand(_scriptDataReader,
                _codepage,
                out ushort commandId);

            if (_scriptPatcher.TryPatchCommandInScript(ScriptType,
                    ScriptId,
                    ScriptDescription,
                    cmdPosition,
                    _codepage,
                    command,
                    out ICommand fixedCommand))
            {
                command = fixedCommand;
            }

            EngineLogger.Log($"{ScriptType} Script " +
                      $"[{ScriptId} {ScriptDescription} [{cmdPosition}]: " +
                      $"{command.GetType().Name.Replace("Command", "")} [{commandId}] " +
                      $"{JsonConvert.SerializeObject(command)}");

            OnCommandExecutionRequested?.Invoke(this, command);
        }

        private void EvaluateAndSetOperationResult(bool value)
        {
            _tempVariable = _operatorType switch
            {
                ScriptOperatorType.Assign => value,
                ScriptOperatorType.And    => value && _tempVariable,
                ScriptOperatorType.Or     => value || _tempVariable,
                _ => throw new ArgumentOutOfRangeException(
                    $"Invalid stack operator type: {_operatorType}")
            };
        }

        private int GetVariableValue(ushort variable)
        {
            return _userVariableStore.Get(variable);
        }

        ~PalScriptRunner()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
                _scriptDataReader.Dispose();
            }

            _isDisposed = true;
            _calledScript = null;
            _waiters.Clear();
        }

        public void Execute(ScriptRunnerSetOperatorCommand command)
        {
            if (!_isExecuting) return;
            _operatorType = (ScriptOperatorType)command.OperatorType;
        }

        public void Execute(ScriptRunnerWaitUntilTimeCommand untilTimeCommand)
        {
            if (!_isExecuting) return;
            _waiters.Add(new WaitUntilTime(untilTimeCommand.Time));
        }

        public void Execute(ScriptRunnerAddWaiterRequest request)
        {
            if (!_isExecuting) return;
            _waiters.Add(request.Waiter);
        }

        public void Execute(ScriptRunnerGotoCommand command)
        {
            if (!_isExecuting) return;
            _scriptDataReader.Seek(command.Offset, SeekOrigin.Begin);
        }

        public void Execute(ScriptRunnerGotoIfNotCommand command)
        {
            if (!_isExecuting) return;
            if (!_tempVariable)
            {
                _scriptDataReader.Seek(command.Offset, SeekOrigin.Begin);
            }
        }

        public void Execute(ScriptRunnerChangeExecutionModeCommand command)
        {
            if (!_isExecuting) return;
            _executionMode = (ScriptExecutionMode)command.Mode;
        }

        public void Execute(ScriptEvaluateVarIsGreaterThanCommand command)
        {
            if (!_isExecuting) return;
            EvaluateAndSetOperationResult(GetVariableValue(command.Variable) > command.Value);
        }

        public void Execute(ScriptEvaluateVarIsGreaterThanOrEqualToCommand command)
        {
            if (!_isExecuting) return;
            EvaluateAndSetOperationResult(GetVariableValue(command.Variable) >= command.Value);
        }

        public void Execute(ScriptEvaluateVarIsGreaterThanOrEqualToAnotherVarCommand command)
        {
            if (!_isExecuting) return;
            EvaluateAndSetOperationResult(GetVariableValue(command.VariableA) >= GetVariableValue(command.VariableB));
        }

        public void Execute(ScriptEvaluateVarIsEqualToCommand command)
        {
            if (!_isExecuting) return;
            EvaluateAndSetOperationResult(GetVariableValue(command.Variable) == command.Value);
        }

        public void Execute(ScriptEvaluateVarIsNotEqualToCommand command)
        {
            if (!_isExecuting) return;
            EvaluateAndSetOperationResult(GetVariableValue(command.Variable) != command.Value);
        }

        public void Execute(ScriptEvaluateVarIsLessThanCommand command)
        {
            if (!_isExecuting) return;
            EvaluateAndSetOperationResult(GetVariableValue(command.Variable) < command.Value);
        }

        public void Execute(ScriptEvaluateVarIsLessThanOrEqualToCommand command)
        {
            if (!_isExecuting) return;
            EvaluateAndSetOperationResult(GetVariableValue(command.Variable) <= command.Value);
        }

        public void Execute(ScriptEvaluateVarIsInRangeCommand command)
        {
            if (!_isExecuting) return;
            int value = GetVariableValue(command.Variable);
            EvaluateAndSetOperationResult((value <= command.Max) && (value >= command.Min));
        }

        public void Execute(ScriptEvaluateVarIfPlayerHaveItemCommand command)
        {
            if (!_isExecuting) return;
            bool haveItem = ServiceLocator.Instance.Get<InventoryManager>()
                .HaveItem(command.ItemId);
            EvaluateAndSetOperationResult(haveItem);
        }

        public void Execute(ScriptEvaluateVarIfActorInTeamCommand command)
        {
            if (!_isExecuting) return;
            bool isActorInTeam = ServiceLocator.Instance.Get<TeamManager>()
                .IsActorInTeam((PlayerActorId) command.ActorId);
            EvaluateAndSetOperationResult(isActorInTeam);
        }
    }
}