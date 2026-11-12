// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

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
        private readonly MultisampleAntiAliasing _antiAliasing;
        private readonly UniversalRenderPipelineAsset _pipeline;
        private readonly int _originalAdditionalShadowAtlasResolution;
        private bool _disposed;

        public RenderingSettingsManager(GameSettings gameSettings)
        {
            _gameSettings = Requires.IsNotNull(gameSettings, nameof(gameSettings));

            _pipeline = UniversalRenderPipeline.asset;
            _originalAdditionalShadowAtlasResolution = _pipeline.additionalLightsShadowmapResolution;
            ApplyShadowAtlasBudget();
            _antiAliasing = new MultisampleAntiAliasing(_gameSettings.AntiAliasing);
            _mainDisplayWidth = Display.main.systemWidth;
            _mainDisplayHeight = Display.main.systemHeight;

            ApplyCurrentSettings(_gameSettings);

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void ApplyCurrentSettings(GameSettings settings)
        {
            QualitySettings.vSyncCount = settings.VSyncCount;
            Application.targetFrameRate = settings.TargetFrameRate;
            ApplyResolutionScale(settings.ResolutionScale);
        }

        private void ApplyShadowAtlasBudget()
        {
            // Each point-light shadow needs six tiles at the fire lights' Low tier.
            int tilesPerAxis = Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(_gameSettings.ToonFireShadowBudget * 6)));
            int requiredResolution = tilesPerAxis * _pipeline.additionalLightsShadowResolutionTierLow;
            _pipeline.additionalLightsShadowmapResolution = Math.Max(_originalAdditionalShadowAtlasResolution, requiredResolution);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            _antiAliasing.Dispose();
            if (_pipeline != null) _pipeline.additionalLightsShadowmapResolution = _originalAdditionalShadowAtlasResolution;
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
            if (command.SettingName == nameof(_gameSettings.ToonFireShadowBudget))
            {
                ApplyShadowAtlasBudget();
            }
            else if (command.SettingName == nameof(_gameSettings.VSyncCount))
            {
                QualitySettings.vSyncCount = _gameSettings.VSyncCount;
            }
            else if (command.SettingName == nameof(_gameSettings.AntiAliasing))
            {
                _antiAliasing.Apply(_gameSettings.AntiAliasing);
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