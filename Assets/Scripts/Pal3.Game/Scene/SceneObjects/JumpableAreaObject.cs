// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Extensions;
    using Engine.Services;
    using GamePlay;
    using UnityEngine;

    [ScnSceneObject(SceneObjectType.JumpableArea)]
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

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            yield break;
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