// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.UI
{
    using System;
    using System.Collections;
    using Command;
    using Command.SceCommands;
    using Core.Animation;
    using Core.Utils;
    using MetaData;
    using TMPro;
    using UnityEngine;

    [RequireComponent(typeof(FpsCounter))]
    public class InformationManager : MonoBehaviour,
        ICommandExecutor<UIDisplayNoteCommand>
    {
        private const float NOTE_LAST_TIME_IN_SECONDS = 2f;
        private const float NOTE_DISAPPEAR_ANIMATION_TIME_IN_SECONDS = 1f;

        private CanvasGroup _noteCanvasGroup;
        private TextMeshProUGUI _noteText;
        private Coroutine _noteAnimation;
        private TextMeshProUGUI _debugInfo;

        private FpsCounter _fpsCounter;
        private string _deviceInfo;

        private double _heapSizeLastQueryTime;
        private float _heapSize;

        public void Init(CanvasGroup noteCanvasGroup, TextMeshProUGUI noteText, TextMeshProUGUI debugInfo)
        {
            _noteCanvasGroup = noteCanvasGroup;
            _noteCanvasGroup.alpha = 0f;
            _noteText = noteText;
            _debugInfo = debugInfo;

            _heapSize = GC.GetTotalMemory(false) / (1024f * 1024f);
            _heapSizeLastQueryTime = Time.realtimeSinceStartupAsDouble;
        }

        private void OnEnable()
        {
            _deviceInfo = $"Device: {SystemInfo.deviceModel.Trim()} OS: {SystemInfo.operatingSystem.Trim()}\n" +
                          $"CPU: {SystemInfo.processorType.Trim()} GPU: {SystemInfo.graphicsDeviceName.Trim()}\n" +
                          $"RAM: {SystemInfo.systemMemorySize / 1024f:0.0} GB VRAM: {SystemInfo.graphicsMemorySize / 1024f:0.0} GB\n" +
                          $"{GameConstants.ContactInfo}\n" +
                          $"Version: Alpha v{GameConstants.Version}";

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
            _debugInfo.text = $"{_deviceInfo}\n" +
                              $"Heap size: {_heapSize:0.00} MB" +
                              $"\n{_fpsCounter.GetFps():0.} fps";
        }

        private IEnumerator AnimateNoteUI()
        {
            _noteCanvasGroup.alpha = 1f;
            yield return new WaitForSeconds(NOTE_LAST_TIME_IN_SECONDS);
            yield return AnimationHelper.EnumerateValue(
                1f, 0f, NOTE_DISAPPEAR_ANIMATION_TIME_IN_SECONDS, AnimationCurveType.Linear,
                alpha =>
                {
                    _noteCanvasGroup.alpha = alpha;
                });
            _noteCanvasGroup.alpha = 0f;
        }

        public void Execute(UIDisplayNoteCommand command)
        {
            if (_noteAnimation != null) StopCoroutine(_noteAnimation);
            _noteText.text = command.Note;
            _noteAnimation = StartCoroutine(AnimateNoteUI());
        }
    }
}