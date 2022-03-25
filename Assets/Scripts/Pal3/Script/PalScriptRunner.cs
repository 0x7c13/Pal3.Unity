// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Script
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Sce;
    using Core.Services;
    using Dev;
    using MetaData;
    using MiniGame;
    using Player;
    using UI;
    using Feature;
    using UnityEngine;
    using Waiter;
    using Random = UnityEngine.Random;

    public class PalScriptRunner:
        ICommandExecutor<ScriptChangeExecutionModeCommand>,
        ICommandExecutor<ScriptSetOperatorCommand>,
        ICommandExecutor<ScriptVarGreaterThanCommand>,
        ICommandExecutor<ScriptVarGreaterThanOrEqualToCommand>,
        ICommandExecutor<ScriptCompareUserVarGreaterThanOrEqualToCommand>,
        ICommandExecutor<ScriptVarEqualToCommand>,
        ICommandExecutor<ScriptVarNotEqualToCommand>,
        ICommandExecutor<ScriptVarLessThanCommand>,
        ICommandExecutor<ScriptVarLessThanOrEqualToCommand>,
        ICommandExecutor<ScriptVarInBetweenCommand>,
        ICommandExecutor<ScriptVarDistractValueCommand>,
        ICommandExecutor<ScriptTestGotoCommand>,
        ICommandExecutor<ScriptGotoCommand>,
        ICommandExecutor<ScriptWaitUntilTimeCommand>,
        ICommandExecutor<ScriptRunnerWaitRequest>,
        ICommandExecutor<ScriptVarSetValueCommand>,
        ICommandExecutor<ScriptVarSetRandomValueCommand>,
        ICommandExecutor<ScriptCheckIfPlayerHaveItemCommand>,
        ICommandExecutor<ScriptCheckIfActorInTeamCommand>,
        ICommandExecutor<ScriptGetDialogueSelectionCommand>,
        ICommandExecutor<ScriptGetLimitTimeDialogueSelectionCommand>,
        ICommandExecutor<ScriptGetMazeSwitchStatusCommand>,
        ICommandExecutor<ScriptGetMoneyCommand>,
        ICommandExecutor<ScriptGetFavorCommand>,
        ICommandExecutor<ScriptVarSetMostFavorableActorIdCommand>,
        ICommandExecutor<ScriptVarSetLeastFavorableActorIdCommand>,
        ICommandExecutor<ScriptVarGetCombatResultCommand>,
        ICommandExecutor<MiniGameGetAppraisalsResultCommand>
    {
        public event EventHandler<ICommand> OnCommandExecutionRequested;

        public uint ScriptId { get; }

        private const int MAX_REGISTER_COUNT = 8;

        private readonly BinaryReader _scriptDataReader;
        private PalScriptExecutionMode _executionMode;
        private readonly object[] _registers;
        private readonly Dictionary<int, int> _globalVariables;
        private readonly Dictionary<int, int> _localVariables = new ();

        private readonly Stack<IWaitUntil> _waiters = new ();
        private bool _isExecuting;

        private PalScriptRunner() {}

        public static PalScriptRunner Create(SceFile sceFile, uint scriptId, Dictionary<int, int> globalVariables)
        {
            if (!sceFile.ScriptBlocks.ContainsKey(scriptId))
            {
                throw new ArgumentException($"Invalid script id: {scriptId}");
            }

            SceScriptBlock sceScriptBlock = sceFile.ScriptBlocks[scriptId];
            Debug.Log($"Create script runner: {sceScriptBlock.Id} {sceScriptBlock.Description}");
            return new PalScriptRunner(scriptId, sceScriptBlock, globalVariables);
        }

        private PalScriptRunner(uint scriptId, SceScriptBlock scriptBlock, Dictionary<int, int> globalVariables,
            PalScriptExecutionMode executionMode = PalScriptExecutionMode.Break)
        {
            ScriptId = scriptId;

            _globalVariables = globalVariables;

            _executionMode = executionMode;
            _registers = new object[MAX_REGISTER_COUNT];
            _scriptDataReader = new BinaryReader(new MemoryStream(scriptBlock.ScriptData));

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            _scriptDataReader.Dispose();
        }

        public bool Update(float deltaTime)
        {
            var canExecute = true;
            if (_waiters.Count > 0)
            {
                UpdateWaiters(deltaTime);
            }
            else
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
            if (_scriptDataReader.BaseStream.Position == _scriptDataReader.BaseStream.Length)
            {
                return false;
            }

            _isExecuting = true;

            while (_scriptDataReader.BaseStream.Position < _scriptDataReader.BaseStream.Length)
            {
                ExecuteNextCommand();
                if (_executionMode == PalScriptExecutionMode.Break) break;
            }

            _isExecuting = false;
            return true;
        }

        private void ExecuteNextCommand()
        {
            var commandId = _scriptDataReader.ReadUInt16();
            var parameterFlag = _scriptDataReader.ReadUInt16();

            if (commandId > ScriptConstants.CommandIdMax)
            {
                throw new InvalidDataException($"Command Id is invalid: {commandId}");
            }

            var command = SceCommandParser.ParseSceCommand(_scriptDataReader, commandId, parameterFlag);

            if (command == null)
            {
                UnknownSceCommandAnalyzer.AnalyzeCommand(_scriptDataReader, commandId, parameterFlag);
            }
            else
            {
                OnCommandExecutionRequested?.Invoke(this, command);
            }
        }

        private void SetVarValueBasedOnOperationResult(bool boolValue)
        {
            var operatorType = _registers[(int) RegisterOperations.Operator];
            _registers[(int) RegisterOperations.Value] = operatorType switch
            {
                0 => boolValue,
                1 => boolValue && (bool) _registers[(int) RegisterOperations.Value],
                2 => boolValue || (bool) _registers[(int) RegisterOperations.Value],
                _ => throw new Exception($"{ScriptId}: Invalid register operation: {operatorType}")
            };
        }

        private int GetVariableValue(int variableName)
        {
            var varDic = variableName < 0 ? _globalVariables : _localVariables;
            return varDic.ContainsKey(variableName) ? varDic[variableName] : 0;
        }

        private void SetVariableValue(int variableName, int value)
        {
            if (variableName < 0)
            {
                Debug.LogWarning($"Set global var {variableName} with value: {value}");
                _globalVariables[variableName] = value;
            }
            else
            {
                Debug.LogWarning($"Setting value for user var: {variableName}, value: {value}");
                _localVariables[variableName] = value;
            }
        }

        public void Execute(ScriptVarSetValueCommand command)
        {
            if (!_isExecuting) return;
            if (command.Variable < 0) return; // Global var
            SetVariableValue(command.Variable, command.Value);
        }

        public void Execute(ScriptSetOperatorCommand command)
        {
            if (!_isExecuting) return;
            _registers[(int) RegisterOperations.Operator] = command.OperatorType;
        }

        public void Execute(ScriptVarGreaterThanCommand command)
        {
            if (!_isExecuting) return;
            SetVarValueBasedOnOperationResult(GetVariableValue(command.Variable) > command.Value);
        }

        public void Execute(ScriptVarGreaterThanOrEqualToCommand command)
        {
            if (!_isExecuting) return;
            SetVarValueBasedOnOperationResult(GetVariableValue(command.Variable) >= command.Value);
        }

        public void Execute(ScriptCompareUserVarGreaterThanOrEqualToCommand command)
        {
            if (!_isExecuting) return;
            SetVarValueBasedOnOperationResult(GetVariableValue(command.VariableA) >= GetVariableValue(command.VariableB));
        }

        public void Execute(ScriptVarEqualToCommand command)
        {
            if (!_isExecuting) return;
            SetVarValueBasedOnOperationResult(GetVariableValue(command.Variable) == command.Value);
        }

        public void Execute(ScriptVarNotEqualToCommand command)
        {
            if (!_isExecuting) return;
            SetVarValueBasedOnOperationResult(GetVariableValue(command.Variable) != command.Value);
        }

        public void Execute(ScriptVarLessThanCommand command)
        {
            if (!_isExecuting) return;
            SetVarValueBasedOnOperationResult(GetVariableValue(command.Variable) < command.Value);
        }

        public void Execute(ScriptVarLessThanOrEqualToCommand command)
        {
            if (!_isExecuting) return;
            SetVarValueBasedOnOperationResult(GetVariableValue(command.Variable) <= command.Value);
        }

        public void Execute(ScriptVarInBetweenCommand command)
        {
            if (!_isExecuting) return;
            var value = GetVariableValue(command.Variable);
            SetVarValueBasedOnOperationResult((value <= command.Max) &&
                                              (value >= command.Min));
        }

        public void Execute(ScriptVarDistractValueCommand command)
        {
            if (!_isExecuting) return;
            var result = GetVariableValue(command.VariableA) - GetVariableValue(command.VariableB);
            if (result >= 0)
            {
                SetVariableValue(command.VariableA, result);
            }
            else
            {
                SetVariableValue(command.VariableA, -result);
            }

        }
        public void Execute(ScriptVarSetRandomValueCommand command)
        {
            if (!_isExecuting) return;
            SetVariableValue(command.Variable, UnityEngine.Random.Range(0, command.MaxValue));
        }

        public void Execute(ScriptWaitUntilTimeCommand untilTimeCommand)
        {
            if (!_isExecuting) return;
            _waiters.Push(new WaitUntilTime(untilTimeCommand.Time));
        }

        public void Execute(ScriptRunnerWaitRequest request)
        {
            if (!_isExecuting) return;
            _waiters.Push(request.WaitUntil);
        }

        public void Execute(ScriptGotoCommand command)
        {
            if (!_isExecuting) return;
            _scriptDataReader.BaseStream.Seek(command.Offset, SeekOrigin.Begin);
        }

        public void Execute(ScriptTestGotoCommand command)
        {
            if (!_isExecuting) return;
            if (!(bool)_registers[(int) RegisterOperations.Value])
            {
                _scriptDataReader.BaseStream.Seek(command.Offset, SeekOrigin.Begin);
            }
        }

        public void Execute(ScriptChangeExecutionModeCommand command)
        {
            if (!_isExecuting) return;
            _executionMode = (PalScriptExecutionMode)command.Mode;
        }

        // TODO: Impl
        public void Execute(MiniGameGetAppraisalsResultCommand command)
        {
            if (!_isExecuting) return;
            var result = ServiceLocator.Instance.Get<AppraisalsMiniGame>().GetResult();
            SetVariableValue(command.Variable, result ? 1: 0);
        }

        public void Execute(ScriptGetDialogueSelectionCommand command)
        {
            if (!_isExecuting) return;
            var selection = ServiceLocator.Instance.Get<DialogueManager>().GetDialogueSelectionButtonIndex();
            SetVariableValue(command.Variable, selection);
        }

        public void Execute(ScriptGetLimitTimeDialogueSelectionCommand command)
        {
            if (!_isExecuting) return;
            var dialogueManager = ServiceLocator.Instance.Get<DialogueManager>();
            var playerReactedInTime = dialogueManager.PlayerReactedInTimeForLimitTimeDialogue() ? 1 : 0;
            SetVariableValue(command.Variable, playerReactedInTime);
        }

        // TODO: Impl
        public void Execute(ScriptCheckIfPlayerHaveItemCommand command)
        {
            if (!_isExecuting) return;
            var haveItem = true;
            SetVarValueBasedOnOperationResult(haveItem);
        }

        public void Execute(ScriptCheckIfActorInTeamCommand command)
        {
            if (!_isExecuting) return;
            var teamManager = ServiceLocator.Instance.Get<TeamManager>();
            SetVarValueBasedOnOperationResult(teamManager.IsActorInTeam((PlayerActorId) command.ActorId));
        }

        // TODO: Impl
        public void Execute(ScriptGetMazeSwitchStatusCommand command)
        {
            if (!_isExecuting) return;
            var switchIsOn = 1;
            SetVariableValue(command.Variable, switchIsOn);
        }

        // TODO: Impl
        public void Execute(ScriptGetMoneyCommand command)
        {
            if (!_isExecuting) return;
            var totalMoney = 30000;
            SetVariableValue(command.Variable, totalMoney);
        }

        public void Execute(ScriptGetFavorCommand command)
        {
            if (!_isExecuting) return;
            var favorManager = ServiceLocator.Instance.Get<FavorManager>();
            SetVariableValue(command.Variable, favorManager.GetCalculatedFavor(command.ActorId));
        }

        public void Execute(ScriptVarSetMostFavorableActorIdCommand command)
        {
            if (!_isExecuting) return;
            var favorManager = ServiceLocator.Instance.Get<FavorManager>();
            SetVariableValue(command.Variable, favorManager.GetMostFavorableActorId());
        }

        public void Execute(ScriptVarSetLeastFavorableActorIdCommand command)
        {
            if (!_isExecuting) return;
            var favorManager = ServiceLocator.Instance.Get<FavorManager>();
            SetVariableValue(command.Variable, favorManager.GetLeastFavorableActorId());
        }

        // TODO: Impl
        public void Execute(ScriptVarGetCombatResultCommand command)
        {
            if (!_isExecuting) return;
            var won = Random.Range(0f, 1f);
            CommandDispatcher<ICommand>.Instance.Dispatch(won > 0.4f
                ? new UIDisplayNoteCommand("你战胜了重楼")
                : new UIDisplayNoteCommand("你输给了重楼"));
            SetVariableValue(command.Variable, won > 0.4f ? 1 : 0);
        }
    }
}