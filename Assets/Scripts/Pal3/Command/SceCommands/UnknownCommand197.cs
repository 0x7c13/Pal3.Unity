﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [AvailableInConsole]
    [SceCommand(197, "设置风雅颂宿主角色ID？")]
    public class UnknownCommand197 : ICommand
    {
        public UnknownCommand197(int actorId, int enable)
        {
            ActorId = actorId;
            Enable = enable;
        }

        public int ActorId { get; }
        public int Enable { get; }
    }
    #endif
}