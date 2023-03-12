// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Settings
{
    using System;
    using Core.Utils;
    using UnityEngine;

    public sealed class SettingsManager
    {
        public void ApplyPlatformSpecificSettings()
        {
            if (Utility.IsHandheldDevice())
            {
                TouchScreenKeyboard.hideInput = true;
            }
        }

        public void ApplyDefaultRenderingSettings()
        {
            //QualitySettings.vSyncCount = 1;

            #if UNITY_2022_1_OR_NEWER
            var targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
            #else
            var targetFrameRate = Screen.currentResolution.refreshRate;
            #endif

            targetFrameRate = Mathf.Max(targetFrameRate, 60); // 60Hz is the minimum

            Application.targetFrameRate = Application.platform switch
            {
                RuntimePlatform.WindowsEditor => targetFrameRate,
                RuntimePlatform.WindowsPlayer => targetFrameRate,
                RuntimePlatform.OSXEditor => targetFrameRate,
                RuntimePlatform.OSXPlayer => targetFrameRate,
                RuntimePlatform.LinuxEditor => targetFrameRate,
                RuntimePlatform.LinuxPlayer => targetFrameRate,
                RuntimePlatform.IPhonePlayer => 120,
                RuntimePlatform.Android => 120,
                RuntimePlatform.Switch => 60,
                _ => -1,
            };

            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            QualitySettings.antiAliasing = 2; // 2xMSAA

            // Downscaling resolution for old Android devices
            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    // Android 6 Marshmallow <=> API Version 23
                    if (GetAndroidSdkLevel() <= 23)
                    {
                        QualitySettings.antiAliasing = 0; // No AA
                        Screen.SetResolution((int) (Screen.width * 0.75f), (int) (Screen.height * 0.75f), true);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private int GetAndroidSdkLevel()
        {
            IntPtr versionClass = AndroidJNI.FindClass("android.os.Build$VERSION");
            IntPtr sdkFieldID = AndroidJNI.GetStaticFieldID(versionClass, "SDK_INT", "I");
            var sdkLevel = AndroidJNI.GetStaticIntField(versionClass, sdkFieldID);
            return sdkLevel;
        }
    }
}