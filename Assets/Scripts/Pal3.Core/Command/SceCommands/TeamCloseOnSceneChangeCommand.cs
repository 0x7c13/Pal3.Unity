// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(212, "出情节关时队伍合并")]
    public sealed class TeamCloseOnSceneChangeCommand : ICommand
    {
        public TeamCloseOnSceneChangeCommand() { }
    }
}