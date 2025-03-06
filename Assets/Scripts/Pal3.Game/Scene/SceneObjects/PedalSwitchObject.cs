// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Actor.Controllers;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Animation;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Services;
    using State;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.PedalSwitch)]
    public sealed class PedalSwitchObject : SceneObject
    {
        private const float DESCENDING_HEIGHT = 0.25f;
        private const float DESCENDING_ANIMATION_DURATION = 2f;

        private StandingPlatformController _platformController;

        private readonly GameStateManager _gameStateManager;

        private bool _isInteractionInProgress;

        public PedalSwitchObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _gameStateManager = ServiceLocator.Instance.Get<GameStateManager>();
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            Bounds bounds = GetMeshBounds();

            #if PAL3
            if (SceneInfo.IsCity("m11") &&
                ObjectInfo.Name.Equals("_h1.pol", StringComparison.OrdinalIgnoreCase))
            {
                bounds = new Bounds
                {
                    center = new Vector3(0f, -0.2f, 0f),
                    size = new Vector3(3f, 0.5f, 3f),
                };
            }
            else if ((SceneInfo.Is("m15", "2") || SceneInfo.Is("m15", "3")) &&
                ObjectInfo.Name.Equals("_c.pol", StringComparison.OrdinalIgnoreCase))
            {
                bounds = new Bounds
                {
                    center = new Vector3(0f, -0.2f, -0.5f),
                    size = new Vector3(3.5f, 0.5f, 6f),
                };
            }
            #endif

            _platformController = sceneObjectGameEntity.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex);
            _platformController.OnPlayerActorEntered += OnPlayerActorEntered;

            // Set to final position if it is already activated
            if (ObjectInfo.Times == 0)
            {
                Vector3 finalPosition = sceneObjectGameEntity.Transform.Position;
                finalPosition.y -= DESCENDING_HEIGHT;
                sceneObjectGameEntity.Transform.Position = finalPosition;
            }

            return sceneObjectGameEntity;
        }

        private void OnPlayerActorEntered(object sender, IGameEntity playerActorGameEntity)
        {
            // Prevent duplicate triggers
            if (_gameStateManager.GetCurrentState() != GameState.Gameplay) return;

            if (!IsInteractableBasedOnTimesCount()) return;

            if (_isInteractionInProgress) return;
            _isInteractionInProgress = true;

            RequestForInteraction();
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            IGameEntity pedalSwitchEntity = GetGameEntity();
            Vector3 platformCenterPosition = _platformController.GetCollider().bounds.center;
            Vector3 actorStandingPosition = new(
                platformCenterPosition.x,
                _platformController.GetPlatformHeight(),
                platformCenterPosition.z);

            ActorMovementController actorMovementController = ctx.PlayerActorGameEntity.GetComponent<ActorMovementController>();

            yield return actorMovementController.MoveDirectlyToAsync(actorStandingPosition, 0, true);

            // Play descending animation
            Vector3 finalPosition = pedalSwitchEntity.Transform.Position;
            finalPosition.y -= DESCENDING_HEIGHT;

            yield return pedalSwitchEntity.Transform.MoveAsync(finalPosition,
                DESCENDING_ANIMATION_DURATION,
                AnimationCurveType.Sine);

            yield return ExecuteScriptAndWaitForFinishIfAnyAsync();

            yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);

            #if PAL3A
            FlipAndSaveSwitchState();
            #endif

            _isInteractionInProgress = false;
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                _platformController.OnPlayerActorEntered -= OnPlayerActorEntered;
                _platformController.Destroy();
                _platformController = null;
            }

            base.Deactivate();
        }
    }
}