// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Effect
{
    using Command;
    using Command.SceCommands;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    public class PostProcessManager : MonoBehaviour, ICommandExecutor<EffectSetScreenEffectCommand>
    {
        private Volume _globalVolume;

        public void Init(Volume volume)
        {
            _globalVolume = volume;
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
                // Remove all post-processing effects
                case -1:
                {
                    if (_globalVolume.profile.TryGet(out ColorAdjustments colorAdjustments))
                    {
                        colorAdjustments.active = false;
                    }
                    if (_globalVolume.profile.TryGet(out Vignette vignette))
                    {
                        vignette.active = false;
                    }
                    break;
                }
                // Vintage + color filter effect
                case 1:
                {
                    if (_globalVolume.profile.TryGet(out ColorAdjustments colorAdjustments))
                    {
                        colorAdjustments.active = true;
                    }
                    if (_globalVolume.profile.TryGet(out Vignette vignette))
                    {
                        vignette.active = true;
                    }
                    break;
                }
            }
        }
    }
}