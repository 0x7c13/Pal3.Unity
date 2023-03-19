// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Settings
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;
    using Command;
    using Command.InternalCommands;
    using Core.Utils;
    using IngameDebugConsole;
    using MetaData;
    using UnityEngine;

    public sealed class SettingsManager : SettingsBase
    {
        private readonly ITransactionalKeyValueStore _settingsStore;

        public SettingsManager(ITransactionalKeyValueStore settingsStore)
        {
            _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));

            InitDefaultSettings();

            PropertyChanged += OnPropertyChanged;

            DebugLogConsole.AddCommand<int>("Settings.VSyncCount",
                "设置垂直同步设定（0：关闭，1：开启，2: 开启（每2帧刷新）)", _ => VSyncCount = _);
            DebugLogConsole.AddCommand<int>("Settings.AntiAliasing",
                "设置抗锯齿设定（0：关闭，2：2倍抗锯齿，4：4倍抗锯齿，8：8倍抗锯齿, 16: 16倍抗锯齿)", _ => AntiAliasing = _);
            DebugLogConsole.AddCommand<int>("Settings.TargetFrameRate",
                "设置目标帧率设定（-1：不限制，30：30帧，60：60帧）", _ => TargetFrameRate = _);
            DebugLogConsole.AddCommand<float>("Settings.ResolutionScale",
                "设置分辨率缩放设定（0.1：10%分辨率，0.5：50%分辨率，1.0：100%分辨率）", _ => ResolutionScale = _);
            DebugLogConsole.AddCommand<int>("Settings.FullScreenMode",
                "设置全屏模式设定（0：Windows独占全屏，1：全屏，2：MacOS全屏，3：窗口", _ => FullScreenMode = (FullScreenMode) _);
            DebugLogConsole.AddCommand<float>("Settings.MusicVolume",
                "设置音乐音量设定（0.0：静音，1.0：最大音量）", _ => MusicVolume = _);
            DebugLogConsole.AddCommand<float>("Settings.SfxVolume",
                "设置音效音量设定（0.0：静音，1.0：最大音量）", _ => SfxVolume = _);
            DebugLogConsole.AddCommand<bool>("Settings.IsRealtimeLightingAndShadowsEnabled",
                "设置实时光照和阴影设定（true：开启，false：关闭）", _ => IsRealtimeLightingAndShadowsEnabled = _);
            DebugLogConsole.AddCommand<bool>("Settings.IsAmbientOcclusionEnabled",
                "设置环境光遮蔽设定（true：开启，false：关闭）", _ => IsAmbientOcclusionEnabled = _);
            DebugLogConsole.AddCommand<bool>("Settings.IsVoiceOverEnabled",
                "设置角色配音设定（true：开启，false：关闭）", _ => IsVoiceOverEnabled = _);
            DebugLogConsole.AddCommand<string>("Settings.GameDataFolderPath",
                "设置自定义游戏数据文件夹路径", _ => GameDataFolderPath = _);

            DebugLogConsole.AddCommand("Settings.Save",
                "保存所有设置", SaveSettings);
            DebugLogConsole.AddCommand("Settings.Reset",
                "重置所有设置", ResetSettings);
            DebugLogConsole.AddCommand("Settings.Print",
                "打印所有设置", PrintSettings);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            string settingName = args.PropertyName;

            CommandDispatcher<ICommand>.Instance.Dispatch(new SettingChangedNotification(settingName));

            if (settingName == nameof(VSyncCount))
            {
                QualitySettings.vSyncCount = VSyncCount;
                _settingsStore.Set(settingName, VSyncCount);
            }
            else if (settingName == nameof(AntiAliasing))
            {
                QualitySettings.antiAliasing = AntiAliasing;
                _settingsStore.Set(settingName, AntiAliasing);
            }
            else if (settingName == nameof(TargetFrameRate))
            {
                Application.targetFrameRate = TargetFrameRate;
                _settingsStore.Set(settingName, TargetFrameRate);
            }
            else if (settingName == nameof(ResolutionScale))
            {
                Screen.SetResolution(
                    (int) (Screen.currentResolution.width * ResolutionScale),
                    (int) (Screen.currentResolution.height * ResolutionScale),
                    Screen.fullScreenMode);
                _settingsStore.Set(settingName, ResolutionScale);
            }
            else if (settingName == nameof(FullScreenMode))
            {
                Screen.fullScreenMode = FullScreenMode;
                _settingsStore.Set(settingName, FullScreenMode);
            }
            else if (settingName == nameof(MusicVolume))
            {
                _settingsStore.Set(settingName, MusicVolume);
            }
            else if (settingName == nameof(SfxVolume))
            {
                _settingsStore.Set(settingName, SfxVolume);
            }
            else if (settingName == nameof(IsRealtimeLightingAndShadowsEnabled))
            {
                _settingsStore.Set(settingName, IsRealtimeLightingAndShadowsEnabled);
            }
            else if (settingName == nameof(IsAmbientOcclusionEnabled))
            {
                _settingsStore.Set(settingName, IsAmbientOcclusionEnabled);
            }
            else if (settingName == nameof(IsVoiceOverEnabled))
            {
                _settingsStore.Set(settingName, IsVoiceOverEnabled);
            }
            else if (settingName == nameof(GameDataFolderPath))
            {
                _settingsStore.Set(settingName, GameDataFolderPath);
            }
        }

        private void InitDefaultSettings()
        {
            // Game should never trigger device sleep
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Hide keyboard on handheld devices
            if (Utility.IsHandheldDevice())
            {
                TouchScreenKeyboard.hideInput = true;
            }
        }

        public void InitSettings()
        {
            if (_settingsStore.TryGet(nameof(VSyncCount), out int vSyncCount))
            {
                VSyncCount = vSyncCount;
            }
            else
            {
                // Disable v-sync on desktop devices by default
                VSyncCount = Utility.IsDesktopDevice() ? 0 : 1;
            }

            if (_settingsStore.TryGet(nameof(AntiAliasing), out int antiAliasing))
            {
                AntiAliasing = antiAliasing;
            }
            else
            {
                // 2x MSAA by default on desktop devices
                AntiAliasing = Utility.IsDesktopDevice() ? 2 : 0;
            }

            if (_settingsStore.TryGet(nameof(TargetFrameRate), out int targetFrameRate))
            {
                TargetFrameRate = targetFrameRate;
            }
            else
            {
                #if UNITY_2022_1_OR_NEWER
                var screenRefreshRate = (int) Screen.currentResolution.refreshRateRatio.value;
                #else
                var screenRefreshRate = Screen.currentResolution.refreshRate;
                #endif

                // Set target frame rate to screen refresh rate by default
                TargetFrameRate = Mathf.Max(screenRefreshRate, 60); // 60Hz is the minimum
            }

            if (_settingsStore.TryGet(nameof(ResolutionScale), out float resolutionScale))
            {
                ResolutionScale = resolutionScale;
            }
            else
            {
                // Full resolution by default unless on Android with SDK version lower than 23 (old devices)
                // SDK version 23 is Android 6.0 Marshmallow
                ResolutionScale = Utility.IsAndroidDeviceAndSdkVersionLowerThanOrEqualTo(23) ? 0.75f : 1.0f;
            }

            if (_settingsStore.TryGet(nameof(FullScreenMode), out FullScreenMode fullScreenMode))
            {
                FullScreenMode = fullScreenMode;
            }
            else
            {
                // Windowed by default on desktop devices
                FullScreenMode = Application.platform
                    is RuntimePlatform.WindowsPlayer
                    or RuntimePlatform.LinuxPlayer
                    or RuntimePlatform.OSXPlayer
                    ? FullScreenMode.Windowed
                    : FullScreenMode.FullScreenWindow;
            }

            if (_settingsStore.TryGet(nameof(MusicVolume), out float musicVolume))
            {
                MusicVolume = musicVolume;
            }
            else
            {
                // 50% music volume by default
                MusicVolume = 0.5f;
            }

            if (_settingsStore.TryGet(nameof(SfxVolume), out float sfxVolume))
            {
                SfxVolume = sfxVolume;
            }
            else
            {
                // 50% sfx volume by default
                SfxVolume = 0.5f;
            }

            if (_settingsStore.TryGet(nameof(IsRealtimeLightingAndShadowsEnabled),
                    out bool isRealtimeLightingAndShadowsEnabled))
            {
                IsRealtimeLightingAndShadowsEnabled = isRealtimeLightingAndShadowsEnabled;
            }
            else
            {
                // Enable realtime lighting and shadows by default on desktop devices
                IsRealtimeLightingAndShadowsEnabled = Utility.IsDesktopDevice();
            }

            if (_settingsStore.TryGet(nameof(IsAmbientOcclusionEnabled), out bool isAmbientOcclusionEnabled))
            {
                IsAmbientOcclusionEnabled = isAmbientOcclusionEnabled;
            }
            else
            {
                // Enable ambient occlusion by default on desktop devices
                IsAmbientOcclusionEnabled = Utility.IsDesktopDevice();
            }

            if (_settingsStore.TryGet(nameof(IsVoiceOverEnabled), out bool isVoiceOverEnabled))
            {
                IsVoiceOverEnabled = isVoiceOverEnabled;
            }
            else
            {
                // Enable voice over by default
                IsVoiceOverEnabled = true;
            }

            if (_settingsStore.TryGet(nameof(GameDataFolderPath), out string gameDataFolderPath))
            {
                GameDataFolderPath = gameDataFolderPath;
            }
            else
            {
                // Set game data folder path to persistent data path by default
                GameDataFolderPath = Application.persistentDataPath +
                                     Path.DirectorySeparatorChar +
                                     GameConstants.AppName +
                                     Path.DirectorySeparatorChar;
            }
        }

        public void SaveSettings()
        {
            _settingsStore.Save();
        }

        public void ResetSettings()
        {
            // Delete all settings keys
            foreach (PropertyInfo property in typeof(SettingsBase).GetProperties())
            {
                _settingsStore.DeleteKey(property.Name);
            }

            // Re-initialize settings
            InitSettings();

            // Save settings
            SaveSettings();
        }

        public void PrintSettings()
        {
            Debug.Log("Current game settings:");

            foreach (PropertyInfo property in typeof(SettingsBase).GetProperties())
            {
                Debug.Log($"{property.Name}: {property.GetValue(this)}");
            }
        }
    }
}