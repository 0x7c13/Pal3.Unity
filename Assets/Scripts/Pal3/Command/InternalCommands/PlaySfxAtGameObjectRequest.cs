// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    using Newtonsoft.Json;
    using UnityEngine;

    public class PlaySfxAtGameObjectRequest : ICommand
    {
        public PlaySfxAtGameObjectRequest(GameObject parent,
            string sfxName,
            string audioSourceName,
            float volume,
            float startDelayInSeconds,
            float interval)
        {
            Parent = parent;
            SfxName = sfxName;
            AudioSourceName = audioSourceName;
            Volume = volume;
            StartDelayInSeconds = startDelayInSeconds;
            Interval = interval;
        }
        
        [JsonIgnore] public GameObject Parent { get; }
        public string SfxName { get; }
        public string AudioSourceName { get; }
        public float Volume { get; }
        public float StartDelayInSeconds { get; }
        public float Interval { get; }

    }
}