// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Services;
    using GamePlay;

    using Color = Core.Primitives.Color;

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

        public override IGameEntity Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            _triggerController = sceneObjectGameEntity.AddComponent<TilemapTriggerController>();
            _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;
            _triggerController.OnPlayerActorExited += OnPlayerActorExited;

            return sceneObjectGameEntity;
        }

        private void OnPlayerActorEntered(object sender, (int x, int y) tilePosition)
        {
            _gamePlayManager.PlayerActorEnteredJumpableArea();
        }

        private void OnPlayerActorExited(object sender, (int x, int y) tilePosition)
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