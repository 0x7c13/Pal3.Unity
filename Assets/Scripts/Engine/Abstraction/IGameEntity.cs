// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Abstraction
{
    using System;

    public interface IGameEntity : IManagedObject
    {
        public string Name { get; set; }

        public bool IsStatic { get; set; }

        public ITransform Transform { get; }

        public void SetParent(IGameEntity parent, bool worldPositionStays);

        public T AddComponent<T>() where T : class;

        public object AddComponent(Type type);

        public T GetComponent<T>() where T : class;

        public T GetComponentInChildren<T>() where T : class;

        public T[] GetComponentsInChildren<T>() where T : class;

        public T GetOrAddComponent<T>() where T : class;

        public void SetLayer(int layerIndex);

        public IGameEntity FindChild(string name);

        public void Destroy();
    }
}