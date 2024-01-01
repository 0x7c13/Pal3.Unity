// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(54, "设置主角的某项属性为满值，例如精加满，" +
                    "参数：角色ID，属性值类型")]
    public sealed class ActorSetAttributeToFullCommand : ICommand
    {
        public ActorSetAttributeToFullCommand(int actorId, int attributeType)
        {
            ActorId = actorId;
            AttributeType = attributeType;
        }

        [SceActorId] public int ActorId { get; set; }
        public int AttributeType { get; }
    }
}