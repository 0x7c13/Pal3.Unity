// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(184, "更改角色透明度，" +
                     "参数：角色ID，透明度(0-1f)")]
    public sealed class ActorChangeTransparencyCommand : ICommand
    {
        public ActorChangeTransparencyCommand(int actorId, float alpha)
        {
            ActorId = actorId;
            Alpha = alpha;
        }

        [SceActorId] public int ActorId { get; set; }
        public float Alpha { get; }
    }
    #endif
}