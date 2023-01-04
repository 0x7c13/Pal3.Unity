// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3
    [AvailableInConsole]
    [SceCommand(86, "显示全屏字幕（例如显示诗句等），" +
                    "参数：字幕序号（对应美术图片，每个字幕一个图片），列数")]
    public class UIDisplayCaptionCommand : ICommand
    {
        public UIDisplayCaptionCommand(string textureName, int numberOfLines)
        {
            TextureName = textureName;
            NumberOfLines = numberOfLines;
        }

        public string TextureName { get; }
        public int NumberOfLines { get; }
    }
    #elif PAL3A
    [AvailableInConsole]
    [SceCommand(86, "显示全屏字幕（例如显示诗句等），" +
                    "参数：字幕序号（对应美术图片，每个字幕一个图片），列数，是否还有下一行")]
    public class UIDisplayCaptionCommand : ICommand
    {
        public UIDisplayCaptionCommand(string textureName, int numberOfLines, int hasNextRow)
        {
            TextureName = textureName;
            NumberOfLines = numberOfLines;
            HasNextRow = hasNextRow;
        }

        public string TextureName { get; }
        public int NumberOfLines { get; }
        public int HasNextRow { get; }
    }
    #endif
}