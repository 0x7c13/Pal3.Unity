// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    public class ScriptFailedToRunNotification : ICommand
    {
        public ScriptFailedToRunNotification(uint scriptId)
        {
            ScriptId = scriptId;
        }

        public uint ScriptId { get; }
    }
}