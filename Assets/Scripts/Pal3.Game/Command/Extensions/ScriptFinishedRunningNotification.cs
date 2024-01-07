// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command.Extensions
{
    using Core.Command;
    using Script;

    public sealed class ScriptFinishedRunningNotification : ICommand
    {
        public ScriptFinishedRunningNotification(uint scriptId, PalScriptType scriptType)
        {
            ScriptId = scriptId;
            ScriptType = scriptType;
        }

        public uint ScriptId { get; }
        public PalScriptType ScriptType { get; }
    }
}