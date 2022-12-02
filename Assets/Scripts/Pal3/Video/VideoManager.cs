// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
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
            _resourceProvider = resourceProvider ?? throw new ArgumentNullException(nameof(resourceProvider));
            _inputActions = inputActions ?? throw new ArgumentNullException(nameof(inputActions));
            _videoPlayerUI = videoPlayerUI != null ? videoPlayerUI : throw new ArgumentNullException(nameof(videoPlayerUI));
            _videoPlayer = videoPlayer != null ? videoPlayer : throw new ArgumentNullException(nameof(videoPlayer));

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
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(_videoPlayingWaiter));

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