// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using SceneObjects;
    using SceneObjects.Common;

    public sealed class SceneObjectFactory : ISceneObjectFactory
    {
        private bool _isInitialized = false;
        private readonly Dictionary<SceneObjectType, Type> _sceneObjectTypeCache = new ();

        public void Init()
        {
            if (_isInitialized) return;

            // All SceneObject types are in Pal3.Game.Scene.SceneObjects namespace
            // So we can just search for all types in current assembly.
            IEnumerable<Type> sceneObjectTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsClass && t.BaseType == typeof(SceneObject));

            // Populate SceneObjectTypeCache with all SceneObject types
            foreach (Type objectType in sceneObjectTypes)
            {
                // One implementation of SceneObject can implement multiple SceneObjectTypes
                foreach (Attribute attribute in objectType.GetCustomAttributes(typeof(ScnSceneObjectAttribute)))
                {
                    if (attribute is not ScnSceneObjectAttribute sceneObjectAttribute) continue;

                    // Verify if SceneObject type is already registered
                    if (_sceneObjectTypeCache.TryGetValue(sceneObjectAttribute.Type, out Type registeredImplType))
                    {
                        throw new InvalidDataContractException($"Scene object type: {sceneObjectAttribute.Type} " +
                            $"is already registered with type: {registeredImplType}. " +
                            $"Please make sure each SceneObjectType is implemented only once.");
                    }

                    // Verify if SceneObject type has a constructor with (ScnObjectInfo, ScnSceneInfo) parameters
                    ConstructorInfo constructorInfo = objectType.GetConstructor(new[]
                    {
                        typeof(ScnObjectInfo),
                        typeof(ScnSceneInfo)
                    });

                    if (constructorInfo == null)
                    {
                        throw new InvalidDataContractException($"Scene object type: {sceneObjectAttribute.Type}" +
                            " does not have a constructor with (ScnObjectInfo, ScnSceneInfo) parameters.");
                    }

                    _sceneObjectTypeCache[sceneObjectAttribute.Type] = objectType;
                }
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Create scene object instance given ScnObjectInfo and ScnSceneInfo
        /// </summary>
        /// <param name="objectInfo">ScnObjectInfo</param>
        /// <param name="sceneInfo">ScnSceneInfo</param>
        /// <returns></returns>
        public SceneObject Create(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    $"[{nameof(SceneObjectFactory)}] is not initialized, " +
                    $"call [{nameof(Init)}] first");
            }

            if (_sceneObjectTypeCache.TryGetValue(objectInfo.Type, out Type type))
            {
                return Activator.CreateInstance(type, objectInfo, sceneInfo) as SceneObject;
            }

            return null;
        }
    }
}