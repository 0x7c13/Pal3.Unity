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
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Mv3;
    using Core.DataReader.Pol;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Core.Utils;
    using Data;
    using MetaData;
    using Renderer;
    using Script.Waiter;
    using UnityEngine;

    public class VertexAnimationActorActionController : ActorActionController,
        ICommandExecutor<ActorAutoStandCommand>,
        ICommandExecutor<ActorStopActionCommand>,
        ICommandExecutor<ActorChangeTextureCommand>
    {
        private GameResourceProvider _resourceProvider;
        private IMaterialFactory _materialFactory;
        private Actor _actor;
        private Color _tintColor;

        private bool _autoStand = true;

        private Mv3ModelRenderer _mv3ModelRenderer;
        private WaitUntilCanceled _animationLoopPointWaiter;

        private Bounds _rendererBounds;
        private Bounds _meshBounds;

        private bool _isHoldAnimationStarted = false;

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

            // Should not auto stand if the actor is on hold.
            _autoStand = actor.Info.InitBehaviour != ScnActorBehaviour.Hold;
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
            bool isNewActionSameAsCurrent =
                string.Equals(GetCurrentAction(), actionName, StringComparison.OrdinalIgnoreCase);

            bool shouldPlayHoldAnimation = !_isHoldAnimationStarted &&
                                           isNewActionSameAsCurrent &&
                                           loopCount == 1 &&
                                           _actor.Info.InitBehaviour == ScnActorBehaviour.Hold &&
                                           _actor.Info.LoopAction == 0;

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
                Debug.LogError($"Action {actionName} not found for actor {_actor.Info.Name}.");
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
                    Utility.GetRelativeDirectoryPath(mv3FilePath));
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                _animationLoopPointWaiter?.CancelWait();
                waiter?.CancelWait();
                return;
            }

            DisposeCurrentAction();

            _animationLoopPointWaiter = waiter;
            _mv3ModelRenderer = gameObject.GetOrAddComponent<Mv3ModelRenderer>();

            ActorActionType? actionType = ActorConstants.NameToActionMap.ContainsKey(actionName.ToLower()) ?
                ActorConstants.NameToActionMap[actionName.ToLower()] : null;

            if (actionType.HasValue &&
                mv3File.TagNodes is {Length: > 0} &&
                _actor.GetWeaponName() is {} weaponName &&
                ActorConstants.ActionNameToWeaponArmTypeMap[actionType.Value] != WeaponArmType.None)
            {
                var separator = CpkConstants.DirectorySeparator;

                var weaponPath = $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}" +
                                 $"{FileConstants.WeaponFolderName}{separator}{weaponName}{separator}{weaponName}.pol";

                PolFile polFile = _resourceProvider.GetGameResourceFile<PolFile>(weaponPath);
                ITextureResourceProvider weaponTextureProvider = _resourceProvider.CreateTextureResourceProvider(
                    Utility.GetRelativeDirectoryPath(weaponPath));
                _mv3ModelRenderer.Init(mv3File,
                    _materialFactory,
                    textureProvider,
                    _tintColor,
                    polFile,
                    weaponTextureProvider);
            }
            else
            {
                _mv3ModelRenderer.Init(mv3File,
                    _materialFactory,
                    textureProvider,
                    _tintColor);
            }

            _mv3ModelRenderer.AnimationLoopPointReached += AnimationLoopPointReached;
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
            if (_mv3ModelRenderer == null || !_mv3ModelRenderer.IsVisible())
            {
                return _meshBounds.size.y;
            }

            return _mv3ModelRenderer.GetMeshBounds().size.y;
        }

        public override Bounds GetRendererBounds()
        {
            return (_mv3ModelRenderer == null || !_mv3ModelRenderer.IsVisible()) ? _rendererBounds :
                _mv3ModelRenderer.GetRendererBounds();
        }

        public override Bounds GetMeshBounds()
        {
            return (_mv3ModelRenderer == null || !_mv3ModelRenderer.IsVisible()) ? _meshBounds :
                _mv3ModelRenderer.GetMeshBounds();
        }

        private void AnimationLoopPointReached(object _, int loopCount)
        {
            if (loopCount is 0 or -2)
            {
                _animationLoopPointWaiter?.CancelWait();
            }

            if (_autoStand && _mv3ModelRenderer.IsVisible())
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
                _mv3ModelRenderer.AnimationLoopPointReached -= AnimationLoopPointReached;
                _mv3ModelRenderer.Dispose();
                _mv3ModelRenderer = null;
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
            if (command.ActorId == _actor.Info.Id) _autoStand = (command.AutoStand == 1);
        }

        public void Execute(ActorStopActionCommand command)
        {
            if (command.ActorId != _actor.Info.Id ||
                _mv3ModelRenderer == null ||
                !_mv3ModelRenderer.IsVisible()) return;

            if (_mv3ModelRenderer.IsActionInHoldState())
            {
                _animationLoopPointWaiter?.CancelWait();
                _animationLoopPointWaiter = new WaitUntilCanceled();
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ScriptRunnerAddWaiterRequest(_animationLoopPointWaiter));
                _mv3ModelRenderer.ResumeAction();
            }
            else
            {
                _mv3ModelRenderer.PauseAnimation();
                _animationLoopPointWaiter?.CancelWait();

                if (_autoStand && _mv3ModelRenderer.IsVisible())
                {
                    PerformAction(_actor.GetIdleAction());
                }
            }
        }

        public void Execute(ActorChangeTextureCommand command)
        {
            if (_actor.Info.Id != command.ActorId) return;
            _mv3ModelRenderer.ChangeTexture(command.TextureName);
        }
    }
}