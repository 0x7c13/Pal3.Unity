// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Actor.Controllers;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Navigation;

    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.SlideWay)]
    public sealed class SlideWayObject : SceneObject
    {
        private const uint ACTOR_SLIDE_SPEED = 25;

        private TilemapTriggerController _triggerController;
        private bool _isInteractionInProgress;

        public SlideWayObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            _triggerController = sceneObjectGameEntity.AddComponent<TilemapTriggerController>();
            _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            return sceneObjectGameEntity;
        }

        private void OnPlayerActorEntered(object sender, (int x, int y) tilePosition)
        {
            if (_isInteractionInProgress) return; // Prevent re-entry
            _isInteractionInProgress = true;
            RequestForInteraction();
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            IGameEntity playerActorGameEntity = ctx.PlayerActorGameEntity;

            var waypoints = new Vector3[ObjectInfo.Path.NumberOfWaypoints];
            for (var i = 0; i < ObjectInfo.Path.NumberOfWaypoints; i++)
            {
                waypoints[i] = ObjectInfo.Path.GameBoxWaypoints[i].ToUnityPosition();
            }

            var movementController = playerActorGameEntity.GetComponent<ActorMovementController>();
            movementController.CancelMovement();

            var actorController = playerActorGameEntity.GetComponent<ActorController>();

            // Temporarily set the speed to a higher value to make the actor slide faster
            actorController.GetActor().ChangeMoveSpeed(ACTOR_SLIDE_SPEED);

            movementController.SetupPath(waypoints, MovementMode.Run, EndOfPathActionType.Idle, ignoreObstacle: true);

            while (movementController.IsMovementInProgress())
            {
                yield return null;
            }

            // Restore the original speed
            actorController.GetActor().ResetMoveSpeed();

            ExecuteScriptIfAny();
            _isInteractionInProgress = false;
        }

        public override void Deactivate()
        {
            _isInteractionInProgress = false;

            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                _triggerController.Destroy();
                _triggerController = null;
            }

            base.Deactivate();
        }
    }
}