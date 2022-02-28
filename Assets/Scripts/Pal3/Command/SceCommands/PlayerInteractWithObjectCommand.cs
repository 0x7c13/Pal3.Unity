// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
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