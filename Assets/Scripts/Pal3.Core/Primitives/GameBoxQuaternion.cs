// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Primitives
{
    using System;
    using System.Runtime.CompilerServices;

    [System.Serializable]
    public struct GameBoxQuaternion
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoxQuaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameBoxQuaternion AngleAxis(float angle, GameBoxVector3 axis)
        {
            if (axis.SqrMagnitude() == 0.0f)
            {
                return new GameBoxQuaternion(0, 0, 0, 1);
            }

            // Convert the angle to radians
            float radian = angle * MathF.PI / 180.0f;
            radian *= 0.5f;

            axis.Normalize();
            float sinAngle = MathF.Sin(radian);
            float cosAngle = MathF.Cos(radian);

            return new GameBoxQuaternion(
                axis.X * sinAngle,
                axis.Y * sinAngle,
                axis.Z * sinAngle,
                cosAngle
            );
        }
    }
}