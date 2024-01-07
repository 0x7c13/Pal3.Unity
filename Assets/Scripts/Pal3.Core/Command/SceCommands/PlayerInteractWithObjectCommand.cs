// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(124, "与指定场景物品交互，" +
                     "参数：场景物品ID")]
    public sealed class PlayerInteractWithObjectCommand : ICommand
    {
        public PlayerInteractWithObjectCommand(int sceneObjectId)
        {
            SceneObjectId = sceneObjectId;
        }

        public int SceneObjectId { get; }
    }
}