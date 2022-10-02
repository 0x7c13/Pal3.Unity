// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
{
    using System;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using UnityEngine;
    using UnityEngine.Rendering.PostProcessing;

    public sealed class PostProcessManager : MonoBehaviour,
        ICommandExecutor<EffectSetScreenEffectCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private PostProcessVolume _postProcessVolume;
        private PostProcessLayer _postProcessLayer;

        private int _currentAppliedEffectMode = -1;
        
        public void Init(PostProcessVolume volume,
            PostProcessLayer postProcessLayer)
        {
            _postProcessVolume = volume != null ? volume : throw new ArgumentNullException(nameof(volume));
            _postProcessLayer = postProcessLayer != null ? postProcessLayer : throw new ArgumentNullException(nameof(postProcessLayer));
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
                    _postProcessLayer.enabled = false;
                    if (_postProcessVolume.profile.TryGetSettings(out ColorGrading colorAdjustments))
                    {
                        colorAdjustments.active = false;
                    }
                    if (_postProcessVolume.profile.TryGetSettings(out Vignette vignette))
                    {
                        vignette.active = false;
                    }
                    break;
                }
                // Vintage + color filter effect
                case 1:
                {
                    if (_postProcessVolume.profile.TryGetSettings(out ColorGrading colorAdjustments))
                    {
                        colorAdjustments.active = true;
                    }
                    if (_postProcessVolume.profile.TryGetSettings(out Vignette vignette))
                    {
                        vignette.active = true;
                    }
                    _postProcessLayer.enabled = true;
                    break;
                }
            }

            _currentAppliedEffectMode = command.Mode;
        }

        public void Execute(ResetGameStateCommand command)
        {
            Execute(new EffectSetScreenEffectCommand(-1));
        }
    }
}