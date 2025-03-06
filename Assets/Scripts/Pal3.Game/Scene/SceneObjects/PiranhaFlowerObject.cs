// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.Extensions;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Core.Primitives;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Rendering.Renderer;

    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.PiranhaFlower)]
    public sealed class PiranhaFlowerObject : SceneObject
    {
        private TilemapTriggerController _triggerController;

        public PiranhaFlowerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
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
            RequestForInteraction();
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            #if PAL3
            Pal3.Instance.Execute(new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            Pal3.Instance.Execute(new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 0));
            Pal3.Instance.Execute(new CameraFocusOnSceneObjectCommand(ObjectInfo.Id));

            PlaySfx("wg008");

            yield return GetCvdModelRenderer().PlayAnimationAsync(2f, 1, 0.5f, true);

            int portalToFlowerObjectId = ObjectInfo.Parameters[2];

            Pal3.Instance.Execute(new CameraFocusOnSceneObjectCommand(portalToFlowerObjectId));

            SceneObject portalToFlowerObject = ctx.CurrentScene.GetSceneObject(portalToFlowerObjectId);

            CvdModelRenderer portalToFlowerObjectCvdModelRenderer = portalToFlowerObject.GetCvdModelRenderer();

            // Skip part of the animation, start from 80% of the animation
            portalToFlowerObjectCvdModelRenderer.SetCurrentTime(
                portalToFlowerObjectCvdModelRenderer.GetDefaultAnimationDuration() * 0.80f);

            // Play reverse animation on portal to flower object
            yield return portalToFlowerObjectCvdModelRenderer.PlayAnimationAsync(-2f, 1, 1f, false);

            Pal3.Instance.Execute(new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 1));
            Pal3.Instance.Execute(new ActorSetTilePositionCommand(ActorConstants.PlayerActorVirtualID,
                    ObjectInfo.Parameters[0],
                    ObjectInfo.Parameters[1]));
            Pal3.Instance.Execute(new CameraFollowPlayerCommand(1));
            #elif PAL3A
            Pal3.Instance.Execute(new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            Pal3.Instance.Execute(new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 0));
            Pal3.Instance.Execute(new CameraFocusOnSceneObjectCommand(ObjectInfo.Id));

            PlaySfx("wg008");

            yield return GetCvdModelRenderer().PlayAnimationAsync(1.9f, 1, 1f, true);

            Vector3 worldPosition = new GameBoxVector3(
                    ObjectInfo.Parameters[0],
                    0f,
                    ObjectInfo.Parameters[1]).ToUnityPosition();

            Pal3.Instance.Execute(new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 1));
            Pal3.Instance.Execute(new ActorSetWorldPositionCommand(ActorConstants.PlayerActorVirtualID, worldPosition.x, worldPosition.z));
            Pal3.Instance.Execute(new CameraFollowPlayerCommand(1));
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