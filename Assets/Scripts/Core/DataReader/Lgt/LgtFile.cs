// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Lgt
{
    using GameBox;
    using UnityEngine;

    [System.Serializable]
    public struct LightNode
    {
        public Matrix4x4 WorldMatrix;

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

    public class LgtFile
    {
        public LightNode[] LightNodes { get; }

        public LgtFile(LightNode[] lightNodes)
        {
            LightNodes = lightNodes;
        }
    }
}