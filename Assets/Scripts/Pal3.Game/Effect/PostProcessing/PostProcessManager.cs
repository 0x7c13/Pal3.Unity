// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

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
        private readonly UniversalAdditionalCameraData _cameraData;
        private readonly GameSettings _settings;
        private readonly VolumeFrameworkUpdateMode _originalVolumeUpdateMode;
        private readonly VolumeStack _originalVolumeStack;
        private readonly bool _originalPostProcessing;
        private readonly bool _originalHdr;
        private readonly Pal3ScreenEffects _screenEffects;
        private readonly bool _originalScreenEffectsEnabled;
        private readonly bool _originalDistortion;
        private readonly bool _originalSnow;
        private readonly Bloom _bloom;
        private readonly ColorAdjustments _colorGrading;
        private readonly Vignette _vignette;
        private VolumeStack _volumeStack;
        private bool _volumeDirty = true;
        private bool _gradingEnabled;
        private int _currentAppliedEffectMode = -1;
        private bool _disposed;

        public PostProcessManager(VolumeProfile profile, Camera camera, GameSettings settings)
        {
            _camera = camera;
            _settings = settings;
            _cameraData = camera.GetUniversalAdditionalCameraData();
            _originalVolumeUpdateMode = camera.GetVolumeFrameworkUpdateMode();
            _originalVolumeStack = _cameraData.volumeStack;
            _originalPostProcessing = _cameraData.renderPostProcessing;
            _originalHdr = camera.allowHDR;
            _screenEffects = camera.TryGetComponent(out Pal3ScreenEffects existingEffects)
                ? existingEffects
                : camera.gameObject.AddComponent<Pal3ScreenEffects>();
            _originalScreenEffectsEnabled = existingEffects != null && existingEffects.enabled;
            _originalDistortion = _screenEffects.Distortion;
            _originalSnow = _screenEffects.Snow;
            _screenEffects.enabled = true;
            profile.TryGet(out _bloom);
            profile.TryGet(out _colorGrading);
            profile.TryGet(out _vignette);

            UpdateBloom();
            RenderPipelineManager.beginCameraRendering += RefreshVolumeStack;
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private bool BloomEnabled => _bloom != null && _settings.BloomEnabled;

        private void UpdateBloom()
        {
            _camera.allowHDR = BloomEnabled;
            _volumeDirty = true;
            UpdateCameraPostProcessing();
        }

        public void Execute(SettingChangedNotification command)
        {
            if (command.SettingName == nameof(GameSettings.BloomEnabled)) UpdateBloom();
        }

        private void UpdateCameraPostProcessing()
        {
            _cameraData.renderPostProcessing = BloomEnabled || _gradingEnabled;
        }

        private void RefreshVolumeStack(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera || (!_volumeDirty && _volumeStack?.isValid == true)) return;

            // Rebuild from URP defaults only when settings change. A private stack avoids
            // blending another camera's Volume even when both cameras share a profile.
            var manager = VolumeManager.instance;
            var previous = _volumeStack;
            _volumeStack = manager.CreateStack();
            _cameraData.volumeStack = _volumeStack;
            camera.SetVolumeFrameworkUpdateMode(VolumeFrameworkUpdateMode.ViaScripting);
            manager.Update(_volumeStack, null, 0);

            if (BloomEnabled) _bloom.Override(_volumeStack.GetComponent<Bloom>(), 1f);
            if (_gradingEnabled)
            {
                _colorGrading?.Override(_volumeStack.GetComponent<ColorAdjustments>(), 1f);
                _vignette?.Override(_volumeStack.GetComponent<Vignette>(), 1f);
            }

            DestroyVolumeStack(previous);
            _volumeDirty = false;
        }

        private static void DestroyVolumeStack(VolumeStack stack)
        {
            if (stack?.isValid != true) return;
            var manager = VolumeManager.instance;
            if (manager.stack == stack) manager.ResetMainStack();
            manager.DestroyStack(stack);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            RenderPipelineManager.beginCameraRendering -= RefreshVolumeStack;
            if (_screenEffects != null)
            {
                _screenEffects.Distortion = _originalDistortion;
                _screenEffects.Snow = _originalSnow;
                _screenEffects.enabled = _originalScreenEffectsEnabled;
            }
            DestroyVolumeStack(_volumeStack);
            if (_cameraData != null)
            {
                if (_volumeStack != null && _cameraData.volumeStack == _volumeStack)
                {
                    _cameraData.volumeStack = _originalVolumeStack?.isValid == true ? _originalVolumeStack : null;
                    _camera.SetVolumeFrameworkUpdateMode(_originalVolumeUpdateMode);
                }
                _cameraData.renderPostProcessing = _originalPostProcessing;
            }
            if (_camera != null) _camera.allowHDR = _originalHdr;
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
                    SetColorGrading(false);
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
                    SetColorGrading(true);
                    break;
                }
            }

            _currentAppliedEffectMode = command.Mode;
            UpdateCameraPostProcessing();
        }

        private void SetColorGrading(bool enabled)
        {
            if (_gradingEnabled == enabled) return;
            _gradingEnabled = enabled;
            _volumeDirty = true;
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