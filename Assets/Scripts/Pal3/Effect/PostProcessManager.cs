﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
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
        
        private int _currentAppliedEffectMode = -1;
        
        public void Init(PostProcessVolume volume,
            PostProcessLayer postProcessLayer)
        {
            _postProcessVolume = volume != null ? volume : throw new ArgumentNullException(nameof(volume));
            _postProcessLayer = postProcessLayer != null ? postProcessLayer : throw new ArgumentNullException(nameof(postProcessLayer));

            _bloom = _postProcessVolume.profile.GetSetting<Bloom>();
            _bloom.active = Utility.IsDesktopDevice(); // Enable bloom for better VFX visual fidelity on desktop devices   

            _ambientOcclusion = _postProcessVolume.profile.GetSetting<AmbientOcclusion>();
            #if RTX_ON
            _ambientOcclusion.active = Utility.IsDesktopDevice(); // Enable AmbientOcclusion for better visual fidelity on desktop devices   
            #else
            _ambientOcclusion.active = false;
            #endif
            
            _colorGrading = _postProcessVolume.profile.GetSetting<ColorGrading>();
            _colorGrading.active = false;

            _vignette = _postProcessVolume.profile.GetSetting<Vignette>();
            _vignette.active = false;

            TogglePostProcessLayerWhenNeeded();
        }

        private void TogglePostProcessLayerWhenNeeded()
        {
            if (_bloom.active ||
                _ambientOcclusion.active ||
                _colorGrading.active ||
                _vignette.active)
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
                // Disable all post-processing effects
                case -1:
                {
                    _colorGrading.active = false;
                    _vignette.active = false;
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