// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(90, "移动场景物体到某处，" +
                    "参数：物件ID，原GameBox引擎下的一个三维坐标插值（X，Y，Z），动画时间")]
    public class SceneMoveObjectCommand : ICommand
    {
        public SceneMoveObjectCommand(int objectId,
            float gameBoxXOffset,
            float gameBoxYOffset,
            float gameBoxZOffset,
            float duration)
        {
            ObjectId = objectId;
            GameBoxXOffset = gameBoxXOffset;
            GameBoxYOffset = gameBoxYOffset;
            GameBoxZOffset = gameBoxZOffset;
            Duration = duration;
        }

        public int ObjectId { get; }
        public float GameBoxXOffset { get; }
        public float GameBoxYOffset { get; }
        public float GameBoxZOffset { get; }
        public float Duration { get; }
    }
}