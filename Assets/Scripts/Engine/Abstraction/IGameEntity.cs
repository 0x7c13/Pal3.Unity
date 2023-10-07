// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Abstraction
{
    using System;
    using UnityEngine;

    public interface IGameEntity
    {
        public bool IsStatic { get; set; }

        public ITransform Transform { get; }

        public void SetParent(IGameEntity parent, bool worldPositionStays);

        public T AddComponent<T>() where T : Component;

        public Component AddComponent(Type type);

        public T GetComponent<T>() where T : Component;

        public T GetComponentInChildren<T>();

        public T[] GetComponentsInChildren<T>();

        public T GetOrAddComponent<T>() where T : Component;

        void SetLayer(int layerIndex);

        IGameEntity FindChild(string name);

        public bool IsDisposed { get; }

        public void Destroy();

        public GameObject GetUnityGameObject();
    }
}