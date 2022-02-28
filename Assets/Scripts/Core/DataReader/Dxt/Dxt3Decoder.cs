// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Dxt
{
	using System.IO;
	using Utils;

	/// <summary>
	/// Decode DXT3 image data and convert it to RGBA32(RGB888)
	/// </summary>
	public static class Dxt3Decoder
	{
		public static byte[] ToRgba32(byte[] data, int width, int height)
		{
			using var stream = new MemoryStream(data);
			return ToRgba32(stream, width, height);
		}

		public static byte[] ToRgba32(Stream stream, int width, int height)
		{
			var data = new byte[width * height * 4];

			using var reader = new BinaryReader(stream);

			for (var j = 0; j < (height + 3) / 4; j++)
			{
				for (var i = 0; i < (width + 3) / 4; i++)
				{
					DecodeDxt3Block(reader, i, j, width, height, data);
				}
			}

			return data;
		}

		private static void DecodeDxt3Block(BinaryReader reader, int x, int y, int width, int height, byte[] data)
		{
			byte[] alpha = reader.ReadBytes(8);

			ushort color0 = reader.ReadUInt16();
			ushort color1 = reader.ReadUInt16();

			Utility.Rgb565ToRgb888(color0, out var r0, out var g0, out var b0);
			Utility.Rgb565ToRgb888(color1, out var r1, out var g1, out var b1);

			uint lookupTable = reader.ReadUInt32();

			int alphaIndex = 0;
			for (var blockY = 0; blockY < 4; blockY++)
			{
				for (var blockX = 0; blockX < 4; blockX++)
				{
					byte r = 0, g = 0, b = 0, a = 0;

					uint index = (lookupTable >> 2 * (4 * blockY + blockX)) & 0x03;

					if (alphaIndex % 2 == 0)
					{
						a = (byte) ((alpha[alphaIndex / 2] & 0x0F) | ((alpha[alphaIndex / 2] & 0x0F) << 4));
					}
					else
					{
						a = (byte) ((alpha[(alphaIndex-1) / 2] & 0xF0) | ((alpha[(alphaIndex-1) / 2] & 0xF0) << 4));
					}

					alphaIndex++;

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

					var px = (x << 2) + blockX;
					var py = (y << 2) + blockY;

					if (px < width && py < height)
					{
						var offset = ((py * width) + px) << 2;
						data[offset] = r;
						data[offset + 1] = g;
						data[offset + 2] = b;
						data[offset + 3] = a;
					}
				}
			}
		}
	}
}