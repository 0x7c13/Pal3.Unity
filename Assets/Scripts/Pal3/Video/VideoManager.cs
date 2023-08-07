// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Video
{
    using System;
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Utils;
    using Data;
    using Input;
    using Script.Waiter;
    using State;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.Video;

    public class VideoManager : MonoBehaviour,
        ICommandExecutor<PlayVideoCommand>
    {
        private GameResourceProvider _resourceProvider;
        private GameStateManager _gameStateManager;
        private PlayerInputActions _inputActions;
        private VideoPlayer _videoPlayer;
        private WaitUntilCanceled _videoPlayingWaiter;
        private Canvas _videoPlayerUI;

        public void Init(GameResourceProvider resourceProvider,
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
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
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

        public IEnumerator PlayAsync(string videoFilePath)
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
            _gameStateManager.GoToState(GameState.VideoPlaying);

            _videoPlayingWaiter?.CancelWait();
            _videoPlayingWaiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(_videoPlayingWaiter));

            try
            {
                var videoFilePath = _resourceProvider.GetVideoFilePath(command.Name);
                StartCoroutine(PlayAsync(videoFilePath));
            }
            catch (Exception ex)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("未找到过场动画文件，动画已跳过"));
                Debug.LogError($"[{nameof(VideoManager)}] Exception: {ex}");
                _videoPlayingWaiter.CancelWait();
                _gameStateManager.GoToPreviousState();
            }
        }
    }
}