// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Extensions
{
    using UnityEngine;

    public static class GameObjectExtension
    {
        /// <summary>
        /// Helper method to get or add a component to the GameObject.
        /// </summary>
        /// <param name="gameObject">this</param>
        /// <typeparam name="T">Type of the component</typeparam>
        /// <returns>Component</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();

            // Since ?? operation does not work well with UnityObject
            // so I have to use the old fashion here checking if it is null.
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }
    }
}