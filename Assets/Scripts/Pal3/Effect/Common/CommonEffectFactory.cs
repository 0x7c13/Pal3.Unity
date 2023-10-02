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
            GameObject result = GameObject.Instantiate(prefab);
            result.name = "[effect][trail]";
            return result;
        }

        public GameObject CreateVisionRange()
        {
            GameObject prefab = GetFromCache("Effects/VisionRange");
            var result = GameObject.Instantiate(prefab);
            result.name = "[effect][vision_range]";
            return result;
        }

        private GameObject GetFromCache(string path)
        {
            if (_cache.ContainsKey(path))
            {
                return _cache[path];
            }

            GameObject prefab = Resources.Load<GameObject>(path);
            _cache.Add(path,prefab);
            return prefab;
        }
    }

}

