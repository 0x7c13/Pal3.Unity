// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Utils
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using DataReader.Cpk;
    using UnityEngine;

    public static class Utility
    {
        public static Color32 ToColor32(byte[] rgba)
        {
            return new Color32(rgba[0], rgba[1], rgba[2], rgba[3]);
        }

        public static Color ToColor(float[] rgba)
        {
            return new Color(rgba[0], rgba[1], rgba[2], rgba[3]);
        }

        // public static unsafe void ApplyTransparencyBasedOnColorLuminance(Texture2D texture)
        // {
        //     var pixels = texture.GetPixels();
        //
        //     fixed (float* src = &pixels[0].r)
        //     {
        //         var p = src;
        //         for (var i = 0; i < texture.width * texture.height; i++, p += 4)
        //         {
        //             *(p + 3) = 0.299f * *p + 0.587f * *(p + 1) + 0.114f * *(p + 2);
        //         }
        //     }
        //
        //     texture.SetPixels(pixels);
        //     texture.Apply(updateMipmaps: false);
        // }

        public static T ReadStruct<T>(Stream stream) where T : struct
        {
            var structSize = Marshal.SizeOf(typeof(T));
            byte[] buffer = new byte[structSize];
            _ = stream.Read(buffer);
            return ReadStructInternal<T>(buffer);
        }

        public static T ReadStruct<T>(ReadOnlySpan<byte> bytes, int offset = 0) where T : struct
        {
            return ReadStructInternal<T>(bytes, offset);
        }

        private static unsafe T ReadStructInternal<T>(ReadOnlySpan<byte> bytes, int offset = 0) where T : struct
        {
            fixed (byte* ptr = &bytes[offset])
            {
                return (T) (Marshal.PtrToStructure((IntPtr) ptr, typeof(T)) ?? default(T));
            }
        }

        private static int GetPatternIndex(ReadOnlySpan<byte> src, ReadOnlySpan<byte> pattern, int startIndex = 0)
        {
            var maxFirstCharSlot = src.Length - pattern.Length + 1;
            for (var i = startIndex; i < maxFirstCharSlot; i++)
            {
                if (src[i] != pattern[0])
                    continue;

                for (var j = pattern.Length - 1; j >= 1; j--)
                {
                    if (src[i + j] != pattern[j]) break;
                    if (j == 1) return i;
                }
            }

            return -1;
        }

        public static int GetFloorIndex<T>(T[] valuesInIncreasingOrder, T lookUpValue)
        {
            /*
             * The index of the specified value in the specified array, if value is found;
             * otherwise, a negative number. If value is not found and value is less than
             * one or more elements in array, the negative number returned is the bitwise
             * complement of the index of the first element that is larger than value.
             * If value is not found and value is greater than all elements in array,
             * the negative number returned is the bitwise complement of
             * (the index of the last element plus 1).
             */
            var index = Array.BinarySearch(valuesInIncreasingOrder, lookUpValue);

            if (index < 0)
            {
                return -index - 2;
            }

            return index;
        }

        public static byte[] TrimEnd(byte[] buffer, ReadOnlySpan<byte> pattern)
        {
            var length = GetPatternIndex(buffer, pattern);
            return length == -1 ? buffer : buffer[..length];
        }

        public static void Rgb565ToRgb888(ushort rgb565Color, out byte r, out byte g, out byte b)
        {
            var temp = (rgb565Color >> 11) * 255 + 16;
            r = (byte) ((temp / 32 + temp) / 32);
            temp = ((rgb565Color & 0x07E0) >> 5) * 255 + 32;
            g = (byte) ((temp / 64 + temp) / 64);
            temp = (rgb565Color & 0x001F) * 255 + 16;
            b = (byte) ((temp / 32 + temp) / 32);
        }

        public static string GetDirectoryName(string filePath, char directorySeparatorChar)
        {
            return !filePath.Contains(directorySeparatorChar) ?
                string.Empty :
                filePath[..filePath.LastIndexOf(directorySeparatorChar)];
        }

        public static string GetFileName(string filePath, char directorySeparatorChar)
        {
            return !filePath.Contains(directorySeparatorChar) ?
                string.Empty :
                filePath[(filePath.LastIndexOf(directorySeparatorChar)+1)..];
        }

        public static bool IsHandheldDevice()
        {
            return SystemInfo.deviceType == DeviceType.Handheld;
        }

        public static bool IsDesktopDevice()
        {
            return SystemInfo.deviceType == DeviceType.Desktop;
        }

        public static bool IsPointWithinCollider(Collider collider, Vector3 point)
        {
            if (collider == null) return false;
            return collider.ClosestPoint(point) == point;
        }

        public static bool IsPointWithinCollider(Collider collider, Vector3 point, float tolerance)
        {
            if (collider == null) return false;
            return Vector3.Distance(collider.ClosestPoint(point), point) < tolerance;
        }

        public static Vector3[] CalculateNormals(Vector3[] vertices, int[] triangles)
        {
            Vector3[] normals = new Vector3[vertices.Length];

            for (var face = 0; face < triangles.Length / 3; face++)
            {
                int v1 = triangles[face * 3];
                int v2 = triangles[face * 3 + 1];
                int v3 = triangles[face * 3 + 2];

                Vector3 pt1 = vertices[v1];
                Vector3 pt2 = vertices[v2];
                Vector3 pt3 = vertices[v3];

                Vector3 d1 = pt2 - pt1;
                Vector3 d2 = pt3 - pt1;
                Vector3 normal = Vector3.Normalize(Vector3.Cross(d1, d2));

                normals[v1] += normal;
                normals[v2] += normal;
                normals[v3] += normal;
            }

            for (var i = 0; i < normals.Length; i++)
            {
                if (normals[i] == Vector3.zero)
                {
                    normals[i] = Vector3.up;
                }
                else
                {
                    normals[i] = Vector3.Normalize(normals[i]);
                }
            }

            return normals;
        }

        public static void DrawBounds(Bounds b, float duration = 1000)
        {
            var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
            var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
            var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
            var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

            Debug.DrawLine(p1, p2, Color.blue, duration);
            Debug.DrawLine(p2, p3, Color.red, duration);
            Debug.DrawLine(p3, p4, Color.yellow, duration);
            Debug.DrawLine(p4, p1, Color.magenta, duration);

            var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
            var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
            var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
            var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

            Debug.DrawLine(p5, p6, Color.blue, duration);
            Debug.DrawLine(p6, p7, Color.red, duration);
            Debug.DrawLine(p7, p8, Color.yellow, duration);
            Debug.DrawLine(p8, p5, Color.magenta, duration);

            Debug.DrawLine(p1, p5, Color.white, duration);
            Debug.DrawLine(p2, p6, Color.gray, duration);
            Debug.DrawLine(p3, p7, Color.green, duration);
            Debug.DrawLine(p4, p8, Color.cyan, duration);
        }

        public static bool IsLegacyMobileDevice()
        {
            if (IsAndroidDeviceAndSdkVersionLowerThanOrEqualTo(23)) return true;
            // TODO: Add iOS legacy mobile device check etc
            return false;
        }

        private static bool IsAndroidDeviceAndSdkVersionLowerThanOrEqualTo(int sdkVersion)
        {
            if (Application.platform != RuntimePlatform.Android) return false;

            try
            {
                int GetAndroidSdkLevel()
                {
                    IntPtr versionClass = AndroidJNI.FindClass("android.os.Build$VERSION");
                    IntPtr sdkFieldID = AndroidJNI.GetStaticFieldID(versionClass, "SDK_INT", "I");
                    var sdkLevel = AndroidJNI.GetStaticIntField(versionClass, sdkFieldID);
                    return sdkLevel;
                }

                if (GetAndroidSdkLevel() <= sdkVersion)
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return false;
        }

        public static bool IsVersionGreater(string latestVersion, string currentVersion)
        {
            string[] latestParts = latestVersion.Split('.');
            string[] currentParts = currentVersion.Split('.');

            for (var i = 0; i < Math.Min(latestParts.Length, currentParts.Length); i++)
            {
                int latest = int.Parse(latestParts[i]);
                int current = int.Parse(currentParts[i]);
                if (latest == current) continue;
                return latest > current;
            }

            return latestParts.Length > currentParts.Length;
        }
    }
}