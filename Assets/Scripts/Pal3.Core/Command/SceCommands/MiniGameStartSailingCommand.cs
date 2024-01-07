// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3
    [SceCommand(127, "进入行船游戏")]
    public sealed class MiniGameStartSailingCommand : ICommand
    {
        public MiniGameStartSailingCommand(int startSegment, int endScriptId)
        {
            StartSegment = startSegment;
            EndScriptId = endScriptId;
        }

        public int StartSegment { get; }
        public int EndScriptId { get; }
    }
    #endif
}