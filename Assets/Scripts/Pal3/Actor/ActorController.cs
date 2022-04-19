// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using System;
    using System.Collections;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Renderer;
    using Data;
    using Input;
    using MetaData;
    using Script.Waiter;
    using UnityEngine;

    public class ActorController : MonoBehaviour,
        ICommandExecutor<ActorSetFacingDirectionCommand>,
        ICommandExecutor<ActorRotateFacingCommand>,
        ICommandExecutor<ActorRotateFacingDirectionCommand>,
        ICommandExecutor<ActorShowEmojiCommand>,
        ICommandExecutor<ActorSetScriptCommand>,
        ICommandExecutor<ActorChangeScaleCommand>
    {
        private const float EMOJI_ANIMATION_FPS = 5f;

        private GameResourceProvider _resourceProvider;
        private Actor _actor;
        private ActorActionController _actionController;
        private ActorMovementController _movementController;
        private PlayerInputActions _inputActions;

        public bool IsActive
        {
            get => _actor.IsActive;
            set
            {
                _actor.IsActive = value;
                if (value) Activate();
                else DeActivate();
            }
        }

        private ScnActorBehaviour _currentBehaviour;

        public void Init(GameResourceProvider resourceProvider,
            Actor actor,
            ActorActionController actionController,
            ActorMovementController movementController)
        {
            _resourceProvider = resourceProvider;
            _actor = actor;
            _actionController = actionController;
            _movementController = movementController;

            // Init facing direction
            transform.rotation = Quaternion.Euler(0, -_actor.Info.FacingDirection, 0);

            // Activate if InitActive == 1
            if (_actor.Info.InitActive == 1) IsActive = true;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public bool IsInteractable(float distance)
        {
            var maxInteractionDistance = _actor.GetInteractionMaxDistance();
            if (distance > maxInteractionDistance) return false;
            return _actor.Info.ScriptId != 0;
        }

        public void Interact()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new PlayerInteractionTriggeredNotification());
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunCommand((int) _actor.Info.ScriptId));
        }

        private void Activate()
        {
            if (_actor.Info.Kind == ScnActorKind.MainActor)
            {
                _actionController.PerformAction(_actor.GetIdleAction());
                return;
            }

            switch (_actor.Info.InitBehaviour)
            {
                case ScnActorBehaviour.None:
                    _currentBehaviour = ScnActorBehaviour.None;
                    _actionController.PerformAction(_actor.GetInitAction());
                    break;
                case ScnActorBehaviour.Hold:
                    _currentBehaviour = ScnActorBehaviour.Hold;
                    _actionController.PerformAction(_actor.GetInitAction());
                    break;
                case ScnActorBehaviour.Wander:
                    _currentBehaviour = ScnActorBehaviour.Wander;
                    _actionController.PerformAction(_actor.GetIdleAction());
                    break;
                case ScnActorBehaviour.PathFollow:
                    _currentBehaviour = ScnActorBehaviour.PathFollow;
                    _actionController.PerformAction(_actor.GetIdleAction());
                    break;
            }

            if (_currentBehaviour == ScnActorBehaviour.PathFollow)
            {
                _movementController.SetupPath(_actor.Info.Path.Waypoints
                        .Where(p => p != Vector3.zero)
                        .Select(waypoint => GameBoxInterpreter.ToUnityPosition(waypoint)).ToArray(),
                    0, EndOfPathActionType.Reverse);
            }
        }

        private void DeActivate()
        {
            _actionController.DeActivate();
        }

        public IEnumerator ShowEmojiAnimation(ActorEmojiType emojiType)
        {
            // For some reason, there are 12 emoji types exist in the game script,
            // but only 11 sprite sheet in the data folder.
            if (!Enum.IsDefined(typeof(ActorEmojiType), emojiType)) yield break;

            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(waiter));

            var sprites = _resourceProvider.GetEmojiSprites(emojiType);

            var emojiGameObject = new GameObject($"Emoji {emojiType.ToString()}");
            emojiGameObject.transform.SetParent(transform);
            emojiGameObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            var parentPosition = transform.position;

            var headPosition = new Vector3(parentPosition.x, _actionController.GetWorldBounds().max.y, parentPosition.z);

            emojiGameObject.transform.position = headPosition;

            var billboardRenderer = emojiGameObject.AddComponent<AnimatedBillboardRenderer>();

            yield return billboardRenderer.PlaySpriteAnimation(sprites,
                EMOJI_ANIMATION_FPS,
                ActorEmoji.EmojiAnimationLoopCount[emojiType]);

            Destroy(billboardRenderer);
            Destroy(emojiGameObject);
            waiter.CancelWait();
        }

        private IEnumerator AnimateScale(float toScale, float duration, WaitUntilCanceled waiter = null)
        {
            yield return AnimationHelper.EnumerateValue(transform.localScale.x,
                toScale, duration, AnimationCurveType.Linear, value =>
                {
                    transform.localScale = new Vector3(value, value, value);
                });
            waiter?.CancelWait();
        }

        public void Execute(ActorRotateFacingCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            #if PAL3
            var currentYAngles = gameObject.transform.rotation.eulerAngles.y;
            gameObject.transform.rotation = Quaternion.Euler(0, currentYAngles - command.Degrees, 0);
            #elif PAL3A
            gameObject.transform.rotation = Quaternion.Euler(0, - command.Degrees, 0);
            #endif
        }

        public void Execute(ActorSetFacingDirectionCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            gameObject.transform.rotation = Quaternion.Euler(0, -command.Direction * 45, 0);
        }

        public void Execute(ActorRotateFacingDirectionCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            gameObject.transform.rotation = Quaternion.Euler(0, -command.Direction * 45, 0);
        }

        public void Execute(ActorShowEmojiCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            StartCoroutine(ShowEmojiAnimation((ActorEmojiType) command.EmojiId));
        }

        public void Execute(ActorSetScriptCommand command)
        {
            if (command.ActorId != _actor.Info.Id) return;
            _actor.Info.ScriptId = (uint)command.ScriptId;
        }

        public void Execute(ActorChangeScaleCommand command)
        {
            if (command.ActorId != _actor.Info.Id) return;
            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ScriptRunnerWaitRequest(waiter));
            StartCoroutine(AnimateScale(command.Scale, 2f, waiter));
        }
    }
}