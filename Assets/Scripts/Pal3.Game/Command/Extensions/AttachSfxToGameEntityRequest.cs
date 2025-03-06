// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;
    using Engine.Core.Abstraction;
    using Newtonsoft.Json;

    public sealed class AttachSfxToGameEntityRequest : ICommand
    {
        public AttachSfxToGameEntityRequest(IGameEntity parent,
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

        [JsonIgnore] public IGameEntity Parent { get; }
        public string SfxName { get; }
        public string AudioSourceName { get; }
        public int LoopCount { get; }
        public float Volume { get; }
        public float StartDelayInSeconds { get; }
        public float Interval { get; }
    }
}