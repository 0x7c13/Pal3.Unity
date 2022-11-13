// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;

    public class CommandDispatcher<TCommand>
    {
        public static CommandDispatcher<TCommand> Instance
        {
            get { return _instance ??= new CommandDispatcher<TCommand>(CommandExecutorRegistry<TCommand>.Instance); }
        }

        private static CommandDispatcher<TCommand> _instance;

        private readonly ICommandExecutorRegistry<TCommand> _commandExecutorRegistry;

        private readonly Dictionary<Type, MethodInfo> _commandExecutorExecuteMethodInfoCache = new();

        private CommandDispatcher() { }

        private CommandDispatcher(ICommandExecutorRegistry<TCommand> commandExecutorRegistry)
        {
            _commandExecutorRegistry = commandExecutorRegistry;
        }

        /// <summary>
        /// Dispatch a command to the registered executor(s) in the registry.
        /// </summary>
        public void Dispatch<T>(T command) where T : TCommand
        {
            var executed = false;
            // Call ToList to prevent modified collection exception since a new executor may be registered during the iteration.
            var executors = _commandExecutorRegistry.GetRegisteredExecutors<T>().ToList();
            foreach (var commandExecutor in executors)
            {
                commandExecutor.Execute(command);
                executed = true;
            }

            if (!executed)
            {
                Debug.LogWarning($"No command executor found for command: {typeof(T).Name}");
            }
        }

        /// <summary>
        /// Dispatch a command to the registered executor(s) in the registry using reflection.
        /// </summary>
        public void Dispatch(TCommand command)
        {
            Type commandExecutorType = typeof(ICommandExecutor<>).MakeGenericType(command.GetType());

            var executed = false;
            // Call ToList to prevent modified collection exception since a new executor may be registered during the iteration.
            var executors = _commandExecutorRegistry.GetRegisteredExecutors(commandExecutorType).ToList();
            foreach (var commandExecutor in executors)
            {
                if (GetCommandExecutorExecuteMethod(commandExecutorType) is { } method)
                {
                    method.Invoke(commandExecutor, new object[] {command});
                    executed = true;
                }
            }

            if (!executed)
            {
                Debug.LogWarning($"No command executor found for command: {command.GetType().Name}");
            }
        }

        private MethodInfo GetCommandExecutorExecuteMethod(Type commandExecutorType)
        {
            if (_commandExecutorExecuteMethodInfoCache.ContainsKey(commandExecutorType))
            {
                return _commandExecutorExecuteMethodInfoCache[commandExecutorType];
            }

            if (commandExecutorType.GetMethod("Execute") is { } method)
            {
                _commandExecutorExecuteMethodInfoCache[commandExecutorType] = method;
                return method;
            }

            return null;
        }
    }
}
