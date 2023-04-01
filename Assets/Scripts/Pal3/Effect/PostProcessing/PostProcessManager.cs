// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect.PostProcessing
{
    using System;
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Utils;
    using Settings;
    using UnityEngine;
    using UnityEngine.Rendering.PostProcessing;

    public sealed class PostProcessManager : MonoBehaviour,
        ICommandExecutor<EffectSetScreenEffectCommand>,
        ICommandExecutor<ResetGameStateCommand>,
        ICommandExecutor<SettingChangedNotification>
    {
        private PostProcessVolume _postProcessVolume;
        private PostProcessLayer _postProcessLayer;
        private GameSettings _gameSettings;

        private Bloom _bloom;
        private AmbientOcclusion _ambientOcclusion;
        private ColorGrading _colorGrading;
        private Vignette _vignette;
        private Distortion _distortion;

        private int _currentAppliedEffectMode = -1;

        public void Init(PostProcessVolume volume,
            PostProcessLayer postProcessLayer,
            GameSettings gameSettings)
        {
            _postProcessVolume = Requires.IsNotNull(volume, nameof(volume));
            _postProcessLayer = Requires.IsNotNull(postProcessLayer, nameof(postProcessLayer));
            _gameSettings = Requires.IsNotNull(gameSettings, nameof(gameSettings));

            _bloom = _postProcessVolume.profile.GetSetting<Bloom>();
            ToggleBloomBasedOnSetting();

            _ambientOcclusion = _postProcessVolume.profile.GetSetting<AmbientOcclusion>();
            ToggleAmbientOcclusionBasedOnSetting();

            // These are effects that are controlled and used by the game scripts
            {
                _colorGrading = _postProcessVolume.profile.GetSetting<ColorGrading>();
                _colorGrading.active = false;

                _vignette = _postProcessVolume.profile.GetSetting<Vignette>();
                _vignette.active = false;

                _distortion = _postProcessVolume.profile.GetSetting<Distortion>();
                _distortion.active = false;
            }

            TogglePostProcessLayerWhenNeeded();
        }

        private void ToggleBloomBasedOnSetting()
        {
            if (_gameSettings.IsRealtimeLightingAndShadowsEnabled)
            {
                // Pointless when lighting is on
                _bloom.active = false;
            }
            else
            {
                // Enable bloom for better VFX visual fidelity on desktop devices
                _bloom.active = Utility.IsDesktopDevice();
            }
        }

        private void ToggleAmbientOcclusionBasedOnSetting()
        {
            if (_gameSettings.IsRealtimeLightingAndShadowsEnabled)
            {
                _ambientOcclusion.active = _gameSettings.IsAmbientOcclusionEnabled;
            }
            else
            {
                // Pointless when lighting is off
                _ambientOcclusion.active = false;
            }
        }

        private void TogglePostProcessLayerWhenNeeded()
        {
            if (_bloom.active ||
                _ambientOcclusion.active ||
                _colorGrading.active ||
                _vignette.active ||
                _distortion.active)
            {
                _postProcessLayer.enabled = true;
            }
            else
            {
                _postProcessLayer.enabled = false;
            }
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public int GetCurrentAppliedEffectMode()
        {
            return _currentAppliedEffectMode;
        }

        public void Execute(EffectSetScreenEffectCommand command)
        {
            switch (command.Mode)
            {
                // Disable all post-processing effects used by the game
                case -1:
                {
                    _colorGrading.active = false;
                    _vignette.active = false;
                    _distortion.active = false;
                    break;
                }
                // Distortion effect
                case 0:
                {
                    _distortion.active = true;
                    break;
                }
                // Vintage + color filter effect
                case 1:
                {
                    _colorGrading.active = true;
                    _vignette.active = true;
                    break;
                }
            }

            _currentAppliedEffectMode = command.Mode;
            TogglePostProcessLayerWhenNeeded();
        }

        public void Execute(ResetGameStateCommand command)
        {
            Execute(new EffectSetScreenEffectCommand(-1));
        }

        public void Execute(SettingChangedNotification command)
        {
            if (command.SettingName is nameof(GameSettings.IsRealtimeLightingAndShadowsEnabled)
                or nameof(GameSettings.IsAmbientOcclusionEnabled))
            {
                ToggleBloomBasedOnSetting();
                ToggleAmbientOcclusionBasedOnSetting();
                TogglePostProcessLayerWhenNeeded();
            }

            #if UNITY_IOS
            if (command.SettingName is nameof(GameSettings.ResolutionScale))
            {
                StartCoroutine(ReloadAmbientOcclusionSetting());
            }
            #endif
        }

        #if UNITY_IOS
        private IEnumerator Start()
        {
            yield return ReloadAmbientOcclusionSetting();
        }

        // On iOS, the SSAO and Resolution changing are causing the RoundedFrostedGlass
        // shader to malfunction. This is a workaround to disable and re-enable SSAO to fix the issue.
        private IEnumerator ReloadAmbientOcclusionSetting()
        {
            if (_gameSettings.IsAmbientOcclusionEnabled)
            {
                yield return null;
                _gameSettings.IsAmbientOcclusionEnabled = false;
                yield return null;
                _gameSettings.IsAmbientOcclusionEnabled = true;
            }
        }
        #endif
    }
}