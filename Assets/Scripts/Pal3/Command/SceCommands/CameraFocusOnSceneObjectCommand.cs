﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
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