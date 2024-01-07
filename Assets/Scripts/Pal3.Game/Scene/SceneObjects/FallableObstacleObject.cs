// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Game.Scene.SceneObjects
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
    using Engine.Core.Abstraction;
    using Engine.Extensions;

    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

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

        public override IGameEntity Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            // Add collider to block player
            _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();

            _triggerController = sceneObjectGameEntity.AddComponent<TilemapTriggerController>();
            _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            return sceneObjectGameEntity;
        }

        private void OnPlayerActorEntered(object sender, (int x, int y) tilePosition)
        {
            if (!IsInteractableBasedOnTimesCount()) return;
            RequestForInteraction();
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            Pal3.Instance.Execute(new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));

            PlaySfx("wg009");

            IGameEntity obstacleEntity = GetGameEntity();

            Vector3 currentPosition = obstacleEntity.Transform.Position;

            var finalYPosition = ctx.PlayerActorGameEntity.Transform.Position.y;
            if (ctx.CurrentScene.GetTilemap().TryGetTile(currentPosition,
                    ObjectInfo.LayerIndex,
                    out NavTile tile))
            {
                finalYPosition = tile.GameBoxYPosition.ToUnityYPosition();
            }

            yield return obstacleEntity.Transform.MoveAsync(
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