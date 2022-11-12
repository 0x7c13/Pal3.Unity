// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect.PostProcessing
{
    using System;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Utils;
    using UnityEngine;
    using UnityEngine.Rendering.PostProcessing;

    public sealed class PostProcessManager : MonoBehaviour,
        ICommandExecutor<EffectSetScreenEffectCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private PostProcessVolume _postProcessVolume;
        private PostProcessLayer _postProcessLayer;

        private Bloom _bloom;
        private AmbientOcclusion _ambientOcclusion;
        private ColorGrading _colorGrading;
        private Vignette _vignette;
        private Distortion _distortion;

        private int _currentAppliedEffectMode = -1;

        public void Init(PostProcessVolume volume,
            PostProcessLayer postProcessLayer)
        {
            _postProcessVolume = volume != null ? volume : throw new ArgumentNullException(nameof(volume));
            _postProcessLayer = postProcessLayer != null ? postProcessLayer : throw new ArgumentNullException(nameof(postProcessLayer));

            _bloom = _postProcessVolume.profile.GetSetting<Bloom>();
            #if RTX_ON
            _bloom.active = false; // Pointless when lighting is on
            #else
            _bloom.active = Utility.IsDesktopDevice(); // Enable bloom for better VFX visual fidelity on desktop devices   
            #endif

            _ambientOcclusion = _postProcessVolume.profile.GetSetting<AmbientOcclusion>();
            #if RTX_ON && !UNITY_ANDROID // AO not working well with OpenGL on Android
            _ambientOcclusion.active = Utility.IsDesktopDevice(); // Enable AmbientOcclusion for better visual fidelity on desktop devices   
            #else
            _ambientOcclusion.active = false; // Pointless when lighting is off
            #endif

            _colorGrading = _postProcessVolume.profile.GetSetting<ColorGrading>();
            _colorGrading.active = false;

            _vignette = _postProcessVolume.profile.GetSetting<Vignette>();
            _vignette.active = false;

            _distortion = _postProcessVolume.profile.GetSetting<Distortion>();
            _distortion.active = false;

            TogglePostProcessLayerWhenNeeded();
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
    }
}