// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(186, "将算命结果写入脚本变量")]
    public sealed class ScriptVarSetFortuneTellingResultCommand : ICommand
    {
        public ScriptVarSetFortuneTellingResultCommand(ushort variable)
        {
            Variable = variable;
        }

        [SceUserVariable]
        public ushort Variable { get; set; }
    }
}
#endif
