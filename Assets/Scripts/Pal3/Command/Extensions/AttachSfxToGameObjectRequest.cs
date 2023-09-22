// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.Extensions
{
    using Core.Command;
    using Newtonsoft.Json;
    using UnityEngine;

    public class AttachSfxToGameObjectRequest : ICommand
    {
        public AttachSfxToGameObjectRequest(GameObject parent,
            string sfxName,
            string audioSourceName,
            int loopCount,
            float volume,
            float startDelayInSeconds,
            float interval)
        {
            Parent = parent;
            SfxName = sfxName;
            AudioSourceName = audioSourceName;
            LoopCount = loopCount;
            Volume = volume;
            StartDelayInSeconds = startDelayInSeconds;
            Interval = interval;
        }

        [JsonIgnore] public GameObject Parent { get; }
        public string SfxName { get; }
        public string AudioSourceName { get; }
        public int LoopCount { get; }
        public float Volume { get; }
        public float StartDelayInSeconds { get; }
        public float Interval { get; }
    }
}