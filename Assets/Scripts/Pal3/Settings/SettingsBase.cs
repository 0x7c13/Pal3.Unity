// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Settings
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    /// <summary>
    /// Base class to hold all settings properties.
    /// </summary>
    public abstract class SettingsBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        internal readonly ITransactionalKeyValueStore SettingsStore;

        internal SettingsBase(ITransactionalKeyValueStore settingsStore)
        {
            SettingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Settings Properties

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
                if (_antiAliasing != value && value >= 0)
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
                if (Math.Abs(_resolutionScale - value) > float.Epsilon && value is > 0 and <= 1.0f)
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
                if (Math.Abs(_musicVolume - value) > float.Epsilon && value is >= 0 and <= 1.0f)
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
                if (Math.Abs(_sfxVolume - value) > float.Epsilon && value is >= 0 and <= 1.0f)
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

        #endregion
    }
}