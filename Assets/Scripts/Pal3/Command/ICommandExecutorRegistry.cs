// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command
{
    using System;
    using System.Collections.Generic;

    public interface ICommandExecutorRegistry<in TCommand>
    {
        /// <summary>
        /// Register a ICommandExecutor instance.
        /// </summary>
        /// <param name="executor">ICommandExecutor instance</param>
        /// <typeparam name="T">TCommand instance type</typeparam>
        public void Register<T>(ICommandExecutor<T> executor) where T : TCommand;

        /// <summary>
        /// Automatically register all ICommandExecutor types for the given instance.
        /// </summary>
        /// <param name="executor">ICommandExecutor instance</param>
        public void Register(object executor);

        /// <summary>
        /// Unregister a ICommandExecutor instance.
        /// </summary>
        /// <param name="executor">ICommandExecutor instance</param>
        /// <typeparam name="T">TCommand instance type</typeparam>
        public void UnRegister<T>(ICommandExecutor<T> executor) where T : TCommand;

        /// <summary>
        /// Automatically unregister all ICommandExecutor types for the given instance.
        /// </summary>
        /// <param name="executor">ICommandExecutor instance</param>
        public void UnRegister(object executor);

        /// <summary>
        /// Get all registered ICommandExecutor based on ICommand instance type.
        /// </summary>
        /// <typeparam name="T">TCommand instance type</typeparam>
        /// <returns>ICommandExecutors</returns>
        public IEnumerable<ICommandExecutor<T>> GetRegisteredExecutors<T>() where T : TCommand;

        /// <summary>
        /// Get all registered ICommandExecutor based on ICommand instance type.
        /// </summary>
        /// <param name="type">ICommand instance type</param>
        /// <returns>ICommandExecutor objects</returns>
        public IEnumerable<object> GetRegisteredExecutors(Type type);
    }
}