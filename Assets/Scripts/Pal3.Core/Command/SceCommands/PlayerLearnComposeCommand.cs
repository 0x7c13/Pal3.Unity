// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(111, "学会合成术，" +
                     "参数：合成物品的ID")]
    public sealed class PlayerLearnComposeCommand : ICommand
    {
        public PlayerLearnComposeCommand(int objectId)
        {
            ObjectId = objectId;
        }

        public int ObjectId { get; }
    }
}