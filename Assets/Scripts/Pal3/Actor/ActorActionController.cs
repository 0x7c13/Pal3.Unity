// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataLoader;
    using Core.DataReader.Mv3;
    using Core.Extensions;
    using Core.Services;
    using MetaData;
    using Renderer;
    using Script.Waiter;
    using State;
    using UnityEngine;

    public class ActorActionController : MonoBehaviour,
        ICommandExecutor<ActorAutoStandCommand>,
        ICommandExecutor<ActorPerformActionCommand>,
        ICommandExecutor<ActorStopActionCommand>,
        ICommandExecutor<ActorStopActionAndStandCommand>,
        #if PAL3
        ICommandExecutor<LongKuiSwitchModeCommand>,
        #endif
        ICommandExecutor<ActorChangeTextureCommand>,
        ICommandExecutor<ActorEnablePlayerControlCommand>,
        ICommandExecutor<GameStateChangedNotification>
    {
        private Actor _actor;
        private Color _tintColor;
        private GameObject _shadowProjector;

        private bool _autoStand = true;
        private string _currentAction = string.Empty;
        private readonly List<Mv3ModelRenderer> _mv3AnimationRenderers = new ();
        private WaitUntilCanceled _animationLoopPointWaiter;

        private Bounds _worldBounds;
        private Bounds _localBounds;

        private Rigidbody _rigidbody;
        private CapsuleCollider _collider;

        public void Init(Actor actor, Color tintColor)
        {
            _actor = actor;
            _tintColor = tintColor;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            DisposeCurrentAction();
            Destroy(_shadowProjector);
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
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
                waiter?.CancelWait();
                return;
            }

            DisposeCurrentAction();

            _animationLoopPointWaiter?.CancelWait();
            _animationLoopPointWaiter = waiter;

            _currentAction = actionName.ToLower();

            for (var i = 0; i < mv3File.Meshes.Length; i++)
            {
                var mesh = mv3File.Meshes[i];
                var material = mv3File.Meshes.Length != mv3File.Materials.Length ?
                    mv3File.Materials[0] :
                    mv3File.Materials[i];
                var events = mv3File.AnimationEvents;
                var keyFrames = mv3File.MeshKeyFrames[i];

                var mv3AnimationRenderer = gameObject.AddComponent<Mv3ModelRenderer>();
                mv3AnimationRenderer.Init(mesh, material, events, keyFrames, textureProvider, _tintColor);
                mv3AnimationRenderer.AnimationLoopPointReached += AnimationLoopPointReached;
                mv3AnimationRenderer.PlayAnimation(loopCount);

                _mv3AnimationRenderers.Add(mv3AnimationRenderer);
            }

            _worldBounds = _mv3AnimationRenderers.First().GetBounds();
            _localBounds = _mv3AnimationRenderers.First().GetLocalBounds();

            var action = ActorConstants.ActionNames
                .FirstOrDefault(a => a.Value.Equals(_currentAction)).Key;

            SetupShadowProjector(action);
            SetupCollider();
        }

        private void SetupShadowProjector(ActorActionType actorAction)
        {
            // Disable shadow for some actions
            if (ActorConstants.ActionWithoutShadow.Contains(actorAction))
            {
                if (_shadowProjector != null) Destroy(_shadowProjector);
            }
            else
            {
                if (_shadowProjector == null) CreateShadowProjector();
            }
        }

        private void SetupCollider()
        {
            var bounds = GetLocalBounds();
            _collider = gameObject.GetOrAddComponent<CapsuleCollider>();
            _collider.center = bounds.center;
            _collider.height = bounds.size.y;
            _collider.radius = bounds.size.x * 0.5f;

            var currentGameState = ServiceLocator.Instance.Get<GameStateManager>().GetCurrentState();
            ToggleCollisionDetectionBasedOnGameState(currentGameState);
        }

        private void SetupRigidBody()
        {
            _rigidbody = gameObject.GetOrAddComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = false;
            _rigidbody.constraints = RigidbodyConstraints.FreezePositionY |
                                     RigidbodyConstraints.FreezeRotation;

            var currentGameState = ServiceLocator.Instance.Get<GameStateManager>().GetCurrentState();
            ToggleCollisionDetectionBasedOnGameState(currentGameState);
        }

        private void CreateShadowProjector()
        {
            var position = transform.position;
            var bounds = GetBounds();

            _shadowProjector = Instantiate(Resources.Load<GameObject>("Prefabs/BlobShadowProjector"),
                new Vector3(position.x, bounds.max.y, position.z),
                Quaternion.Euler(90f, 0, 0));

            var projector = _shadowProjector.GetComponent<Projector>();
            projector.nearClipPlane = bounds.size.y - 0.3f;
            projector.farClipPlane = bounds.size.y + 5f;

            _shadowProjector.transform.parent = gameObject.transform;
        }

        public Bounds GetBounds()
        {
            return _mv3AnimationRenderers.Count == 0 ? _worldBounds : _mv3AnimationRenderers.First().GetBounds();
        }

        public Bounds GetLocalBounds()
        {
            return _mv3AnimationRenderers.Count == 0 ? _localBounds : _mv3AnimationRenderers.First().GetLocalBounds();
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
                    (loopCount is -2 && !_mv3AnimationRenderers
                        .Any(animationRenderer => animationRenderer.IsActionInHoldState())))
                {
                    PerformAction(_actor.GetIdleAction());
                }
            }
        }

        public void DisposeCurrentAction()
        {
            foreach (var animationRenderer in _mv3AnimationRenderers)
            {
                animationRenderer.AnimationLoopPointReached -= AnimationLoopPointReached;
                animationRenderer.StopAnimation();
                Destroy(animationRenderer);
            }
            _mv3AnimationRenderers.Clear();
            _currentAction = string.Empty;
        }

        public void DisposeShadow()
        {
            if (_shadowProjector != null)
            {
                Destroy(_shadowProjector);
            }
        }

        public void DisposeCollider()
        {
            if (_collider != null)
            {
                Destroy(_collider);
            }
        }

        public void DisposeRigidBody()
        {
            if (_rigidbody != null)
            {
                Destroy(_rigidbody);
            }
        }

        public void Execute(ActorAutoStandCommand command)
        {
            if (command.ActorId == _actor.Info.Id) _autoStand = (command.AutoStand == 1);
        }

        public void Execute(ActorPerformActionCommand command)
        {
            if (command.ActorId == _actor.Info.Id)
            {
                if (!_actor.IsActive)
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
            if (command.ActorId != _actor.Info.Id || _mv3AnimationRenderers.Count == 0) return;

            if (_mv3AnimationRenderers
                .Any(animationRenderer => animationRenderer.IsActionInHoldState()))
            {
                _animationLoopPointWaiter?.CancelWait();
                _animationLoopPointWaiter = new WaitUntilCanceled(this);
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ScriptRunnerWaitRequest(_animationLoopPointWaiter));

                foreach (var animationRenderer in _mv3AnimationRenderers
                             .Where(animationRenderer => animationRenderer.IsActionInHoldState()))
                {
                    animationRenderer.ResumeAction();
                }
            }
            else
            {
                foreach (var animationRenderer in _mv3AnimationRenderers)
                {
                    animationRenderer.StopAnimation();
                }
                _animationLoopPointWaiter?.CancelWait();
            }
        }

        public void Execute(ActorStopActionAndStandCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            PerformAction(ActorActionType.Stand);
        }

        public void Execute(ActorChangeTextureCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            _mv3AnimationRenderers.First().ChangeTexture(command.TextureName);
        }

        public void Execute(ActorEnablePlayerControlCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID) return;
            var enableRigidBody = _actor.Info.Id == command.ActorId;
            if (enableRigidBody && _rigidbody == null) SetupRigidBody();
            if (!enableRigidBody && _rigidbody != null) Destroy(_rigidbody);
        }

        public void Execute(GameStateChangedNotification command)
        {
            ToggleCollisionDetectionBasedOnGameState(command.NewState);
        }

        // TODO: Temporarily disable collision detection during cutscene since
        // the current path finding solution is not ideal and might cause issues
        public void ToggleCollisionDetectionBasedOnGameState(GameState state)
        {
            if (_collider != null)
            {
                _collider.enabled = state switch
                {
                    GameState.Gameplay => true,
                    _ => false
                };
            }

            if (_rigidbody != null)
            {
                _rigidbody.detectCollisions = state switch
                {
                    GameState.Gameplay => true,
                    _ => false
                };
            }
        }

        #if PAL3
        public void Execute(LongKuiSwitchModeCommand command)
        {
            if (_actor.Info.Id == (byte) PlayerActorId.LongKui)
            {
                _actor.ChangeName(command.Mode == 0 ?
                    ActorConstants.LongKuiHumanModeActorName :
                    ActorConstants.LongKuiGhostModeActorName);
            }
        }
        #endif
    }
}