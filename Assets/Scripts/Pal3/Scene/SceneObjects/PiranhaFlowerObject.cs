// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Services;
    using Data;
    using MetaData;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.PiranhaFlower)]
    public class PiranhaFlowerObject : SceneObject
    {
        private TilemapAutoTriggerController _triggerController;

        public PiranhaFlowerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            
            _triggerController = sceneGameObject.AddComponent<TilemapAutoTriggerController>();
            _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex);
            _triggerController.OnTriggerEntered += OnTriggerEntered;

            return sceneGameObject;
        }

        private void OnTriggerEntered(object sender, Vector2Int playerActorTilePosition)
        {
            #if PAL3
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new PlayerEnableInputCommand(0));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 0));
            
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFocusOnSceneObjectCommand(ObjectInfo.Id));

            CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wg008", 1));
            
            GetCvdModelRenderer().PlayAnimation(2f, 1, 0.5f, true, () =>
            {
                var portalToFlowerObjectId = ObjectInfo.Parameters[2];
            
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new CameraFocusOnSceneObjectCommand(portalToFlowerObjectId));

                SceneObject portalToFlowerObject = ServiceLocator.Instance.Get<SceneManager>()
                    .GetCurrentScene().
                    GetSceneObject(portalToFlowerObjectId);
                
                var portalToFlowerObjectCvdModelRenderer = portalToFlowerObject.GetCvdModelRenderer();
                
                // Skip part of the animation, start from 80% of the animation
                portalToFlowerObjectCvdModelRenderer.SetCurrentTime(
                    portalToFlowerObjectCvdModelRenderer.GetDefaultAnimationDuration() * 0.80f);
                
                // Play reverse animation on portal to flower object
                portalToFlowerObjectCvdModelRenderer.PlayAnimation(-2f, 1, 1f, false, () =>
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(
                        new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 1));
                    CommandDispatcher<ICommand>.Instance.Dispatch(
                        new ActorSetTilePositionCommand(-1, 
                            ObjectInfo.Parameters[0],
                            ObjectInfo.Parameters[1]));
                    CommandDispatcher<ICommand>.Instance.Dispatch(
                        new CameraFreeCommand(1));
                    CommandDispatcher<ICommand>.Instance.Dispatch(
                        new PlayerEnableInputCommand(1));
                });
            });
            #elif PAL3A
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new PlayerEnableInputCommand(0));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 0));
            
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFocusOnSceneObjectCommand(ObjectInfo.Id));

            CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wg008", 1));
            
            GetCvdModelRenderer().PlayAnimation(1.9f, 1, 1f, true, () =>
            {
                Vector3 worldPosition = GameBoxInterpreter.ToUnityPosition(
                    new Vector3(ObjectInfo.Parameters[0],
                        0f,
                        ObjectInfo.Parameters[1]));
                
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 1));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorSetWorldPositionCommand(-1, worldPosition.x, worldPosition.z));
                
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new CameraFreeCommand(1));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new PlayerEnableInputCommand(1));
                
            });
            #endif
        }

        public override void Deactivate()
        {
            if (_triggerController != null)
            {
                _triggerController.OnTriggerEntered -= OnTriggerEntered;
                Object.Destroy(_triggerController);
            }

            base.Deactivate();
        }
    }
}