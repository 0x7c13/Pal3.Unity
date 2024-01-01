// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Video
{
    using System;
    using System.Collections;
    using System.IO;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Utilities;
    using Data;
    using Engine.Logging;
    using Input;
    using Script.Waiter;
    using State;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.Video;

    public sealed class VideoManager : IDisposable,
        ICommandExecutor<PlayVideoCommand>
    {
        private readonly GameResourceProvider _resourceProvider;
        private readonly GameStateManager _gameStateManager;
        private readonly PlayerInputActions _inputActions;
        private readonly VideoPlayer _videoPlayer;
        private readonly Canvas _videoPlayerUI;

        private WaitUntilCanceled _videoPlayingWaiter;

        public VideoManager(GameResourceProvider resourceProvider,
            GameStateManager gameStateManager,
            PlayerInputActions inputActions,
            Canvas videoPlayerUI,
            VideoPlayer videoPlayer)
        {
            _resourceProvider = Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _gameStateManager = Requires.IsNotNull(gameStateManager, nameof(gameStateManager));
            _inputActions = Requires.IsNotNull(inputActions, nameof(inputActions));
            _videoPlayerUI = Requires.IsNotNull(videoPlayerUI, nameof(videoPlayerUI));
            _videoPlayer = Requires.IsNotNull(videoPlayer, nameof(videoPlayer));

            _videoPlayerUI.enabled = false;
            _videoPlayer.loopPointReached += StopVideoInternal;
            _inputActions.VideoPlaying.SkipVideo.performed += SkipVideoPerformed;

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

            _inputActions.VideoPlaying.SkipVideo.performed -= SkipVideoPerformed;
            _videoPlayer.loopPointReached -= StopVideoInternal;

            // In case video is still playing
            _videoPlayer.Stop();
            _videoPlayer.targetTexture.Release();
            _videoPlayingWaiter?.CancelWait();
            _videoPlayerUI.enabled = false;
        }

        private void SkipVideoPerformed(InputAction.CallbackContext _)
        {
            StopVideoInternal(_videoPlayer);
        }

        private IEnumerator PlayAsync(string videoFilePath)
        {
            _videoPlayer.waitForFirstFrame = true;
            _videoPlayer.url = videoFilePath;
            _videoPlayer.frame = 0;
            _videoPlayer.isLooping = false;
            _videoPlayer.Prepare();

            while (!_videoPlayer.isPrepared)
            {
                yield return null;
            }

            _videoPlayer.Play();
            _videoPlayerUI.enabled = true;
        }

        private void StopVideoInternal(VideoPlayer source)
        {
            if (_gameStateManager.GetCurrentState() == GameState.VideoPlaying)
            {
                source.Stop();
                source.targetTexture.Release();
                _videoPlayingWaiter?.CancelWait();
                _videoPlayerUI.enabled = false;
                _gameStateManager.GoToPreviousState();
            }
        }

        public void Execute(PlayVideoCommand command)
        {
            _gameStateManager.TryGoToState(GameState.VideoPlaying);

            _videoPlayingWaiter?.CancelWait();
            _videoPlayingWaiter = new WaitUntilCanceled();
            Pal3.Instance.Execute(new ScriptRunnerAddWaiterRequest(_videoPlayingWaiter));

            try
            {
                string videoFilePath = _resourceProvider.GetVideoFilePath(command.Name);
                Pal3.Instance.StartCoroutine(PlayAsync(videoFilePath));
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException or DirectoryNotFoundException)
                {
                    Pal3.Instance.Execute(new UIDisplayNoteCommand("未找到过场动画文件，动画已跳过"));
                }
                EngineLogger.LogException(ex);
                _videoPlayingWaiter.CancelWait();
                _gameStateManager.GoToPreviousState();
            }
        }
    }
}