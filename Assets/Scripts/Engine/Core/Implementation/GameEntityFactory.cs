// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using System;
    using Abstraction;

    public static class GameEntityFactory
    {
        public static IGameEntity Create(string name)
        {
            return name != null ? new GameEntity(name) : new GameEntity();
        }

        public static IGameEntity Create(string name, IGameEntity parent, bool worldPositionStays = false)
        {
            IGameEntity gameEntity = Create(name);
            gameEntity.SetParent(parent, worldPositionStays);
            return gameEntity;
        }

        public static IGameEntity Create(string name, object prefab, IGameEntity parent, bool worldPositionStays = false)
        {
            if (prefab.GetType() != typeof(UnityEngine.Object) && !prefab.GetType().IsSubclassOf(typeof(UnityEngine.Object)))
            {
                throw new ArgumentException($"The prefab must be of type {typeof(UnityEngine.Object)}", nameof(prefab));
            }

            Object gameObject = UnityEngine.Object.Instantiate(
                (UnityEngine.Object) prefab,
                (UnityEngine.Transform) parent?.Transform?.NativeObject,
                worldPositionStays);

            if (gameObject == null || gameObject.GetType() != typeof(UnityEngine.GameObject))
            {
                throw new NullReferenceException("The instantiated object is not UnityEngine.GameObject or prefab is not found");
            }

            ((UnityEngine.GameObject)gameObject).name = name;
            return new GameEntity((UnityEngine.GameObject) gameObject);
        }
    }
}