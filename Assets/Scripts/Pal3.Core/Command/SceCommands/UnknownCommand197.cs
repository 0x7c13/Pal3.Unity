// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(197, "设置风雅颂宿主角色ID？")]
    public sealed class UnknownCommand197 : ICommand
    {
        public UnknownCommand197(int actorId, int enable)
        {
            ActorId = actorId;
            Enable = enable;
        }

        [SceActorId] public int ActorId { get; set; }
        public int Enable { get; }
    }
    #endif
}