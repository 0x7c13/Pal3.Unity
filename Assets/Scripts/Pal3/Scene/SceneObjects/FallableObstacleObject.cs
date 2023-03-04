// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.Animation;
    using Core.DataReader.Nav;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Services;
    using Data;
    using MetaData;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.FallableObstacle)]
    public sealed class FallableObstacleObject : SceneObject
    {
        private const float FALLING_DURATION = 1.1f;

        private readonly Tilemap _tilemap;

        private SceneObjectMeshCollider _meshCollider;
        private TilemapTriggerController _triggerController;

        public FallableObstacleObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _tilemap = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene().GetTilemap();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (Activated) return GetGameObject();

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

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));

            PlaySfx("wg009");

            GameObject obstacleObject = GetGameObject();

            Vector3 currentPosition = obstacleObject.transform.position;

            var finalYPosition = ctx.PlayerActorGameObject.transform.position.y;
            if (_tilemap.TryGetTile(currentPosition, ObjectInfo.LayerIndex, out NavTile tile))
            {
                finalYPosition = GameBoxInterpreter.ToUnityYPosition(tile.GameBoxYPosition);
            }

            yield return AnimationHelper.MoveTransformAsync(obstacleObject.transform,
                new Vector3(currentPosition.x, finalYPosition, currentPosition.z),
                FALLING_DURATION);

            SaveCurrentPosition();

            yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
        }

        public override void Deactivate()
        {
            if (_meshCollider != null)
            {
                Object.Destroy(_meshCollider);
            }

            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                Object.Destroy(_triggerController);
            }

            base.Deactivate();
        }
    }
}