// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;

    /// <summary>
    /// A generic class that dispatches commands to their registered executors.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to dispatch.</typeparam>
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
            // Call ToList to prevent modified collection exception since a new executor may be registered during the iteration.
            var executors = _commandExecutorRegistry.GetRegisteredExecutors<T>().ToList();

            if (executors.Any())
            {
                foreach (var executor in executors)
                {
                    executor.Execute(command);
                }
            }
            else if (Attribute.GetCustomAttribute(typeof(T), typeof(SceCommandAttribute)) != null)
            {
                Debug.LogWarning($"[{nameof(CommandDispatcher<TCommand>)}] No command executor found for sce command: {typeof(T).Name}");
            }
        }

        /// <summary>
        /// Dispatch a command to the registered executor(s) in the registry using reflection.
        /// </summary>
        public void Dispatch(TCommand command)
        {
            Type commandExecutorType = typeof(ICommandExecutor<>).MakeGenericType(command.GetType());

            // Call ToList to prevent modified collection exception since a new executor may be registered during the iteration.
            var executors = _commandExecutorRegistry.GetRegisteredExecutors(commandExecutorType).ToList();

            if (executors.Any())
            {
                foreach (var executor in executors)
                {
                    if (GetCommandExecutorExecuteMethod(commandExecutorType) is { } method)
                    {
                        method.Invoke(executor, new object[] {command});
                    }
                }
            }
            else if (Attribute.GetCustomAttribute(command.GetType(), typeof(SceCommandAttribute)) != null)
            {
                Debug.LogWarning($"[{nameof(CommandDispatcher<TCommand>)}] No command executor found for sce command: {command.GetType().Name}");
            }
        }

        private MethodInfo GetCommandExecutorExecuteMethod(Type commandExecutorType)
        {
            if (_commandExecutorExecuteMethodInfoCache.TryGetValue(commandExecutorType, out MethodInfo executeMethod))
            {
                return executeMethod;
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
