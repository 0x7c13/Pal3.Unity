// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(51, "某个主角学会新特技，" +
                    "参数：主角ID，特技ID")]
    public class ActorAddSkillCommand : ICommand
    {
        public ActorAddSkillCommand(int actorId, int skillId)
        {
            ActorId = actorId;
            SkillId = skillId;
        }

        public int ActorId { get; }
        public int SkillId { get; }
    }
}