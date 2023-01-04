// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [AvailableInConsole]
    [SceCommand(203, "设置角色所在的地层，" +
                     "参数：ID，层数（0或1）")]
    public class ActorSetNavLayerCommand : ICommand
    {
        public ActorSetNavLayerCommand(int actorId, int layerIndex)
        {
            ActorId = actorId;
            LayerIndex = layerIndex;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public int LayerIndex { get; }
    }
}