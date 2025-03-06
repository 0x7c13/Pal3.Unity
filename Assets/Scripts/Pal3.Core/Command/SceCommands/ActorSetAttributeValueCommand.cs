// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(41, "设置角色某项属性值的数值" +
                    "参数：角色ID，属性值类型，数值")]
    public sealed class ActorSetAttributeValueCommand : ICommand
    {
        public ActorSetAttributeValueCommand(int actorId,
            int attributeType,
            int value)
        {
            ActorId = actorId;
            AttributeType = attributeType;
            Value = value;
        }

        [SceActorId] public int ActorId { get; set; }
        public int AttributeType { get; }
        public int Value { get; }
    }
}