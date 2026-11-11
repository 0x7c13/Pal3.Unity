// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Settings
{
    using System;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Utilities;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    public sealed class RenderingSettingsManager : IDisposable,
        ICommandExecutor<SettingChangedNotification>
    {
        private int _mainDisplayWidth;
        private int _mainDisplayHeight;

        private readonly GameSettings _gameSettings;
        private readonly UniversalRenderPipelineAsset _pipeline;
        private readonly int _initialMsaaSampleCount;

        public RenderingSettingsManager(GameSettings gameSettings)
        {
            _gameSettings = Requires.IsNotNull(gameSettings, nameof(gameSettings));

            _pipeline = UniversalRenderPipeline.asset;
            _initialMsaaSampleCount = _pipeline.msaaSampleCount;
            _mainDisplayWidth = Display.main.systemWidth;
            _mainDisplayHeight = Display.main.systemHeight;

            ApplyCurrentSettings(_gameSettings);

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void ApplyCurrentSettings(GameSettings settings)
        {
            QualitySettings.vSyncCount = settings.VSyncCount;
            ApplyAntiAliasing(settings.AntiAliasing);
            Application.targetFrameRate = settings.TargetFrameRate;
            ApplyResolutionScale(settings.ResolutionScale);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            _pipeline.msaaSampleCount = _initialMsaaSampleCount;
        }

        public void Update(float deltaTime)
        {
            // This could happen on Android foldable devices
            if (_mainDisplayWidth != Display.main.systemWidth ||
                _mainDisplayHeight != Display.main.systemHeight)
            {
                _mainDisplayWidth = Display.main.systemWidth;
                _mainDisplayHeight = Display.main.systemHeight;
                OnResolutionChanged();
            }
        }

        private void OnResolutionChanged()
        {
            ApplyResolutionScale(_gameSettings.ResolutionScale);
        }

        private void ApplyAntiAliasing(int samples)
        {
            _pipeline.msaaSampleCount = samples is 2 or 4 or 8 ? samples : 1;
        }

        private static void ApplyResolutionScale(float scale)
        {
            #if UNITY_IOS || UNITY_ANDROID
            Screen.SetResolution(
                (int) (Display.main.systemWidth * scale),
                (int) (Display.main.systemHeight * scale),
                true);
            #endif
        }

        public void Execute(SettingChangedNotification command)
        {
            if (command.SettingName == nameof(_gameSettings.VSyncCount))
            {
                QualitySettings.vSyncCount = _gameSettings.VSyncCount;
            }
            else if (command.SettingName == nameof(_gameSettings.AntiAliasing))
            {
                ApplyAntiAliasing(_gameSettings.AntiAliasing);
            }
            else if (command.SettingName == nameof(_gameSettings.TargetFrameRate))
            {
                Application.targetFrameRate = _gameSettings.TargetFrameRate;
            }
            else if (command.SettingName == nameof(_gameSettings.ResolutionScale))
            {
                ApplyResolutionScale(_gameSettings.ResolutionScale);
            }
        }
    }
}