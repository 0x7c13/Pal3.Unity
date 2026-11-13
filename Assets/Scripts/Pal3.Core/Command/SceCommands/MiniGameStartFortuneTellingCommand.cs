// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(185, "显示算命界面并等待结果")]
    public sealed class MiniGameStartFortuneTellingCommand : ICommand
    {
        public MiniGameStartFortuneTellingCommand(int symbol1, int symbol2)
        {
            Symbol1 = symbol1;
            Symbol2 = symbol2;
        }

        public int Symbol1 { get; }

        public int Symbol2 { get; }
    }
}
#endif
