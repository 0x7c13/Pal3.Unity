// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [SceCommand(184, "更改角色透明度???")]
    public class UnknownCommand184 : ICommand
    {
        public UnknownCommand184(int actorId, float transparency)
        {
            ActorId = actorId;
            Transparency = transparency;
        }

        public int ActorId { get; }
        public float Transparency { get; }
    }
    #endif
}