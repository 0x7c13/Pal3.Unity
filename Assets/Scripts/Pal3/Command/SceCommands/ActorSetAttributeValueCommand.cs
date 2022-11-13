// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(41, "设置角色属性值" +
                    "参数：属性值ID，属性值类型，数值")]
    public class ActorSetAttributeValueCommand : ICommand
    {
        public ActorSetAttributeValueCommand(int attributeId, int attributeType, int value)
        {
            AttributeId = attributeId;
            AttributeType = attributeType;
            Value = value;
        }
        
        public int AttributeId { get; }
        public int AttributeType { get; }
        public int Value { get; }
    }
}