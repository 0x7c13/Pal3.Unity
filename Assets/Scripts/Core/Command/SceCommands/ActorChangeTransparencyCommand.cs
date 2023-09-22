// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(184, "更改角色透明度，" +
                     "参数：角色ID，透明度(0-1f)")]
    public class ActorChangeTransparencyCommand : ICommand
    {
        public ActorChangeTransparencyCommand(int actorId, float alpha)
        {
            ActorId = actorId;
            Alpha = alpha;
        }

        public int ActorId { get; }
        public float Alpha { get; }
    }
    #endif
}