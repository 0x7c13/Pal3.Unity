// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(72, "在角色头顶出现表情符号，" +
                    "参数：角色ID，表情编号ID")]
    public class ActorShowEmojiCommand : ICommand
    {
        public ActorShowEmojiCommand(int actorId, int emojiId)
        {
            ActorId = actorId;
            EmojiId = emojiId;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public int EmojiId { get; }
    }
}