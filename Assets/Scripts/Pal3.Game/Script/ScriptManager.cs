// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

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
        private readonly UserVariableManager _userVariableStore;
        private readonly Action<ICommand> _executeCommand;

        private readonly SceFile _systemSceFile;
        private readonly SceFile _worldMapSceFile;
        private SceFile _currentSceFile;

        private readonly Queue<PalScriptRunner> _pendingScripts = new ();
        private readonly List<PalScriptRunner> _runningScripts = new ();
        private readonly List<PalScriptRunner> _finishedScripts = new ();

        private bool _pendingSceneScriptExecution = false;
        private PalScriptRunner _executingScript;
        private ICommand _executingCommand;
        private int _generation;

        public ScriptManager(GameResourceProvider resourceProvider,
            UserVariableManager userVariableStore,
            ISceCommandParser sceCommandParser,
            IPalScriptPatcher scriptPatcher)
            : this(Requires.IsNotNull(resourceProvider, nameof(resourceProvider))
                    .GetGameResourceFile<SceFile>(FileConstants.SystemSceFileVirtualPath),
                resourceProvider.GetGameResourceFile<SceFile>(FileConstants.WorldMapSceFileVirtualPath),
                userVariableStore, sceCommandParser, scriptPatcher,
                command => Pal3.Instance.Execute(command))
        {
        }

        internal ScriptManager(SceFile systemSceFile,
            SceFile worldMapSceFile,
            UserVariableManager userVariableStore,
            ISceCommandParser sceCommandParser,
            IPalScriptPatcher scriptPatcher,
            Action<ICommand> executeCommand)
        {
            _userVariableStore = Requires.IsNotNull(userVariableStore, nameof(userVariableStore));
            _sceCommandParser = Requires.IsNotNull(sceCommandParser, nameof(sceCommandParser));
            _scriptPatcher = Requires.IsNotNull(scriptPatcher, nameof(scriptPatcher));
            _executeCommand = Requires.IsNotNull(executeCommand, nameof(executeCommand));
            _systemSceFile = Requires.IsNotNull(systemSceFile, nameof(systemSceFile));
            _worldMapSceFile = Requires.IsNotNull(worldMapSceFile, nameof(worldMapSceFile));

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            ClearScripts();
        }

        public int GetNumberOfRunningScripts()
        {
            return _pendingScripts.Count + _runningScripts.Count;
        }

        public bool AddScript(uint scriptId, bool isWorldMapScript = false)
        {
            return TryAddScript(scriptId, isWorldMapScript, out _);
        }

        private bool TryAddScript(uint scriptId, bool isWorldMapScript, out PalScriptRunner scriptRunner)
        {
            scriptRunner = null;
            if (scriptId == ScriptConstants.InvalidScriptId) return false;

            if (_pendingScripts.Any(s => s.ScriptId == scriptId) ||
                _runningScripts.Any(s => s.ScriptId == scriptId))
            {
                EngineLogger.LogError($"Script is already running: {scriptId}");
                return false;
            }

            SceFile file;
            PalScriptType type;
            if (isWorldMapScript)
            {
                file = _worldMapSceFile;
                type = PalScriptType.WorldMap;
            }
            else if (scriptId < ScriptConstants.SystemScriptIdMax)
            {
                file = _systemSceFile;
                type = PalScriptType.System;
            }
            else
            {
                file = _currentSceFile;
                type = PalScriptType.Scene;
            }

            if (file == null || !file.ScriptBlocks.ContainsKey(scriptId))
            {
                EngineLogger.LogError($"Script not found: {type} {scriptId}");
                return false;
            }

            scriptRunner = PalScriptRunner.Create(file, type, scriptId,
                _userVariableStore, _sceCommandParser, _scriptPatcher);
            scriptRunner.OnCommandExecutionRequested += OnCommandExecutionRequested;
            _pendingScripts.Enqueue(scriptRunner);
            return true;
        }

        private void OnCommandExecutionRequested(object sender, ICommand command)
        {
            var script = (PalScriptRunner)sender;
            var previous = _executingScript;
            var previousCommand = _executingCommand;
            _executingScript = script;
            _executingCommand = command;
            try { _userVariableStore.ExecuteInScope(script.Variables, command, _executeCommand); }
            finally
            {
                _executingScript = previous;
                _executingCommand = previousCommand;
            }
        }

        public void Update(float deltaTime)
        {
            // A previous update may have been interrupted by a command/notification
            // exception. Completed runners still in the running list are collected
            // again below; never carry over a partially processed completion batch.
            _finishedScripts.Clear();
            while (_pendingScripts.Count > 0)
            {
                _runningScripts.Insert(0, _pendingScripts.Dequeue());
            }

            if (_runningScripts.Count == 0) return;

            int generation = _generation;
            for (int i = 0; i < _runningScripts.Count; i++)
            {
                PalScriptRunner script = _runningScripts[i];
                bool running = script.Update(deltaTime);
                // A command can reset the game during this update. All old script
                // instances have been disposed; do not emit completion notifications.
                if (generation != _generation) return;
                if (!running)
                {
                    _finishedScripts.Add(script);
                }
            }

            for (int i = 0; i < _finishedScripts.Count; i++)
            {
                PalScriptRunner finishedScript = _finishedScripts[i];
                _runningScripts.Remove(finishedScript);
                finishedScript.OnCommandExecutionRequested -= OnCommandExecutionRequested;
                finishedScript.Dispose();

                EngineLogger.Log($"Script [{finishedScript.ScriptId} " +
                          $"{finishedScript.ScriptDescription}] finished running");
                _executeCommand(new ScriptFinishedRunningNotification(
                    finishedScript.ScriptId,
                    finishedScript.ScriptType));
                if (generation != _generation) return;
            }

            _finishedScripts.Clear();

            // Scene script can be added during above script execution.
            // Call update again to trigger the execution of the scene script.
            if (_pendingSceneScriptExecution)
            {
                _pendingSceneScriptExecution = false;
                Update(0f);
            }
        }

        public bool TryAddSceneScript(SceFile sceFile, string sceneScriptDescription, out uint sceneScriptId)
        {
            sceneScriptId = ScriptConstants.InvalidScriptId;

            _currentSceFile = sceFile;

            foreach (KeyValuePair<uint, SceScriptBlock> scriptBlock in _currentSceFile.ScriptBlocks
                         .Where(scriptBlock =>
                             string.Equals(scriptBlock.Value.Description,
                                 sceneScriptDescription,
                                 StringComparison.OrdinalIgnoreCase)))
            {
                sceneScriptId = scriptBlock.Key;
                if (!AddScript(sceneScriptId)) return false;
                _pendingSceneScriptExecution = true;
                // This is to break the current running script from executing and let scene script to execute first
                _executeCommand(new ScriptRunnerWaitUntilTimeCommand(0f));
                return true;
            }

            return false;
        }

        public void Execute(ScriptExecuteCommand command)
        {
            if (TryAddScript((uint)command.ScriptId, false, out PalScriptRunner child))
            {
                #if PAL3A
                // Only the command decoded by this runner is a synchronous call.
                // Object interactions may dispatch another 16 during command 124.
                if (ReferenceEquals(command, _executingCommand))
                    _executingScript?.WaitForCalledScript(child);
                #endif
            }
            else
            {
                _executeCommand(new ScriptFailedToRunNotification((uint) command.ScriptId));
            }
        }

        public void Execute(ResetGameStateCommand command)
        {
            ClearScripts();
            _currentSceFile = null;
        }

        private void ClearScripts()
        {
            _generation++;
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

            _pendingScripts.Clear();
            _runningScripts.Clear();
            _finishedScripts.Clear();
        }
    }
}