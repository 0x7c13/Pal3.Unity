// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Settings
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Constants;
    using Core.Utilities;
    using Engine.Logging;
    using Engine.Utilities;
    using IngameDebugConsole;
    using UnityEngine;

    public sealed class GameSettings : SettingsBase, IDisposable
    {
        public event Action<string> OnGameSettingsChanged;

        private bool? _sessionUsesToon;

        public GameSettings(ITransactionalKeyValueStore settingsStore) : base(settingsStore)
        {
            InitDefaultSettings();

            DebugLogConsole.AddCommand<int>("Settings.VSyncCount",
                "设置垂直同步设定（0：关闭，1：开启，2: 开启（每2帧刷新）)", _ => VSyncCount = _);
            DebugLogConsole.AddCommand<int>("Settings.AntiAliasing",
                "设置抗锯齿设定（0：关闭，2：2倍抗锯齿，4：4倍抗锯齿，8：8倍抗锯齿)", _ => AntiAliasing = _);
            DebugLogConsole.AddCommand<int>("Settings.TargetFrameRate",
                "设置目标帧率设定（-1：不限制，30：30帧，60：60帧）", _ => TargetFrameRate = _);
            DebugLogConsole.AddCommand<float>("Settings.ResolutionScale",
                "设置分辨率缩放设定（0.1：10%分辨率，0.5：50%分辨率，1.0：100%分辨率）", _ => ResolutionScale = _);
            DebugLogConsole.AddCommand<float>("Settings.MusicVolume",
                "设置音乐音量设定（0.0：静音，1.0：最大音量）", _ => MusicVolume = _);
            DebugLogConsole.AddCommand<float>("Settings.SfxVolume",
                "设置音效音量设定（0.0：静音，1.0：最大音量）", _ => SfxVolume = _);
            DebugLogConsole.AddCommand<bool>("Settings.IsVoiceOverEnabled",
                "设置角色配音设定（true：开启，false：关闭）", _ => IsVoiceOverEnabled = _);
            DebugLogConsole.AddCommand<bool>("Settings.IsDebugInfoEnabled",
                "设置调试信息显示设定（true：开启，false：关闭）", _ => IsDebugInfoEnabled = _);
            DebugLogConsole.AddCommand<bool>("Settings.IsTurnBasedCombatEnabled",
                "设置回合制战斗开启关闭设定（true：开启，false：关闭）", _ => IsTurnBasedCombatEnabled = _);
            DebugLogConsole.AddCommand<string>("Settings.GameDataFolderPath",
                "设置自定义游戏数据文件夹路径", _ => GameDataFolderPath = _);

            DebugLogConsole.AddCommand<bool>("Settings.ToonRendering",
                "设置卡通画风与实时光影，重启后生效", enabled =>
                {
                    ToonRendering = enabled;
                    SaveSettings();
                });

            RegisterVisualOption(nameof(ToonContactShadows), "接触阴影", enabled => ToonContactShadows = enabled);
            RegisterVisualOption(nameof(ToonCharacterLighting), "角色光照", enabled => ToonCharacterLighting = enabled);
            RegisterVisualOption(nameof(ToonNightFireBoost), "增强夜景火光", enabled => ToonNightFireBoost = enabled);
            RegisterVisualOption(nameof(BloomEnabled), "泛光", enabled => BloomEnabled = enabled);
            RegisterVisualOption(nameof(ToonNightFireShadows), "夜景火光投影", enabled => ToonNightFireShadows = enabled);

            DebugLogConsole.AddCommand("Settings.Save",
                "保存所有设置", SaveSettings);
            DebugLogConsole.AddCommand("Settings.Reset",
                "重置所有设置", ResetSettings);
            DebugLogConsole.AddCommand("Settings.Print",
                "打印所有设置", PrintCurrentSettings);

            PropertyChanged += OnPropertyChanged;
        }

        private void RegisterVisualOption(string name, string label, Action<bool> apply)
        {
            DebugLogConsole.AddCommand<bool>($"Settings.{name}", $"设置{label}", enabled =>
            {
                apply(enabled);
                SaveSettings();
            });
        }

        public void Dispose()
        {
            PropertyChanged -= OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            OnGameSettingsChanged?.Invoke(args.PropertyName);
        }

        private void InitDefaultSettings()
        {
            // Game should never trigger device sleep
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Hide keyboard on handheld devices
            if (UnityEngineUtility.IsHandheldDevice())
            {
                TouchScreenKeyboard.hideInput = true;
            }
        }

        public void InitOrLoadSettings()
        {
            if (SettingsStore.TryGet(nameof(Language), out Language language))
            {
                Language = language;
            }
            else
            {
                // Simplified Chinese by default
                Language = Language.SimplifiedChinese;
            }

            if (SettingsStore.TryGet(nameof(VSyncCount), out int vSyncCount))
            {
                VSyncCount = vSyncCount;
            }
            else
            {
                // Disable v-sync by default
                VSyncCount = 0;
            }

            if (SettingsStore.TryGet(nameof(AntiAliasing), out int antiAliasing))
            {
                AntiAliasing = antiAliasing;
            }
            else
            {
                // 2x MSAA by default on desktop devices
                AntiAliasing = UnityEngineUtility.IsDesktopDevice() ? 2 : 0;
            }

            #if UNITY_2022_1_OR_NEWER
            int screenRefreshRate = (int) Screen.currentResolution.refreshRateRatio.value;
            #else
            int screenRefreshRate = Screen.currentResolution.refreshRate;
            #endif

            if (SettingsStore.TryGet(nameof(TargetFrameRate), out int targetFrameRate) &&
                targetFrameRate >= screenRefreshRate)
                // Only respect the target frame rate if it is higher than the screen refresh rate
            {
                TargetFrameRate = targetFrameRate;
            }
            else
            {
                #if UNITY_STANDALONE
                const int minimumTargetFrameRate = 120;
                #elif UNITY_ANDROID || UNITY_IOS
                const int minimumTargetFrameRate = 90;
                #else
                const int minimumTargetFrameRate = 60;
                #endif

                // Set target frame rate to screen refresh rate by default
                // unless it is lower than the minimum target frame rate
                TargetFrameRate = Math.Max(screenRefreshRate, minimumTargetFrameRate);
            }

            if (SettingsStore.TryGet(nameof(ResolutionScale), out float resolutionScale))
            {
                ResolutionScale = resolutionScale;
            }
            else
            {
                // 75% resolution scale by default on mobile devices
                #if UNITY_ANDROID || UNITY_IOS
                ResolutionScale = 0.75f;
                #else
                ResolutionScale = 1.0f;
                #endif
            }

            #if UNITY_STANDALONE
            // Reset resolution scale if not in fullscreen mode for desktop devices
            if (Screen.fullScreenMode != FullScreenMode.ExclusiveFullScreen &&
                Screen.fullScreenMode != FullScreenMode.FullScreenWindow &&
                MathF.Abs(ResolutionScale - 1.0f) > 0.01f)
            {
                ResolutionScale = 1.0f;
            }
            #endif

            if (SettingsStore.TryGet(nameof(MusicVolume), out float musicVolume))
            {
                MusicVolume = musicVolume;
            }
            else
            {
                // 50% music volume by default
                MusicVolume = 0.5f;
            }

            if (SettingsStore.TryGet(nameof(SfxVolume), out float sfxVolume))
            {
                SfxVolume = sfxVolume;
            }
            else
            {
                // 50% sfx volume by default
                SfxVolume = 0.5f;
            }

            ToonRendering = SettingsStore.TryGet(nameof(ToonRendering), out bool toonRendering) && toonRendering;
            bool desktop = UnityEngineUtility.IsDesktopDevice();
            ToonContactShadows = SettingsStore.TryGet(nameof(ToonContactShadows), out bool contactShadows)
                ? contactShadows : desktop;
            ToonCharacterLighting = !SettingsStore.TryGet(nameof(ToonCharacterLighting), out bool characterLighting)
                || characterLighting;
            ToonNightFireBoost = !SettingsStore.TryGet(NightFireBoostSettingsKey, out bool nightFireBoost)
                || nightFireBoost;
            _sessionUsesToon ??= ToonRendering;
            LoadBloomSetting(defaultEnabled: desktop && !_sessionUsesToon.Value);
            ToonNightFireShadows = SettingsStore.TryGet(nameof(ToonNightFireShadows), out bool nightFireShadows)
                ? nightFireShadows : desktop;

            if (SettingsStore.TryGet(nameof(IsVoiceOverEnabled), out bool isVoiceOverEnabled))
            {
                IsVoiceOverEnabled = isVoiceOverEnabled;
            }
            else
            {
                // Enable voice over by default
                IsVoiceOverEnabled = true;
            }

            if (SettingsStore.TryGet(nameof(IsTurnBasedCombatEnabled), out bool isTurnBasedCombatEnabled))
            {
                IsTurnBasedCombatEnabled = isTurnBasedCombatEnabled;
            }
            else
            {
                // TODO: Enable turn-based combat by default when it is implemented
                IsTurnBasedCombatEnabled = false;
            }

            if (SettingsStore.TryGet(nameof(IsDebugInfoEnabled), out bool isDebugInfoEnabled))
            {
                IsDebugInfoEnabled = isDebugInfoEnabled;
            }
            else
            {
                // Enable debug info by default
                IsDebugInfoEnabled = true;
            }

            if (SettingsStore.TryGet(nameof(GameDataFolderPath), out string gameDataFolderPath))
            {
                GameDataFolderPath = gameDataFolderPath;
            }

            EngineLogger.Log($"Settings initialized");
            PrintCurrentSettings();
        }

        public IEnumerable<string> GetGameDataFolderSearchLocations()
        {
            // Use game data folder path if it is not empty
            // GameDataFolderPath is either set by the user or the last
            // successfully loaded game data folder path
            if (!string.IsNullOrEmpty(GameDataFolderPath))
            {
                yield return GameDataFolderPath;
            }

            // Try current executable directory on Windows and Linux
            if (Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.LinuxPlayer)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath).Parent;
                if (directoryInfo != null)
                {
                    yield return directoryInfo.FullName + Path.DirectorySeparatorChar;
                }
            }

            // Streaming assets path
            yield return Application.streamingAssetsPath +
                         Path.DirectorySeparatorChar +
                         GameConstants.AppName +
                         Path.DirectorySeparatorChar;

            // Persistent data path
            yield return Application.persistentDataPath +
                         Path.DirectorySeparatorChar +
                         GameConstants.AppName +
                         Path.DirectorySeparatorChar;
        }

        public void SaveSettings()
        {
            SettingsStore.Commit();
        }

        public void ResetSettings()
        {
            // Delete all settings keys
            foreach (PropertyInfo property in typeof(SettingsBase).GetProperties())
            {
                SettingsStore.DeleteKey(property.Name);
            }

            SettingsStore.DeleteKey(NightFireBoostSettingsKey);

            // Re-initialize settings
            InitOrLoadSettings();

            // Save settings
            SaveSettings();
        }

        public void PrintCurrentSettings()
        {
            EngineLogger.Log($"Current settings: {this}");
        }

        public override string ToString()
        {
            StringBuilder sb = new ();
            foreach (PropertyInfo property in typeof(SettingsBase).GetProperties())
            {
                sb.Append($", {property.Name}: {property.GetValue(this)}");
            }
            return sb.Length == 0 ? string.Empty : sb.ToString()[2..];
        }
    }
}