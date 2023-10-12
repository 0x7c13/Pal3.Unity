// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.DataLoader
{
	using System;
	using System.IO;
	using Core.Implementation;
	using Pal3.Core.DataReader.Dxt;
	using UnityEngine;

	/// <summary>
	/// .dds file loader and Texture2D converter.
	/// </summary>
	public sealed class DxtTextureLoader : ITextureLoader
	{
		private int _width;
		private int _height;
		private byte[] _rawRgbaData;

		public void Load(byte[] data, out bool hasAlphaChannel)
		{
			using var stream = new MemoryStream(data);
			using var headerReader = new BinaryReader(stream);

			if (new string(headerReader.ReadChars(4)) != "DDS ")
			{
				throw new InvalidDataException("Not a valid DXT file");
			}

			DxtHeader header = DxtHeader.ReadHeader(headerReader);

			_width = header.Width;
			_height = header.Height;

			switch (header.DxtPixelFormat.Format)
			{
				case "DXT1":
					hasAlphaChannel = false;
					LoadDxt1Texture(data);
					break;
				case "DXT3":
					hasAlphaChannel = true;
					LoadDxt3Texture(data);
					break;
				case "DXT5":
					throw new NotImplementedException("DXT5 decoder not implemented yet");
				default:
					throw new Exception($"DXT Texture format: {header.DxtPixelFormat.Format} is not supported");
			}
		}

		// Texture2D.LoadRawTextureData does not support DXT1 format on iOS/Android
		// devices so we have to manually decode it to RGBA32(RGB888) format and then
		// invoke LoadRawTextureData method later to convert it to Texture2D.
		private void LoadDxt1Texture(byte[] data)
		{
			_rawRgbaData = Dxt1Decoder.ToRgba32(data, _width, _height);
		}

		// Texture2D.LoadRawTextureData does not support DXT3 format
		// so we have to manually decode it to RGBA32(RGB888) format and then
		// invoke LoadRawTextureData method later to convert it to Texture2D.
		private void LoadDxt3Texture(byte[] data)
		{
			_rawRgbaData = Dxt3Decoder.ToRgba32(data, _width, _height);
		}

		public Texture2D ToTexture2D()
		{
			return _rawRgbaData == null ? null : TextureFactory.CreateTexture2D(_width, _height, _rawRgbaData);
		}
	}
}
