// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    using Newtonsoft.Json;
    using UnityEngine;

    public class PlaySfxAtGameObjectRequest : ICommand
    {
        public PlaySfxAtGameObjectRequest(string sfxName, int loopCount, GameObject parent)
        {
            SfxName = sfxName;
            LoopCount = loopCount;
            Parent = parent;
        }

        public string SfxName { get; }
        public int LoopCount { get; }

        [JsonIgnore]
        public GameObject Parent { get; }
    }
}