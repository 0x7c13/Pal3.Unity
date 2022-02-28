// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Services
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using Utils;

    /// <summary>
    /// ServiceLocator singleton
    /// </summary>
    public class ServiceLocator : Singleton<ServiceLocator>
    {
        /// <summary>
        /// currently registered services
        /// </summary>
        private readonly Dictionary<Type, object> _services = new ();

        /// <summary>
        /// Gets the service instance of the given type
        /// </summary>
        /// <typeparam name="T">The type of the service to lookup</typeparam>
        /// <returns>The service instance</returns>
        public T Get<T>()
        {
            if (!_services.ContainsKey(typeof(T)))
            {
                Debug.LogError($"{typeof(T)} not registered with {GetType().Name}");
                throw new InvalidOperationException();
            }

            return (T)_services[typeof(T)];
        }

        /// <summary>
        /// Registers the service with the current service locator
        /// </summary>
        /// <typeparam name="T">Service typ.</typeparam>
        /// <param name="service">Service instance</param>
        public void Register<T>(T service)
        {
            if (_services.ContainsKey(typeof(T)))
            {
                Debug.LogError($"{typeof(T)} already registered.");
                return;
            }

            _services.Add(typeof(T), service);
        }

        /// <summary>
        /// Unregisters the service from the current service locator
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        public void Unregister<T>()
        {
            if (!_services.ContainsKey(typeof(T)))
            {
                Debug.LogError($"Failed to unregister since {typeof(T)} is not registered yet.");
                return;
            }

            _services.Remove(typeof(T));
        }

        /// <summary>
        /// Unregisters all services
        /// </summary>
        public void UnregisterAll()
        {
            _services.Clear();
        }
    }
}