// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Utilities
{
    using Extensions;
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
                gameObject.Destroy();
                return;
            }

            _instance = GetComponent<T>();

            DontDestroyOnLoad(gameObject);
        }
    }
}