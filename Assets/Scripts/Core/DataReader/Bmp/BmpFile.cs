// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Bmp
{
    using System.Collections.Generic;
    using UnityEngine;

    public sealed class BmpFile
    {
        public BmpFileHeader Header;
        public BitmapInfoHeader Info;
        public uint RMask = 0x00FF0000;
        public uint GMask = 0x0000FF00;
        public uint BMask = 0x000000FF;
        public uint AMask = 0x00000000;
        public List<Color32> Palette;
        public Color32[] ImageData;

        // private void ReplaceColor(Color32 aColorToSearch, Color32 aReplacementColor)
        // {
        //     Color32 s = aColorToSearch;
        //     for (int i = 0; i < ImageData.Length; i++)
        //     {
        //         Color32 c = ImageData[i];
        //         if (c.r == s.r && c.g == s.g && c.b == s.b && c.a == s.a)
        //             ImageData[i] = aReplacementColor;
        //     }
        // }
        //
        // private void ReplaceFirstPixelColor(Color32 aReplacementColor)
        // {
        //     ReplaceColor(ImageData[0], aReplacementColor);
        // }
        //
        // public void ReplaceFirstPixelColorWithTransparency()
        // {
        //     ReplaceFirstPixelColor(new Color32(0, 0, 0, 0));
        // }
    }
}