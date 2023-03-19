// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Settings
{
    using System;
    using Core.Utils;
    using IngameDebugConsole;
    using UnityEngine;

    public sealed class SettingsManager
    {
        private readonly ITransactionalKeyValueStore _settingsStore;
        private bool _settingsChanged = false;

        public SettingsManager(ITransactionalKeyValueStore keyValueStore)
        {
            _settingsStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));

            InitDefaultValuesForSettings();

            DebugLogConsole.AddCommand("Settings.Reset", "重置所有设置", ResetSettings);
            DebugLogConsole.AddCommand<int>("Settings.VSyncCount", "设置垂直同步设定（0：关闭，1：开启，2: 开启（每2帧刷新）)", SetVSyncCount);
            DebugLogConsole.AddCommand<int>("Settings.AntiAliasing", "设置抗锯齿设定（0：关闭，2：2倍抗锯齿，4：4倍抗锯齿，8：8倍抗锯齿, 16: 16倍抗锯齿)", SetAntiAliasing);
            DebugLogConsole.AddCommand<int>("Settings.TargetFrameRate", "设置目标帧率设定（-1：不限制，30：30帧，60：60帧）", SetTargetFrameRate);
            DebugLogConsole.AddCommand<float>("Settings.ResolutionScale", "设置分辨率缩放设定（0.1：10%分辨率，0.5：50%分辨率，1.0：100%分辨率）", SetResolutionScale);
            DebugLogConsole.AddCommand<int>("Settings.FullScreenMode", "设置全屏模式设定（0：Windows独占全屏，1：全屏，2：MacOS全屏，3：窗口", SetFullScreenMode);
            DebugLogConsole.AddCommand<bool>("Settings.IsRealtimeLightingAndShadowsEnabled", "设置实时光照和阴影设定（true：开启，false：关闭）", SetIsRealtimeLightingAndShadowsEnabled);
            DebugLogConsole.AddCommand<bool>("Settings.IsAmbientOcclusionEnabled", "设置环境光遮蔽设定（true：开启，false：关闭）", SetIsAmbientOcclusionEnabled);
            DebugLogConsole.AddCommand<bool>("Settings.IsVoiceOverEnabled", "设置角色配音设定（true：开启，false：关闭）", SetIsVoiceOverEnabled);
        }

        private int _vSyncCount = 0; // Default to 0 for no v-sync
        public int VSyncCount
        {
            get => _vSyncCount;
            set
            {
                if (_vSyncCount != value &&
                    _vSyncCount >= 0)
                {
                    _vSyncCount = value;
                    _settingsChanged = true;
                    _settingsStore.Set(nameof(VSyncCount), value);
                }
            }
        }

        private void SetVSyncCount(int vSyncCount)
        {
            VSyncCount = vSyncCount;
            if (_settingsChanged)
            {
                ApplySettings();
            }
        }

        private int _antiAliasing = 0; // Default to 0 for no anti-aliasing
        public int AntiAliasing
        {
            get => _antiAliasing;
            set
            {
                if (_antiAliasing != value &&
                    _antiAliasing >= 0)
                {
                    _antiAliasing = value;
                    _settingsChanged = true;
                    _settingsStore.Set(nameof(AntiAliasing), value);
                }
            }
        }

        private void SetAntiAliasing(int antiAliasing)
        {
            AntiAliasing = antiAliasing;
            if (_settingsChanged)
            {
                ApplySettings();
            }
        }

        private int _targetFrameRate = -1; // Default to -1 for unlimited frame rate
        public int TargetFrameRate
        {
            get => _targetFrameRate;
            set
            {
                if (_targetFrameRate != value &&
                    _targetFrameRate > 0 || _targetFrameRate == -1)
                {
                    _targetFrameRate = value;
                    _settingsChanged = true;
                    _settingsStore.Set(nameof(TargetFrameRate), value);
                }
            }
        }

        private void SetTargetFrameRate(int targetFrameRate)
        {
            TargetFrameRate = targetFrameRate;
            if (_settingsChanged)
            {
                ApplySettings();
            }
        }

        private float _resolutionScale = 1.0f; // Default to 1.0f for full resolution
        public float ResolutionScale
        {
            get => _resolutionScale;
            set
            {
                if (Math.Abs(_resolutionScale - value) > float.Epsilon &&
                    _resolutionScale is > 0 and <= 1.0f)
                {
                    _resolutionScale = value;
                    _settingsChanged = true;
                    _settingsStore.Set(nameof(ResolutionScale), value);
                }
            }
        }

        private void SetResolutionScale(float resolutionScale)
        {
            ResolutionScale = resolutionScale;
            if (_settingsChanged)
            {
                ApplySettings();
            }
        }

        private FullScreenMode _fullScreenMode = FullScreenMode.FullScreenWindow; // Default to full screen window
        public FullScreenMode FullScreenMode
        {
            get => _fullScreenMode;
            set
            {
                if (_fullScreenMode != value)
                {
                    _fullScreenMode = value;
                    _settingsChanged = true;
                    _settingsStore.Set(nameof(FullScreenMode), value);
                }
            }
        }

        private void SetFullScreenMode(int fullScreenMode)
        {
            FullScreenMode = (FullScreenMode)fullScreenMode;
            if (_settingsChanged)
            {
                ApplySettings();
            }
        }

        private bool _isRealtimeLightingAndShadowsEnabled = false; // Default to false for realtime lighting and shadows
        public bool IsRealtimeLightingAndShadowsEnabled
        {
            get => _isRealtimeLightingAndShadowsEnabled;
            set
            {
                if (_isRealtimeLightingAndShadowsEnabled != value)
                {
                    _isRealtimeLightingAndShadowsEnabled = value;
                    _settingsChanged = true;
                    _settingsStore.Set(nameof(IsRealtimeLightingAndShadowsEnabled), value);
                }
            }
        }

        private void SetIsRealtimeLightingAndShadowsEnabled(bool isRealtimeLightingAndShadowsEnabled)
        {
            IsRealtimeLightingAndShadowsEnabled = isRealtimeLightingAndShadowsEnabled;
            if (_settingsChanged)
            {
                ApplySettings();
            }
        }

        private bool _isAmbientOcclusionEnabled = false; // Default to false for ambient occlusion
        public bool IsAmbientOcclusionEnabled
        {
            get => _isAmbientOcclusionEnabled;
            set
            {
                if (_isAmbientOcclusionEnabled != value)
                {
                    _isAmbientOcclusionEnabled = value;
                    _settingsChanged = true;
                    _settingsStore.Set(nameof(IsAmbientOcclusionEnabled), value);
                }
            }
        }

        private void SetIsAmbientOcclusionEnabled(bool isAmbientOcclusionEnabled)
        {
            IsAmbientOcclusionEnabled = isAmbientOcclusionEnabled;
            if (_settingsChanged)
            {
                ApplySettings();
            }
        }

        private bool _isVoiceOverEnabled = true; // Default to true for voice over
        public bool IsVoiceOverEnabled
        {
            get => _isVoiceOverEnabled;
            set
            {
                if (_isVoiceOverEnabled != value)
                {
                    _isVoiceOverEnabled = value;
                    _settingsChanged = true;
                    _settingsStore.Set(nameof(IsVoiceOverEnabled), value);
                }
            }
        }

        private void SetIsVoiceOverEnabled(bool isVoiceOverEnabled)
        {
            IsVoiceOverEnabled = isVoiceOverEnabled;
            if (_settingsChanged)
            {
                ApplySettings();
            }
        }

        public void LoadUserSettings()
        {
            if (_settingsStore.TryGet(nameof(VSyncCount), out int vSyncCount))
            {
                _vSyncCount = vSyncCount;
            }

            if (_settingsStore.TryGet(nameof(AntiAliasing), out int antiAliasing))
            {
                _antiAliasing = antiAliasing;
            }

            if (_settingsStore.TryGet(nameof(TargetFrameRate), out int targetFrameRate))
            {
                _targetFrameRate = targetFrameRate;
            }

            if (_settingsStore.TryGet(nameof(ResolutionScale), out float resolutionScale))
            {
                _resolutionScale = resolutionScale;
            }

            if (_settingsStore.TryGet(nameof(FullScreenMode), out FullScreenMode fullScreenMode))
            {
                _fullScreenMode = fullScreenMode;
            }

            if (_settingsStore.TryGet(nameof(IsRealtimeLightingAndShadowsEnabled), out bool isRealtimeLightingAndShadowsEnabled))
            {
                _isRealtimeLightingAndShadowsEnabled = isRealtimeLightingAndShadowsEnabled;
            }

            if (_settingsStore.TryGet(nameof(IsAmbientOcclusionEnabled), out bool isAmbientOcclusionEnabled))
            {
                _isAmbientOcclusionEnabled = isAmbientOcclusionEnabled;
            }

            if (_settingsStore.TryGet(nameof(IsVoiceOverEnabled), out bool isVoiceOverEnabled))
            {
                _isVoiceOverEnabled = isVoiceOverEnabled;
            }
        }

        public void ApplySettings()
        {
            QualitySettings.vSyncCount = VSyncCount;

            QualitySettings.antiAliasing = AntiAliasing;

            Application.targetFrameRate = TargetFrameRate;

            Screen.SetResolution(
                (int)(Screen.currentResolution.width * ResolutionScale),
                (int)(Screen.currentResolution.height * ResolutionScale),
                FullScreenMode);

            // TODO: Notify realtime lighting and shadows setting change
            // TODO: Notify ambient occlusion setting change
            // TODO: Notify voice over setting change
        }

        public void SaveSettings()
        {
            if (_settingsChanged)
            {
                _settingsStore.Save();
                _settingsChanged = false;
            }
        }

        public void ResetSettings()
        {
            InitDefaultValuesForSettings();
            ApplySettings();
            SaveSettings();
        }

        private void InitDefaultValuesForSettings()
        {
            // Game should never trigger device sleep
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Hide keyboard on handheld devices
            if (Utility.IsHandheldDevice())
            {
                TouchScreenKeyboard.hideInput = true;
            }

            // Disable v-sync on desktop devices by default
            _vSyncCount = Utility.IsDesktopDevice() ? 0 : 1;

            // 2x MSAA by default
            _antiAliasing = 2;

            #if UNITY_2022_1_OR_NEWER
            var targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
            #else
            var targetFrameRate = Screen.currentResolution.refreshRate;
            #endif

            // Set target frame rate to screen refresh rate by default
            _targetFrameRate = Mathf.Max(targetFrameRate, 60); // 60Hz is the minimum

            // Full resolution by default
            _resolutionScale = 1.0f;

            // Windowed by default on desktop devices
            _fullScreenMode = Application.platform
                is RuntimePlatform.WindowsPlayer
                or RuntimePlatform.LinuxPlayer
                or RuntimePlatform.OSXPlayer ?
                    FullScreenMode.Windowed :
                    FullScreenMode.FullScreenWindow;

            // Enable realtime lighting and shadows by default on desktop devices
            _isRealtimeLightingAndShadowsEnabled = Utility.IsDesktopDevice();

            // Enable ambient occlusion by default on desktop devices
            _isAmbientOcclusionEnabled = Utility.IsDesktopDevice();

            // Enable voice over by default
            _isVoiceOverEnabled = true;

            // Downscaling resolution for old Android devices
            if (Application.platform == RuntimePlatform.Android)
            {
                SetDefaultSettingsForOldAndroidDevices();
            }
        }

        private void SetDefaultSettingsForOldAndroidDevices()
        {
            try
            {
                int GetAndroidSdkLevel()
                {
                    IntPtr versionClass = AndroidJNI.FindClass("android.os.Build$VERSION");
                    IntPtr sdkFieldID = AndroidJNI.GetStaticFieldID(versionClass, "SDK_INT", "I");
                    var sdkLevel = AndroidJNI.GetStaticIntField(versionClass, sdkFieldID);
                    return sdkLevel;
                }

                // Android 6 Marshmallow <=> API Version 23
                if (GetAndroidSdkLevel() <= 23)
                {
                    _antiAliasing = 0; // No AA
                    _resolutionScale = 0.75f; // 75% of full resolution
                    _isRealtimeLightingAndShadowsEnabled = false; // Disable realtime lighting and shadows
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}