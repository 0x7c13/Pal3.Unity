// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;
    using Core.Services;
    using Data;
    using MetaData;
    using Player;
    using State;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Trap)]
    public class TrapObject : SceneObject
    {
        private TilemapAutoTriggerController _triggerController;

        public TrapObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
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

        private void OnTriggerEntered(object sender, Vector2Int actorTilePosition)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new GameStateChangeRequest(GameState.Cutscene));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));

            CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wg007", 1));

            PlayerActorId playerActor = ServiceLocator.Instance.Get<PlayerManager>().GetPlayerActor();
            GameObject playerActorGo = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene()
                .GetActorGameObject((int) playerActor);

            // Let player actor fall down
            if (playerActorGo.GetComponent<Rigidbody>() is { } rigidbody)
            {
                rigidbody.useGravity = true;
                rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                rigidbody.isKinematic = false;
            }

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                GetCvdModelRenderer().StartOneTimeAnimation(true, () =>
                {
                    ExecuteScriptIfAny();
                });
            }
            else
            {
                ExecuteScriptIfAny();   
            }
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