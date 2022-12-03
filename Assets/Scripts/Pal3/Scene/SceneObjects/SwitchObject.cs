// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;
    using Core.Services;
    using Data;
    using MetaData;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.Switch)]
    public class SwitchObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 5f;

        private SceneObjectMeshCollider _meshCollider;

        private readonly Scene _currentScene;

        public SwitchObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _currentScene = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            if (ObjectInfo.IsNonBlocking == 0)
            {
                if (!(ObjectInfo.SwitchState == 1 && ObjectInfo.Parameters[0] == 1) &&
                    !IsDivineTreeMasterFlower())
                {
                    // Add collider to block player
                    _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
                }
            }
            return sceneGameObject;
        }

        public override bool IsInteractable(InteractionContext ctx)
        {
            return Activated && ctx.DistanceToActor < MAX_INTERACTION_DISTANCE && ObjectInfo.Times > 0;
        }

        public override IEnumerator Interact(bool triggerredByPlayer)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            var currentSwitchState = ObjectInfo.SwitchState;

            ToggleAndSaveSwitchState();

            if (triggerredByPlayer)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new PlayerActorLookAtSceneObjectCommand(ObjectInfo.Id));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                        ActorConstants.ActionNames[ActorActionType.Check], 1));
            }

            PlaySfxIfAny();

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                yield return GetCvdModelRenderer().PlayOneTimeAnimation(true,
                    currentSwitchState == 0 ? 1f : -1f);

                // Remove collider to allow player to pass through
                if (ObjectInfo.Parameters[0] == 1 && _meshCollider != null)
                {
                    Object.Destroy(_meshCollider);
                }
            }

            ExecuteScriptIfAny();

            yield return ActivateOrInteractWithLinkedObjectIfAny();

            // Special handling for master flower switch located in
            // the scene m16 4
            if (IsDivineTreeMasterFlower())
            {
                // Save master flower switch state
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new SceneSaveGlobalObjectSwitchStateCommand(SceneInfo.CityName,
                        SceneInfo.SceneName,
                        ObjectInfo.Id,
                        ObjectInfo.SwitchState));

                var allActivatedSceneObjects = _currentScene.GetAllActivatedSceneObjects();
                var allFlowerObjects = _currentScene.GetAllSceneObjects().Where(
                    _ => allActivatedSceneObjects.Contains(_.Key) &&
                         _.Value.ObjectInfo.Type == ScnSceneObjectType.DivineTreeFlower);
                foreach (var flowerObject in allFlowerObjects)
                {
                    // Re-activate all flowers in current scene to refresh their state
                    _currentScene.DeactivateSceneObject(flowerObject.Key);
                    _currentScene.ActivateSceneObject(flowerObject.Key);
                }
            }
        }

        private bool IsDivineTreeMasterFlower()
        {
            return ObjectInfo.Parameters[2] == 1 &&
                   ObjectInfo.Id == 22 &&
                   SceneInfo.CityName.Equals("m16", StringComparison.OrdinalIgnoreCase) &&
                   SceneInfo.SceneName.Equals("4", StringComparison.OrdinalIgnoreCase);
        }
    }
}