// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(212, "出情节关时队伍合并")]
    public class TeamCloseOnSceneChangeCommand : ICommand
    {
        public TeamCloseOnSceneChangeCommand() { }
    }
}