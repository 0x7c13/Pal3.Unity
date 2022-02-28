namespace Core.DataLoader
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

    public class TgaTextureLoader : ITextureLoader
    {
        public Texture2D LoadTexture(byte[] data, out bool hasAlphaChannel)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            // Skip header
            reader.BaseStream.Seek(12, SeekOrigin.Begin);

            var width = reader.ReadInt16();
            var height = reader.ReadInt16();
            var bitDepth = reader.ReadByte();

            // Skip a byte of header information we don't care about.
            reader.BaseStream.Seek(1, SeekOrigin.Current);

            Texture2D texture = new Texture2D(width, height);
            Color32[] pulledColors = new Color32[width * height];

            var alphaValues = new HashSet<byte>();

            if (bitDepth == 32)
            {
                for (var i = 0; i < width * height; i++)
                {
                    var red = reader.ReadByte();
                    var green = reader.ReadByte();
                    var blue = reader.ReadByte();
                    var alpha = reader.ReadByte();
                    alphaValues.Add(alpha);
                    pulledColors[i] = new Color32(blue, green, red, alpha);
                }

                // The only way to make sure if this image has alpha channel
                // is by verifying the number of different alpha values presented.
                hasAlphaChannel = alphaValues.Count >= 2;
            }
            else if (bitDepth == 24)
            {
                for (var i = 0; i < width * height; i++)
                {
                    var red = reader.ReadByte();
                    var green = reader.ReadByte();
                    var blue = reader.ReadByte();
                    pulledColors[i] = new Color32(blue, green, red, 1);
                }

                hasAlphaChannel = false;
            }
            else
            {
                throw new Exception("TGA texture had non 32/24 bit depth.");
            }

            texture.SetPixels32(pulledColors);
            texture.Apply();
            return texture;
        }
    }
}