// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(116, "给角色更换纹理贴图" +
                    "参数：角色ID，贴图名")]
    public sealed class ActorChangeTextureCommand : ICommand
    {
        public ActorChangeTextureCommand(int actorId, string textureName)
        {
            ActorId = actorId;
            TextureName = textureName;
        }

        [SceActorId] public int ActorId { get; set; }
        public string TextureName { get; }
    }
}