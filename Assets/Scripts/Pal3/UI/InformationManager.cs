// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.UI
{
    using System;
    using System.Collections;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Animation;
    using Core.Utils;
    using MetaData;
    using TMPro;
    using UnityEngine;

    [RequireComponent(typeof(FpsCounter))]
    public sealed class InformationManager : MonoBehaviour,
        ICommandExecutor<UIDisplayNoteCommand>,
        ICommandExecutor<UIShowDealerMenuCommand>,
        ICommandExecutor<SceneLeavingCurrentSceneNotification>
    {
        private const float NOTE_LAST_TIME_IN_SECONDS = 2f;
        private const float NOTE_DISAPPEAR_ANIMATION_TIME_IN_SECONDS = 1f;

        private CanvasGroup _noteCanvasGroup;
        private TextMeshProUGUI _noteText;
        private Coroutine _noteAnimation;
        private TextMeshProUGUI _debugInfo;

        private FpsCounter _fpsCounter;
        private string _debugInfoStringFormat;

        private double _heapSizeLastQueryTime;
        private float _heapSize;

        private bool _isNoteShowingEnabled = true;

        public void Init(CanvasGroup noteCanvasGroup, TextMeshProUGUI noteText, TextMeshProUGUI debugInfo)
        {
            _noteCanvasGroup = noteCanvasGroup != null ? noteCanvasGroup : throw new ArgumentNullException(nameof(noteCanvasGroup));
            _noteText = noteText != null ? noteText : throw new ArgumentNullException(nameof(noteText));
            _debugInfo = debugInfo != null ? debugInfo : throw new ArgumentNullException(nameof(debugInfo));

            _noteCanvasGroup.alpha = 0f;
            _heapSize = GC.GetTotalMemory(false) / (1024f * 1024f);
            _heapSizeLastQueryTime = Time.realtimeSinceStartupAsDouble;
        }

        private void OnEnable()
        {
            var deviceInfo =
                $"Device: {SystemInfo.deviceModel.Trim()} OS: {SystemInfo.operatingSystem.Trim()}\n" +
                $"CPU: {SystemInfo.processorType.Trim()} ({SystemInfo.processorCount} vCores)\n" +
                $"GPU: {SystemInfo.graphicsDeviceName.Trim()} ({SystemInfo.graphicsDeviceType})\n" +
                $"RAM: {SystemInfo.systemMemorySize / 1024f:0.0} GB VRAM: {SystemInfo.graphicsMemorySize / 1024f:0.0} GB\n" +
                $"{GameConstants.ContactInfo}\n" +
                $"Version: Alpha v{Application.version}\n";

            _debugInfoStringFormat = deviceInfo + "Heap size: {0:0.00} MB\n" + "{3:0.} fps ({1}x{2})";

            _fpsCounter = GetComponent<FpsCounter>();

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            if (_noteAnimation != null) StopCoroutine(_noteAnimation);
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private void Update()
        {
            var currentTime = Time.realtimeSinceStartupAsDouble;
            if (currentTime - _heapSizeLastQueryTime > 5f)
            {
                _heapSize = GC.GetTotalMemory(false) / (1024f * 1024f);
                _heapSizeLastQueryTime = currentTime;
            }
            _debugInfo.SetText(_debugInfoStringFormat,
                _heapSize,
                Screen.width,
                Screen.height,
                Mathf.Ceil(_fpsCounter.GetFps()));
        }

        private IEnumerator AnimateNoteUIAsync()
        {
            _noteCanvasGroup.alpha = 1f;
            yield return new WaitForSeconds(NOTE_LAST_TIME_IN_SECONDS);
            yield return AnimationHelper.EnumerateValueAsync(
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
                    StopCoroutine(_noteAnimation);
                }

                if (!string.Equals(currentText, command.Note) &&
                    !string.Equals(currentText.Split('\n').Last(), command.Note))
                {
                    _noteText.text = currentText + '\n' + command.Note;
                }
            }
            else
            {
                _noteText.text = command.Note;
            }

            _noteAnimation = StartCoroutine(AnimateNoteUIAsync());
        }

        public void Execute(SceneLeavingCurrentSceneNotification command)
        {
            if (_noteAnimation != null)
            {
                StopCoroutine(_noteAnimation);
            }
            _noteCanvasGroup.alpha = 0f;
            _noteText.text = string.Empty;
        }

        // TODO: Remove this
        public void Execute(UIShowDealerMenuCommand command)
        {
            Execute(new UIDisplayNoteCommand("交易功能暂未开启"));
        }
    }
}