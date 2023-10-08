// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Game.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using System.Threading;
    using Actor.Controllers;
    using Command;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Cpk;
    using Core.DataReader.Pol;
    using Core.DataReader.Scn;
    using Core.Utilities;
    using Data;
    using Engine.Abstraction;
    using Engine.Animation;
    using Engine.DataLoader;
    using Engine.Extensions;
    using Rendering.Renderer;
    using UnityEngine;
    using Color = Core.Primitives.Color;

    [ScnSceneObject(SceneObjectType.Impulsive)]
    public sealed class ImpulsiveObject : SceneObject
    {
        private const float HIT_ANIMATION_DURATION = 0.5f;

        private IGameEntity _subObjectGameEntity;
        private ImpulsiveMechanismSubObjectController _subObjectController;

        private bool _isDuringInteraction;

        public ImpulsiveObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            Color subObjectTintColor = tintColor;

            #if PAL3
            // Fix the color(texture) issue of impulsive mechanism in M11-2 which uses _r.pol as the main model
            if (string.Equals(ModelFileVirtualPath, @"M11.cpk\2\_r.pol", StringComparison.OrdinalIgnoreCase))
            {
                tintColor = new Color(0.9f, 0.45f, 0f, 0.1f);
            }
            #endif

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            _subObjectGameEntity = new GameEntity($"Object_{ObjectInfo.Id}_{ObjectInfo.Type}_SubObject");

            var subObjectModelPath = ModelFileVirtualPath.Insert(ModelFileVirtualPath.LastIndexOf('.'), "a");
            PolFile polFile = resourceProvider.GetGameResourceFile<PolFile>(subObjectModelPath);
            ITextureResourceProvider textureProvider = resourceProvider.CreateTextureResourceProvider(
                CoreUtility.GetDirectoryName(subObjectModelPath, CpkConstants.DirectorySeparatorChar));
            var subObjectModelRenderer = _subObjectGameEntity.AddComponent<PolyModelRenderer>();
            subObjectModelRenderer.Render(polFile,
                textureProvider,
                resourceProvider.GetMaterialFactory(),
                isStaticObject: false,
                subObjectTintColor);

            _subObjectController = _subObjectGameEntity.AddComponent<ImpulsiveMechanismSubObjectController>();
            _subObjectController.Init();
            _subObjectController.OnPlayerActorHit += OnPlayerActorHit;

            _subObjectGameEntity.SetParent(sceneObjectGameEntity, worldPositionStays: false);

            return sceneObjectGameEntity;
        }

        private void OnPlayerActorHit(object sender, IGameEntity playerActorGameEntity)
        {
            if (_isDuringInteraction) return; // Prevent multiple interactions during animation
            _isDuringInteraction = true;

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFollowPlayerCommand(0));
            RequestForInteraction();
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            PlaySfx("wb002");

            ctx.PlayerActorGameEntity.GetComponent<ActorActionController>()
                .PerformAction(ActorActionType.BeAttack, true, 1);

            Vector3 targetPosition = ctx.PlayerActorGameEntity.Transform.Position +
                                     (_subObjectGameEntity.Transform.Forward * 6f) + Vector3.up * 2f;

            yield return ctx.PlayerActorGameEntity.Transform.MoveAsync(targetPosition,
                HIT_ANIMATION_DURATION);

            ctx.PlayerActorGameEntity.GetComponent<ActorMovementController>().SetNavLayer(ObjectInfo.Parameters[2]);
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetTilePositionCommand(ActorConstants.PlayerActorVirtualID,
                    ObjectInfo.Parameters[0],
                    ObjectInfo.Parameters[1]));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFollowPlayerCommand(1));

            _isDuringInteraction = false;
        }

        public override void Deactivate()
        {
            if (_subObjectController != null)
            {
                _subObjectController.OnPlayerActorHit -= OnPlayerActorHit;
                _subObjectController.Destroy();
                _subObjectController = null;
            }

            if (_subObjectGameEntity != null)
            {
                _subObjectGameEntity.Destroy();
                _subObjectGameEntity = null;
            }

            base.Deactivate();
        }
    }

    internal sealed class ImpulsiveMechanismSubObjectController : GameEntityScript
    {
        public event EventHandler<IGameEntity> OnPlayerActorHit;

        private const float MIN_Z_POSITION = -1.7f;
        private const float MAX_Z_POSITION = 4f;
        private const float POSITION_HOLD_TIME = 3f;
        private const float MOVEMENT_ANIMATION_DURATION = 2.5f;

        private BoundsTriggerController _triggerController;
        private CancellationTokenSource _movementAnimationCts = new ();

        protected override void OnDisableGameEntity()
        {
            if (_movementAnimationCts is {IsCancellationRequested: false})
            {
                _movementAnimationCts.Cancel();
            }

            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                _triggerController.Destroy();
                _triggerController = null;
            }
        }

        public void Init()
        {
            // Add collider
            var bounds = new Bounds
            {
                center = new Vector3(0f, 1f, -1f),
                size = new Vector3(3f, 2f, 7f),
            };

            _triggerController = GameEntity.AddComponent<BoundsTriggerController>();
            _triggerController.SetBounds(bounds, true);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            // Set initial position
            Vector3 subObjectInitPosition = Transform.Position;
            subObjectInitPosition.z = MIN_Z_POSITION;
            Transform.Position = subObjectInitPosition;

            // Start movement
            _movementAnimationCts = new CancellationTokenSource();
            StartCoroutine(StartMovementAsync(_movementAnimationCts.Token));
        }

        private void OnPlayerActorEntered(object sender, IGameEntity playerActorGameEntity)
        {
            OnPlayerActorHit?.Invoke(sender, playerActorGameEntity);
        }

        private IEnumerator StartMovementAsync(CancellationToken cancellationToken)
        {
            var startDelay = RandomGenerator.Range(0f, 3.5f);
            yield return new WaitForSeconds(startDelay);

            Vector3 initPosition = Transform.LocalPosition;
            var holdTimeWaiter = new WaitForSeconds(POSITION_HOLD_TIME);

            while (!cancellationToken.IsCancellationRequested)
            {
                yield return CoreAnimation.EnumerateValueAsync(MIN_Z_POSITION,
                    MAX_Z_POSITION,
                    MOVEMENT_ANIMATION_DURATION,
                    AnimationCurveType.Linear,
                    (value) =>
                {
                    Vector3 newPosition = initPosition;
                    newPosition.z = value;
                    Transform.LocalPosition = newPosition;
                }, cancellationToken);

                yield return holdTimeWaiter;

                yield return CoreAnimation.EnumerateValueAsync(MAX_Z_POSITION,
                    MIN_Z_POSITION,
                    MOVEMENT_ANIMATION_DURATION,
                    AnimationCurveType.Linear,
                    (value) =>
                    {
                        Vector3 newPosition = initPosition;
                        newPosition.z = value;
                        Transform.LocalPosition = newPosition;
                    }, cancellationToken);

                yield return holdTimeWaiter;
            }
        }
    }
}

#endif