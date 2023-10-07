// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Abstraction
{
    using System;
    using Extensions;
    using UnityEngine;

    public sealed class GameEntity : IGameEntity
    {
        private GameObject _gameObject;

        public GameEntity(string name)
        {
            _gameObject = new GameObject(name);
            Transform = new Transform(_gameObject.transform);
        }

        public GameEntity(GameObject gameObject)
        {
            _gameObject = gameObject;
            Transform = new Transform(_gameObject.transform);
        }

        public ITransform Transform { get; }

        public bool IsStatic
        {
            get => _gameObject.isStatic;
            set => _gameObject.isStatic = value;
        }

        public void SetParent(IGameEntity parent, bool worldPositionStays)
        {
            _gameObject.transform.SetParent(parent?.Transform.GetUnityTransform(), worldPositionStays);
        }

        public T AddComponent<T>() where T : Component
        {
            return _gameObject.AddComponent<T>();
        }

        public Component AddComponent(Type type)
        {
            return _gameObject.AddComponent(type);
        }

        public T GetComponent<T>() where T : Component
        {
            return _gameObject.GetComponent<T>();
        }

        public T GetComponentInChildren<T>()
        {
            return _gameObject.GetComponentInChildren<T>();
        }

        public T[] GetComponentsInChildren<T>()
        {
            return _gameObject.GetComponentsInChildren<T>();
        }

        public T GetOrAddComponent<T>() where T : Component
        {
            var component = GetComponent<T>();

            // Since ?? operation does not work well with UnityObject
            // so I have to use the old fashion here checking if it is null.
            if (component == null)
            {
                component = AddComponent<T>();
            }

            return component;
        }

        public void SetLayer(int layerIndex)
        {
            _gameObject.layer = layerIndex;
        }

        public void SetStatic(bool isStatic)
        {
            _gameObject.isStatic = isStatic;
        }

        public IGameEntity FindChild(string name)
        {
            UnityEngine.Transform childTransform = _gameObject.transform.Find(name);
            return childTransform != null ? new GameEntity(childTransform.gameObject) : null;
        }

        public bool IsDisposed => _gameObject == null;

        public void Destroy()
        {
            if (_gameObject != null)
            {
                _gameObject.Destroy();
                _gameObject = null;
            }
        }

        public GameObject GetUnityGameObject()
        {
            return _gameObject;
        }
    }
}