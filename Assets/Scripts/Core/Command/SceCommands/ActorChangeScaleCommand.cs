// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
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