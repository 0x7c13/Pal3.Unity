// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Lgt
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Primitives;
    using Utilities;

    public sealed class LgtFileReader : IFileReader<LgtFile>
    {
        public LgtFile Read(IBinaryReader reader, int codepage)
        {
            int numOfLightNodes = reader.ReadInt32();
            const int recordBytes = 148;
            if (numOfLightNodes < 0 || numOfLightNodes > (reader.Length - reader.Position) / recordBytes)
                throw new InvalidDataException("The LGT light count exceeds its 148-byte native records.");
            List<LightNode> lightNodes = new();

            for (int i = 0; i < numOfLightNodes; i++)
            {
                LightNode lightNode = ReadLightNode(reader);

                if (Enum.IsDefined(typeof(GameBoxLightType), lightNode.LightType) &&
                    Enum.IsDefined(typeof(GameBoxLightShapeType), lightNode.LightShapeType))
                {
                    lightNodes.Add(lightNode);
                }
            }

            return new LgtFile(lightNodes.ToArray());
        }

        private static LightNode ReadLightNode(IBinaryReader reader)
        {
            GameBoxMatrix4x4 transformMatrix = new()
            {
                Xx = reader.ReadSingle(),
                Xy = reader.ReadSingle(),
                Xz = reader.ReadSingle(),
                Xw = reader.ReadSingle(),
                Yx = reader.ReadSingle(),
                Yy = reader.ReadSingle(),
                Yz = reader.ReadSingle(),
                Yw = reader.ReadSingle(),
                Zx = reader.ReadSingle(),
                Zy = reader.ReadSingle(),
                Zz = reader.ReadSingle(),
                Zw = reader.ReadSingle(),
                Tx = reader.ReadSingle(),
                Ty = reader.ReadSingle(),
                Tz = reader.ReadSingle(),
                Tw = reader.ReadSingle()
            };
            transformMatrix.Tw = 1f;

            LightNode lightNode = new()
            {
                GameBoxWorldMatrix = transformMatrix,
                LightType = (GameBoxLightType)reader.ReadInt32(),
                LightColor = reader.ReadColor(),
                AmbientColor = reader.ReadColor(),
                // gbBOOL is signed int32 in both shipped engine ABIs.
                UseDiffuse = reader.ReadInt32() != 0,
                UseSpecular = reader.ReadInt32() != 0,
                NearStart = reader.ReadSingle(),
                NearEnd = reader.ReadSingle(),
                FarStart = reader.ReadSingle(),
                FarEnd = reader.ReadSingle(),
                LightDecayType = (GameBoxLightDecayType)reader.ReadInt32(),
                DecayRadius = reader.ReadSingle(),
                LightShapeType = (GameBoxLightShapeType)reader.ReadInt32(),
                Size = reader.ReadSingle(),
                Falloff = reader.ReadSingle(),
                AspectRatio = reader.ReadSingle()
            };

            return lightNode;
        }
    }
}
