// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [AvailableInConsole]
    [SceCommand(163, "设置某个物件出现或隐藏，" +
                    "参数：场景名，物件ID，是否出现（0隐藏，1出现）")]
    public class SceneActivateObject2Command : ICommand
    {
        public SceneActivateObject2Command(
            string sceneName,
            int objectId,
            int isActive)
        {
            SceneName = sceneName;
            ObjectId = objectId;
            IsActive = isActive;
        }
    
        public string SceneName { get; }
        public int ObjectId { get; }
        public int IsActive { get; }
    }
    #endif
}