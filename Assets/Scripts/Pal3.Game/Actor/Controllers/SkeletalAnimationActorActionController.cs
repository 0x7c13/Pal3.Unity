﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Actor.Controllers
{
    using System;
    using Command;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.DataReader.Cpk;
    using Core.DataReader.Mov;
    using Core.DataReader.Msh;
    using Core.DataReader.Mtl;
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

    public sealed class SkeletalAnimationActorActionController : ActorActionController,
        ICommandExecutor<ActorStopActionCommand>
    {
        private GameResourceProvider _resourceProvider;
        private IMaterialManager _materialManager;
        private ActorBase _actor;
        private Color _tintColor;

        private SkeletalModelRenderer _skeletalModelRenderer;

        private Bounds _rendererBounds;
        private Bounds _meshBounds;

        protected override void OnEnableGameEntity()
        {
            base.OnEnableGameEntity();
            _skeletalModelRenderer = GameEntity.AddComponent<SkeletalModelRenderer>();
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        protected override void OnDisableGameEntity()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

            DeActivate();

            if (_skeletalModelRenderer != null)
            {
                _skeletalModelRenderer.Destroy();
                _skeletalModelRenderer = null;
            }

            base.OnDisableGameEntity();
        }

        public void Init(GameResourceProvider resourceProvider,
            ActorBase actor,
            bool hasColliderAndRigidBody,
            bool isDropShadowEnabled,
            Color tintColor)
        {
            base.Init(resourceProvider, actor, hasColliderAndRigidBody, isDropShadowEnabled);

            _resourceProvider = resourceProvider;
            _actor = actor;
            _tintColor = tintColor;
            _materialManager = resourceProvider.GetMaterialManager();
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
                EngineLogger.LogError($"Action {actionName} not found for actor {_actor.Name}");
                waiter?.CancelWait();
                return;
            }

            MshFile mshFile;
            MtlFile mtlFile;
            MovFile movFile;
            ITextureResourceProvider textureProvider;
            try
            {
                string mshFilePath = _actor.GetMeshFilePath(actionName);
                mshFile = _resourceProvider.GetGameResourceFile<MshFile>(mshFilePath);

                string mtlFilePath = _actor.GetMaterialFilePath(actionName);
                mtlFile = _resourceProvider.GetGameResourceFile<MtlFile>(mtlFilePath);

                string movFilePath = _actor.GetActionFilePath(actionName);
                movFile = _resourceProvider.GetGameResourceFile<MovFile>(movFilePath);

                textureProvider = _resourceProvider.CreateTextureResourceProvider(
                    CoreUtility.GetDirectoryName(mtlFilePath, CpkConstants.DirectorySeparatorChar));
            }
            catch (Exception ex)
            {
                EngineLogger.LogException(ex);
                waiter?.CancelWait();
                return;
            }

            DisposeCurrentAction();

            _skeletalModelRenderer.Init(mshFile,
                mtlFile,
                _materialManager,
                textureProvider,
                _tintColor);

            _skeletalModelRenderer.StartAnimation(movFile, loopCount);

            _rendererBounds = _skeletalModelRenderer.GetRendererBounds();
            _meshBounds = _skeletalModelRenderer.GetMeshBounds();

            base.PerformAction(actionName, overwrite, loopCount, waiter);
        }

        public override void PauseAnimation()
        {
            if (_skeletalModelRenderer != null)
            {
                _skeletalModelRenderer.PauseAnimation();
            }
        }

        public override float GetActorHeight()
        {
            if (_skeletalModelRenderer == null || !_skeletalModelRenderer.IsInitialized)
            {
                return _meshBounds.size.y;
            }

            return _skeletalModelRenderer.GetMeshBounds().size.y;
        }

        public override Bounds GetRendererBounds()
        {
            return (_skeletalModelRenderer == null || !_skeletalModelRenderer.IsInitialized) ? _rendererBounds :
                _skeletalModelRenderer.GetRendererBounds();
        }

        public override Bounds GetMeshBounds()
        {
            return (_skeletalModelRenderer == null || !_skeletalModelRenderer.IsInitialized) ? _meshBounds :
                _skeletalModelRenderer.GetMeshBounds();
        }

        internal override void DisposeCurrentAction()
        {
            if (_skeletalModelRenderer != null)
            {
                _skeletalModelRenderer.Dispose();
            }
            base.DisposeCurrentAction();
        }

        internal override void DeActivate()
        {
            DisposeCurrentAction();
            base.DeActivate();
        }

        public void Execute(ActorStopActionCommand command)
        {
            if (command.ActorId != _actor.Id ||
                _skeletalModelRenderer == null ||
                !_skeletalModelRenderer.IsInitialized) return;

            PerformAction(_actor.GetIdleAction());
        }
    }
}