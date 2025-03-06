// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Utilities
{
    using System;
    using UnityEngine;

    public static class UnityEngineUtility
    {
        public static bool IsHandheldDevice()
        {
            return SystemInfo.deviceType == DeviceType.Handheld;
        }

        public static bool IsDesktopDevice()
        {
            return SystemInfo.deviceType == DeviceType.Desktop;
        }

        public static bool IsPointInsideCollider(Collider collider, Vector3 point)
        {
            if (collider == null) return false;
            return collider.ClosestPoint(point) == point;
        }

        public static bool IsPointInsideCollider(Collider collider, Vector3 point, float tolerance)
        {
            if (collider == null) return false;
            return Vector3.Distance(collider.ClosestPoint(point), point) < tolerance;
        }

        public static void DrawBounds(Bounds b, float duration = 1000)
        {
            Vector3 p1 = new(b.min.x, b.min.y, b.min.z);
            Vector3 p2 = new(b.max.x, b.min.y, b.min.z);
            Vector3 p3 = new(b.max.x, b.min.y, b.max.z);
            Vector3 p4 = new (b.min.x, b.min.y, b.max.z);

            Debug.DrawLine(p1, p2, Color.blue, duration);
            Debug.DrawLine(p2, p3, Color.red, duration);
            Debug.DrawLine(p3, p4, Color.yellow, duration);
            Debug.DrawLine(p4, p1, Color.magenta, duration);

            Vector3 p5 = new(b.min.x, b.max.y, b.min.z);
            Vector3 p6 = new(b.max.x, b.max.y, b.min.z);
            Vector3 p7 = new(b.max.x, b.max.y, b.max.z);
            Vector3 p8 = new(b.min.x, b.max.y, b.max.z);

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
                    int sdkLevel = AndroidJNI.GetStaticIntField(versionClass, sdkFieldID);
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
    }
}