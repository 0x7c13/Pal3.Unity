// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
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
            var header = reader.ReadChars(4);
            var headerStr = new string(header[..^1]);

            if (headerStr != "mtl")
            {
                throw new InvalidDataException("Invalid MTL(.mtl) file: header != mtl");
            }

            var version = reader.ReadInt32();
            if (version != 100)
            {
                throw new InvalidDataException("Invalid MTL(.mtl) file: version != 100");
            }

            int numberOfMaterials = reader.ReadInt32();
            var materials = new GameBoxMaterial[numberOfMaterials];

            for (var i = 0; i < numberOfMaterials; i++)
            {
                materials[i] = new GameBoxMaterial
                {
                    Diffuse = CoreUtility.ToColor(reader.ReadSingles(4)),
                    Ambient = CoreUtility.ToColor(reader.ReadSingles(4)),
                    Specular = CoreUtility.ToColor(reader.ReadSingles(4)),
                    Emissive = CoreUtility.ToColor(reader.ReadSingles(4)),
                    SpecularPower = reader.ReadSingle(),
                };

                List<string> textureNames = new ();
                for (var j = 0; j < 4; j++)
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