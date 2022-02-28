// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Video
{
    using System;
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Data;
    using Input;
    using Script.Waiter;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.Video;
    using Object = UnityEngine.Object;

    public class VideoManager : MonoBehaviour,
        ICommandExecutor<PlayVideoCommand>
    {
        private GameResourceProvider _resourceProvider;
        private PlayerInputActions _inputActions;
        private VideoPlayer _videoPlayer;
        private WaitUntilCanceled _videoPlayingWaiter;
        private Canvas _videoPlayerUI;

        public void Init(GameResourceProvider resourceProvider,
            PlayerInputActions inputActions,
            Canvas videoPlayerUI,
            VideoPlayer videoPlayer)
        {
            _resourceProvider = resourceProvider;
            _inputActions = inputActions;

            _videoPlayerUI = videoPlayerUI;
            _videoPlayerUI.enabled = false;

            _videoPlayer = videoPlayer;
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
            StopVideoInternal(_videoPlayer); // In case video is still playing
        }

        private void SkipVideoPerformed(InputAction.CallbackContext obj)
        {
            StopVideoInternal(_videoPlayer);
        }

        public IEnumerator Play(string videoFilePath)
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
            source.Stop();
            source.targetTexture.Release();
            _videoPlayingWaiter?.CancelWait();
            _videoPlayerUI.enabled = false;
            CommandDispatcher<ICommand>.Instance.Dispatch(new VideoEndedNotification());
        }

        public void Execute(PlayVideoCommand command)
        {
            _videoPlayingWaiter?.CancelWait();
            _videoPlayingWaiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(_videoPlayingWaiter));

            try
            {
                var videoFilePath = _resourceProvider.GetVideoFilePath(command.Name);
                StartCoroutine(Play(videoFilePath));
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                _videoPlayingWaiter.CancelWait();
                CommandDispatcher<ICommand>.Instance.Dispatch(new VideoEndedNotification());
            }
        }
    }
}