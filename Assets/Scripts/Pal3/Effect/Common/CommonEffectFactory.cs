using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pal3.Effect
{
    public class CommonEffectFactory
    {
        private Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();
        
        public GameObject CreateTrail()
        {
            GameObject prefab = GetFromCache("Effects/Trail");
            return GameObject.Instantiate(prefab);
        }
        
        private GameObject GetFromCache(string path)
        {
            if (_cache.ContainsKey(path))
            {
                return _cache[path];
            }
            GameObject prefab = Resources.Load<GameObject>("Effects/Trail");
            _cache.Add(path,prefab);
            return prefab;
        }
    }

}

