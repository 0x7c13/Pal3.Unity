// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.GameBox
{
    using UnityEngine;

    [System.Serializable]
    public struct GameBoxMaterial
    {
        public Color Diffuse;
        public Color Ambient;
        public Color Specular;
        public Color Emissive;
        public float SpecularPower;
        public string[] TextureFileNames;
    }

    // To meet opengl, use this order instead:
    // float xx , yx , zx , tx;
    // float xy , yy , zy , ty;
    // float xz , yz , zz , tz;
    // float xw , yw , zw , tw;
    public struct GameBoxMatrix4X4
    {
        public float Xx, Xy, Xz, Xw;
        public float Yx, Yy, Yz, Yw;
        public float Zx, Zy, Zz, Zw;
        public float Tx, Ty, Tz, Tw;
    }

    [System.Serializable]
    public struct GameBoxQuaternion
    {
        public float X;
        public float Y;
        public float Z;
        public float W;
    }

    [System.Serializable]
    public struct GameBoxRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public bool IsEmpty => Left == 0 &&
                               Top == 0 &&
                               Right == 0 &&
                               Bottom == 0;
    }

    public static class GameBoxVertexType
    {
        public const int Null        = 0;
        public const int MaxUvSet    = 4;
        public const int XYZ         = (1 << 0);
        public const int Normal      = (1 << 1);
        public const int Diffuse     = (1 << 2);
        public const int Specular    = (1 << 3);
        public const int UV0         = (1 << 4);
        public const int UV1         = (1 << 5);
        public const int UV2         = (1 << 6);
        public const int UV3         = (1 << 7);
        public const int XYZRHW      = (1 << 8);
        public const int FlagMask    = (1 << 31);
    }

    public enum GameBoxBlendFlag
    {
        Opaque,             // opaque
        AlphaBlend,         // src.alpha, 1-src.alpha, +
        InvertColorBlend,   // srcAlpha, One , +
        AdditiveBlend,      //
    }

    public enum GameBoxLightType
    {
        Omni,           // point light
        Spot,           // spot light
        Directional,    // directional light
    }

    public enum GameBoxLightDecayType
    {
        None,
        Distance,
        Square,
    }

    public enum GameBoxLightShapeType
    {
        Cone,
        Square,
    }

    public static class GameBoxVertex
    {
        public static int GetSize(uint vertexType)
        {
            var size = 0;

            if ((vertexType & GameBoxVertexType.FlagMask) != 0)  return (int) (vertexType&~GameBoxVertexType.FlagMask);
            if ((vertexType & GameBoxVertexType.XYZ) != 0)       size += 3 * sizeof(float);
            if ((vertexType & GameBoxVertexType.XYZRHW) != 0)    size += 4 * sizeof(float);
            if ((vertexType & GameBoxVertexType.Normal) != 0)    size += 3 * sizeof(float);
            if ((vertexType & GameBoxVertexType.Diffuse) != 0)   size += 4;
            if ((vertexType & GameBoxVertexType.Specular) != 0)  size += 4;
            if ((vertexType & GameBoxVertexType.UV0) != 0)       size += 2 * sizeof(float);
            if ((vertexType & GameBoxVertexType.UV1) != 0)       size += 2 * sizeof(float);
            if ((vertexType & GameBoxVertexType.UV2) != 0)       size += 2 * sizeof(float);
            if ((vertexType & GameBoxVertexType.UV3) != 0)       size += 2 * sizeof(float);

            return size;
        }
    }
}