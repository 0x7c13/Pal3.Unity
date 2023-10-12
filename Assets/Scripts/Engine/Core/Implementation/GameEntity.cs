// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using System;
    using Abstraction;
    using Extensions;
    using Pal3.Core.Utilities;
    using UnityEngine;

    public sealed class GameEntity : IGameEntity
    {
        public object NativeObject => _gameObject;

        public bool IsNativeObjectDisposed => _gameObject == null;

        private GameObject _gameObject;

        public GameEntity()
        {
            _gameObject = new GameObject();
            Transform = new Transform(_gameObject.transform);
        }

        public GameEntity(string name)
        {
            _gameObject = new GameObject(name);
            Transform = new Transform(_gameObject.transform);
        }

        public GameEntity(GameObject gameObject)
        {
            _gameObject = Requires.IsNotNull(gameObject, nameof(gameObject));
            Transform = new Transform(_gameObject.transform);
        }

        public ITransform Transform { get; }

        public string Name
        {
            get => _gameObject.name;
            set => _gameObject.name = value;
        }

        public bool IsStatic
        {
            get => _gameObject.isStatic;
            set => _gameObject.isStatic = value;
        }

        public int Layer
        {
            get => _gameObject.layer;
            set => _gameObject.layer = value;
        }

        public void SetParent(IGameEntity parent, bool worldPositionStays)
        {
            _gameObject.transform.SetParent((UnityEngine.Transform)parent?.Transform.NativeObject, worldPositionStays);
        }

        public T AddComponent<T>() where T : class
        {
            if (typeof(T) != typeof(Component) && !typeof(T).IsSubclassOf(typeof(Component)))
            {
                throw new ArgumentException("T must be a subclass of UnityEngine.Component");
            }
            return _gameObject.AddComponent(typeof(T)) as T;
        }

        public object AddComponent(Type type)
        {
            if (type != typeof(Component) && !type.IsSubclassOf(typeof(Component)))
            {
                throw new ArgumentException("Type must be a subclass of UnityEngine.Component");
            }
            return _gameObject.AddComponent(type);
        }

        public T GetComponent<T>() where T : class
        {
            if (typeof(T) != typeof(Component) && !typeof(T).IsSubclassOf(typeof(Component)))
            {
                throw new ArgumentException("T must be a subclass of UnityEngine.Component");
            }
            return _gameObject.GetComponent<T>();
        }

        public T GetComponentInChildren<T>() where T : class
        {
            if (typeof(T) != typeof(Component) && !typeof(T).IsSubclassOf(typeof(Component)))
            {
                throw new ArgumentException("T must be a subclass of UnityEngine.Component");
            }
            return _gameObject.GetComponentInChildren<T>();
        }

        public T[] GetComponentsInChildren<T>() where T : class
        {
            if (typeof(T) != typeof(Component) && !typeof(T).IsSubclassOf(typeof(Component)))
            {
                throw new ArgumentException("T must be a subclass of UnityEngine.Component");
            }
            return _gameObject.GetComponentsInChildren<T>();
        }

        public T GetOrAddComponent<T>() where T : class
        {
            var component = GetComponent<T>();

            // Since ?? operation does not work well with UnityObject
            // so I have to use the old fashion here checking if it is null.
            if (component as UnityEngine.Object == null)
            {
                component = AddComponent<T>();
            }

            return component;
        }

        public IGameEntity FindChild(string name)
        {
            UnityEngine.Transform childTransform = _gameObject.transform.Find(name);
            return childTransform != null ? new GameEntity(childTransform.gameObject) : null;
        }

        public void Destroy()
        {
            if (_gameObject != null)
            {
                _gameObject.Destroy();
                _gameObject = null;
            }
        }
    }
}