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
    #if PAL3
    using MiniGame;
    #endif
    using Player;
    using UI;
    using Feature;
    using Scene;
    using UnityEngine;
    using Waiter;
    using Random = UnityEngine.Random;

    public class PalScriptRunner : IDisposable,
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
        ICommandExecutor<ScriptVarAddValueCommand>,
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
        #if PAL3
        ICommandExecutor<MiniGameGetAppraisalsResultCommand>,
        #endif
        ICommandExecutor<ScriptVarGetCombatResultCommand>
    {
        public event EventHandler<ICommand> OnCommandExecutionRequested;

        public uint ScriptId { get; }
        public PalScriptType ScriptType { get; }

        private const int MAX_REGISTER_COUNT = 8;

        private readonly int _codepage;
        private readonly BinaryReader _scriptDataReader;
        private PalScriptExecutionMode _executionMode;
        private readonly object[] _registers;
        private readonly Dictionary<int, int> _globalVariables;
        private readonly Dictionary<int, int> _localVariables = new ();

        private readonly Stack<IWaitUntil> _waiters = new ();
        private bool _isExecuting;
        private bool _isDisposed;

        private PalScriptRunner() {}

        public static PalScriptRunner Create(SceFile sceFile,
            PalScriptType scriptType,
            uint scriptId,
            Dictionary<int, int> globalVariables)
        {
            if (!sceFile.ScriptBlocks.ContainsKey(scriptId))
            {
                throw new ArgumentException($"Invalid script id: {scriptId}");
            }

            SceScriptBlock sceScriptBlock = sceFile.ScriptBlocks[scriptId];
            Debug.Log($"Create script runner: {sceScriptBlock.Id} {sceScriptBlock.Description}");
            return new PalScriptRunner(scriptType, scriptId, sceScriptBlock, globalVariables, sceFile.Codepage);
        }

        private PalScriptRunner(PalScriptType scriptType,
            uint scriptId,
            SceScriptBlock scriptBlock,
            Dictionary<int, int> globalVariables,
            int codepage,
            PalScriptExecutionMode executionMode = PalScriptExecutionMode.Break)
        {
            ScriptType = scriptType;
            ScriptId = scriptId;

            _globalVariables = globalVariables;
            _codepage = codepage;
            _executionMode = executionMode;
            
            _registers = new object[MAX_REGISTER_COUNT];
            _registers[(int) RegisterOperations.Operator] = 0; // Init operator
            
            _scriptDataReader = new BinaryReader(new MemoryStream(scriptBlock.ScriptData));

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
                    _scriptDataReader.Dispose();
                }
                _isDisposed = true;
            }
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
            
            if (_scriptDataReader.BaseStream.Position == _scriptDataReader.BaseStream.Length)
            {
                return false;
            }

            _isExecuting = true;

            while (!_isDisposed &&
                   _scriptDataReader.BaseStream.Position < _scriptDataReader.BaseStream.Length)
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

            ICommand command = SceCommandParser.ParseSceCommand(_scriptDataReader, commandId, parameterFlag, _codepage);

            if (command == null)
            {
                UnknownSceCommandAnalyzer.AnalyzeCommand(_scriptDataReader, commandId, parameterFlag, _codepage);
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

        public void Execute(ScriptVarAddValueCommand command)
        {
            if (!_isExecuting) return;
            if (command.Variable < 0) return; // Global var
            SetVariableValue(command.Variable, GetVariableValue(command.Variable) + command.Value);
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
            if (_registers[(int) RegisterOperations.Value] != null &&
                !(bool)_registers[(int) RegisterOperations.Value])
            {
                _scriptDataReader.BaseStream.Seek(command.Offset, SeekOrigin.Begin);
            }
        }

        public void Execute(ScriptChangeExecutionModeCommand command)
        {
            if (!_isExecuting) return;
            _executionMode = (PalScriptExecutionMode)command.Mode;
        }

        #if PAL3
        // TODO: Impl
        public void Execute(MiniGameGetAppraisalsResultCommand command)
        {
            if (!_isExecuting) return;
            var result = ServiceLocator.Instance.Get<AppraisalsMiniGame>().GetResult();
            SetVariableValue(command.Variable, result ? 1: 0);
        }
        #endif

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
        
        public void Execute(ScriptCheckIfPlayerHaveItemCommand command)
        {
            if (!_isExecuting) return;
            var haveItem = ServiceLocator.Instance.Get<InventoryManager>().HaveItem(command.ItemId);
            SetVarValueBasedOnOperationResult(haveItem);
        }

        public void Execute(ScriptCheckIfActorInTeamCommand command)
        {
            if (!_isExecuting) return;
            var isActorInTeam = ServiceLocator.Instance.Get<TeamManager>().IsActorInTeam((PlayerActorId) command.ActorId);
            SetVarValueBasedOnOperationResult(isActorInTeam);
        }
        
        public void Execute(ScriptGetMazeSwitchStatusCommand command)
        {
            if (!_isExecuting) return;
            var currentCity = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene().GetSceneInfo().CityName;
            if (ServiceLocator.Instance.Get<SceneStateManager>()
                .TryGetSceneObjectSwitchState(currentCity, command.SceneName, command.ObjectId, out bool isSwitchOn))
            {
                SetVariableValue(command.Variable, isSwitchOn ? 1 : 0);
            }
            else
            {
                SetVariableValue(command.Variable, 0); // Default to off
            }
        }
        
        public void Execute(ScriptGetMoneyCommand command)
        {
            if (!_isExecuting) return;
            var totalMoney = ServiceLocator.Instance.Get<InventoryManager>().GetTotalMoney();
            SetVariableValue(command.Variable, totalMoney);
        }

        public void Execute(ScriptGetFavorCommand command)
        {
            if (!_isExecuting) return;
            var favor = ServiceLocator.Instance.Get<FavorManager>().GetFavorByActor(command.ActorId);
            SetVariableValue(command.Variable, favor);
        }

        public void Execute(ScriptVarSetMostFavorableActorIdCommand command)
        {
            if (!_isExecuting) return;
            var mostFavorableActorId = ServiceLocator.Instance.Get<FavorManager>().GetMostFavorableActorId();
            SetVariableValue(command.Variable, mostFavorableActorId);
        }

        public void Execute(ScriptVarSetLeastFavorableActorIdCommand command)
        {
            if (!_isExecuting) return;
            var leastFavorableActorId = ServiceLocator.Instance.Get<FavorManager>().GetLeastFavorableActorId();
            SetVariableValue(command.Variable, leastFavorableActorId);
        }

        // TODO: Impl
        public void Execute(ScriptVarGetCombatResultCommand command)
        {
            if (!_isExecuting) return;
            var won = Random.Range(0f, 1f);
            #if PAL3
            CommandDispatcher<ICommand>.Instance.Dispatch(won > 0.35f
                ? new UIDisplayNoteCommand("你战胜了重楼")
                : new UIDisplayNoteCommand("你输给了重楼"));
            #elif PAL3A
            CommandDispatcher<ICommand>.Instance.Dispatch(won > 0.35f
                ? new UIDisplayNoteCommand("你战胜了景小楼")
                : new UIDisplayNoteCommand("你输给了景小楼"));
            #endif
            SetVariableValue(command.Variable, won > 0.35f ? 1 : 0);
        }
    }
}