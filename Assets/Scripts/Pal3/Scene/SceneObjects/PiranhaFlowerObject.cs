// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.Contracts;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Core.GameBox;
    using Data;
    using MetaData;
    using UnityEngine;

    [ScnSceneObject(SceneObjectType.PiranhaFlower)]
    public sealed class PiranhaFlowerObject : SceneObject
    {
        private TilemapTriggerController _triggerController;

        public PiranhaFlowerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (IsActivated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            _triggerController = sceneGameObject.AddComponent<TilemapTriggerController>();
            _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, Vector2Int playerActorTilePosition)
        {
            RequestForInteraction();
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            #if PAL3
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 0));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFocusOnSceneObjectCommand(ObjectInfo.Id));

            PlaySfx("wg008");

            yield return GetCvdModelRenderer().PlayAnimationAsync(2f, 1, 0.5f, true);

            var portalToFlowerObjectId = ObjectInfo.Parameters[2];

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFocusOnSceneObjectCommand(portalToFlowerObjectId));

            SceneObject portalToFlowerObject = ctx.CurrentScene.GetSceneObject(portalToFlowerObjectId);

            var portalToFlowerObjectCvdModelRenderer = portalToFlowerObject.GetCvdModelRenderer();

            // Skip part of the animation, start from 80% of the animation
            portalToFlowerObjectCvdModelRenderer.SetCurrentTime(
                portalToFlowerObjectCvdModelRenderer.GetDefaultAnimationDuration() * 0.80f);

            // Play reverse animation on portal to flower object
            yield return portalToFlowerObjectCvdModelRenderer.PlayAnimationAsync(-2f, 1, 1f, false);

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 1));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetTilePositionCommand(ActorConstants.PlayerActorVirtualID,
                    ObjectInfo.Parameters[0],
                    ObjectInfo.Parameters[1]));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFollowPlayerCommand(1));
            #elif PAL3A
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 0));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFocusOnSceneObjectCommand(ObjectInfo.Id));

            PlaySfx("wg008");

            yield return  GetCvdModelRenderer().PlayAnimationAsync(1.9f, 1, 1f, true);

            Vector3 worldPosition = new Vector3(
                    ObjectInfo.Parameters[0],
                    0f,
                    ObjectInfo.Parameters[1]).ToUnityPosition();

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 1));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetWorldPositionCommand(ActorConstants.PlayerActorVirtualID, worldPosition.x, worldPosition.z));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFollowPlayerCommand(1));
            #endif
        }

        public override void Deactivate()
        {
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