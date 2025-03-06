// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Primitives
{
    using System;
    using System.Runtime.CompilerServices;

    [System.Serializable]
    public struct GameBoxVector3 : IEquatable<GameBoxVector3>
    {
        public float X;
        public float Y;
        public float Z;

        public static GameBoxVector3 Zero
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new ();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoxVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Magnitude(GameBoxVector3 vector) => (float) Math.Sqrt(
            (double) vector.X * (double) vector.X +
            (double) vector.Y * (double) vector.Y +
            (double) vector.Z * (double) vector.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SqrtMagnitude(GameBoxVector3 vector) => (float) (
            (double) vector.X * (double) vector.X +
            (double) vector.Y * (double) vector.Y +
            (double) vector.Z * (double) vector.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameBoxVector3 Cross(GameBoxVector3 a, GameBoxVector3 b) => new (
            (float) ((double) a.Y * (double) b.Z - (double) a.X * (double) b.Y),
            (float) ((double) a.Z * (double) b.X - (double) a.X * (double) b.Z),
            (float) ((double) a.X * (double) b.Y - (double) a.Y * (double) b.X));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(GameBoxVector3 a, GameBoxVector3 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return (float) Math.Sqrt((double) dx * (double) dx +
                                     (double) dy * (double) dy +
                                     (double) dz * (double) dz);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameBoxVector3 Normalize(GameBoxVector3 value)
        {
            float num = Magnitude(value);
            return (double) num > 9.999999747378752E-06 ? value / num : Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            float num = Magnitude(this);
            this = (double) num > 9.999999747378752E-06 ? this / num : Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float SqrMagnitude()
        {
            return X * X + Y * Y + Z * Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameBoxVector3 operator +(GameBoxVector3 a, GameBoxVector3 b) =>
            new (a.X+ b.X, a.Y + b.Y, a.Z + b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameBoxVector3 operator -(GameBoxVector3 a, GameBoxVector3 b) =>
            new (a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameBoxVector3 operator -(GameBoxVector3 a) =>
            new (-a.X, -a.Y, -a.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameBoxVector3 operator *(GameBoxVector3 a, float d) =>
            new (a.X * d, a.Y * d, a.Z * d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameBoxVector3 operator *(float d, GameBoxVector3 a) =>
            new (a.X * d, a.Y * d, a.Z * d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameBoxVector3 operator /(GameBoxVector3 a, float d) =>
            new GameBoxVector3(a.X / d, a.Y / d, a.Z / d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(GameBoxVector3 a, GameBoxVector3 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.X;

            return
                (double) dx * (double) dx +
                (double) dy * (double) dy +
                (double) dz * (double) dz < 9.999999439624929E-11;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(GameBoxVector3 a, GameBoxVector3 b) => !(a == b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is GameBoxVector3 gbVector3 && Equals(gbVector3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(GameBoxVector3 other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            float dz = Z - other.X;

            return
                (double) dx * (double) dx +
                (double) dy * (double) dy +
                (double) dz * (double) dz < 9.999999439624929E-11;
        }

        public override int GetHashCode()
        {
            int hash = this.X.GetHashCode();
            hash = HashCode.Combine(hash, this.Y.GetHashCode());
            hash = HashCode.Combine(hash, this.Z.GetHashCode());
            return hash;
        }
    }
}