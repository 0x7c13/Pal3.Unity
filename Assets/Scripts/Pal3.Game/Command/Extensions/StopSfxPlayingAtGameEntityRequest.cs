// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;
    using Engine.Core.Abstraction;
    using Newtonsoft.Json;

    public sealed class StopSfxPlayingAtGameEntityRequest : ICommand
    {
        public StopSfxPlayingAtGameEntityRequest(IGameEntity parent, string audioSourceName, bool disposeSource)
        {
            Parent = parent;
            AudioSourceName = audioSourceName;
            DisposeSource = disposeSource;
        }

        [JsonIgnore] public IGameEntity Parent { get; }
        public string AudioSourceName { get; }
        public bool DisposeSource { get; }
    }
}