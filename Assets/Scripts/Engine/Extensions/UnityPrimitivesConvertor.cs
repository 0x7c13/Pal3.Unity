// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Extensions
{
    using System;
    using System.Runtime.CompilerServices;
    using Pal3.Core.Primitives;

    /// <summary>
    /// Convertor to convert GameBox specific rendering related metrics to Unity standard units that fits
    /// well with the engine and editor.
    /// </summary>
    public static class UnityPrimitivesConvertor
    {
        public const float GameBoxUnitToUnityUnit = 20f;
        public const float GameBoxMv3UnitToUnityUnit = 1270f;
        public const uint GameBoxTicksPerSecond = 4800;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Vector3 ToUnityPosition(this GameBoxVector3 gameBoxPosition,
            float scale = GameBoxUnitToUnityUnit)
        {
            return ToUnityVector3(gameBoxPosition, scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Vector3[] ToUnityPositions(this GameBoxVector3[] gameBoxPositions,
            float scale = GameBoxUnitToUnityUnit)
        {
            if (gameBoxPositions == null) return null;
            UnityEngine.Vector3[] unityPositions = new UnityEngine.Vector3[gameBoxPositions.Length];
            for (int i = 0; i < gameBoxPositions.Length; i++)
            {
                unityPositions[i] = ToUnityVector3(gameBoxPositions[i], scale);
            }

            return unityPositions;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToUnityPositions(this GameBoxVector3[] gameBoxPositions,
            UnityEngine.Vector3[] unityPositionsBuffer, float scale = GameBoxUnitToUnityUnit)
        {
            if (gameBoxPositions == null || unityPositionsBuffer == null || unityPositionsBuffer.Length != gameBoxPositions.Length)
            {
                throw new ArgumentException("gameBoxPositions and unityPositionsBuffer must be non-null and have the same length");
            }

            for (int i = 0; i < gameBoxPositions.Length; i++)
            {
                unityPositionsBuffer[i] = ToUnityVector3(gameBoxPositions[i], scale);
            }
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
        public static UnityEngine.Quaternion ToUnityQuaternion(this GameBoxVector3 gameBoxEulerAngles)
        {
            return UnityEngine.Quaternion.Euler(
                gameBoxEulerAngles.X,
                -gameBoxEulerAngles.Y,
                gameBoxEulerAngles.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameBoxVector3 ToGameBoxPosition(this UnityEngine.Vector3 position,
            float scale = GameBoxUnitToUnityUnit)
        {
            return new GameBoxVector3(
                -position.x * scale,
                position.y * scale,
                position.z * scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Vector3 CvdPositionToUnityPosition(this GameBoxVector3 cvdGameBoxPosition,
            float scale = GameBoxUnitToUnityUnit)
        {
            return new UnityEngine.Vector3(
                -cvdGameBoxPosition.X / scale,
                cvdGameBoxPosition.Z / scale,
                -cvdGameBoxPosition.Y / scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Vector3 CvdScaleToUnityScale(this GameBoxVector3 cvdGameBoxScale)
        {
            return new UnityEngine.Vector3(
                cvdGameBoxScale.X,
                cvdGameBoxScale.Z,
                cvdGameBoxScale.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Vector3 ToUnityNormal(this GameBoxVector3 gameBoxNormal)
        {
            return new UnityEngine.Vector3(
                -gameBoxNormal.X,
                gameBoxNormal.Y,
                gameBoxNormal.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Vector3[] ToUnityNormals(this GameBoxVector3[] gameBoxNormals)
        {
            if (gameBoxNormals == null) return null;
            UnityEngine.Vector3[] unityNormals = new UnityEngine.Vector3[gameBoxNormals.Length];
            for (int i = 0; i < gameBoxNormals.Length; i++)
            {
                unityNormals[i] = ToUnityNormal(gameBoxNormals[i]);
            }

            return unityNormals;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToUnityNormals(this GameBoxVector3[] gameBoxNormals,
            UnityEngine.Vector3[] unityNormalsBuffer)
        {
            if (gameBoxNormals == null || unityNormalsBuffer == null || gameBoxNormals.Length != unityNormalsBuffer.Length)
            {
                throw new ArgumentException("gameBoxNormals and unityNormalsBuffer must be non-null and have the same length");
            }

            for (int i = 0; i < gameBoxNormals.Length; i++)
            {
                unityNormalsBuffer[i] = ToUnityNormal(gameBoxNormals[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Quaternion CvdQuaternionToUnityQuaternion(this GameBoxQuaternion cvdGameBoxQuaternion)
        {
            return new UnityEngine.Quaternion(
                -cvdGameBoxQuaternion.X,
                cvdGameBoxQuaternion.Z,
                -cvdGameBoxQuaternion.Y,
                cvdGameBoxQuaternion.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Quaternion Mv3QuaternionToUnityQuaternion(this GameBoxQuaternion mv3GameBoxQuaternion)
        {
            return new UnityEngine.Quaternion(
                -mv3GameBoxQuaternion.X,
                mv3GameBoxQuaternion.Y,
                mv3GameBoxQuaternion.Z,
                mv3GameBoxQuaternion.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Quaternion MovQuaternionToUnityQuaternion(this GameBoxQuaternion movGameBoxQuaternion)
        {
            return new UnityEngine.Quaternion(
                -movGameBoxQuaternion.X,
                movGameBoxQuaternion.Y,
                movGameBoxQuaternion.Z,
                movGameBoxQuaternion.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Quaternion MshQuaternionToUnityQuaternion(this GameBoxQuaternion mshGameBoxQuaternion)
        {
            return new UnityEngine.Quaternion(
                -mshGameBoxQuaternion.X,
                mshGameBoxQuaternion.Y,
                mshGameBoxQuaternion.Z,
                mshGameBoxQuaternion.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Quaternion LgtQuaternionToUnityQuaternion(this GameBoxQuaternion lgtGameBoxQuaternion)
        {
            var unityQuaternion = new UnityEngine.Quaternion(
                lgtGameBoxQuaternion.X,
                lgtGameBoxQuaternion.Y,
                -lgtGameBoxQuaternion.Z,
                lgtGameBoxQuaternion.W);
            unityQuaternion.eulerAngles = new UnityEngine.Vector3(unityQuaternion.eulerAngles.x + 90f,
                -unityQuaternion.eulerAngles.y,
                unityQuaternion.eulerAngles.z);
            return unityQuaternion;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Quaternion ToUnityQuaternion(float gameBoxPitch, float gameBoxYaw, float gameBoxRoll)
        {
            return UnityEngine.Quaternion.Euler(gameBoxPitch - 180, gameBoxYaw, gameBoxRoll -180);
        }

        // Since GameBox engine uses different internal axis system.
        // Here we are flipping the x here to minor it back to the Unity axis.
        // Also we want to scale it to proper unity unit.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UnityEngine.Vector3 ToUnityVector3(this GameBoxVector3 gameBoxVector3, float scale)
        {
            return new UnityEngine.Vector3(
                -gameBoxVector3.X / scale,
                gameBoxVector3.Y / scale,
                gameBoxVector3.Z / scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] ToUnityTriangles(this int[] gameBoxTriangles)
        {
            if (gameBoxTriangles == null) return null;
            int[] unityTriangles = new int[gameBoxTriangles.Length];
            for (int i = 0; i < gameBoxTriangles.Length; i++)
            {
                unityTriangles[i] = gameBoxTriangles[gameBoxTriangles.Length - 1 - i];
            }
            return unityTriangles;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToUnityTrianglesInPlace(this int[] gameBoxTriangles)
        {
            if (gameBoxTriangles == null) return;
            Array.Reverse(gameBoxTriangles);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToUnityTriangles(this int[] gameBoxTriangles, int[] unityTrianglesBuffer)
        {
            if (gameBoxTriangles == null || unityTrianglesBuffer == null || gameBoxTriangles.Length != unityTrianglesBuffer.Length)
            {
                throw new ArgumentException("gameBoxTriangles and unityTrianglesBuffer must be non-null and have the same length");
            }

            for (int i = 0; i < gameBoxTriangles.Length; i++)
            {
                unityTrianglesBuffer[i] = gameBoxTriangles[gameBoxTriangles.Length - 1 - i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Matrix4x4 ToUnityMatrix4x4(this GameBoxMatrix4x4 gameBoxMatrix)
        {
            return new UnityEngine.Matrix4x4(
                new UnityEngine.Vector4(gameBoxMatrix.Xx, gameBoxMatrix.Xy, gameBoxMatrix.Xz, gameBoxMatrix.Xw),
                new UnityEngine.Vector4(gameBoxMatrix.Yx, gameBoxMatrix.Yy, gameBoxMatrix.Yz, gameBoxMatrix.Yw),
                new UnityEngine.Vector4(gameBoxMatrix.Zx, gameBoxMatrix.Zy, gameBoxMatrix.Zz, gameBoxMatrix.Zw),
                new UnityEngine.Vector4(gameBoxMatrix.Tx, gameBoxMatrix.Ty, gameBoxMatrix.Tz, gameBoxMatrix.Tw));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Vector2 ToUnityVector2(this GameBoxVector2 gameBoxVector2)
        {
            return new UnityEngine.Vector2(gameBoxVector2.X, gameBoxVector2.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Vector2Int ToUnityVector2Int(this GameBoxVector2Int gameBoxVector2Int)
        {
            return new UnityEngine.Vector2Int(gameBoxVector2Int.X, gameBoxVector2Int.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameBoxVector2Int ToGameBoxVector2Int(this UnityEngine.Vector2Int vector2Int)
        {
            return new GameBoxVector2Int(vector2Int.x, vector2Int.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Vector2[] ToUnityVector2s(this GameBoxVector2[] gameBoxVector2s)
        {
            if (gameBoxVector2s == null) return null;
            UnityEngine.Vector2[] unityVector2s = new UnityEngine.Vector2[gameBoxVector2s.Length];
            for (int i = 0; i < gameBoxVector2s.Length; i++)
            {
                unityVector2s[i] = gameBoxVector2s[i].ToUnityVector2();
            }

            return unityVector2s;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToUnityVector2s(this GameBoxVector2[] gameBoxVector2s, UnityEngine.Vector2[] unityVector2sBuffer)
        {
            if (gameBoxVector2s == null || unityVector2sBuffer == null || gameBoxVector2s.Length != unityVector2sBuffer.Length)
            {
                throw new ArgumentException("gameBoxVector2s and unityVector2sBuffer must be non-null and have the same length");
            }

            for (int i = 0; i < gameBoxVector2s.Length; i++)
            {
                unityVector2sBuffer[i] = gameBoxVector2s[i].ToUnityVector2();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SecondsToGameBoxTick(this float seconds)
        {
            return (uint)(seconds * GameBoxTicksPerSecond);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GameBoxTickToSeconds(this uint tick)
        {
            return (float)tick / GameBoxTicksPerSecond;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Color32 ToUnityColor32(this Color32 gameBoxColor32)
        {
            return new UnityEngine.Color32(
                gameBoxColor32.R,
                gameBoxColor32.G,
                gameBoxColor32.B,
                gameBoxColor32.A);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Color32[] ToUnityColor32s(this Color32[] gameBoxColor32s)
        {
            if (gameBoxColor32s == null) return null;
            UnityEngine.Color32[] unityColor32s = new UnityEngine.Color32[gameBoxColor32s.Length];
            for (int i = 0; i < gameBoxColor32s.Length; i++)
            {
                unityColor32s[i] = gameBoxColor32s[i].ToUnityColor32();
            }

            return unityColor32s;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnityEngine.Color ToUnityColor(this Color gameBoxColor)
        {
            return new UnityEngine.Color(
                gameBoxColor.R,
                gameBoxColor.G,
                gameBoxColor.B,
                gameBoxColor.A);
        }
    }
}