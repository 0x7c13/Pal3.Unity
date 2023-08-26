// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Script
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Sce;
    using Core.Utils;
    using Data;
    using MetaData;
    using UnityEngine;

    public sealed class ScriptManager : IDisposable,
        ICommandExecutor<ScriptRunCommand>,
        ICommandExecutor<ScriptVarSetValueCommand>,
        ICommandExecutor<ScriptVarAddValueCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private readonly Dictionary<int, int> _globalVariables = new ();

        private readonly PalScriptCommandPreprocessor _cmdPreprocessor;

        private readonly SceFile _systemSceFile;
        private readonly SceFile _worldMapSceFile;
        private SceFile _currentSceFile;

        private readonly Queue<PalScriptRunner> _pendingScripts = new ();
        private readonly List<PalScriptRunner> _runningScripts = new ();
        private readonly List<PalScriptRunner> _finishedScripts = new ();

        private bool _pendingSceneScriptExecution = false;

        public ScriptManager(GameResourceProvider resourceProvider,
            PalScriptCommandPreprocessor commandPreprocessor)
        {
            resourceProvider = Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _cmdPreprocessor = Requires.IsNotNull(commandPreprocessor, nameof(commandPreprocessor));

            _systemSceFile = resourceProvider.GetGameResourceFile<SceFile>(FileConstants.SystemSceFileVirtualPath);
            _worldMapSceFile = resourceProvider.GetGameResourceFile<SceFile>(FileConstants.WorldMapSceFileVirtualPath);
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

            _pendingScripts.Clear();

            foreach (PalScriptRunner scriptRunner in _runningScripts)
            {
                scriptRunner.OnCommandExecutionRequested -= OnCommandExecutionRequested;
                scriptRunner.Dispose();
            }

            _runningScripts.Clear();

            foreach (PalScriptRunner finishedScript in _finishedScripts)
            {
                finishedScript.OnCommandExecutionRequested -= OnCommandExecutionRequested;
                finishedScript.Dispose();
            }

            _finishedScripts.Clear();
        }

        public void SetGlobalVariable(int variable, int value)
        {
            _globalVariables[variable] = value;
        }

        public Dictionary<int, int> GetGlobalVariables()
        {
            return _globalVariables;
        }

        public int GetGlobalVariable(int variable)
        {
            return _globalVariables.TryGetValue(variable, out int value) ? value : 0;
        }

        public int GetNumberOfRunningScripts()
        {
            return _pendingScripts.Count + _runningScripts.Count;
        }

        public bool AddScript(uint scriptId, bool isWorldMapScript = false)
        {
            if (scriptId == ScriptConstants.InvalidScriptId) return false;

            if (_pendingScripts.Any(s => s.ScriptId == scriptId) ||
                _runningScripts.Any(s => s.ScriptId == scriptId))
            {
                Debug.LogError($"[{nameof(ScriptManager)}] Script is already running: {scriptId}");
                return false;
            }

            PalScriptRunner scriptRunner;
            if (isWorldMapScript)
            {
                Debug.Log($"[{nameof(ScriptManager)}] Add WorldMap script id: {scriptId}");
                scriptRunner = PalScriptRunner.Create(_worldMapSceFile,
                    PalScriptType.WorldMap, scriptId, _globalVariables, _cmdPreprocessor);
            }
            else if (scriptId <= ScriptConstants.SystemScriptIdMax)
            {
                Debug.Log($"[{nameof(ScriptManager)}] Add System script id: {scriptId}");
                scriptRunner = PalScriptRunner.Create(_systemSceFile,
                    PalScriptType.System, scriptId, _globalVariables, _cmdPreprocessor);
            }
            else
            {
                Debug.Log($"[{nameof(ScriptManager)}] Add Scene script id: {scriptId}");
                scriptRunner = PalScriptRunner.Create(_currentSceFile,
                    PalScriptType.Scene, scriptId, _globalVariables, _cmdPreprocessor);
            }

            scriptRunner.OnCommandExecutionRequested += OnCommandExecutionRequested;
            _pendingScripts.Enqueue(scriptRunner);
            return true;
        }

        private void OnCommandExecutionRequested(object sender, ICommand command)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(command);
        }

        public void Update(float deltaTime)
        {
            while (_pendingScripts.Count > 0)
            {
                _runningScripts.Insert(0, _pendingScripts.Dequeue());
            }

            if (_runningScripts.Count == 0) return;

            foreach (PalScriptRunner script in _runningScripts)
            {
                if (!script.Update(deltaTime))
                {
                    _finishedScripts.Add(script);
                }
            }

            foreach (PalScriptRunner finishedScript in _finishedScripts)
            {
                _runningScripts.Remove(finishedScript);
                finishedScript.OnCommandExecutionRequested -= OnCommandExecutionRequested;
                finishedScript.Dispose();

                Debug.Log($"[{nameof(ScriptManager)}] Script [{finishedScript.ScriptId} " +
                          $"{finishedScript.ScriptDescription}] finished running.");
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ScriptFinishedRunningNotification(finishedScript.ScriptId, finishedScript.ScriptType));
            }

            _finishedScripts.Clear();

            // Scene script can be added during above script execution.
            // Call update again to trigger the execution of the scene script.
            if (_pendingSceneScriptExecution)
            {
                _pendingSceneScriptExecution = false;
                Update(1f);
            }
        }

        public bool TryAddSceneScript(SceFile sceFile, string sceneScriptDescription, out uint sceneScriptId)
        {
            sceneScriptId = ScriptConstants.InvalidScriptId;

            _currentSceFile = sceFile;

            foreach (var scriptBlock in _currentSceFile.ScriptBlocks
                         .Where(scriptBlock =>
                             string.Equals(scriptBlock.Value.Description,
                                 sceneScriptDescription,
                                 StringComparison.OrdinalIgnoreCase)))
            {
                sceneScriptId = scriptBlock.Key;
                AddScript(sceneScriptId);
                _pendingSceneScriptExecution = true;
                // This is to break the current running script from executing and let scene script to execute first
                CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptWaitUntilTimeCommand(0f));
                return true;
            }

            return false;
        }

        public void Execute(ScriptRunCommand command)
        {
            if (!AddScript((uint) command.ScriptId))
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptFailedToRunNotification((uint) command.ScriptId));
            }
        }

        public void Execute(ScriptVarSetValueCommand command)
        {
            if (command.Variable < 0)
            {
                Debug.LogWarning($"[{nameof(ScriptManager)}] Set global var {command.Variable} with value: {command.Value}");
                SetGlobalVariable(command.Variable, command.Value);
            }
        }

        public void Execute(ScriptVarAddValueCommand command)
        {
            if (command.Variable < 0)
            {
                var currentValue = _globalVariables.TryGetValue(command.Variable, out var globalVariable) ? globalVariable : 0;
                SetGlobalVariable(command.Variable, currentValue + command.Value);
            }
        }

        public void Execute(ResetGameStateCommand command)
        {
            _pendingSceneScriptExecution = false;

            foreach (PalScriptRunner script in _pendingScripts)
            {
                script.OnCommandExecutionRequested -= OnCommandExecutionRequested;
                script.Dispose();
            }

            foreach (PalScriptRunner script in _runningScripts)
            {
                script.OnCommandExecutionRequested -= OnCommandExecutionRequested;
                script.Dispose();
            }

            _globalVariables.Clear();
        }
    }
}