// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Contract.Constants
{
    using System.Collections.Generic;
    using Enums;

    public static class ActorEmojiConstants
    {
        public static readonly Dictionary<ActorEmojiType, (int Width, int Height, int Frames)> TextureInfo = new()
        {
            { ActorEmojiType.Sleepy,      (38,  54,  6) },
            { ActorEmojiType.Shock,       (25,  57, 10) },
            { ActorEmojiType.Doubt,       (31,  43, 10) },
            { ActorEmojiType.Anger,       (82,  51,  3) },
            { ActorEmojiType.Happy,       (54,  32,  2) },
            { ActorEmojiType.Heart,       (31,  41,  4) },
            { ActorEmojiType.Sweat,       (16, 111,  7) },
            { ActorEmojiType.Bother,      (64,  46,  3) },
            { ActorEmojiType.Anxious,     (54,  32,  2) },
            { ActorEmojiType.Cry,         (49,  40,  8) },
            { ActorEmojiType.Dizzy,       (70,  42,  6) },
            #if PAL3A
            { ActorEmojiType.Speechless,  (64,  46,  4) },
            #endif
        };

        public static readonly Dictionary<ActorEmojiType, int> AnimationLoopCountInfo = new()
        {
            { ActorEmojiType.Sleepy,      3 },
            { ActorEmojiType.Shock,       1 },
            { ActorEmojiType.Doubt,       1 },
            { ActorEmojiType.Anger,       3 },
            { ActorEmojiType.Happy,       3 },
            { ActorEmojiType.Heart,       3 },
            { ActorEmojiType.Sweat,       1 },
            { ActorEmojiType.Bother,      3 },
            { ActorEmojiType.Anxious,     3 },
            { ActorEmojiType.Cry,         1 },
            { ActorEmojiType.Dizzy,       3 },
            #if PAL3A
            { ActorEmojiType.Speechless,  3 },
            #endif
        };

        #if PAL3
        public static readonly Dictionary<ActorEmojiType, string> EmojiSfxInfo = new()
        {
            { ActorEmojiType.Sleepy,      "we06" },
            { ActorEmojiType.Shock,       "" },
            { ActorEmojiType.Doubt,       "" },
            { ActorEmojiType.Anger,       "" },
            { ActorEmojiType.Happy,       "" },
            { ActorEmojiType.Heart,       "" },
            { ActorEmojiType.Sweat,       "" },
            { ActorEmojiType.Bother,      "" },
            { ActorEmojiType.Anxious,     "" },
            { ActorEmojiType.Cry,         "" },
            { ActorEmojiType.Dizzy,       "we05" },
        };
        #endif
    }
}