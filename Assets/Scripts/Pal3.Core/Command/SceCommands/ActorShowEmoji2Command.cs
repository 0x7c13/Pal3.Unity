// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command.SceCommands
{
    #if PAL3A
    [SceCommand(173, "在角色头顶出现表情符号，" +
                    "参数：角色ID，表情编号ID")]
    public sealed class ActorShowEmoji2Command : ICommand
    {
        public ActorShowEmoji2Command(int actorId, int emojiId)
        {
            ActorId = actorId;
            EmojiId = emojiId;
        }

        [SceActorId] public int ActorId { get; set; }
        public int EmojiId { get; }
    }
    #endif
}