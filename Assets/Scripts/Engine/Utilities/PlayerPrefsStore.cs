// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Utilities
{
    using System;
    using Core.Utilities;
    using UnityEngine;

    public sealed class PlayerPrefsStore : ITransactionalKeyValueStore
    {
        public void Set<T>(string key, T value)
        {
            if (typeof(T) == typeof(int))
            {
                PlayerPrefs.SetInt(key, (int)(object)value);
            }
            else if (typeof(T) == typeof(float))
            {
                PlayerPrefs.SetFloat(key, (float)(object)value);
            }
            else if (typeof(T) == typeof(string))
            {
                PlayerPrefs.SetString(key, (string)(object)value);
            }
            else if (typeof(T) == typeof(bool))
            {
                PlayerPrefs.SetInt(key, (bool)(object)value ? 1 : 0);
            }
            else if (typeof(T).IsEnum)
            {
                PlayerPrefs.SetInt(key, Convert.ToInt32(value));
            }
            else
            {
                throw new NotSupportedException("Unsupported type: " + typeof(T).Name);
            }
        }

        public bool TryGet<T>(string key, out T value)
        {
            // If the key doesn't exist, return the default value
            if (!PlayerPrefs.HasKey(key))
            {
                value = default;
                return false;
            }

            if (typeof(T) == typeof(int))
            {
                value = (T)(object)PlayerPrefs.GetInt(key);
                return true;
            }
            else if (typeof(T) == typeof(float))
            {
                value = (T)(object)PlayerPrefs.GetFloat(key);
                return true;
            }
            else if (typeof(T) == typeof(string))
            {
                value = (T)(object)PlayerPrefs.GetString(key);
                return true;
            }
            else if (typeof(T) == typeof(bool))
            {
                value = (T)(object)(PlayerPrefs.GetInt(key) == 1);
                return true;
            }
            else if (typeof(T).IsEnum)
            {
                value = (T)Enum.ToObject(typeof(T), PlayerPrefs.GetInt(key));
                return true;
            }

            Debug.LogError($"[{nameof(PlayerPrefsStore)}] Unsupported type: " + typeof(T).Name);
            value = default;
            return false;
        }

        public void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}