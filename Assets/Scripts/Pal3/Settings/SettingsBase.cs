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

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Settings Properties

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
                    NotifyPropertyChanged();
                }
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
                    NotifyPropertyChanged();
                }
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
                    NotifyPropertyChanged();
                }
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
                    NotifyPropertyChanged();
                }
            }
        }

        private FullScreenMode _fullScreenMode = FullScreenMode.FullScreenWindow; // Default to full screen window
        public FullScreenMode FullScreenMode
        {
            get => _fullScreenMode;
            set
            {
                if (_fullScreenMode != value &&
                    Enum.IsDefined(typeof(FullScreenMode), value))
                {
                    _fullScreenMode = value;
                    NotifyPropertyChanged();
                }
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
                    NotifyPropertyChanged();
                }
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
                    NotifyPropertyChanged();
                }
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
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion
    }
}