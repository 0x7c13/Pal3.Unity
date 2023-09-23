// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Command;
    using Engine.Logging;

    public class CommandExecutorRegistry<TCommand> : ICommandExecutorRegistry<TCommand>
    {
        public static ICommandExecutorRegistry<TCommand> Instance
        {
            get { return _instance ??= new CommandExecutorRegistry<TCommand>(); }
        }

        private static ICommandExecutorRegistry<TCommand> _instance;

        private readonly Dictionary<Type, HashSet<object>> _executors = new ();

        private CommandExecutorRegistry() { }

        /// <inheritdoc />
        public void Register<T>(ICommandExecutor<T> executor) where T : TCommand
        {
            if (_executors.ContainsKey(typeof(ICommandExecutor<T>)))
            {
                if (_executors[typeof(ICommandExecutor<T>)].Contains(executor))
                {
                    EngineLogger.LogError($"Executor already registered: {executor.GetType()}");
                }
                else _executors[typeof(ICommandExecutor<T>)].Add(executor);
            }
            else
            {
                _executors[typeof(ICommandExecutor<T>)] = new HashSet<object> { executor };
            }
        }

        /// <inheritdoc />
        public void Register(object executor)
        {
            var executorTypes = executor.GetType().GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandExecutor<>)).ToList();

            if (executorTypes.Count == 0)
            {
                EngineLogger.LogError($"No ICommandExecutor interface found for type: {executor.GetType()}");
                return;
            }

            foreach (Type executorType in executorTypes)
            {
                if (_executors.ContainsKey(executorType))
                {
                    if (_executors[executorType].Contains(executor))
                    {
                        EngineLogger.LogError($"Executor already registered: {executor.GetType()}");
                    }
                    else _executors[executorType].Add(executor);
                }
                else
                {
                    _executors[executorType] = new HashSet<object> { executor };
                }
            }
        }

        /// <inheritdoc />
        public void UnRegister<T>(ICommandExecutor<T> executor) where T : TCommand
        {
            if (!_executors.ContainsKey(typeof(ICommandExecutor<T>)))
            {
                EngineLogger.LogError($"Executor has not been registered yet: {executor.GetType()}");
                return;
            }
            _executors[typeof(ICommandExecutor<T>)].Remove(executor);
            if (_executors[typeof(ICommandExecutor<T>)].Count == 0)
            {
                _executors.Remove(typeof(ICommandExecutor<T>));
            }
        }

        /// <inheritdoc />
        public void UnRegister(object executor)
        {
            var executorTypes = executor.GetType().GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandExecutor<>)).ToList();

            if (executorTypes.Count == 0)
            {
                EngineLogger.LogError($"No ICommandExecutor interface found for type: {executor.GetType()}");
                return;
            }

            foreach (Type executorType in executorTypes)
            {
                if (!_executors.ContainsKey(executorType))
                {
                    EngineLogger.LogError($"Executor has not been registered yet: {executor.GetType()}");
                    return;
                }
                _executors[executorType].Remove(executor);
                if (_executors[executorType].Count == 0)
                {
                    _executors.Remove(executorType);
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<ICommandExecutor<T>> GetRegisteredExecutors<T>() where T : TCommand
        {
            Type handlerType = typeof(ICommandExecutor<T>);

            if (!_executors.ContainsKey(handlerType)) yield break;

            foreach (var executor in _executors[handlerType])
            {
                yield return executor as ICommandExecutor<T>;
            }
        }

        /// <inheritdoc />
        public IEnumerable<object> GetRegisteredExecutors(Type type)
        {
            if (!_executors.ContainsKey(type)) yield break;

            foreach (var executor in _executors[type])
            {
                yield return executor;
            }
        }
    }
}
