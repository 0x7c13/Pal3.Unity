// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.DataLoader
{
	using System;
	using System.Buffers;
	using System.IO;
	using Core.Abstraction;
	using Pal3.Core.DataReader.Dxt;

	/// <summary>
	/// .dds file loader and Texture2D converter.
	/// </summary>
	public sealed class DxtTextureLoader : ITextureLoader
	{
		private readonly ITextureFactory _textureFactory;

		private int _width;
		private int _height;
		private byte[] _rawRgbaDataBuffer;

		public DxtTextureLoader(ITextureFactory textureFactory)
		{
			_textureFactory = textureFactory;
		}

		public void Load(byte[] data, out bool hasAlphaChannel)
		{
			if (_rawRgbaDataBuffer != null) throw new Exception("DXT texture already loaded");

			using var stream = new MemoryStream(data);
			using var headerReader = new BinaryReader(stream);

			if (new string(headerReader.ReadChars(4)) != "DDS ")
			{
				throw new InvalidDataException("Not a valid DXT file");
			}

			DxtHeader header = DxtHeader.ReadHeader(headerReader);

			_width = header.Width;
			_height = header.Height;

			_rawRgbaDataBuffer = ArrayPool<byte>.Shared.Rent(_width * _height * 4);

			switch (header.DxtPixelFormat.Format)
			{
				case "DXT1":
					hasAlphaChannel = false;
					Dxt1Decoder.ToRgba32(data, _width, _height, _rawRgbaDataBuffer);
					break;
				case "DXT3":
					hasAlphaChannel = true;
					Dxt3Decoder.ToRgba32(data, _width, _height, _rawRgbaDataBuffer);
					break;
				case "DXT5":
					throw new NotImplementedException("DXT5 decoder not implemented");
				default:
					throw new Exception($"DXT Texture format: {header.DxtPixelFormat.Format} is not supported");
			}
		}

		public ITexture2D ToTexture()
		{
			if (_rawRgbaDataBuffer == null) return null;

			try
			{
				return _textureFactory.CreateTexture(_width, _height, _rawRgbaDataBuffer);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(_rawRgbaDataBuffer);
				_rawRgbaDataBuffer = null;
			}
		}
	}
}
