// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Dxt
{
	using Utilities;

	/// <summary>
	/// Decode DXT1 image data and convert it to RGBA32(RGB888).
	/// </summary>
	public static class Dxt1Decoder
	{
		// 4 chars header label
		// 30 integers + 4 chars format info
		// Total of: 4 + 30 x 4 + 4 = 128
		private const int DDS_FILE_HEADER_SIZE = 128;

		public static unsafe byte[] ToRgba32(byte[] data, int width, int height)
		{
			byte[] buffer = new byte[width * height * 4];

			fixed (byte* srcStart = &data[DDS_FILE_HEADER_SIZE], dstStart = &buffer[0])
			{
				var src = srcStart;
				var dst = dstStart;

				for (var j = 0; j < (height + 3) / 4; j++)
				{
					for (var i = 0; i < (width + 3) / 4; i++)
					{
						DecodeDxt1Block(i, j, width, height, src, dst);
						src += 8; // 2 + 2 + 4
					}
				}
			}
			return buffer;
		}

		private static unsafe void DecodeDxt1Block(int x, int y, int width, int height, byte* src, byte* dst)
		{
			ushort color0 = *(ushort*)src;
			ushort color1 = *(ushort*)(src + 2);

			CoreUtility.Rgb565ToRgb888(color0, out var r0, out var g0, out var b0);
			CoreUtility.Rgb565ToRgb888(color1, out var r1, out var g1, out var b1);

			uint lookupTable = *(uint*)(src + 4);

			for (var blockY = 0; blockY < 4; blockY++)
			{
				for (var blockX = 0; blockX < 4; blockX++)
				{
					byte r = 0, g = 0, b = 0, a = 255;
					uint index = (lookupTable >> 2 * (4 * blockY + blockX)) & 0x03;

					if (color0 > color1)
					{
						switch (index)
						{
							case 0:
								r = r0; g = g0; b = b0;
								break;
							case 1:
								r = r1; g = g1; b = b1;
								break;
							case 2:
								r = (byte) ((2 * r0 + r1) / 3);
								g = (byte) ((2 * g0 + g1) / 3);
								b = (byte) ((2 * b0 + b1) / 3);
								break;
							case 3:
								r = (byte) ((r0 + 2 * r1) / 3);
								g = (byte) ((g0 + 2 * g1) / 3);
								b = (byte) ((b0 + 2 * b1) / 3);
								break;
						}
					}
					else
					{
						switch (index)
						{
							case 0:
								r = r0; g = g0; b = b0;
								break;
							case 1:
								r = r1; g = g1; b = b1;
								break;
							case 2:
								r = (byte) ((r0 + r1) / 2);
								g = (byte) ((g0 + g1) / 2);
								b = (byte) ((b0 + b1) / 2);
								break;
							case 3:
								r = 0;
								g = 0;
								b = 0;
								a = 0;
								break;
						}
					}

					int px = (x << 2) + blockX;
					int py = (y << 2) + blockY;
					if ((px < width) && (py < height))
					{
						int offset = ((py * width) + px) << 2;
						*(dst + offset) = r;
						*(dst + offset + 1) = g;
						*(dst + offset + 2) = b;
						*(dst + offset + 3) = a;
					}
				}
			}
		}
	}
}