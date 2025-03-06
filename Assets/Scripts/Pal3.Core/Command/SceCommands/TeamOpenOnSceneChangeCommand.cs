// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(211, "进入情节关时队伍散开")]
    public sealed class TeamOpenOnSceneChangeCommand : ICommand
    {
        public TeamOpenOnSceneChangeCommand() { }
    }
}