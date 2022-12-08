// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.GameBox
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Interpreter/convertor to convert GameBox specific rendering related metrics to Unity standard units that fits
    /// well with the engine and editor.
    /// </summary>
    public static class GameBoxInterpreter
    {
        public const float GameBoxUnitToUnityUnit = 20f;
        public const float GameBoxMv3UnitToUnityUnit = 1270f;

        public static Vector3 ToUnityVertex(Vector3 vertex, float scale)
        {
            return ToUnityVector3(vertex, scale);
        }

        public static Vector3 ToUnityPosition(Vector3 position, float scale = GameBoxUnitToUnityUnit)
        {
            return ToUnityVector3(position, scale);
        }

        public static float ToUnityDistance(float gameBoxDistance)
        {
            return gameBoxDistance / GameBoxUnitToUnityUnit;
        }

        public static float ToUnityXPosition(float gameBoxXPosition)
        {
            return -gameBoxXPosition / GameBoxUnitToUnityUnit;
        }

        public static float ToUnityYPosition(float gameBoxYPosition)
        {
            return gameBoxYPosition / GameBoxUnitToUnityUnit;
        }

        public static float ToUnityZPosition(float gameBoxZPosition)
        {
            return gameBoxZPosition / GameBoxUnitToUnityUnit;
        }

        public static Vector3 ToGameBoxPosition(Vector3 position, float scale = GameBoxUnitToUnityUnit)
        {
            return new Vector3(
                -position.x * scale,
                position.y * scale,
                position.z * scale);
        }

        public static Vector3 CvdPositionToUnityPosition(Vector3 cvdPosition, float scale = GameBoxUnitToUnityUnit)
        {
            return new Vector3(
                -cvdPosition.x / scale,
                cvdPosition.z / scale,
                -cvdPosition.y / scale);
        }

        public static Vector3 CvdScaleToUnityScale(Vector3 scale)
        {
            return new Vector3(
                scale.x,
                scale.z,
                scale.y);
        }

        public static Vector3 ToUnityNormal(Vector3 normal)
        {
            return new Vector3(
                -normal.x,
                normal.y,
                normal.z);
        }

        public static Quaternion CvdQuaternionToUnityQuaternion(GameBoxQuaternion quaternion)
        {
            return new Quaternion(-quaternion.X, quaternion.Z, -quaternion.Y, quaternion.W);
        }

        public static Quaternion Mv3QuaternionToUnityQuaternion(GameBoxQuaternion quaternion)
        {
            return new Quaternion(-quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        public static Quaternion LgtQuaternionToUnityQuaternion(GameBoxQuaternion quaternion)
        {
            var rotation = new Quaternion(quaternion.X, quaternion.Y, -quaternion.Z, quaternion.W);
            rotation.eulerAngles = new Vector3(rotation.eulerAngles.x + 90f,
                -rotation.eulerAngles.y,
                rotation.eulerAngles.z);
            return rotation;
        }

        public static Quaternion ToUnityRotation(float pitch, float yaw, float roll)
        {
            return Quaternion.Euler(pitch - 180, yaw, roll -180);
        }

        // Since GameBox engine uses different internal axis system.
        // Here we are flipping the x here to minor it back to the Unity axis.
        // Also we want to scale it to proper unity unit.
        private static Vector3 ToUnityVector3(Vector3 vector3, float scale)
        {
            return new Vector3(
                -vector3.x / scale,
                vector3.y / scale,
                vector3.z / scale);
        }

        public static void ToUnityTriangles(List<int> triangles)
        {
            triangles.Reverse();
        }

        public static void ToUnityTriangles(int[] triangles)
        {
            Array.Reverse(triangles);
        }

        public static bool IsPointInsideRect(GameBoxRect rect, Vector2Int point)
        {
            return point.x >= rect.Left &&
                   point.x <= rect.Right &&
                   point.y >= rect.Top &&
                   point.y <= rect.Bottom;
        }

        public static Matrix4x4 ToUnityMatrix4x4(GameBoxMatrix4X4 matrix)
        {
            return new Matrix4x4(
                new Vector4(matrix.Xx, matrix.Xy, matrix.Xz, matrix.Xw),
                new Vector4(matrix.Yx, matrix.Yy, matrix.Yz, matrix.Yw),
                new Vector4(matrix.Zx, matrix.Zy, matrix.Zz, matrix.Zw),
                new Vector4(matrix.Tx, matrix.Ty, matrix.Tz, matrix.Tw));
        }
    }
}