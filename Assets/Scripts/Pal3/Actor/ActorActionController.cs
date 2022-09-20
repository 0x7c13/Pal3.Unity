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
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Mv3;
    using Core.Extensions;
    using Core.Renderer;
    using Core.Services;
    using Data;
    using MetaData;
    using Player;
    using Renderer;
    using Script.Waiter;
    using State;
    using UnityEngine;

    public class ActorActionController : MonoBehaviour,
        ICommandExecutor<ActorAutoStandCommand>,
        ICommandExecutor<ActorPerformActionCommand>,
        ICommandExecutor<ActorStopActionCommand>,
        ICommandExecutor<ActorStopActionAndStandCommand>,
        ICommandExecutor<ActorChangeTextureCommand>,
        ICommandExecutor<ActorEnablePlayerControlCommand>,
        ICommandExecutor<ActorShowEmojiCommand>,
        #if PAL3A
        ICommandExecutor<ActorShowEmoji2Command>,
        #endif
        ICommandExecutor<GameStateChangedNotification>
    {
        private const float EMOJI_ANIMATION_FPS = 5f;
        
        private GameResourceProvider _resourceProvider;
        private Actor _actor;
        private Color _tintColor;
        private GameObject _shadow;
        private SpriteRenderer _shadowSpriteRenderer;

        private bool _autoStand = true;
        private string _currentAction = string.Empty;
        private Mv3ModelRenderer _mv3AnimationRenderer;
        private WaitUntilCanceled _animationLoopPointWaiter;

        private Bounds _worldBounds;
        private Bounds _localBounds;

        private Rigidbody _rigidbody;
        private CapsuleCollider _collider;

        // By default, all none-player actors are kinematic.
        // Also, the player actor is only non-kinematic during gameplay state.
        private bool _isKinematic = true;

        public void Init(GameResourceProvider resourceProvider, Actor actor, Color tintColor)
        {
            _resourceProvider = resourceProvider;
            _actor = actor;
            _tintColor = tintColor;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

            DeActivate();

            if (_mv3AnimationRenderer != null)
            {
                Destroy(_mv3AnimationRenderer);
            }

            if (_shadow != null)
            {
                Destroy(_shadow);
            }

            if (_collider != null)
            {
                Destroy(_collider);
            }

            if (_rigidbody != null)
            {
                Destroy(_rigidbody);
            }
        }

        public string GetCurrentAction()
        {
            return _currentAction;
        }

        public Rigidbody GetRigidBody()
        {
            return _rigidbody;
        }

        public void PerformAction(ActorActionType actorActionType, bool overwrite = false, int loopCount = -1)
        {
            PerformAction(ActorConstants.ActionNames[actorActionType]);
        }

        public void PerformAction(string actionName,
            bool overwrite = false,
            int loopCount = -1,
            WaitUntilCanceled waiter = null)
        {
            if (!overwrite && string.Equals(_currentAction, actionName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!_actor.HasAction(actionName))
            {
                Debug.LogError($"Action {actionName} not found for actor {_actor.Info.Name}.");
                _animationLoopPointWaiter?.CancelWait();
                waiter?.CancelWait();
                return;
            }

            Mv3File mv3File;
            ITextureResourceProvider textureProvider;
            try
            {
                (mv3File, textureProvider) = _actor.GetActionMv3(actionName);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                _animationLoopPointWaiter?.CancelWait();
                waiter?.CancelWait();
                return;
            }

            DisposeAction();

            _animationLoopPointWaiter = waiter;
            _currentAction = actionName.ToLower();
            _mv3AnimationRenderer = gameObject.GetOrAddComponent<Mv3ModelRenderer>();

            var actionType = ActorConstants.ActionNames
                .FirstOrDefault(_ => string.Equals(_.Value, actionName, StringComparison.OrdinalIgnoreCase)).Key;
            
            if (mv3File.TagNodes is {Length: > 0} && _actor.GetWeaponName() is {} weaponName &&
                ActorConstants.ActionNameToWeaponArmTypeMap[actionType] != WeaponArmType.None)
            {
                var separator = CpkConstants.CpkDirectorySeparatorChar;

                var weaponPath = $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}" +
                                 $"{FileConstants.WeaponFolderName}{separator}{weaponName}{separator}{weaponName}.pol";

                var (polFile, weaponTextureProvider) = _resourceProvider.GetPol(weaponPath);
                _mv3AnimationRenderer.Init(mv3File, textureProvider, _tintColor,
                    polFile, weaponTextureProvider, Color.white);
            }
            else
            {
                _mv3AnimationRenderer.Init(mv3File, textureProvider, _tintColor);
            }

            _mv3AnimationRenderer.AnimationLoopPointReached += AnimationLoopPointReached;
            _mv3AnimationRenderer.PlayAnimation(loopCount);

            _worldBounds = _mv3AnimationRenderer.GetWorldBounds();
            _localBounds = _mv3AnimationRenderer.GetLocalBounds();

            var action = ActorConstants.ActionNames
                .FirstOrDefault(a => a.Value.Equals(_currentAction)).Key;

            SetupShadow(action);
            SetupCollider();
            SetupRigidBody();
        }

        private void SetupShadow(ActorActionType actorAction)
        {
            // Disable shadow for HuaYing and TaoZi
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
            if (ActorConstants.ActionWithoutShadow.Contains(actorAction))
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

            var bounds = GetLocalBounds();
            _collider.center = bounds.center;
            _collider.height = bounds.size.y;
            _collider.radius = bounds.size.x * 0.35f;
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

            var currentGameState = ServiceLocator.Instance.Get<GameStateManager>().GetCurrentState();
            _rigidbody.isKinematic = currentGameState != GameState.Gameplay || _isKinematic;
        }

        private void RenderShadow()
        {
            _shadow = new GameObject("Shadow");
            var shadowTexture = _resourceProvider.GetShadowTexture();
            _shadowSpriteRenderer = _shadow.AddComponent<SpriteRenderer>();
            _shadowSpriteRenderer.sprite = Sprite.Create(shadowTexture,
                new Rect(0, 0, shadowTexture.width, shadowTexture.height),
                new Vector2(0.5f, 0.5f));
            _shadowSpriteRenderer.color = new Color(0f, 0f, 0f, 0.7f);
            var shadowTransform = _shadow.transform;
            shadowTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
            shadowTransform.localScale = new Vector3(1.4f, 1.4f, 1f);
            shadowTransform.localPosition = new Vector3(0f, 0.07f, 0f);
            _shadow.transform.SetParent(transform, false);
        }

        private IEnumerator ShowEmojiAnimation(ActorEmojiType emojiType)
        {
            // For some reason, there are 12 emoji types exist in the game script,
            // but only 11 sprite sheet in the data folder (PAL3A has 12 but PAL3 has 11).
            if (!Enum.IsDefined(typeof(ActorEmojiType), emojiType)) yield break;

            var waiter = new WaitUntilCanceled(this);
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerWaitRequest(waiter));

            var sprites = _resourceProvider.GetEmojiSprites(emojiType);

            var emojiGameObject = new GameObject($"Emoji_{emojiType.ToString()}");
            emojiGameObject.transform.SetParent(transform);
            emojiGameObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            var actorHeadPosition = GetActorHeadWorldPosition();
            emojiGameObject.transform.position = new Vector3(actorHeadPosition.x,
                actorHeadPosition.y + 0.1f, // With a small Y offset
                actorHeadPosition.z);

            var billboardRenderer = emojiGameObject.AddComponent<AnimatedBillboardRenderer>();

            #if PAL3
            var emojiSfx = ActorEmojiConstants.EmojiSfxInfo[emojiType];
            if (!string.IsNullOrEmpty(emojiSfx))
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand(emojiSfx, 1));
            }
            #endif
            
            yield return billboardRenderer.PlaySpriteAnimation(sprites,
                EMOJI_ANIMATION_FPS,
                ActorEmojiConstants.AnimationLoopCountInfo[emojiType]);

            Destroy(billboardRenderer);
            Destroy(emojiGameObject);
            waiter.CancelWait();
        }
        
        public Vector3 GetActorHeadWorldPosition()
        {
            var parentPosition = transform.position;
            
            if (_mv3AnimationRenderer == null || !_mv3AnimationRenderer.IsVisible())
            {
                return new Vector3(parentPosition.x,
                    _worldBounds.min.y + _localBounds.max.y,
                    parentPosition.z);
            }
            
            return new Vector3(parentPosition.x,
                _mv3AnimationRenderer.GetWorldBounds().min.y +
                _mv3AnimationRenderer.GetLocalBounds().max.y,
                parentPosition.z);
        }
        
        public Bounds GetWorldBounds()
        {
            return (_mv3AnimationRenderer == null || !_mv3AnimationRenderer.IsVisible()) ? _worldBounds :
                _mv3AnimationRenderer.GetWorldBounds();
        }

        public Bounds GetLocalBounds()
        {
            return (_mv3AnimationRenderer == null || !_mv3AnimationRenderer.IsVisible()) ? _localBounds :
                _mv3AnimationRenderer.GetLocalBounds();
        }

        private void AnimationLoopPointReached(object _, int loopCount)
        {
            if (loopCount is 0 or -2)
            {
                _animationLoopPointWaiter?.CancelWait();
            }

            if (_autoStand)
            {
                if (loopCount is 0 ||
                    (loopCount is -2 && !_mv3AnimationRenderer.IsActionInHoldState()))
                {
                    PerformAction(_actor.GetIdleAction());
                }
            }
        }

        private void DisposeAction()
        {
            _animationLoopPointWaiter?.CancelWait();

            if (_mv3AnimationRenderer != null)
            {
                _mv3AnimationRenderer.AnimationLoopPointReached -= AnimationLoopPointReached;
                _mv3AnimationRenderer.DisposeAnimation();
            }

            _currentAction = string.Empty;
        }

        private void DisposeShadow()
        {
            if (_shadow != null)
            {
                Destroy(_shadow);
            }
        }

        private void DisposeCollider()
        {
            if (_collider != null)
            {
                Destroy(_collider);
            }
        }

        private void DisposeRigidBody()
        {
            if (_rigidbody != null)
            {
                Destroy(_rigidbody);
            }
        }

        public void DeActivate()
        {
            DisposeAction();
            DisposeShadow();
            DisposeRigidBody();
            DisposeCollider();
        }

        public void Execute(ActorAutoStandCommand command)
        {
            if (command.ActorId == _actor.Info.Id) _autoStand = (command.AutoStand == 1);
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
                    waiter = new WaitUntilCanceled(this);
                    CommandDispatcher<ICommand>.Instance.Dispatch(
                        new ScriptRunnerWaitRequest(waiter));
                }
                PerformAction(command.ActionName, true, command.LoopCount, waiter);
            }
        }

        public void Execute(ActorStopActionCommand command)
        {
            if (command.ActorId != _actor.Info.Id ||
                _mv3AnimationRenderer == null ||
                !_mv3AnimationRenderer.IsVisible()) return;

            if (_mv3AnimationRenderer.IsActionInHoldState())
            {
                _animationLoopPointWaiter?.CancelWait();
                _animationLoopPointWaiter = new WaitUntilCanceled(this);
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ScriptRunnerWaitRequest(_animationLoopPointWaiter));

                _mv3AnimationRenderer.ResumeAction();
            }
            else
            {
                _mv3AnimationRenderer.PauseAnimation();
                _animationLoopPointWaiter?.CancelWait();

                if (_autoStand)
                {
                    PerformAction(_actor.GetIdleAction());
                }
            }
        }

        public void Execute(ActorStopActionAndStandCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            PerformAction(_actor.GetIdleAction());
        }

        public void Execute(ActorChangeTextureCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            _mv3AnimationRenderer.ChangeTexture(command.TextureName);
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
                var playerActor = ServiceLocator.Instance.Get<PlayerManager>().GetPlayerActor();
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
            StartCoroutine(ShowEmojiAnimation((ActorEmojiType) command.EmojiId));
        }
        
        #if PAL3A
        public void Execute(ActorShowEmoji2Command command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            StartCoroutine(ShowEmojiAnimation((ActorEmojiType) command.EmojiId));
        }
        #endif
    }
}