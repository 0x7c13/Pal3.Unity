// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Feature
{
    using System;
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Animation;
    using Data;
    using Input;
    using Script.Waiter;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.UI;

    public sealed class CaptionRenderer : MonoBehaviour,
        ICommandExecutor<UIDisplayCaptionCommand>
    {
        private const float CAPTION_ANIMATION_DURATION = 10f;

        private GameResourceProvider _gameResourceProvider;
        private PlayerInputActions _playerInputActions;
        private Image _captionImage;

        private WaitUntilCanceled _skipCaptionWaiter;
        private bool _skipCaptionRequested;

        public void Init(GameResourceProvider gameResourceProvider,
            PlayerInputActions playerInputActions,
            Image captionImage)
        {
            _gameResourceProvider = gameResourceProvider ?? throw new ArgumentNullException(nameof(gameResourceProvider));
            _playerInputActions = playerInputActions ?? throw new ArgumentNullException(nameof(playerInputActions));
            _captionImage = captionImage != null ? captionImage : throw new ArgumentNullException(nameof(captionImage));
            
            _captionImage.preserveAspect = true;
            _playerInputActions.Cutscene.Continue.performed += CutsceneContinueOnPerformed;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            _playerInputActions.Cutscene.Continue.performed -= CutsceneContinueOnPerformed;
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private void CutsceneContinueOnPerformed(InputAction.CallbackContext obj)
        {
            _skipCaptionRequested = true;
        }

        private IEnumerator AnimateCaption(string textureName)
        {
            _skipCaptionWaiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(_skipCaptionWaiter));

            Texture2D texture = _gameResourceProvider.GetCaptionTexture(textureName);
            var sprite = Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));

            _captionImage.sprite = sprite;
            _captionImage.color = Color.clear;

            yield return AnimationHelper.EnumerateValue(0, 1, CAPTION_ANIMATION_DURATION, AnimationCurveType.Linear,
                alpha =>
                {
                    _captionImage.color = new Color(1f, 1f, 1f, alpha);
                });

            _skipCaptionRequested = false;
            yield return new WaitUntil(() => _skipCaptionRequested);

            _captionImage.color = Color.clear;
            Destroy(texture);
            Destroy(sprite);

            _skipCaptionWaiter.CancelWait();
            _skipCaptionWaiter = null;
        }

        public void Execute(UIDisplayCaptionCommand command)
        {
            StartCoroutine(AnimateCaption(command.TextureName));
        }
    }
}