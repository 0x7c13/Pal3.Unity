// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Actor.Controllers
{
    using System;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Cpk;
    using Core.DataReader.Mv3;
    using Core.DataReader.Pol;
    using Core.Utilities;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Logging;
    using Rendering.Material;
    using Rendering.Renderer;
    using Script.Waiter;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;

    public sealed class VertexAnimationActorActionController : ActorActionController,
        ICommandExecutor<ActorAutoStandCommand>,
        ICommandExecutor<ActorStopActionCommand>,
        ICommandExecutor<ActorChangeTextureCommand>
    {
        private GameResourceProvider _resourceProvider;
        private IMaterialManager _materialManager;
        private ActorBase _actor;
        private Color? _tintColor;

        // Auto perform idle action when the current action is finished.
        private bool _autoStand;
        private bool _canPerformHoldAnimation;

        private Mv3ModelRenderer _mv3ModelRenderer;
        private WaitUntilCanceled _animationLoopPointWaiter;

        private Bounds _rendererBounds;
        private Bounds _meshBounds;

        private bool _isHoldAnimationStarted = false;

        protected override void OnEnableGameEntity()
        {
            base.OnEnableGameEntity();
            _mv3ModelRenderer = GameEntity.AddComponent<Mv3ModelRenderer>();
            _mv3ModelRenderer.AnimationLoopPointReached += AnimationLoopPointReached;
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        protected override void OnDisableGameEntity()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

            DeActivate();

            if (_mv3ModelRenderer != null)
            {
                _mv3ModelRenderer.AnimationLoopPointReached -= AnimationLoopPointReached;
                _mv3ModelRenderer.Destroy();
                _mv3ModelRenderer = null;
            }

            base.OnDisableGameEntity();
        }

        public void Init(GameResourceProvider resourceProvider,
            ActorBase actor,
            bool hasColliderAndRigidBody,
            bool isDropShadowEnabled,
            bool autoStand,
            bool canPerformHoldAnimation,
            Color? tintColor = default)
        {
            base.Init(resourceProvider, actor, hasColliderAndRigidBody, isDropShadowEnabled);

            _resourceProvider = resourceProvider;
            _actor = actor;
            _autoStand = autoStand;
            _canPerformHoldAnimation = canPerformHoldAnimation;
            _tintColor = tintColor;

            _materialManager = resourceProvider.GetMaterialManager();
        }

        public override void PerformAction(string actionName,
            bool overwrite = false,
            int loopCount = -1,
            WaitUntilCanceled waiter = null)
        {
            bool isNewActionSameAsCurrent =
                string.Equals(GetCurrentAction(), actionName, StringComparison.OrdinalIgnoreCase);

            bool shouldPlayHoldAnimation = !_isHoldAnimationStarted &&
                                           isNewActionSameAsCurrent &&
                                           loopCount == 1 &&
                                           _canPerformHoldAnimation;

            // Skip if the action is the same as current and not overwrite or hold animation.
            if (isNewActionSameAsCurrent && !overwrite && !shouldPlayHoldAnimation)
            {
                return;
            }

            if (shouldPlayHoldAnimation)
            {
                _isHoldAnimationStarted = true;
            }

            if (!_actor.HasAction(actionName))
            {
                EngineLogger.LogError($"Action {actionName} not found for actor {_actor.Name}");
                _animationLoopPointWaiter?.CancelWait();
                waiter?.CancelWait();
                return;
            }

            Mv3File mv3File;
            ITextureResourceProvider textureProvider;
            try
            {
                string mv3FilePath = _actor.GetActionFilePath(actionName);
                mv3File = _resourceProvider.GetGameResourceFile<Mv3File>(mv3FilePath);
                textureProvider = _resourceProvider.CreateTextureResourceProvider(
                    CoreUtility.GetDirectoryName(mv3FilePath, CpkConstants.DirectorySeparatorChar));
            }
            catch (Exception ex)
            {
                EngineLogger.LogException(ex);
                _animationLoopPointWaiter?.CancelWait();
                waiter?.CancelWait();
                return;
            }

            DisposeCurrentAction();

            _animationLoopPointWaiter = waiter;

            ActorActionType? actionType = ActorConstants.NameToActionMap.ContainsKey(actionName.ToLower()) ?
                ActorConstants.NameToActionMap[actionName.ToLower()] : null;

            if (actionType.HasValue &&
                mv3File.TagNodes is {Length: > 0} &&
                _actor.GetTagObjectName() is {} weaponName &&
                ActorConstants.ActionNameToWeaponArmTypeMap[actionType.Value] != WeaponArmType.None)
            {
                string weaponPath = FileConstants.GetWeaponModelFileVirtualPath(weaponName);
                PolFile polFile = _resourceProvider.GetGameResourceFile<PolFile>(weaponPath);
                ITextureResourceProvider weaponTextureProvider = _resourceProvider.CreateTextureResourceProvider(
                    CoreUtility.GetDirectoryName(weaponPath, CpkConstants.DirectorySeparatorChar));
                _mv3ModelRenderer.Init(mv3File,
                    _materialManager,
                    textureProvider,
                    _tintColor,
                    polFile,
                    weaponTextureProvider);
            }
            else
            {
                _mv3ModelRenderer.Init(mv3File,
                    _materialManager,
                    textureProvider,
                    _tintColor);
            }

            _mv3ModelRenderer.StartAnimation(loopCount);

            _rendererBounds = _mv3ModelRenderer.GetRendererBounds();
            _meshBounds = _mv3ModelRenderer.GetMeshBounds();

            base.PerformAction(actionName, overwrite, loopCount, waiter);
        }

        public override void PauseAnimation()
        {
            if (_mv3ModelRenderer != null)
            {
                _mv3ModelRenderer.PauseAnimation();
            }
        }

        public override float GetActorHeight()
        {
            if (_mv3ModelRenderer == null || !_mv3ModelRenderer.IsInitialized)
            {
                return _meshBounds.size.y;
            }

            return _mv3ModelRenderer.GetMeshBounds().size.y;
        }

        public override Bounds GetRendererBounds()
        {
            return (_mv3ModelRenderer == null || !_mv3ModelRenderer.IsInitialized) ? _rendererBounds :
                _mv3ModelRenderer.GetRendererBounds();
        }

        public override Bounds GetMeshBounds()
        {
            return (_mv3ModelRenderer == null || !_mv3ModelRenderer.IsInitialized) ? _meshBounds :
                _mv3ModelRenderer.GetMeshBounds();
        }

        private void AnimationLoopPointReached(object mv3ModelRenderer, int loopCount)
        {
            if (loopCount is 0 or -2)
            {
                _animationLoopPointWaiter?.CancelWait();
            }

            if (_autoStand && _mv3ModelRenderer.IsInitialized)
            {
                if (loopCount is 0 ||
                    (loopCount is -2 && !_mv3ModelRenderer.IsActionInHoldState()))
                {
                    PerformAction(_actor.GetIdleAction());
                }
            }
        }

        internal override void DisposeCurrentAction()
        {
            _animationLoopPointWaiter?.CancelWait();
            if (_mv3ModelRenderer != null)
            {
                _mv3ModelRenderer.Dispose();
            }
            base.DisposeCurrentAction();
        }

        internal override void DeActivate()
        {
            DisposeCurrentAction();
            base.DeActivate();
        }

        public void Execute(ActorAutoStandCommand command)
        {
            if (command.ActorId == _actor.Id) _autoStand = (command.AutoStand == 1);
        }

        public void Execute(ActorStopActionCommand command)
        {
            if (command.ActorId != _actor.Id ||
                _mv3ModelRenderer == null ||
                !_mv3ModelRenderer.IsInitialized) return;

            if (_mv3ModelRenderer.IsActionInHoldState())
            {
                _animationLoopPointWaiter?.CancelWait();
                _animationLoopPointWaiter = new WaitUntilCanceled();
                Pal3.Instance.Execute(new ScriptRunnerAddWaiterRequest(_animationLoopPointWaiter));
                _mv3ModelRenderer.ResumeAction();
            }
            else
            {
                _animationLoopPointWaiter?.CancelWait();
                PerformAction(_actor.GetIdleAction());
            }
        }

        public void Execute(ActorChangeTextureCommand command)
        {
            if (_actor.Id != command.ActorId || _mv3ModelRenderer == null) return;
            _mv3ModelRenderer.ChangeTexture(command.TextureName);
        }
    }
}