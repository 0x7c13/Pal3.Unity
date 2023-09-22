// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(124, "与指定场景物品交互，" +
                     "参数：场景物品ID")]
    public class PlayerInteractWithObjectCommand : ICommand
    {
        public PlayerInteractWithObjectCommand(int sceneObjectId)
        {
            SceneObjectId = sceneObjectId;
        }

        public int SceneObjectId { get; }
    }
}