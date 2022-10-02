// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    using Newtonsoft.Json;
    using UnityEngine;

    public class StopSfxPlayingAtGameObjectRequest : ICommand
    {
        public StopSfxPlayingAtGameObjectRequest(GameObject parent, string audioSourceName)
        {
            Parent = parent;
            AudioSourceName = audioSourceName;
        }
        
        [JsonIgnore] public GameObject Parent { get; }
        public string AudioSourceName { get; }
    }
}