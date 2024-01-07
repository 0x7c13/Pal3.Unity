// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(206, "指定角色的缩放系数，" +
                    "参数：角色ID，缩放系数")]
    public sealed class ActorChangeScaleCommand : ICommand
    {
        public ActorChangeScaleCommand(int actorId, float scale)
        {
            ActorId = actorId;
            Scale = scale;
        }

        [SceActorId] public int ActorId { get; set; }
        public float Scale { get; }
    }
}