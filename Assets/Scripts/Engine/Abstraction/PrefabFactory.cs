// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Abstraction
{
    using System;

    public static class PrefabFactory
    {
        public static IGameEntity Instantiate(object prefab, ITransform transform, bool worldPositionStays = false)
        {
            if (prefab.GetType() != typeof(UnityEngine.Object) && !prefab.GetType().IsSubclassOf(typeof(UnityEngine.Object)))
            {
                throw new ArgumentException($"The prefab must be of type {typeof(UnityEngine.Object)}", nameof(prefab));
            }

            Object gameObject = UnityEngine.Object.Instantiate(
                (UnityEngine.Object) prefab,
                (UnityEngine.Transform) transform.NativeObject,
                worldPositionStays);

            if (gameObject == null || gameObject.GetType() != typeof(UnityEngine.GameObject))
            {
                throw new NullReferenceException("The instantiated object is not UnityEngine.GameObject or game object is null");
            }

            return new GameEntity((UnityEngine.GameObject) gameObject);
        }
    }
}