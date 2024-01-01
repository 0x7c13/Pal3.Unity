// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Core.Command;

    /// <summary>
    /// A generic class that dispatches commands to their registered executors.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to dispatch.</typeparam>
    public sealed class CommandDispatcher<TCommand>
    {
        private readonly ICommandExecutorRegistry<TCommand> _commandExecutorRegistry;

        private readonly Dictionary<Type, MethodInfo> _commandExecutorExecuteMethodInfoCache = new();

        private CommandDispatcher() { }

        public CommandDispatcher(ICommandExecutorRegistry<TCommand> commandExecutorRegistry)
        {
            _commandExecutorRegistry = commandExecutorRegistry;
        }

        /// <summary>
        /// Route a command to the registered executor(s) in the registry and execute the command.
        /// </summary>
        public bool TryDispatchAndExecute<T>(T command) where T : TCommand
        {
            // Call ToList to prevent modified collection exception since a new executor may be registered during the iteration.
            IList<ICommandExecutor<T>> executors = _commandExecutorRegistry.GetRegisteredExecutors<T>().ToList();

            if (!executors.Any()) return false;

            foreach (var executor in executors)
            {
                executor.Execute(command);
            }

            return true;
        }

        /// <summary>
        /// Route a command to the registered executor(s) in the registry and execute the command.
        /// </summary>
        public bool TryDispatchAndExecute(TCommand command)
        {
            Type commandExecutorType = typeof(ICommandExecutor<>).MakeGenericType(command.GetType());

            // Call ToList to prevent modified collection exception since a new executor may be registered during the iteration.
            IList<object> executors = _commandExecutorRegistry.GetRegisteredExecutors(commandExecutorType).ToList();

            if (!executors.Any()) return false;

            foreach (object executor in executors)
            {
                if (GetCommandExecutorExecuteMethod(commandExecutorType) is { } method)
                {
                    method.Invoke(executor, new object[] {command});
                }
            }

            return true;
        }

        private MethodInfo GetCommandExecutorExecuteMethod(Type commandExecutorType)
        {
            if (_commandExecutorExecuteMethodInfoCache.TryGetValue(commandExecutorType, out MethodInfo executeMethod))
            {
                return executeMethod;
            }

            if (commandExecutorType.GetMethod(nameof(ICommandExecutor<ICommand>.Execute)) is { } method)
            {
                _commandExecutorExecuteMethodInfoCache[commandExecutorType] = method;
                return method;
            }

            return null;
        }
    }
}
