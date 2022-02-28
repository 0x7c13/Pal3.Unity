// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(206, "指定角色的缩放系数，" +
                    "参数：角色ID，缩放系数")]
    public class ActorChangeScaleCommand : ICommand
    {
        public ActorChangeScaleCommand(int actorId, float scale)
        {
            ActorId = actorId;
            Scale = scale;
        }

        public int ActorId { get; }
        public float Scale { get; }
    }
}