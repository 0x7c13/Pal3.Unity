// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

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

        private readonly IPalScriptPatcher _scriptPatcher;
        private readonly UserVariableManager _userVariableManager;

        private readonly int _codepage;
        private readonly IBinaryReader _scriptDataReader;
        private ScriptExecutionMode _executionMode;

        // Stack operator and value are used to evaluate logical expressions
        // over multiple commands and branches in the script in assembly fashion.
        private ScriptOperatorType _operatorType = ScriptOperatorType.Assign;
        private bool _tempVariable = false;

        private readonly Stack<IScriptRunnerWaiter> _waiters = new ();
        private bool _isExecuting;
        private bool _isDisposed;

        public static PalScriptRunner Create(SceFile sceFile,
            PalScriptType scriptType,
            uint scriptId,
            UserVariableManager userVariableManager,
            IPalScriptPatcher scriptPatcher)
        {
            if (!sceFile.ScriptBlocks.ContainsKey(scriptId))
            {
                throw new ArgumentException($"Invalid script id: {scriptId}");
            }

            SceScriptBlock sceScriptBlock = sceFile.ScriptBlocks[scriptId];
            EngineLogger.Log($"Create script runner: [{sceScriptBlock.Id} {sceScriptBlock.Description}]");

            return new PalScriptRunner(scriptType,
                scriptId,
                sceScriptBlock,
                sceFile.Codepage,
                userVariableManager,
                scriptPatcher);
        }

        private PalScriptRunner(PalScriptType scriptType,
            uint scriptId,
            SceScriptBlock scriptBlock,
            int codepage,
            UserVariableManager userVariableManager,
            IPalScriptPatcher scriptPatcher,
            ScriptExecutionMode executionMode = ScriptExecutionMode.Asynchronous)
        {
            ScriptType = scriptType;
            ScriptId = scriptId;
            ScriptDescription = scriptBlock.Description;

            _userVariableManager = userVariableManager;
            _codepage = codepage;
            _scriptPatcher = scriptPatcher;
            _executionMode = executionMode;

            _scriptDataReader = new SafeBinaryReader(scriptBlock.ScriptData);

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public bool Update(float deltaTime)
        {
            var canExecute = true;

            if (_waiters.Count > 0)
            {
                UpdateWaiters(deltaTime);
            }

            if (_waiters.Count == 0)
            {
                do { canExecute = Execute(); }
                while (canExecute && _waiters.Count == 0);
            }

            return canExecute;
        }

        private void UpdateWaiters(float deltaTime)
        {
            if (_waiters.Count <= 0) return;

            if (!_waiters.Peek().ShouldWait(deltaTime))
            {
                _waiters.Pop();
            }
        }

        private bool Execute()
        {
            if (_isDisposed) return false;

            if (_scriptDataReader.Position == _scriptDataReader.Length)
            {
                return false;
            }

            _isExecuting = true;

            while (!_isDisposed &&
                   _scriptDataReader.Position < _scriptDataReader.Length)
            {
                ExecuteNextCommand();
                if (_executionMode == ScriptExecutionMode.Asynchronous) break;
            }

            _isExecuting = false;
            return true;
        }

        private void ExecuteNextCommand()
        {
            long cmdPosition = _scriptDataReader.Position;

            ICommand command = SceCommandParser.ParseSceCommand(_scriptDataReader,
                _codepage,
                out ushort commandId,
                out _);

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
                      $"[{ScriptId} {ScriptDescription}]: " +
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
            return _userVariableManager.GetVariableValue(variable);
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
        }

        public void Execute(ScriptRunnerSetOperatorCommand command)
        {
            if (!_isExecuting) return;
            _operatorType = (ScriptOperatorType)command.OperatorType;
        }

        public void Execute(ScriptRunnerWaitUntilTimeCommand untilTimeCommand)
        {
            if (!_isExecuting) return;
            _waiters.Push(new WaitUntilTime(untilTimeCommand.Time));
        }

        public void Execute(ScriptRunnerAddWaiterRequest request)
        {
            if (!_isExecuting) return;
            _waiters.Push(request.Waiter);
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