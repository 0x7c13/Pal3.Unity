// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Par
{
    using System;
    using Primitives;

    [Serializable]
    public sealed class SparController
    {
        public int Kind;
        public float[] Parameters;
    }

    [Serializable]
    public sealed class SparKeyframe
    {
        public float Time;
        public GameBoxVector3 Position;
        public int EmissionMinimum, EmissionMaximum, Shape;
        public float[] ShapeParameters;
        public bool HasPath;
        public float PathRate;
        public GameBoxVector3[] PathPoints;
        public GameBoxVector3 VelocityMinimum, VelocityMaximum;
        public float RotationMinimum, RotationMaximum, SpinMinimum, SpinMaximum;
        public GameBoxVector3 ColorMinimum, ColorMaximum;
        public float AlphaMinimum, AlphaMaximum, LifeMinimum, LifeMaximum, SizeMinimum, SizeMaximum;
        public int FrameMinimum, FrameMaximum;
        public SparController[] Controllers;
        public float[] Reserved;
    }

    [Serializable]
    public sealed class SparGroup
    {
        public string Name, Texture;
        public int Capacity, VertexCapacity, Mode;
        public bool Loop;
        // Rebuilt by the binary reader; jagged arrays are not Unity-serializable.
        [NonSerialized]
        public float[][] TextureFrames;
        public SparKeyframe[] Keyframes;
    }

    [Serializable]
    public sealed class SparFile
    {
        public float Version;
        public SparGroup[] Groups;
        public int Shader;
        public float[] ShaderParameters;
        // Some exported files contain an editor tail. The original loader closes
        // the file after the shader parameters and never consumes this tail.
        public int UnusedByteCount;
    }
}
