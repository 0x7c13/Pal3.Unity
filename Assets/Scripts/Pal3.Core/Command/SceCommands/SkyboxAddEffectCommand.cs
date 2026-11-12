// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Core.Command.SceCommands
{
    [SceCommand(167, "在天空盒面添加循环序列帧特效")]
    public sealed class SkyboxAddEffectCommand : ICommand
    {
        public SkyboxAddEffectCommand(string sequence, int face, float x, float y, float scale, float fps, float interval, float initialDelay)
        {
            Sequence = sequence;
            Face = face;
            X = x;
            Y = y;
            Scale = scale;
            Fps = fps;
            Interval = interval;
            InitialDelay = initialDelay;
        }

        public string Sequence { get; }

        public int Face { get; }

        public float X { get; }

        public float Y { get; }

        public float Scale { get; }

        public float Fps { get; }

        public float Interval { get; }

        public float InitialDelay { get; }
    }
}
#endif
