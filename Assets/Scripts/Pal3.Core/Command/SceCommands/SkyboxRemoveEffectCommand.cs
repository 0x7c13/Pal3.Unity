// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(168, "按序列名称移除天空盒特效")]
    public sealed class SkyboxRemoveEffectCommand : ICommand
    {
        public SkyboxRemoveEffectCommand(string sequence)
        {
            Sequence = sequence;
        }

        public string Sequence { get; }
    }
}
#endif
