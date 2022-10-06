// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
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
            var vSyncCount = Utility.IsDesktopDevice() ? 0 : 1;
            QualitySettings.vSyncCount = vSyncCount;

            var monitorRefreshRate = Screen.currentResolution.refreshRate;
            
            Application.targetFrameRate = Application.platform switch
            {
                RuntimePlatform.WindowsEditor => monitorRefreshRate,
                RuntimePlatform.WindowsPlayer => monitorRefreshRate,
                RuntimePlatform.OSXEditor => monitorRefreshRate,
                RuntimePlatform.OSXPlayer => monitorRefreshRate,
                RuntimePlatform.LinuxEditor => monitorRefreshRate,
                RuntimePlatform.LinuxPlayer => monitorRefreshRate,
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