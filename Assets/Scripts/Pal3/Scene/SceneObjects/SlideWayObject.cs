// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Actor;
    using Common;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Data;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.SlideWay)]
    public sealed class SlideWayObject : SceneObject
    {
        private const uint ACTOR_SLIDE_SPEED = 300;

        private TilemapTriggerController _triggerController;
        private bool _isInteractionInProgress;

        public SlideWayObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (Activated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            _triggerController = sceneGameObject.AddComponent<TilemapTriggerController>();
            _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, Vector2Int actorTilePosition)
        {
            if (_isInteractionInProgress) return; // Prevent re-entry
            _isInteractionInProgress = true;
            RequestForInteraction();
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            GameObject playerActorGameObject = ctx.PlayerActorGameObject;

            var waypoints = new Vector3[ObjectInfo.Path.NumberOfWaypoints];
            for (var i = 0; i < ObjectInfo.Path.NumberOfWaypoints; i++)
            {
                waypoints[i] = GameBoxInterpreter.ToUnityPosition(ObjectInfo.Path.GameBoxWaypoints[i]);
            }

            var actorController = playerActorGameObject.GetComponent<ActorController>();
            uint originalSpeed = actorController.GetActor().Info.Speed;

            // Temporarily set the speed to a higher value to make the actor slide
            actorController.GetActor().Info.Speed = ACTOR_SLIDE_SPEED;

            var movementController = playerActorGameObject.GetComponent<ActorMovementController>();
            movementController.CancelMovement();
            movementController.SetupPath(waypoints, 1, EndOfPathActionType.Idle, ignoreObstacle: true);

            while (movementController.IsMovementInProgress())
            {
                yield return null;
            }

            // Restore the original speed
            actorController.GetActor().Info.Speed = originalSpeed;

            ExecuteScriptIfAny();
            _isInteractionInProgress = false;
        }

        public override void Deactivate()
        {
            _isInteractionInProgress = false;

            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                Object.Destroy(_triggerController);
            }

            base.Deactivate();
        }
    }
}