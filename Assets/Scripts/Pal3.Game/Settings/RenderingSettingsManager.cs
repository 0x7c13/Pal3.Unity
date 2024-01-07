// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
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

    public sealed class RenderingSettingsManager : IDisposable,
        ICommandExecutor<SettingChangedNotification>
    {
        private int _mainDisplayWidth;
        private int _mainDisplayHeight;

        private readonly GameSettings _gameSettings;

        public RenderingSettingsManager(GameSettings gameSettings)
        {
            _gameSettings = Requires.IsNotNull(gameSettings, nameof(gameSettings));

            _mainDisplayWidth = Display.main.systemWidth;
            _mainDisplayHeight = Display.main.systemHeight;

            ApplyCurrentSettings(_gameSettings);

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void ApplyCurrentSettings(GameSettings settings)
        {
            QualitySettings.vSyncCount = settings.VSyncCount;
            QualitySettings.antiAliasing = settings.AntiAliasing;
            Application.targetFrameRate = settings.TargetFrameRate;
            ApplyResolutionScale(settings.ResolutionScale);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
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
                QualitySettings.antiAliasing = _gameSettings.AntiAliasing;
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