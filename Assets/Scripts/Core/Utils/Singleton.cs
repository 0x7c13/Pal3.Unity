﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Utils
{
    using UnityEngine;

    /// <summary>
    /// Singleton implementation using MonoBehaviour.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = FindObjectOfType<T>();

                if (_instance != null) return _instance;

                var singletonObj = new GameObject
                {
                    name = typeof(T).ToString()
                };
                _instance = singletonObj.AddComponent<T>();

                return _instance;
            }
        }

        public virtual void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = GetComponent<T>();

            DontDestroyOnLoad(gameObject);
        }
    }
}