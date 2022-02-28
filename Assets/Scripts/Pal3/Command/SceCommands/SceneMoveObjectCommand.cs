// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(90, "移动场景物体到某处，" +
                    "参数：物件ID，XOffset，YOffset，ZOffset，动画时间")]
    public class SceneMoveObjectCommand : ICommand
    {
        public SceneMoveObjectCommand(int objectId, float xOffset, float yOffset, float zOffset, float duration)
        {
            ObjectId = objectId;
            XOffset = xOffset;
            YOffset = yOffset;
            ZOffset = zOffset;
            Duration = duration;
        }

        public int ObjectId { get; }
        public float XOffset { get; }
        public float YOffset { get; }
        public float ZOffset { get; }
        public float Duration { get; }
    }
}