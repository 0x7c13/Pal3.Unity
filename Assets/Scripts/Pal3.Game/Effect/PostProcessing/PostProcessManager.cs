// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Effect.PostProcessing
{
    using System;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Utilities;
    using Rendering.ScreenEffects;
    using Settings;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    public sealed class PostProcessManager : IDisposable,
        ICommandExecutor<EffectSetScreenEffectCommand>,
        ICommandExecutor<EffectShowSnowCommand>,
        ICommandExecutor<ScenePostLoadingNotification>,
        ICommandExecutor<SceneLeavingCurrentSceneNotification>,
        ICommandExecutor<ResetGameStateCommand>,
        ICommandExecutor<SettingChangedNotification>
    {
        private readonly Camera _camera;
        private readonly Volume _volume;
        private readonly UniversalAdditionalCameraData _cameraData;
        private readonly GameSettings _settings;
        private readonly LayerMask _originalVolumeLayerMask;
        private readonly bool _originalPostProcessing;
        private readonly bool _originalHdr;
        private readonly Pal3ScreenEffects _screenEffects;
        private readonly VolumeProfile _profile;
        private readonly Bloom _bloom;
        private readonly ColorAdjustments _colorGrading;
        private readonly Vignette _vignette;
        private int _currentAppliedEffectMode = -1;
        private bool _disposed;

        public PostProcessManager(Volume volume, Camera camera, GameSettings settings)
        {
            _camera = camera;
            _volume = volume;
            _settings = settings;
            _cameraData = camera.GetUniversalAdditionalCameraData();
            _originalVolumeLayerMask = _cameraData.volumeLayerMask;
            _originalPostProcessing = _cameraData.renderPostProcessing;
            _originalHdr = camera.allowHDR;
            _cameraData.volumeLayerMask = 1 << volume.gameObject.layer;
            _screenEffects = camera.TryGetComponent(out Pal3ScreenEffects existingEffects)
                ? existingEffects
                : camera.gameObject.AddComponent<Pal3ScreenEffects>();
            _screenEffects.enabled = true;
            _profile = volume.profile;
            _profile.TryGet(out _bloom);
            _profile.TryGet(out _colorGrading);
            _profile.TryGet(out _vignette);

            UpdateBloom();
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void UpdateBloom()
        {
            _camera.allowHDR = _settings.BloomEnabled;
            _bloom.active = _settings.BloomEnabled;
            UpdateCameraPostProcessing();
        }

        public void Execute(SettingChangedNotification command)
        {
            if (command.SettingName == nameof(GameSettings.BloomEnabled)) UpdateBloom();
        }

        private void UpdateCameraPostProcessing()
        {
            _cameraData.renderPostProcessing = _bloom.active || _colorGrading.active || _vignette.active;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            if (_screenEffects != null)
            {
                _screenEffects.Distortion = false;
                _screenEffects.Snow = false;
                _screenEffects.enabled = false;
            }
            if (_cameraData != null)
            {
                _cameraData.volumeLayerMask = _originalVolumeLayerMask;
                _cameraData.renderPostProcessing = _originalPostProcessing;
            }
            if (_camera != null) _camera.allowHDR = _originalHdr;
            if (_volume != null && _volume.HasInstantiatedProfile() && _volume.profile == _profile)
            {
                _volume.profile = null;
            }
            UnityEngine.Object.Destroy(_profile);
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
                    _screenEffects.Distortion = false;
                    break;
                }
                // Distortion effect
                case 0:
                {
                    _screenEffects.Distortion = true;
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
            UpdateCameraPostProcessing();
        }

        public void Execute(ResetGameStateCommand command)
        {
            Execute(new EffectSetScreenEffectCommand(-1));
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
                _screenEffects.Snow = true;
            }
            #elif PAL3A
            if (command.NewSceneInfo.Is("q02", "hs") ||
                command.NewSceneInfo.Is("q02", "qs") ||
                command.NewSceneInfo.Is("q02", "xs") ||
                command.NewSceneInfo.Is("q02", "zs"))
            {
                _screenEffects.Snow = true;
            }
            #endif
        }

        public void Execute(SceneLeavingCurrentSceneNotification command)
        {
            _screenEffects.Snow = false;
        }
    }
}