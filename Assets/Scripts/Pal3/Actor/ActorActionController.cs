// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using System;
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Renderer;
    using Core.Services;
    using Data;
    using MetaData;
    using GamePlay;
    using Script.Waiter;
    using State;
    using UnityEngine;

    public abstract class ActorActionController : MonoBehaviour,
        ICommandExecutor<ActorStopActionAndStandCommand>,
        ICommandExecutor<ActorEnablePlayerControlCommand>,
        ICommandExecutor<ActorShowEmojiCommand>,
        #if PAL3A
        ICommandExecutor<ActorShowEmoji2Command>,
        #endif
        ICommandExecutor<GameStateChangedNotification>,
        ICommandExecutor<ActorPerformActionCommand>
    {
        private const float EMOJI_ANIMATION_FPS = 5f;
        private const float ACTOR_COLLIDER_RADIUS_MIN = 0.5f;
        private const float ACTOR_COLLIDER_RADIUS_MAX = 1.5f;

        private GameResourceProvider _resourceProvider;
        private Actor _actor;

        private bool _isDropShadowEnabled;
        private GameObject _shadow;
        private SpriteRenderer _shadowSpriteRenderer;

        private string _currentAction = string.Empty;

        private bool _hasColliderAndRigidBody;
        private Rigidbody _rigidbody;
        private CapsuleCollider _collider;

        // By default, all none-player actors are kinematic.
        // Also, the player actor is only non-kinematic during gameplay state.
        private bool _isKinematic = true;

        internal void Init(GameResourceProvider resourceProvider,
            Actor actor,
            bool hasColliderAndRigidBody,
            bool isDropShadowEnabled)
        {
            _resourceProvider = resourceProvider;
            _actor = actor;
            _hasColliderAndRigidBody = hasColliderAndRigidBody;
            _isDropShadowEnabled = isDropShadowEnabled;
        }

        public string GetCurrentAction()
        {
            return _currentAction;
        }

        public bool IsCurrentActionIdleAction()
        {
            return !string.IsNullOrEmpty(_currentAction) &&
                   string.Equals(_currentAction, _actor.GetIdleAction(), StringComparison.OrdinalIgnoreCase);
        }

        public Rigidbody GetRigidBody()
        {
            return _rigidbody;
        }

        public CapsuleCollider GetCollider()
        {
            return _collider;
        }

        public void PerformAction(ActorActionType actorActionType,
            bool overwrite = false,
            int loopCount = -1,
            WaitUntilCanceled waiter = null)
        {
            PerformAction(ActorConstants.ActionToNameMap[actorActionType], overwrite, loopCount, waiter);
        }

        public virtual void PerformAction(string actionName,
            bool overwrite = false,
            int loopCount = -1,
            WaitUntilCanceled waiter = null)
        {
            _currentAction = actionName.ToLower();

            if (_isDropShadowEnabled)
            {
                SetupShadow(_currentAction);
            }

            if (_hasColliderAndRigidBody)
            {
                SetupCollider();
                SetupRigidBody();
            }
        }

        public virtual void PauseAnimation()
        {
        }

        public virtual float GetActorHeight()
        {
            return 0f;
        }

        public virtual Bounds GetRendererBounds()
        {
            return new Bounds(transform.position, Vector3.one);
        }

        public virtual Bounds GetMeshBounds()
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        internal virtual void DisposeCurrentAction()
        {
            _currentAction = string.Empty;
        }

        internal virtual void DeActivate()
        {
            DisposeCurrentAction();

            if (_shadow != null)
            {
                Destroy(_shadow);
            }

            if (_rigidbody != null)
            {
                Destroy(_rigidbody);
            }

            if (_collider != null)
            {
                Destroy(_collider);
            }
        }

        private void SetupShadow(string actionName)
        {
            ActorActionType? actionType = ActorConstants.NameToActionMap.ContainsKey(actionName.ToLower()) ?
                ActorConstants.NameToActionMap[actionName.ToLower()] : null;

            // Disable shadow for some of the actors.
            #if PAL3
            if (_actor.Info.Id == (byte) PlayerActorId.HuaYing) return;
            #elif PAL3A
            switch (_actor.Info.Id)
            {
                case (byte) PlayerActorId.TaoZi:
                case (byte) FengYaSongActorId.Feng:
                case (byte) FengYaSongActorId.Ya:
                case (byte) FengYaSongActorId.Song:
                    return;
            }
            #endif

            // Disable shadow for some actions
            if (actionType.HasValue && ActorConstants.ActionWithoutShadow.Contains(actionType.Value))
            {
                if (_shadow != null) _shadowSpriteRenderer.enabled = false;
            }
            else
            {
                if (_shadow == null) RenderShadow();
                else _shadowSpriteRenderer.enabled = true;
            }
        }

        private void SetupCollider()
        {
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<CapsuleCollider>();
            }

            Bounds bounds = GetMeshBounds();
            _collider.center = bounds.center;

            // Add a little bit of height to make sure the bottom point of the
            // collider is below the ground. This is to make sure actor can
            // interact with StandingPlatform correctly.
            _collider.height = bounds.size.y + 1f;

            if (_actor.IsMainActor())
            {
                // Unify the radius of the collider for main actor.
                // This is to make sure the main actor can always have
                // expected behavior when interacting with standing platform.
                _collider.radius = ACTOR_COLLIDER_RADIUS_MIN;
            }
            else
            {
                _collider.radius = Mathf.Min(Mathf.Max(
                        Mathf.Sqrt(bounds.size.x * bounds.size.x + bounds.size.z * bounds.size.z) * 0.3f,
                        ACTOR_COLLIDER_RADIUS_MIN),
                    ACTOR_COLLIDER_RADIUS_MAX);
            }
        }

        private void SetupRigidBody()
        {
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
                _rigidbody.useGravity = false;
                _rigidbody.constraints = RigidbodyConstraints.FreezePositionY |
                                         RigidbodyConstraints.FreezeRotation;
            }

            GameState currentGameState = ServiceLocator.Instance.Get<GameStateManager>().GetCurrentState();
            _rigidbody.isKinematic = currentGameState != GameState.Gameplay || _isKinematic;
        }

        private void RenderShadow()
        {
            _shadow = new GameObject("Shadow");
            _shadow.transform.SetParent(transform, false);
            Transform shadowTransform = _shadow.transform;
            shadowTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadowTransform.localScale = new Vector3(1.4f, 1.4f, 1f);
            shadowTransform.localPosition = new Vector3(0f, 0.07f, 0f);

            Texture2D shadowTexture = _resourceProvider.GetShadowTexture();
            _shadowSpriteRenderer = _shadow.AddComponent<SpriteRenderer>();
            _shadowSpriteRenderer.sprite = Sprite.Create(shadowTexture,
                new Rect(0, 0, shadowTexture.width, shadowTexture.height),
                new Vector2(0.5f, 0.5f));
            _shadowSpriteRenderer.color = new Color(0f, 0f, 0f, 0.7f);
        }

        private IEnumerator ShowEmojiAnimationAsync(ActorEmojiType emojiType)
        {
            if (!_actor.IsActive) yield break;

            // For some reason, there are 12 emoji types exist in the game script,
            // but only 11 sprite sheet in the data folder (PAL3A has 12 but PAL3 has 11).
            if (!Enum.IsDefined(typeof(ActorEmojiType), emojiType)) yield break;

            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));

            var sprites = _resourceProvider.GetEmojiSprites(emojiType);

            var emojiGameObject = new GameObject($"Emoji_{emojiType.ToString()}");
            emojiGameObject.transform.SetParent(transform, false);
            emojiGameObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            emojiGameObject.transform.localPosition = new Vector3(0f, GetActorHeight(), 0f);

            var billboardRenderer = emojiGameObject.AddComponent<AnimatedBillboardRenderer>();
            billboardRenderer.Init(sprites, EMOJI_ANIMATION_FPS);

            #if PAL3
            var emojiSfx = ActorEmojiConstants.EmojiSfxInfo[emojiType];
            if (!string.IsNullOrEmpty(emojiSfx))
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand(emojiSfx, 1));
            }
            #endif

            yield return billboardRenderer.PlayAnimationAsync(ActorEmojiConstants.AnimationLoopCountInfo[emojiType]);

            Destroy(billboardRenderer);
            Destroy(emojiGameObject);
            waiter.CancelWait();
        }

        public void Execute(ActorPerformActionCommand command)
        {
            if (command.ActorId == _actor.Info.Id)
            {
                if (!_actor.IsActive && !_actor.IsMainActor())
                {
                    Debug.LogError($"Failed to perform action since actor {command.ActorId} is inactive.");
                    return;
                }

                // We can safely assume that when loop count > 0 or == -2 (1 time),
                // the requested action will be played for finite times, which means
                // it has to be triggered by the story script and we should wait for
                // the action sequence to complete before executing next command.
                WaitUntilCanceled waiter = null;
                if (command.LoopCount is > 0 or -2)
                {
                    waiter = new WaitUntilCanceled();
                    CommandDispatcher<ICommand>.Instance.Dispatch(
                        new ScriptRunnerAddWaiterRequest(waiter));
                }
                PerformAction(command.ActionName, true, command.LoopCount, waiter);
            }
        }

        public void Execute(ActorStopActionAndStandCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            PerformAction(_actor.GetIdleAction());
        }

        public void Execute(ActorEnablePlayerControlCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID) return;

            _isKinematic = _actor.Info.Id != command.ActorId;

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = _isKinematic;
            }
        }

        public void Execute(GameStateChangedNotification command)
        {
            if (command.NewState != GameState.Gameplay)
            {
                _isKinematic = true;
            }
            else if (command.NewState == GameState.Gameplay)
            {
                PlayerActorId playerActor = ServiceLocator.Instance.Get<PlayerManager>().GetPlayerActor();
                _isKinematic = _actor.Info.Id != (byte)playerActor;
            }

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = _isKinematic;
            }
        }

        public void Execute(ActorShowEmojiCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            StartCoroutine(ShowEmojiAnimationAsync((ActorEmojiType) command.EmojiId));
        }

        #if PAL3A
        public void Execute(ActorShowEmoji2Command command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            StartCoroutine(ShowEmojiAnimationAsync((ActorEmojiType) command.EmojiId));
        }
        #endif
    }
}