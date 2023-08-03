// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Core.DataReader.Scn;
    using SceneObjects;
    using SceneObjects.Common;
    using UnityEngine;

    public static class SceneObjectFactory
    {
        private static readonly Dictionary<ScnSceneObjectType, Type> SceneObjectTypeCache = new ();

        private static readonly IEnumerable<Type> SceneObjectTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && t.BaseType == typeof(SceneObject));

        /// <summary>
        /// Create scene object based on type using reflection
        /// </summary>
        /// <param name="objectInfo">ScnObjectInfo</param>
        /// <param name="sceneInfo">ScnSceneInfo</param>
        /// <returns></returns>
        public static SceneObject Create(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
        {
            if (SceneObjectTypeCache.TryGetValue(objectInfo.Type, out Type type))
            {
                return Activator.CreateInstance(type, objectInfo, sceneInfo) as SceneObject;
            }

            foreach (Type objectType in SceneObjectTypes)
            {
                foreach (Attribute attribute in objectType.GetCustomAttributes(typeof(ScnSceneObjectAttribute)))
                {
                    if (attribute is ScnSceneObjectAttribute sceneObjectAttribute &&
                        sceneObjectAttribute.Type == objectInfo.Type)
                    {
                        SceneObjectTypeCache[objectInfo.Type] = objectType;
                        return Activator.CreateInstance(objectType, objectInfo, sceneInfo) as SceneObject;
                    }
                }
            }

            Debug.LogError($"[{nameof(SceneObjectFactory)}] Scene object type: {objectInfo.Type} is not supported.");
            return null;
        }
    }
}