// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(116, "给角色更换纹理贴图" +
                    "参数：角色ID，贴图名")]
    public class ActorChangeTextureCommand : ICommand
    {
        public ActorChangeTextureCommand(int actorId, string textureName)
        {
            ActorId = actorId;
            TextureName = textureName;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public string TextureName { get; }
    }
}