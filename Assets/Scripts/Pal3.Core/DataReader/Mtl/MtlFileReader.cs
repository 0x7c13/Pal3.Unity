// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Mtl
{
    using System.Collections.Generic;
    using System.IO;
    using Primitives;
    using Utilities;

    public sealed class MtlFileReader : IFileReader<MtlFile>
    {
        public MtlFile Read(IBinaryReader reader, int codepage)
        {
            char[] header = reader.ReadChars(4);
            string headerStr = new string(header[..^1]);

            if (headerStr != "mtl")
            {
                throw new InvalidDataException("Invalid MTL(.mtl) file: header != mtl");
            }

            int version = reader.ReadInt32();
            if (version != 100)
            {
                throw new InvalidDataException("Invalid MTL(.mtl) file: version != 100");
            }

            int numberOfMaterials = reader.ReadInt32();
            GameBoxMaterial[] materials = new GameBoxMaterial[numberOfMaterials];

            for (int i = 0; i < numberOfMaterials; i++)
            {
                materials[i] = new GameBoxMaterial
                {
                    Diffuse = reader.ReadColor(),
                    Ambient = reader.ReadColor(),
                    Specular = reader.ReadColor(),
                    Emissive = reader.ReadColor(),
                    SpecularPower = reader.ReadSingle(),
                };

                List<string> textureNames = new ();
                for (int j = 0; j < 4; j++)
                {
                    int textureNameLength = reader.ReadInt32();
                    if (textureNameLength == 0) continue;
                    textureNames.Add(reader.ReadString(textureNameLength, codepage));
                }

                materials[i].TextureFileNames = textureNames.ToArray();
            }

            return new MtlFile(materials);
        }
    }
}