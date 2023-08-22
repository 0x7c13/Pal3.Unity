// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Common;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Core.Services;
    using Data;
    using GamePlay;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.JumpableArea)]
    public sealed class JumpableAreaObject : SceneObject
    {
        private TilemapTriggerController _triggerController;
        private readonly PlayerGamePlayManager _gamePlayManager;

        public JumpableAreaObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _gamePlayManager = ServiceLocator.Instance.Get<PlayerGamePlayManager>();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (IsActivated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            _triggerController = sceneGameObject.AddComponent<TilemapTriggerController>();
            _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;
            _triggerController.OnPlayerActorExited += OnPlayerActorExited;

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, Vector2Int actorTilePosition)
        {
            _gamePlayManager.PlayerActorEnteredJumpableArea();
        }

        private void OnPlayerActorExited(object sender, Vector2Int actorTilePosition)
        {
            _gamePlayManager.PlayerActorExitedJumpableArea();
        }

        public override void Deactivate()
        {
            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                _triggerController.OnPlayerActorExited -= OnPlayerActorExited;
                _triggerController.Destroy();
                _triggerController = null;
            }

            base.Deactivate();
        }
    }
}