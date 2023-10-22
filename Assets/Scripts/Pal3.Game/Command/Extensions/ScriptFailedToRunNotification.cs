// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;

    public sealed class ScriptFailedToRunNotification : ICommand
    {
        public ScriptFailedToRunNotification(uint scriptId)
        {
            ScriptId = scriptId;
        }

        public uint ScriptId { get; }
    }
}