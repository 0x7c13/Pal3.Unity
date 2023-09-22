// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Nav;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Animation;
    using Engine.Extensions;
    using UnityEngine;

    [ScnSceneObject(SceneObjectType.FallableObstacle)]
    public sealed class FallableObstacleObject : SceneObject
    {
        private const float FALLING_DURATION = 1.1f;

        private SceneObjectMeshCollider _meshCollider;
        private TilemapTriggerController _triggerController;

        public FallableObstacleObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (IsActivated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // Add collider to block player
            _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();

            _triggerController = sceneGameObject.AddComponent<TilemapTriggerController>();
            _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, Vector2Int actorTilePosition)
        {
            if (!IsInteractableBasedOnTimesCount()) return;
            RequestForInteraction();
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));

            PlaySfx("wg009");

            GameObject obstacleObject = GetGameObject();

            Vector3 currentPosition = obstacleObject.transform.position;

            var finalYPosition = ctx.PlayerActorGameObject.transform.position.y;
            if (ctx.CurrentScene.GetTilemap().TryGetTile(currentPosition,
                    ObjectInfo.LayerIndex,
                    out NavTile tile))
            {
                finalYPosition = tile.GameBoxYPosition.ToUnityYPosition();
            }

            yield return obstacleObject.transform.MoveAsync(
                new Vector3(currentPosition.x, finalYPosition, currentPosition.z),
                FALLING_DURATION);

            SaveCurrentPosition();

            yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
        }

        public override void Deactivate()
        {
            if (_meshCollider != null)
            {
                _meshCollider.Destroy();
                _meshCollider = null;
            }

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

#endif