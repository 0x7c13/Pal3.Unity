// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Data
{
    //using System.Runtime.InteropServices;

    //[StructLayout(LayoutKind.Explicit)]
    public struct EffectDefinition
    {
        //[FieldOffset(0)]
        public uint ClassNameCrc;

        //[FieldOffset(4)]
        //[MarshalAs (UnmanagedType.ByValArray, SizeConst = 30)]
        public string TypeName;

        //[FieldOffset(36)]
        public uint TypeNameCrc;

        //[FieldOffset(40)]
        //[MarshalAs (UnmanagedType.ByValArray, SizeConst = 128)]
        // Union
        public string TextureName;
        public string ParticleFileName;
        public string SfxFileNamePrefix;
        public string VideoFileNamePrefix;

        //[FieldOffset(168)]
        public int FramesPerRow;

        //[FieldOffset(172)]
        public int FramesPerColumn;

        //[FieldOffset(176)]
        public int FrameCount;

        //[FieldOffset(180)]
        public float FramePerSecond;

        //[FieldOffset(184)]
        public float InitXPosition;

        //[FieldOffset(188)]
        public float InitYPosition;

        //[FieldOffset(192)]
        public float InitZPosition;

        //[FieldOffset(196)]
        // Union
        public float InitYRotation;
        public float InitAngle;

        //[FieldOffset(200)]
        public int InitSize;

        //[FieldOffset(204)]
        public uint InitColor;

        //[FieldOffset(208)]
        public int MaxQuadCount;

        //[FieldOffset(212)]
        public bool IsValid;

        //[FieldOffset(213)]
        public bool IsAdditiveBlend;

        //[FieldOffset(214)]
        public bool IsBillboard;

        //[FieldOffset(215)]
        public bool IsZTestIgnored;

        //[FieldOffset(216)]
        public float InitXRotation;

        //[FieldOffset(220)]
        public float InitZRotation;

        //[FieldOffset(224)]
        public float RotationAngle;

        //[FieldOffset(228)]
        public float Width;

        //[FieldOffset(232)]
        public float DivisionCount;

        //[FieldOffset(236)]
        public float TopShrink;

        //[FieldOffset(240)]
        public float BottomShrink;

        //[FieldOffset(244)]
        public float TopRadius;

        //[FieldOffset(248)]
        public float BottomRadius;

        //[FieldOffset(252)]
        public float TopRadiusIncreaseSpeed;

        //[FieldOffset(256)]
        public float BottomRadiusIncreaseSpeed;

        //[FieldOffset(260)]
        public float TopRiseSpeed;

        //[FieldOffset(264)]
        public float BottomRiseSpeed;

        //[FieldOffset(268)]
        public float TextureRepeat;

        //[FieldOffset(272)]
        //[MarshalAs (UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] Param;
    }

    public sealed class EffectDefinitionFile
    {
        public EffectDefinitionFile(EffectDefinition[] effectDefinitions)
        {
            EffectDefinitions = effectDefinitions;
        }

        public EffectDefinition[] EffectDefinitions { get; }
    }
}