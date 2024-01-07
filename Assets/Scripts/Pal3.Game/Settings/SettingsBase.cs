// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Settings
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using Constants;
    using Core.Utilities;

    /// <summary>
    /// Base class to hold all settings properties.
    /// </summary>
    public abstract class SettingsBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        internal readonly ITransactionalKeyValueStore SettingsStore;

        internal SettingsBase(ITransactionalKeyValueStore settingsStore)
        {
            SettingsStore = Requires.IsNotNull(settingsStore, nameof(settingsStore));
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Settings Properties

        private Language _language = Language.SimplifiedChinese; // Default to simplified chinese
        public Language Language
        {
            get => _language;
            internal set
            {
                if (_language != value)
                {
                    _language = value;
                    SettingsStore.Set(nameof(Language), value);
                    NotifyPropertyChanged();
                }
            }
        }

        private int _vSyncCount = 0; // Default to 0 for no v-sync
        public int VSyncCount
        {
            get => _vSyncCount;
            internal set
            {
                if (_vSyncCount != value && value >= 0)
                {
                    _vSyncCount = value;
                    SettingsStore.Set(nameof(VSyncCount), value);
                    NotifyPropertyChanged();
                }
            }
        }

        private int _antiAliasing = 0; // Default to 0 for no anti-aliasing
        public int AntiAliasing
        {
            get => _antiAliasing;
            internal set
            {
                if (_antiAliasing != value && value is 0 or 2 or 4 or 8)
                {
                    _antiAliasing = value;
                    SettingsStore.Set(nameof(AntiAliasing), value);
                    NotifyPropertyChanged();
                }
            }
        }

        private int _targetFrameRate = -1; // Default to -1 for unlimited frame rate
        public int TargetFrameRate
        {
            get => _targetFrameRate;
            internal set
            {
                if (_targetFrameRate != value && value is > 0 or -1)
                {
                    _targetFrameRate = value;
                    SettingsStore.Set(nameof(TargetFrameRate), value);
                    NotifyPropertyChanged();
                }
            }
        }

        private float _resolutionScale = 1.0f; // Default to 1.0f for full resolution
        public float ResolutionScale
        {
            get => _resolutionScale;
            internal set
            {
                if (MathF.Abs(_resolutionScale - value) > float.Epsilon && value is > 0 and <= 1.0f)
                {
                    _resolutionScale = value;
                    SettingsStore.Set(nameof(ResolutionScale), value);
                    NotifyPropertyChanged();
                }
            }
        }

        private float _musicVolume = 0.5f; // Default to half volume
        public float MusicVolume
        {
            get => _musicVolume;
            internal set
            {
                if (MathF.Abs(_musicVolume - value) > float.Epsilon && value is >= 0 and <= 1.0f)
                {
                    _musicVolume = value;
                    SettingsStore.Set(nameof(MusicVolume), value);
                    NotifyPropertyChanged();
                }
            }
        }

        private float _sfxVolume = 0.5f; // Default to half volume
        public float SfxVolume
        {
            get => _sfxVolume;
            internal set
            {
                if (MathF.Abs(_sfxVolume - value) > float.Epsilon && value is >= 0 and <= 1.0f)
                {
                    _sfxVolume = value;
                    SettingsStore.Set(nameof(SfxVolume), value);
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _isRealtimeLightingAndShadowsEnabled = false; // Default disable realtime lighting and shadows
        public bool IsRealtimeLightingAndShadowsEnabled
        {
            get => _isRealtimeLightingAndShadowsEnabled;
            internal set
            {
                if (_isRealtimeLightingAndShadowsEnabled != value)
                {
                    _isRealtimeLightingAndShadowsEnabled = value;
                    SettingsStore.Set(nameof(IsRealtimeLightingAndShadowsEnabled), value);
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _isAmbientOcclusionEnabled = false; // Default to disable ambient occlusion
        public bool IsAmbientOcclusionEnabled
        {
            get => _isAmbientOcclusionEnabled;
            internal set
            {
                if (_isAmbientOcclusionEnabled != value)
                {
                    _isAmbientOcclusionEnabled = value;
                    SettingsStore.Set(nameof(IsAmbientOcclusionEnabled), value);
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _isVoiceOverEnabled = true; // Default to enable voice over
        public bool IsVoiceOverEnabled
        {
            get => _isVoiceOverEnabled;
            internal set
            {
                if (_isVoiceOverEnabled != value)
                {
                    _isVoiceOverEnabled = value;
                    SettingsStore.Set(nameof(IsVoiceOverEnabled), value);
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _isDebugInfoEnabled = true; // Default to enable debug info
        public bool IsDebugInfoEnabled
        {
            get => _isDebugInfoEnabled;
            internal set
            {
                if (_isDebugInfoEnabled != value)
                {
                    _isDebugInfoEnabled = value;
                    SettingsStore.Set(nameof(IsDebugInfoEnabled), value);
                    NotifyPropertyChanged();
                }
            }
        }

        private string _gameDataFolderPath = string.Empty;
        public string GameDataFolderPath
        {
            get => _gameDataFolderPath;
            internal set
            {
                if (_gameDataFolderPath != value)
                {
                    _gameDataFolderPath = value;
                    SettingsStore.Set(nameof(GameDataFolderPath), value);
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _isTurnBasedCombatEnabled = false;
        public bool IsTurnBasedCombatEnabled
        {
            get => _isTurnBasedCombatEnabled;
            internal set
            {
                if (_isTurnBasedCombatEnabled != value)
                {
                    _isTurnBasedCombatEnabled = value;
                    SettingsStore.Set(nameof(IsTurnBasedCombatEnabled), value);
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion
    }
}