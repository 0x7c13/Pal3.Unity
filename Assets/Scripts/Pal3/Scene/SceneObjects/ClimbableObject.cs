// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Actor;
    using Common;
    using Core.DataReader.Scn;
    using Core.Services;
    using MetaData;
    using Player;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Climbable)]
    public sealed class ClimbableObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 6f;

        private readonly SceneManager _sceneManager;
        private readonly PlayerManager _playerManager;
        private readonly PlayerGamePlayController _playerGamePlayController;

        public ClimbableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _sceneManager = ServiceLocator.Instance.Get<SceneManager>();
            _playerManager = ServiceLocator.Instance.Get<PlayerManager>();
            _playerGamePlayController = ServiceLocator.Instance.Get<PlayerGamePlayController>();
        }

        public override bool IsInteractable(InteractionContext ctx)
        {
            return Activated && ctx.DistanceToActor < MAX_INTERACTION_DISTANCE;
        }

        public override IEnumerator Interact(bool triggerredByPlayer)
        {
            if (ObjectInfo.ScriptId == ScriptConstants.InvalidScriptId)
            {
                var fromTilePosition = new Vector2Int(ObjectInfo.Parameters[0], ObjectInfo.Parameters[1]);
                var toTilePosition = new Vector2Int(ObjectInfo.Parameters[2], ObjectInfo.Parameters[3]);
                var crossLayer = ObjectInfo.Parameters[4] == 1;

                Scene scene = _sceneManager.GetCurrentScene();
                PlayerActorId playerActor = _playerManager.GetPlayerActor();
                GameObject playerActorGo = scene.GetActorGameObject((int) playerActor);
                var actorMovementController = playerActorGo.GetComponent<ActorMovementController>();
                GameObject climbableObject = GetGameObject();

                Vector3 climbableObjectPosition = climbableObject.transform.position;
                Vector3 climbableObjectFacing = Quaternion.Euler(0f, -ObjectInfo.YRotation, 0f) * Vector3.forward;

                Tilemap tileMap = scene.GetTilemap();

                int upperLayer, lowerLayer;

                if (crossLayer)
                {
                    upperLayer = 1;
                    lowerLayer = 0;
                }
                else
                {
                    var playerCurrentLayer = actorMovementController.GetCurrentLayerIndex();
                    upperLayer = playerCurrentLayer;
                    lowerLayer = playerCurrentLayer;
                }

                Vector3 fromPosition = tileMap.GetWorldPosition(fromTilePosition, lowerLayer);
                Vector3 toPosition = tileMap.GetWorldPosition(toTilePosition, upperLayer);

                Vector3 upperStandingPosition, lowerStandingPosition;
                if (fromPosition.y > toPosition.y)
                {
                    upperStandingPosition = fromPosition;
                    lowerStandingPosition = toPosition;
                }
                else
                {
                    upperStandingPosition = toPosition;
                    lowerStandingPosition = fromPosition;
                }

                Vector3 upperPosition = -climbableObjectFacing.normalized * 1f + climbableObjectPosition;
                Vector3 lowerPosition = climbableObjectFacing.normalized * 1f + climbableObjectPosition;
                upperPosition.y = upperStandingPosition.y;
                lowerPosition.y = lowerStandingPosition.y;

                Vector3 playerActorPosition = actorMovementController.GetWorldPosition();
                var climbUp = Mathf.Abs(playerActorPosition.y - lowerPosition.y) <
                                  Mathf.Abs(playerActorPosition.y - upperPosition.y);

                var climbableHeight = upperPosition.y - lowerPosition.y;

                yield return _playerGamePlayController.PlayerActorMoveToClimbableObjectAndClimb(climbableObject,
                    climbUp,
                    false,
                    climbableHeight,
                    lowerPosition,
                    lowerStandingPosition,
                    upperPosition,
                    upperStandingPosition,
                    lowerLayer,
                    upperLayer);
            }
            else
            {
                yield return ExecuteScriptAndWaitForFinishIfAny();
            }
        }
    }
}