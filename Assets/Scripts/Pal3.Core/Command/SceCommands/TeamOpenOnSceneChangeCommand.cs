// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
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