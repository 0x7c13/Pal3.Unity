// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(67, "渲染人物头像立绘，" +
                    "参数：角色ID，头像图片编号，问或答（0头像在左，1头像在右）")]
    public sealed class DialogueRenderActorAvatarCommand : ICommand
    {
        public DialogueRenderActorAvatarCommand(int actorId,
            string avatarTextureName,
            int rightAligned)
        {
            ActorId = actorId;
            AvatarTextureName = avatarTextureName;
            RightAligned = rightAligned;
        }

        [SceActorId] public int ActorId { get; set; }
        public string AvatarTextureName { get; }
        public int RightAligned { get; }
    }
}