// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.DataLoader
{
    using System.Buffers;
    using Core.Abstraction;
    using Core.Implementation;
    using Pal3.Core.DataReader.Bmp;

    public sealed class BmpTextureLoader : ITextureLoader
    {
        public ITextureDataSource Load(byte[] data, out bool hasAlphaChannel)
        {
            hasAlphaChannel = false;
            BmpFile image = new BmpFileReader().Read(data);
            int width = image.Info.AbsWidth;
            int height = image.Info.AbsHeight;

            if (image.Info.Height < 0)
            {
                for (int y = 0; y < height / 2; y++)
                {
                    for (int x = 0, o1 = y * width, o2 = (height - y - 1) * width; x < width; x++, o1++, o2++)
                        (image.ImageData[o1], image.ImageData[o2]) = (image.ImageData[o2], image.ImageData[o1]);
                }
            }

            byte[] rgba = ArrayPool<byte>.Shared.Rent(checked(image.ImageData.Length * 4));
            try
            {
                for (int i = 0; i < image.ImageData.Length; i++)
                {
                    rgba[i * 4 + 0] = image.ImageData[i].B;
                    rgba[i * 4 + 1] = image.ImageData[i].G;
                    rgba[i * 4 + 2] = image.ImageData[i].R;
                    rgba[i * 4 + 3] = image.ImageData[i].A;
                }
                return Rgba32TextureDataSource.TakePooledBuffer(width, height, rgba, hasTransparency: false);
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(rgba);
                throw;
            }
        }
    }
}
