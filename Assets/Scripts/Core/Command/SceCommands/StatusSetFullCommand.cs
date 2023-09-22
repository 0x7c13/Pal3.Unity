// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    [SceCommand(54, "设置主角的某项属性为满值，例如精加满，" +
                    "参数：角色ID，属性编号")]
    public class StatusSetFullCommand : ICommand
    {
        public StatusSetFullCommand(int actorId, int statusId)
        {
            ActorId = actorId;
            StatusId = statusId;
        }

        public int ActorId { get; }
        public int StatusId { get; }
    }
}