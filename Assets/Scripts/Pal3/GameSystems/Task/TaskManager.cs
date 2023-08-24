// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.GameSystems.Task
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Txt;
    using Core.Utils;
    using Data;
    using MetaData;
    using State;
    using TMPro;

    public sealed class TaskManager :
        ICommandExecutor<TaskOpenCommand>,
        ICommandExecutor<TaskCompleteCommand>,
        ICommandExecutor<GameStateChangedNotification>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const string TASK_DEFINITION_FILE_NAME = "task.txt";

        private readonly Dictionary<string, Task> _tasks = new();

        private readonly HashSet<string> _openedTasks = new ();
        private readonly HashSet<string> _completedTasks = new ();

        private readonly TextMeshProUGUI _taskInfoText;

        public TaskManager(GameResourceProvider resourceProvider, TextMeshProUGUI taskInfoText)
        {
            Requires.IsNotNull(resourceProvider, nameof(resourceProvider));

            var taskDefinitionFile = resourceProvider.GetGameResourceFile<TaskDefinitionFile>(
                FileConstants.DataScriptFolderVirtualPath + TASK_DEFINITION_FILE_NAME);

            Requires.IsNotNull(taskDefinitionFile, nameof(taskDefinitionFile));

            _taskInfoText = Requires.IsNotNull(taskInfoText, nameof(taskInfoText));

            foreach (Task task in taskDefinitionFile.Tasks)
            {
                _tasks[task.Id] = task;
            }

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public Task[] GetOpenedTasks()
        {
            return _openedTasks.Select(taskId => _tasks[taskId]).ToArray();
        }

        public Task[] GetCompletedTasks()
        {
            return _completedTasks.Select(taskId => _tasks[taskId]).ToArray();
        }

        public void Execute(TaskOpenCommand command)
        {
            // Special handling for completing the initial task
            if (command.TaskId.Equals(TaskConstants.SecondTaskId, StringComparison.Ordinal))
            {
                Execute(new TaskCompleteCommand(TaskConstants.InitTaskId));
            }

            _openedTasks.Add(command.TaskId);

            if (_tasks.TryGetValue(command.TaskId, out Task task) && task.IsMainTask)
            {
                _taskInfoText.text = task.Description;
            }
        }

        public void Execute(TaskCompleteCommand command)
        {
            _openedTasks.Remove(command.TaskId);
            _completedTasks.Add(command.TaskId);
        }

        public void Execute(GameStateChangedNotification command)
        {
            _taskInfoText.enabled = command.NewState == GameState.Gameplay;
        }

        public void Execute(ResetGameStateCommand command)
        {
            _openedTasks.Clear();
            _completedTasks.Clear();
            _taskInfoText.text = string.Empty;
        }
    }
}

#endif