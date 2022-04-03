// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
{
    using Command;
    using Command.SceCommands;
    using UnityEngine;
    using UnityEngine.Rendering.PostProcessing;

    public class PostProcessManager : MonoBehaviour, ICommandExecutor<EffectSetScreenEffectCommand>
    {
        private PostProcessVolume _postProcessVolume;

        public void Init(PostProcessVolume  volume)
        {
            _postProcessVolume = volume;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(EffectSetScreenEffectCommand command)
        {
            switch (command.Mode)
            {
                // Disable all post-processing effects
                case -1:
                {
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
                    break;
                }
            }
        }
    }
}