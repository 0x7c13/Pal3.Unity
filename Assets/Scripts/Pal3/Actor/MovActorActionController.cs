// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using System;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Data;
    using MetaData;
    using Renderer;
    using Script.Waiter;
    using UnityEngine;

    public class MovActorActionController : ActorActionController,
        ICommandExecutor<ActorAutoStandCommand>,
        ICommandExecutor<ActorStopActionCommand>,
        ICommandExecutor<ActorPerformActionCommand>,
        ICommandExecutor<ActorStopActionAndStandCommand>,
        ICommandExecutor<ActorEnablePlayerControlCommand>,
        ICommandExecutor<ActorShowEmojiCommand>,
        #if PAL3A
        ICommandExecutor<ActorShowEmoji2Command>,
        #endif
        ICommandExecutor<GameStateChangedNotification>
    {
        private GameResourceProvider _resourceProvider;
        private IMaterialFactory _materialFactory;
        private Actor _actor;
        private Color _tintColor;

        private bool _autoStand = true;

        // private MovModelRenderer _movAnimationRenderer;
        // private WaitUntilCanceled _animationLoopPointWaiter;

        private Bounds _rendererBounds;
        private Bounds _meshBounds;

        public void Init(GameResourceProvider resourceProvider,
            Actor actor,
            bool hasColliderAndRigidBody,
            bool isDropShadowEnabled,
            Color tintColor)
        {
            base.Init(resourceProvider, actor, hasColliderAndRigidBody, isDropShadowEnabled);

            _resourceProvider = resourceProvider;
            _actor = actor;
            _tintColor = tintColor;
            _materialFactory = resourceProvider.GetMaterialFactory();
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            DeActivate();
            base.DeActivate();
        }

        public override void PerformAction(string actionName,
            bool overwrite = false,
            int loopCount = -1,
            WaitUntilCanceled waiter = null)
        {
            if (!overwrite && string.Equals(GetCurrentAction(), actionName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }


            if (!_actor.HasAction(actionName))
            {
                Debug.LogError($"Action {actionName} not found for actor {_actor.Info.Name}.");
                //_animationLoopPointWaiter?.CancelWait();
                waiter?.CancelWait();
                return;
            }

            Debug.LogWarning($"Actor {_actor.Info.Name} is performing bone action {actionName}.");

            // MovFile movFile;
            // ITextureResourceProvider textureProvider;
            // try
            // {
            //     (movFile, textureProvider) = _actor.GetActionMov(actionName);
            // }
            // catch (Exception ex)
            // {
            //     Debug.LogError(ex);
            //     _animationLoopPointWaiter?.CancelWait();
            //     waiter?.CancelWait();
            //     return;
            // }
            //
            // DisposeAction();
            //
            // _animationLoopPointWaiter = waiter;
            // _movAnimationRenderer = gameObject.GetOrAddComponent<MovModelRenderer>();
            //
            // ActorActionType actionType = ActorConstants.ActionNames
            //     .FirstOrDefault(_ => string.Equals(_.Value, actionName, StringComparison.OrdinalIgnoreCase)).Key;
            //
            // if (movFile.TagNodes is {Length: > 0} && _actor.GetWeaponName() is {} weaponName &&
            //     ActorConstants.ActionNameToWeaponArmTypeMap[actionType] != WeaponArmType.None)
            // {
            //     var separator = CpkConstants.DirectorySeparator;
            //
            //     var weaponPath = $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}" +
            //                      $"{FileConstants.WeaponFolderName}{separator}{weaponName}{separator}{weaponName}.pol";
            //
            //     (PolFile polFile, ITextureResourceProvider weaponTextureProvider) = _resourceProvider.GetPol(weaponPath);
            //     _movAnimationRenderer.Init(movFile,
            //         _materialFactory,
            //         textureProvider,
            //         _tintColor,
            //         polFile,
            //         weaponTextureProvider);
            // }
            // else
            // {
            //     _movAnimationRenderer.Init(movFile,
            //         _materialFactory,
            //         textureProvider,
            //         _tintColor);
            // }
            //
            // _movAnimationRenderer.AnimationLoopPointReached += AnimationLoopPointReached;
            // _movAnimationRenderer.StartAnimation(loopCount);
            //
            // _rendererBounds = _movAnimationRenderer.GetRendererBounds();
            // _meshBounds = _movAnimationRenderer.GetMeshBounds();

            base.PerformAction(actionName, overwrite, loopCount, waiter);
        }

        // public override float GetActorHeight()
        // {
        //     if (_movAnimationRenderer == null || !_movAnimationRenderer.IsVisible())
        //     {
        //         return _meshBounds.size.y;
        //     }
        //
        //     return _movAnimationRenderer.GetMeshBounds().size.y;
        // }
        //
        // public override Bounds GetRendererBounds()
        // {
        //     return (_movAnimationRenderer == null || !_movAnimationRenderer.IsVisible()) ? _rendererBounds :
        //         _movAnimationRenderer.GetRendererBounds();
        // }
        //
        // public override Bounds GetMeshBounds()
        // {
        //     return (_movAnimationRenderer == null || !_movAnimationRenderer.IsVisible()) ? _meshBounds :
        //         _movAnimationRenderer.GetMeshBounds();
        // }

        internal override void DisposeCurrentAction()
        {
            // _animationLoopPointWaiter?.CancelWait();
            //
            // if (_movAnimationRenderer != null)
            // {
            //     _movAnimationRenderer.AnimationLoopPointReached -= AnimationLoopPointReached;
            //     _movAnimationRenderer.DisposeAnimation();
            // }

            base.DisposeCurrentAction();
        }

        internal override void DeActivate()
        {
            DisposeCurrentAction();

            // if (_movAnimationRenderer != null)
            // {
            //     Destroy(_movAnimationRenderer);
            // }

            base.DeActivate();
        }

        public void Execute(ActorAutoStandCommand command)
        {
            if (command.ActorId == _actor.Info.Id) _autoStand = (command.AutoStand == 1);
        }

        public void Execute(ActorStopActionCommand command)
        {
            // if (command.ActorId != _actor.Info.Id ||
            //     _movAnimationRenderer == null ||
            //     !_movAnimationRenderer.IsVisible()) return;
            //
            // if (_movAnimationRenderer.IsActionInHoldState())
            // {
            //     _animationLoopPointWaiter?.CancelWait();
            //     _animationLoopPointWaiter = new WaitUntilCanceled();
            //     CommandDispatcher<ICommand>.Instance.Dispatch(
            //         new ScriptRunnerAddWaiterRequest(_animationLoopPointWaiter));
            //
            //     _movAnimationRenderer.ResumeAction();
            // }
            // else
            // {
            //     _movAnimationRenderer.PauseAnimation();
            //     _animationLoopPointWaiter?.CancelWait();
            //
            //     if (_autoStand && _movAnimationRenderer.IsVisible())
            //     {
            //         PerformAction(_actor.GetIdleAction());
            //     }
            // }
        }
    }
}