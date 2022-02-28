// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(251, "设置摄像机锁定场景物品，" +
                    "参数：物品ID")]
    public class CameraFocusOnSceneObjectCommand : ICommand
    {
        public CameraFocusOnSceneObjectCommand(int sceneObjectId)
        {
            SceneObjectId = sceneObjectId;
        }

        public int SceneObjectId { get; }
    }
}