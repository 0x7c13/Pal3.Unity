// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.Extensions
{
    using Core.Command;
    using Newtonsoft.Json;
    using UnityEngine;

    public class StopSfxPlayingAtGameObjectRequest : ICommand
    {
        public StopSfxPlayingAtGameObjectRequest(GameObject parent, string audioSourceName, bool disposeSource)
        {
            Parent = parent;
            AudioSourceName = audioSourceName;
            DisposeSource = disposeSource;
        }

        [JsonIgnore] public GameObject Parent { get; }
        public string AudioSourceName { get; }
        public bool DisposeSource { get; }
    }
}