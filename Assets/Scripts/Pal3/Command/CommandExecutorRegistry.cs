// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public class CommandExecutorRegistry<TCommand> : ICommandExecutorRegistry<TCommand>
    {
        public static ICommandExecutorRegistry<TCommand> Instance
        {
            get { return _instance ??= new CommandExecutorRegistry<TCommand>(); }
        }

        private static ICommandExecutorRegistry<TCommand> _instance;

        private readonly Dictionary<Type, HashSet<object>> _executors = new ();

        private CommandExecutorRegistry() { }

        /// <summary>
        /// Register a ICommandExecutor instance
        /// </summary>
        /// <param name="executor">ICommandExecutor instance</param>
        /// <typeparam name="T">TCommand instance type</typeparam>
        public void Register<T>(ICommandExecutor<T> executor) where T : TCommand
        {
            if (_executors.ContainsKey(typeof(ICommandExecutor<T>)))
            {
                if (_executors[typeof(ICommandExecutor<T>)].Contains(executor))
                {
                    Debug.LogError($"Executor already registered: {executor.GetType()}");
                }
                else _executors[typeof(ICommandExecutor<T>)].Add(executor);
            }
            else
            {
                _executors[typeof(ICommandExecutor<T>)] = new HashSet<object> { executor };
            }
        }

        /// <summary>
        /// Automatically register all ICommandExecutor types for the given instance
        /// </summary>
        /// <param name="executor">ICommandExecutor instance</param>
        public void Register(object executor)
        {
            var executorTypes = executor.GetType().GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandExecutor<>)).ToList();

            if (executorTypes.Count == 0)
            {
                Debug.LogError($"No ICommandExecutor interface found for type: {executor.GetType()}");
                return;
            }

            foreach (var executorType in executorTypes)
            {
                if (_executors.ContainsKey(executorType))
                {
                    if (_executors[executorType].Contains(executor))
                    {
                        Debug.LogError($"Executor already registered: {executor.GetType()}");
                    }
                    else _executors[executorType].Add(executor);
                }
                else
                {
                    _executors[executorType] = new HashSet<object> { executor };
                }
            }
        }

        /// <summary>
        /// Unregister a ICommandExecutor instance
        /// </summary>
        /// <param name="executor">ICommandExecutor instance</param>
        /// <typeparam name="T">TCommand instance type</typeparam>
        public void UnRegister<T>(ICommandExecutor<T> executor) where T : TCommand
        {
            if (!_executors.ContainsKey(typeof(ICommandExecutor<T>)))
            {
                Debug.LogError($"Executor has not been registered yet: {executor.GetType()}");
                return;
            }
            _executors[typeof(ICommandExecutor<T>)].Remove(executor);
            if (_executors[typeof(ICommandExecutor<T>)].Count == 0)
            {
                _executors.Remove(typeof(ICommandExecutor<T>));
            }
        }

        /// <summary>
        /// Automatically unregister all ICommandExecutor types for the given instance
        /// </summary>
        /// <param name="executor">ICommandExecutor instance</param>
        public void UnRegister(object executor)
        {
            var executorTypes = executor.GetType().GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandExecutor<>)).ToList();

            if (executorTypes.Count == 0)
            {
                Debug.LogError($"No ICommandExecutor interface found for type: {executor.GetType()}");
                return;
            }

            foreach (var executorType in executorTypes)
            {
                if (!_executors.ContainsKey(executorType))
                {
                    Debug.LogError($"Executor has not been registered yet: {executor.GetType()}");
                    return;
                }
                _executors[executorType].Remove(executor);
                if (_executors[executorType].Count == 0)
                {
                    _executors.Remove(executorType);
                }
            }
        }

        /// <summary>
        /// Get all registered ICommandExecutor based on ICommand instance type
        /// </summary>
        /// <typeparam name="T">TCommand instance type</typeparam>
        /// <returns>ICommandExecutors</returns>
        public IEnumerable<ICommandExecutor<T>> GetRegisteredExecutors<T>() where T : TCommand
        {
            var handlerType = typeof(ICommandExecutor<T>);

            if (!_executors.ContainsKey(handlerType)) yield break;

            foreach (var executor in _executors[handlerType])
            {
                yield return executor as ICommandExecutor<T>;
            }
        }

        /// <summary>
        /// Get all registered ICommandExecutor based on TCommand instance type
        /// using reflection
        /// </summary>
        /// <returns>ICommandExecutors</returns>
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
