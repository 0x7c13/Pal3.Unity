// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(211, "进入情节关时队伍散开")]
    public class TeamOpenOnSceneChangeCommand : ICommand
    {
        public TeamOpenOnSceneChangeCommand() { }
    }
}