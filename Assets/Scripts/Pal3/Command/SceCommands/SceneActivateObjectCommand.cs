// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(85, "设置某个物件出现或隐藏，" +
                    "参数：物件ID，是否出现（0隐藏，1出现）")]
    public class SceneActivateObjectCommand : ICommand
    {
        public SceneActivateObjectCommand(int objectId, int isActive)
        {
            ObjectId = objectId;
            IsActive = isActive;
        }

        public int ObjectId { get; }
        public int IsActive { get; }
    }
}