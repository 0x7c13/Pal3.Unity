// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.InternalCommands
{
    using Script;

    public class ScriptFinishedRunningNotification : ICommand
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