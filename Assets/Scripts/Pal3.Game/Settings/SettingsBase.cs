// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

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

        private bool _toonRendering;
        public bool ToonRendering
        {
            get => _toonRendering;
            internal set
            {
                if (_toonRendering != value)
                {
                    _toonRendering = value;
                    SettingsStore.Set(nameof(ToonRendering), value);
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _toonContactShadows;
        public bool ToonContactShadows
        {
            get => _toonContactShadows;
            internal set => SetBooleanOption(ref _toonContactShadows, value);
        }

        private bool _toonCharacterLighting;
        public bool ToonCharacterLighting
        {
            get => _toonCharacterLighting;
            internal set => SetBooleanOption(ref _toonCharacterLighting, value);
        }

        internal const string NightFireBoostSettingsKey = "ToonNightFireLighting";

        private bool _toonNightFireBoost;
        public bool ToonNightFireBoost
        {
            get => _toonNightFireBoost;
            internal set => SetBooleanOption(ref _toonNightFireBoost, value, settingsKey: NightFireBoostSettingsKey);
        }

        internal const string FireShadowsSettingsKey = "ToonNightFireShadows";

        private bool _toonFireShadows;
        public bool ToonFireShadows
        {
            get => _toonFireShadows;
            internal set => SetBooleanOption(ref _toonFireShadows, value, settingsKey: FireShadowsSettingsKey);
        }

        private bool _bloomEnabled;
        public bool BloomEnabled
        {
            get => _bloomEnabled;
            internal set => SetBooleanOption(ref _bloomEnabled, value);
        }

        private bool _toonEnvironmentContrast;
        public bool ToonEnvironmentContrast
        {
            get => _toonEnvironmentContrast;
            internal set => SetBooleanOption(ref _toonEnvironmentContrast, value);
        }

        private bool _toonFireLighting;
        public bool ToonFireLighting
        {
            get => _toonFireLighting;
            internal set => SetBooleanOption(ref _toonFireLighting, value);
        }

        private bool _toonBakedShadows;
        public bool ToonBakedShadows
        {
            get => _toonBakedShadows;
            internal set => SetBooleanOption(ref _toonBakedShadows, value);
        }

        private bool _toonRealtimeShadows;
        public bool ToonRealtimeShadows
        {
            get => _toonRealtimeShadows;
            internal set => SetBooleanOption(ref _toonRealtimeShadows, value);
        }

        private float _toonEnvironmentContrastStrength = 1.0f;
        public float ToonEnvironmentContrastStrength
        {
            get => _toonEnvironmentContrastStrength;
            internal set => SetFloatOption(ref _toonEnvironmentContrastStrength, value, 1.0f);
        }

        private float _toonContactShadowStrength = 1.25f;
        public float ToonContactShadowStrength
        {
            get => _toonContactShadowStrength;
            internal set => SetFloatOption(ref _toonContactShadowStrength, value, 2.0f);
        }

        private float _toonCharacterLightingStrength = 0.9f;
        public float ToonCharacterLightingStrength
        {
            get => _toonCharacterLightingStrength;
            internal set => SetFloatOption(ref _toonCharacterLightingStrength, value, 1.0f);
        }

        private float _toonFireIntensityScale = 1.0f;
        public float ToonFireIntensityScale
        {
            get => _toonFireIntensityScale;
            internal set => SetFloatOption(ref _toonFireIntensityScale, value, 3.0f);
        }

        private float _toonFireRangeScale = 1.0f;
        public float ToonFireRangeScale
        {
            get => _toonFireRangeScale;
            internal set => SetFloatOption(ref _toonFireRangeScale, value, 2.0f);
        }

        internal const int MaximumFireShadowBudget = 4;

        private int _toonFireShadowBudget = 1;
        public int ToonFireShadowBudget
        {
            get => _toonFireShadowBudget;
            internal set
            {
                if (_toonFireShadowBudget == value || value < 0 || value > MaximumFireShadowBudget) return;
                _toonFireShadowBudget = value;
                SettingsStore.Set(nameof(ToonFireShadowBudget), value);
                NotifyPropertyChanged();
            }
        }

        private float _toonShadowStrength = 0.95f;
        public float ToonShadowStrength
        {
            get => _toonShadowStrength;
            internal set => SetFloatOption(ref _toonShadowStrength, value, 1.0f);
        }

        private float _toonBakedShadowStrength = 0.65f;
        public float ToonBakedShadowStrength
        {
            get => _toonBakedShadowStrength;
            internal set => SetFloatOption(ref _toonBakedShadowStrength, value, 1.0f);
        }

        private void SetFloatOption(ref float option, float value, float maximum,
            [CallerMemberName] string propertyName = null)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > maximum || option == value)
                return;
            option = value;
            SettingsStore.Set(propertyName, value);
            NotifyPropertyChanged(propertyName);
        }

        internal void LoadBloomSetting(bool defaultEnabled)
        {
            bool bloom = SettingsStore.TryGet(nameof(BloomEnabled), out bool enabled) ? enabled : defaultEnabled;
            if (_bloomEnabled == bloom) return;
            _bloomEnabled = bloom;
            NotifyPropertyChanged(nameof(BloomEnabled));
        }

        private void SetBooleanOption(ref bool option, bool value,
            [CallerMemberName] string propertyName = null, string settingsKey = null)
        {
            if (option == value) return;
            option = value;
            SettingsStore.Set(settingsKey ?? propertyName, value);
            NotifyPropertyChanged(propertyName);
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