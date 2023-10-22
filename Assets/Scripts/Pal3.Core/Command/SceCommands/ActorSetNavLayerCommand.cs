// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(203, "设置角色所在的地层，" +
                     "参数：ID，层数（0或1）")]
    public sealed class ActorSetNavLayerCommand : ICommand
    {
        public ActorSetNavLayerCommand(int actorId, int layerIndex)
        {
            ActorId = actorId;
            LayerIndex = layerIndex;
        }

        [SceActorId] public int ActorId { get; set; }
        public int LayerIndex { get; }
    }
}