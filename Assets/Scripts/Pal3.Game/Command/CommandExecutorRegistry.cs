// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Command;
    using Engine.Logging;

    public sealed class CommandExecutorRegistry<TCommand> : ICommandExecutorRegistry<TCommand>
    {
        public static ICommandExecutorRegistry<TCommand> Instance
        {
            get { return _instance ??= new CommandExecutorRegistry<TCommand>(); }
        }

        private static ICommandExecutorRegistry<TCommand> _instance;

        private readonly Dictionary<Type, HashSet<object>> _executors = new ();

        private CommandExecutorRegistry() { } // Hide constructor, use Instance instead.

        /// <inheritdoc />
        public void Register<T>(ICommandExecutor<T> executor) where T : TCommand
        {
            if (_executors.ContainsKey(typeof(ICommandExecutor<T>)))
            {
                if (!_executors[typeof(ICommandExecutor<T>)].Contains(executor))
                {
                    _executors[typeof(ICommandExecutor<T>)].Add(executor);
                }
            }
            else
            {
                _executors[typeof(ICommandExecutor<T>)] = new HashSet<object> { executor };
            }
        }

        /// <inheritdoc />
        public void Register(object executor)
        {
            IList<Type> executorTypes = executor.GetType().GetInterfaces()
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
                    if (!_executors[executorType].Contains(executor))
                    {
                        _executors[executorType].Add(executor);
                    }
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
            IList<Type> executorTypes = executor.GetType().GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandExecutor<>)).ToList();

            if (executorTypes.Count == 0)
            {
                EngineLogger.LogError($"No ICommandExecutor interface found for type: {executor.GetType()}");
                return;
            }

            foreach (Type executorType in executorTypes)
            {
                if (!_executors.TryGetValue(executorType, out HashSet<object> executors))
                {
                    continue;
                }
                
                executors.Remove(executor);
                
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

            if (!_executors.TryGetValue(handlerType, out HashSet<object> executors)) yield break;

            foreach (ICommandExecutor<T> executor in executors)
            {
                yield return executor;
            }
        }

        /// <inheritdoc />
        public IEnumerable<object> GetRegisteredExecutors(Type type)
        {
            if (!_executors.TryGetValue(type, out HashSet<object> executors)) yield break;

            foreach (object executor in executors)
            {
                yield return executor;
            }
        }
    }
}
