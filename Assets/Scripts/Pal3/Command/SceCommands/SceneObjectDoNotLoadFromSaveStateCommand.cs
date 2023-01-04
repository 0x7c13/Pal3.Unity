// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(158, "设置某个物件出场时候的不从存档里拿状态，" +
                    "参数：物件ID")]
    public class SceneObjectDoNotLoadFromSaveStateCommand : ICommand
    {
        public SceneObjectDoNotLoadFromSaveStateCommand(int objectId)
        {
            ObjectId = objectId;
        }

        public int ObjectId { get; }
    }
}