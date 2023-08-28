// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect.PostProcessing
{
    using System;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Utils;
    using Settings;
    using UnityEngine.Rendering.PostProcessing;

    public sealed class PostProcessManager : IDisposable,
        ICommandExecutor<EffectSetScreenEffectCommand>,
        ICommandExecutor<EffectShowSnowCommand>,
        ICommandExecutor<ScenePostLoadingNotification>,
        ICommandExecutor<SceneLeavingCurrentSceneNotification>,
        ICommandExecutor<ResetGameStateCommand>,
        ICommandExecutor<SettingChangedNotification>
    {
        private readonly PostProcessVolume _postProcessVolume;
        private readonly PostProcessLayer _postProcessLayer;
        private readonly GameSettings _gameSettings;

        private readonly Bloom _bloom;
        private readonly AmbientOcclusion _ambientOcclusion;
        private readonly ColorGrading _colorGrading;
        private readonly Vignette _vignette;
        private readonly Distortion _distortion;
        private readonly Snow _snow;

        private int _currentAppliedEffectMode = -1;

        public PostProcessManager(PostProcessVolume volume,
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

            // Snow effect is controlled by pre-configured scene list
            _snow = _postProcessVolume.profile.GetSetting<Snow>();
            _snow.active = false;

            TogglePostProcessLayerWhenNeeded();

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
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

        public void Dispose()
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
        }

        public void Execute(EffectShowSnowCommand command)
        {
            // Ignoring this command since now we control the snow effect based
            // on pre-configured scene list
        }

        public void Execute(ScenePostLoadingNotification command)
        {
            #if PAL3
            if (command.NewSceneInfo.Is("q15", "q15") ||
                command.NewSceneInfo.IsCity("m22"))
            {
                _snow.active = true;
            }
            #elif PAL3A
            if (command.NewSceneInfo.Is("q02", "hs") ||
                command.NewSceneInfo.Is("q02", "qs") ||
                command.NewSceneInfo.Is("q02", "xs") ||
                command.NewSceneInfo.Is("q02", "zs"))
            {
                _snow.active = true;
            }
            #endif
        }

        public void Execute(SceneLeavingCurrentSceneNotification command)
        {
            _snow.active = false;
        }
    }
}