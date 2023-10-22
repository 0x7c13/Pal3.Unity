// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(153, "瞬间回满全队精气神")]
    public sealed class TeamSetAttributeToFullCommand : ICommand
    {
        public TeamSetAttributeToFullCommand() {}
    }
}