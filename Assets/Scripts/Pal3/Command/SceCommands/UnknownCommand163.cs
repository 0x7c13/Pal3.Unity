// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [SceCommand(163, "???")]
    public class UnknownCommand163 : ICommand
    {
        public UnknownCommand163(
            string sceneName,
            int objectId,
            int enable)
        {
            SceneName = sceneName;
            ObjectId = objectId;
            Enable = enable;
        }
    
        public string SceneName { get; }
        public int ObjectId { get; }
        public int Enable { get; }
    }
    #endif
}