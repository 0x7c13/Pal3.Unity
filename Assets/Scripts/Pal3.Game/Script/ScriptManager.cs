// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Script
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.DataReader.Sce;
    using Core.Utilities;
    using Data;
    using Engine.Logging;
    using Patcher;

    public sealed class ScriptManager : IDisposable,
        ICommandExecutor<ScriptExecuteCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private readonly ISceCommandParser _sceCommandParser;
        private readonly IPalScriptPatcher _scriptPatcher;
        private readonly UserVariableManager _userVariableManager;

        private readonly SceFile _systemSceFile;
        private readonly SceFile _worldMapSceFile;
        private SceFile _currentSceFile;

        private readonly Queue<PalScriptRunner> _pendingScripts = new ();
        private readonly List<PalScriptRunner> _runningScripts = new ();
        private readonly List<PalScriptRunner> _finishedScripts = new ();

        private bool _pendingSceneScriptExecution = false;

        public ScriptManager(GameResourceProvider resourceProvider,
            UserVariableManager userVariableManager,
            ISceCommandParser sceCommandParser,
            IPalScriptPatcher scriptPatcher)
        {
            resourceProvider = Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _userVariableManager = Requires.IsNotNull(userVariableManager, nameof(userVariableManager));
            _sceCommandParser = Requires.IsNotNull(sceCommandParser, nameof(sceCommandParser));
            _scriptPatcher = Requires.IsNotNull(scriptPatcher, nameof(scriptPatcher));

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
                EngineLogger.LogError($"Script is already running: {scriptId}");
                return false;
            }

            PalScriptRunner scriptRunner;
            if (isWorldMapScript)
            {
                EngineLogger.Log($"Add WorldMap script id: {scriptId}");
                scriptRunner = PalScriptRunner.Create(_worldMapSceFile,
                    PalScriptType.WorldMap,
                    scriptId,
                    _userVariableManager,
                    _sceCommandParser,
                    _scriptPatcher);
            }
            else if (scriptId < ScriptConstants.SystemScriptIdMax)
            {
                EngineLogger.Log($"Add System script id: {scriptId}");
                scriptRunner = PalScriptRunner.Create(_systemSceFile,
                    PalScriptType.System,
                    scriptId,
                    _userVariableManager,
                    _sceCommandParser,
                    _scriptPatcher);
            }
            else
            {
                EngineLogger.Log($"Add Scene script id: {scriptId}");
                scriptRunner = PalScriptRunner.Create(_currentSceFile,
                    PalScriptType.Scene,
                    scriptId,
                    _userVariableManager,
                    _sceCommandParser,
                    _scriptPatcher);
            }

            scriptRunner.OnCommandExecutionRequested += OnCommandExecutionRequested;
            _pendingScripts.Enqueue(scriptRunner);
            return true;
        }

        private void OnCommandExecutionRequested(object sender, ICommand command)
        {
            Pal3.Instance.Execute(command);
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

                EngineLogger.Log($"Script [{finishedScript.ScriptId} " +
                          $"{finishedScript.ScriptDescription}] finished running");
                Pal3.Instance.Execute(new ScriptFinishedRunningNotification(
                    finishedScript.ScriptId,
                    finishedScript.ScriptType));
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
                Pal3.Instance.Execute(new ScriptRunnerWaitUntilTimeCommand(0f));
                return true;
            }

            return false;
        }

        public void Execute(ScriptExecuteCommand command)
        {
            if (!AddScript((uint) command.ScriptId))
            {
                Pal3.Instance.Execute(new ScriptFailedToRunNotification((uint) command.ScriptId));
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

            _finishedScripts.Clear();
        }
    }
}