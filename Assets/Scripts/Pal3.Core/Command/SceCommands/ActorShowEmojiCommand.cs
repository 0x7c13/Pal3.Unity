// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(72, "在角色头顶出现表情符号，" +
                    "参数：角色ID，表情编号ID")]
    public sealed class ActorShowEmojiCommand : ICommand
    {
        public ActorShowEmojiCommand(int actorId, int emojiId)
        {
            ActorId = actorId;
            EmojiId = emojiId;
        }

        [SceActorId] public int ActorId { get; set; }
        public int EmojiId { get; }
    }
}