// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(51, "某个主角学会新特技，" +
                    "参数：主角ID，特技ID")]
    public sealed class ActorAddSkillCommand : ICommand
    {
        public ActorAddSkillCommand(int actorId, int skillId)
        {
            ActorId = actorId;
            SkillId = skillId;
        }

        [SceActorId] public int ActorId { get; set; }
        public int SkillId { get; }
    }
}