// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Services
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using Utilities;

    /// <summary>
    /// ServiceLocator singleton.
    /// </summary>
    public sealed class ServiceLocator : Singleton<ServiceLocator>
    {
        private ServiceLocator() { } // Hide constructor, use Instance instead.

        /// <summary>
        /// Currently registered services.
        /// </summary>
        private readonly Dictionary<Type, object> _services = new ();

        /// <summary>
        /// Gets the service instance of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the service to lookup</typeparam>
        /// <returns>The service instance</returns>
        /// <exception cref="InvalidOperationException">Thrown if the service is not registered.</exception>
        public T Get<T>()
        {
            if (!_services.ContainsKey(typeof(T)))
            {
                string error = $"{typeof(T)} is not registered with {GetType().Name}";
                EngineLogger.LogError(error);
                throw new InvalidOperationException(error);
            }

            return (T)_services[typeof(T)];
        }

        /// <summary>
        /// Registers the service with the current service locator.
        /// </summary>
        /// <typeparam name="T">Service typ.</typeparam>
        /// <param name="service">Service instance</param>
        /// <exception cref="ArgumentNullException">Thrown if the service is null.</exception>
        public void Register<T>(T service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            if (_services.ContainsKey(typeof(T)))
            {
                EngineLogger.LogWarning($"[{typeof(T)}] is already registered");
                return;
            }
            else
            {
                EngineLogger.Log($"Service type [{typeof(T)}] registered");
            }

            _services.Add(typeof(T), service);
        }

        /// <summary>
        /// Unregisters the service from the current service locator.
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        public void Unregister<T>()
        {
            if (!_services.ContainsKey(typeof(T)))
            {
                EngineLogger.LogWarning($"Failed to unregister service since [{typeof(T)}] is not registered yet");
                return;
            }
            else
            {
                EngineLogger.Log($"Service type [{typeof(T)}] unregistered");
            }

            _services.Remove(typeof(T));
        }

        /// <summary>
        /// Get all registered services.
        /// </summary>
        public IEnumerable<object> GetAllRegisteredServices()
        {
            return _services.Values;
        }

        /// <summary>
        /// Unregisters all services.
        /// </summary>
        public void UnregisterAll()
        {
            EngineLogger.Log("All services unregistered");
            _services.Clear();
        }
    }
}