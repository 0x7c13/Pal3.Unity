// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.UI
{
    using System;
    using System.Collections;
    using Command;
    using Command.Extensions;
    using Constants;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Utilities;
    using Engine.Animation;
    using Engine.Coroutine;
    using Engine.Services;
    using Engine.Utilities;
    using Settings;
    using State;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Profiling;

    public sealed class InformationManager : IDisposable,
        ICommandExecutor<UIDisplayNoteCommand>,
        ICommandExecutor<SceneLeavingCurrentSceneNotification>,
        ICommandExecutor<SettingChangedNotification>,
        ICommandExecutor<GameStateChangedNotification>
    {
        private const float NOTE_LAST_TIME_IN_SECONDS = 2.5f;
        private const float NOTE_DISAPPEAR_ANIMATION_TIME_IN_SECONDS = 1f;

        private readonly IGameTimeProvider _gameTimeProvider;
        private readonly GameSettings _gameSettings;
        private readonly CanvasGroup _noteCanvasGroup;
        private readonly TextMeshProUGUI _noteText;
        private readonly TextMeshProUGUI _debugInfo;

        private readonly FpsCounter _fpsCounter;
        private readonly string _debugInfoStringFormat;

        private Coroutine _noteAnimation;

        private double _heapSizeLastQueryTime;
        private float _heapSize;
        private float _totalAllocatedMemorySize;
        private float _totalReservedMemorySize;

        private bool _isNoteShowingEnabled = true;

        private readonly string _defaultText;

        public InformationManager(IGameTimeProvider gameTimeProvider,
            GameSettings gameSettings,
            FpsCounter fpsCounter,
            CanvasGroup noteCanvasGroup,
            TextMeshProUGUI noteText,
            TextMeshProUGUI debugInfo)
        {
            _gameTimeProvider = Requires.IsNotNull(gameTimeProvider, nameof(gameTimeProvider));
            _gameSettings = Requires.IsNotNull(gameSettings, nameof(gameSettings));
            _fpsCounter = Requires.IsNotNull(fpsCounter, nameof(fpsCounter));
            _noteCanvasGroup = Requires.IsNotNull(noteCanvasGroup, nameof(noteCanvasGroup));
            _noteText = Requires.IsNotNull(noteText, nameof(noteText));
            _debugInfo = Requires.IsNotNull(debugInfo, nameof(debugInfo));

            _noteCanvasGroup.alpha = 0f;
            _heapSize = GC.GetTotalMemory(false) / (1024f * 1024f);
            _heapSizeLastQueryTime = gameTimeProvider.RealTimeSinceStartup;

            #if ENABLE_IL2CPP
            const string scriptingBackend = "IL2CPP";
            #else
            const string scriptingBackend = "Mono";
            #endif

            _defaultText = $"{GameConstants.ContactInfo} v{Application.version} {GameConstants.TestingType} ({Application.platform}-{scriptingBackend})\n";
            _debugInfo.SetText(_defaultText);

            #if UNITY_2022_1_OR_NEWER
            var refreshRate = (int)Screen.currentResolution.refreshRateRatio.value;
            #else
            var refreshRate = Screen.currentResolution.refreshRate;
            #endif

            var deviceInfo =
                $"Device: {SystemInfo.deviceModel.Trim()} OS: {SystemInfo.operatingSystem.Trim()}\n" +
                $"CPU: {SystemInfo.processorType.Trim()} ({SystemInfo.processorCount} vCores) RAM: {SystemInfo.systemMemorySize / 1024f:0.0} GB\n" +
                $"GPU: {SystemInfo.graphicsDeviceName.Trim()} ({SystemInfo.graphicsDeviceType}) VRAM: {SystemInfo.graphicsMemorySize / 1024f:0.0} GB\n";

            _debugInfoStringFormat = _defaultText + deviceInfo +
                                     "Heap: {0:0.} MB Allocated: {1:0.} MB Reserved: {2:0.} MB\n" +
                                     "{3:0.} fps ({4}x{5}, " + refreshRate + "Hz)";

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            if (_noteAnimation != null) Pal3.Instance.StopCoroutine(_noteAnimation);
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Update(float deltaTime)
        {
            if (!_gameSettings.IsDebugInfoEnabled) return;

            var currentTime = _gameTimeProvider.RealTimeSinceStartup;
            if (currentTime - _heapSizeLastQueryTime > 5f)
            {
                _heapSize = GC.GetTotalMemory(false) / (1024f * 1024f);
                _totalAllocatedMemorySize = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
                _totalReservedMemorySize = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
                _heapSizeLastQueryTime = currentTime;
            }
            _debugInfo.SetText(_debugInfoStringFormat,
                _heapSize,
                _totalAllocatedMemorySize,
                _totalReservedMemorySize,
                MathF.Ceiling(_fpsCounter.GetFps()),
                Screen.width,
                Screen.height);
        }

        private IEnumerator AnimateNoteUIAsync()
        {
            _noteCanvasGroup.alpha = 1f;
            yield return CoroutineYieldInstruction.WaitForSeconds(NOTE_LAST_TIME_IN_SECONDS);
            yield return CoreAnimation.EnumerateValueAsync(
                1f, 0f, NOTE_DISAPPEAR_ANIMATION_TIME_IN_SECONDS, AnimationCurveType.Linear,
                alpha =>
                {
                    _noteCanvasGroup.alpha = alpha;
                });
            _noteCanvasGroup.alpha = 0f;
        }

        public void EnableNoteDisplay(bool enable)
        {
            _isNoteShowingEnabled = enable;
        }

        public void Execute(UIDisplayNoteCommand command)
        {
            if (!_isNoteShowingEnabled) return;

            if (_noteCanvasGroup.alpha > 0f)
            {
                var currentText = _noteText.text;
                if (_noteAnimation != null)
                {
                    Pal3.Instance.StopCoroutine(_noteAnimation);
                }

                if (!string.Equals(currentText, command.Note) &&
                    !string.Equals(currentText.Split('\n')[^1], command.Note))
                {
                    _noteText.text = currentText + '\n' + command.Note;
                }
            }
            else
            {
                _noteText.text = command.Note;
            }

            _noteAnimation = Pal3.Instance.StartCoroutine(AnimateNoteUIAsync());
        }

        public void Execute(SceneLeavingCurrentSceneNotification command)
        {
            if (_noteAnimation != null)
            {
                Pal3.Instance.StopCoroutine(_noteAnimation);
            }
            _noteCanvasGroup.alpha = 0f;
            _noteText.text = string.Empty;
        }

        public void Execute(SettingChangedNotification command)
        {
            if (command.SettingName is nameof(GameSettings.IsDebugInfoEnabled))
            {
                if (!_gameSettings.IsDebugInfoEnabled)
                {
                    _debugInfo.SetText(_defaultText);
                }
            }
        }

        public void Execute(GameStateChangedNotification command)
        {
            if (command.NewState == GameState.VideoPlaying)
            {
                _debugInfo.enabled = false;
            }
            else if (command.PreviousState == GameState.VideoPlaying)
            {
                _debugInfo.enabled = true;
            }
        }
    }
}