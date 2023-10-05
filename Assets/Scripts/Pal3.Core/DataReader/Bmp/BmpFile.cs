// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Bmp
{
    using Primitives;
    using System.Collections.Generic;

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
    }
}