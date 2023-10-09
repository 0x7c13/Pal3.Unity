// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GameSystems.Caption
{
    using System;
    using System.Collections;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Utilities;
    using Data;
    using Engine.Animation;
    using Engine.Coroutine;
    using Engine.Extensions;
    using Input;
    using Script.Waiter;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.UI;

    public sealed class CaptionRenderer : IDisposable,
        #if PAL3A
        ICommandExecutor<UIDisplayNextCaptionCommand>,
        #endif
        ICommandExecutor<UIDisplayCaptionCommand>
    {
        private const float CAPTION_ANIMATION_DURATION = 10f;

        private readonly GameResourceProvider _gameResourceProvider;
        private readonly PlayerInputActions _playerInputActions;
        private readonly Image _captionImage;

        private WaitUntilCanceled _skipCaptionWaiter;
        private bool _skipCaptionRequested;

        public CaptionRenderer(GameResourceProvider gameResourceProvider,
            PlayerInputActions playerInputActions,
            Image captionImage)
        {
            _gameResourceProvider = Requires.IsNotNull(gameResourceProvider, nameof(gameResourceProvider));
            _playerInputActions = Requires.IsNotNull(playerInputActions, nameof(playerInputActions));
            _captionImage = Requires.IsNotNull(captionImage, nameof(captionImage));

            _captionImage.preserveAspect = true;
            _playerInputActions.Cutscene.Continue.performed += CutsceneContinueOnPerformed;

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            _playerInputActions.Cutscene.Continue.performed -= CutsceneContinueOnPerformed;
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private void CutsceneContinueOnPerformed(InputAction.CallbackContext obj)
        {
            _skipCaptionRequested = true;
        }

        private IEnumerator AnimateCaptionAsync(string textureName)
        {
            _skipCaptionWaiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ScriptRunnerAddWaiterRequest(_skipCaptionWaiter));

            Texture2D texture = _gameResourceProvider.GetCaptionTexture(textureName);
            var sprite = Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));

            _captionImage.sprite = sprite;
            _captionImage.color = Color.clear;

            yield return CoreAnimation.EnumerateValueAsync(0, 1, CAPTION_ANIMATION_DURATION,
                AnimationCurveType.Linear, alpha =>
                {
                    _captionImage.color = new Color(1f, 1f, 1f, alpha);
                });

            _skipCaptionRequested = false;
            yield return CoroutineYieldInstruction.WaitUntil(() => _skipCaptionRequested);

            _captionImage.color = Color.clear;
            texture.Destroy();
            sprite.Destroy();

            _skipCaptionWaiter.CancelWait();
            _skipCaptionWaiter = null;
        }

        public void Execute(UIDisplayCaptionCommand command)
        {
            Pal3.Instance.StartCoroutine(AnimateCaptionAsync(command.TextureName));
        }

        #if PAL3A
        public void Execute(UIDisplayNextCaptionCommand command)
        {
            Pal3.Instance.StartCoroutine(AnimateCaptionAsync(command.TextureName));
        }
        #endif
    }
}