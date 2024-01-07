// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Lgt
{
    using Primitives;

    [System.Serializable]
    public struct LightNode
    {
        public GameBoxMatrix4x4 GameBoxWorldMatrix;

        public GameBoxLightType LightType;
        public Color LightColor;
        public Color AmbientColor;
        public bool UseDiffuse;
        public bool UseSpecular;
        public float NearStart;
        public float NearEnd;
        public float FarStart;
        public float FarEnd;

        public GameBoxLightDecayType LightDecayType;
        public float DecayRadius;

        public GameBoxLightShapeType LightShapeType;
        public float Size;
        public float Falloff;
        public float AspectRatio;
    }

    public sealed class LgtFile
    {
        public LightNode[] LightNodes { get; }

        public LgtFile(LightNode[] lightNodes)
        {
            LightNodes = lightNodes;
        }
    }
}