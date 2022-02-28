// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(127, "进入行船游戏")]
    public class MiniGameStartSailingCommand : ICommand
    {
        public MiniGameStartSailingCommand(int startSegment, int endScriptId)
        {
            StartSegment = startSegment;
            EndScriptId = endScriptId;
        }

        public int StartSegment { get; }
        public int EndScriptId { get; }
    }
}