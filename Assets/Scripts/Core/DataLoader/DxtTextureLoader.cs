// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataLoader
{
	using System;
	using System.IO;
	using DataReader.Dxt;
	using UnityEngine;

	public class DxtTextureLoader : ITextureLoader
	{
		// 4 chars header label
		// 30 integers + 4 chars format info
		// Total of: 4 + 30 x 4 + 4 = 128
		private const int DDS_FILE_HEADER_SIZE = 128;

		public Texture2D LoadTexture(byte[] data, out bool hasAlphaChannel)
		{
			using var stream = new MemoryStream(data);
			using var reader = new BinaryReader(stream);

			if (!IsValidDxtFile(reader))
			{
				throw new ArgumentException($"Not a valid DXT file.");
			}

			var header = DxtHeader.ReadHeader(reader);
			var format = header.DxtPixelFormat.Format;
			hasAlphaChannel = format == "DXT1";
			return format switch
			{
				"DXT1" => LoadDxt1Texture(data[DDS_FILE_HEADER_SIZE..], header.Width, header.Height),
				"DXT3" => LoadDxt3Texture(data[DDS_FILE_HEADER_SIZE..], header.Width, header.Height),
				_ => throw new Exception($"Texture format: {format} not supported.")
			};
		}

		private bool IsValidDxtFile(BinaryReader reader)
		{
			var header = new string(reader.ReadChars(4));
			if (header == "DDS ") return true;
			Debug.Log($"Invalid DXT header: {header}");
			return false;
		}

		private Texture2D LoadDxt1Texture(byte[] data, int width, int height)
		{
			// Texture2D.LoadRawTextureData does not support DXT1 format on iOS/Android
			// devices so we need to first decode it to RGBA32(RGB888) format
			var decompressedData = Dxt1Decoder.ToRgba32(data, width, height);
			return LoadRawTextureData(decompressedData, width, height, TextureFormat.RGBA32);
		}

		private Texture2D LoadDxt3Texture(byte[] data, int width, int height)
		{
			// Texture2D.LoadRawTextureData does not support DXT3 format
			// We need to first decode it to RGBA32(RGB888) format
			var decompressedData = Dxt3Decoder.ToRgba32(data, width, height);
			return LoadRawTextureData(decompressedData, width, height, TextureFormat.RGBA32);
		}

		private Texture2D LoadRawTextureData(byte[] data, int width, int height, TextureFormat format)
		{
			Texture2D texture = new Texture2D(width, height, format, false);
			texture.LoadRawTextureData(data);
			texture.Apply();
			return texture;
		}
	}
}
