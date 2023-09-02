// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.GameBox
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    /// <summary>
    /// Convertor to convert GameBox specific rendering related metrics to Unity standard units that fits
    /// well with the engine and editor.
    /// </summary>
    public static class GameBoxConvertor
    {
        public const float GameBoxUnitToUnityUnit = 20f;
        public const float GameBoxMv3UnitToUnityUnit = 1270f;
        public const uint GameBoxTicksPerSecond = 4800;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToUnityPosition(this Vector3 position, float scale = GameBoxUnitToUnityUnit)
        {
            return ToUnityVector3(position, scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToUnityDistance(this float gameBoxDistance)
        {
            return gameBoxDistance / GameBoxUnitToUnityUnit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToUnityXPosition(this float gameBoxXPosition)
        {
            return -gameBoxXPosition / GameBoxUnitToUnityUnit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToUnityYPosition(this float gameBoxYPosition)
        {
            return gameBoxYPosition / GameBoxUnitToUnityUnit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToUnityZPosition(this float gameBoxZPosition)
        {
            return gameBoxZPosition / GameBoxUnitToUnityUnit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToGameBoxXEulerAngle(this float xEulerAngle)
        {
            return xEulerAngle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToGameBoxYEulerAngle(this float yEulerAngle)
        {
            return -yEulerAngle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToGameBoxZEulerAngle(this float zEulerAngle)
        {
            return zEulerAngle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToUnityQuaternion(this Vector3 gameBoxEulerAngles)
        {
            return Quaternion.Euler(
                gameBoxEulerAngles.x,
                -gameBoxEulerAngles.y,
                gameBoxEulerAngles.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToGameBoxPosition(this Vector3 position, float scale = GameBoxUnitToUnityUnit)
        {
            return new Vector3(
                -position.x * scale,
                position.y * scale,
                position.z * scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 CvdPositionToUnityPosition(this Vector3 cvdPosition, float scale = GameBoxUnitToUnityUnit)
        {
            return new Vector3(
                -cvdPosition.x / scale,
                cvdPosition.z / scale,
                -cvdPosition.y / scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 CvdScaleToUnityScale(this Vector3 scale)
        {
            return new Vector3(
                scale.x,
                scale.z,
                scale.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToUnityNormal(this Vector3 normal)
        {
            return new Vector3(
                -normal.x,
                normal.y,
                normal.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion CvdQuaternionToUnityQuaternion(this GameBoxQuaternion quaternion)
        {
            return new Quaternion(-quaternion.X, quaternion.Z, -quaternion.Y, quaternion.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Mv3QuaternionToUnityQuaternion(this GameBoxQuaternion quaternion)
        {
            return new Quaternion(-quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion MovQuaternionToUnityQuaternion(this GameBoxQuaternion quaternion)
        {
            return new Quaternion(-quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion MshQuaternionToUnityQuaternion(this GameBoxQuaternion quaternion)
        {
            return new Quaternion(-quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion LgtQuaternionToUnityQuaternion(this GameBoxQuaternion quaternion)
        {
            var unityQuaternion = new Quaternion(quaternion.X, quaternion.Y, -quaternion.Z, quaternion.W);
            unityQuaternion.eulerAngles = new Vector3(unityQuaternion.eulerAngles.x + 90f,
                -unityQuaternion.eulerAngles.y,
                unityQuaternion.eulerAngles.z);
            return unityQuaternion;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToUnityQuaternion(float pitch, float yaw, float roll)
        {
            return Quaternion.Euler(pitch - 180, yaw, roll -180);
        }

        // Since GameBox engine uses different internal axis system.
        // Here we are flipping the x here to minor it back to the Unity axis.
        // Also we want to scale it to proper unity unit.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 ToUnityVector3(this Vector3 vector3, float scale)
        {
            return new Vector3(
                -vector3.x / scale,
                vector3.y / scale,
                vector3.z / scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToUnityTriangles(this List<int> triangles)
        {
            triangles.Reverse();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToUnityTriangles(this int[] triangles)
        {
            Array.Reverse(triangles);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPointInsideRect(this GameBoxRect rect, Vector2Int point)
        {
            return point.x >= rect.Left &&
                   point.x <= rect.Right &&
                   point.y >= rect.Top &&
                   point.y <= rect.Bottom;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 ToUnityMatrix4x4(this GameBoxMatrix4X4 matrix)
        {
            return new Matrix4x4(
                new Vector4(matrix.Xx, matrix.Xy, matrix.Xz, matrix.Xw),
                new Vector4(matrix.Yx, matrix.Yy, matrix.Yz, matrix.Yw),
                new Vector4(matrix.Zx, matrix.Zy, matrix.Zz, matrix.Zw),
                new Vector4(matrix.Tx, matrix.Ty, matrix.Tz, matrix.Tw));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GameBoxSecondsToTick(this float seconds)
        {
            return (uint)(seconds * GameBoxTicksPerSecond);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GameBoxTickToSeconds(this uint tick)
        {
            return (float)tick / GameBoxTicksPerSecond;
        }
    }
}