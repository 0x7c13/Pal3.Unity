// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
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
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Services;
    using Data;
    using MetaData;
    using Renderer;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.Switch)]
    public sealed class SwitchObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        private SceneObjectMeshCollider _meshCollider;

        #if PAL3
        private readonly string _interactionIndicatorModelPath = FileConstants.ObjectFolderVirtualPath +
                                                                 CpkConstants.DirectorySeparator + "g03.cvd";
        private CvdModelRenderer _interactionIndicatorRenderer;
        private GameObject _interactionIndicatorGameObject;
        #endif

        public SwitchObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            #if PAL3A
            // All switches in PAL3A have 0xFF as Times, which means infinite times.
            // but all switches in PAL3A can only be interacted once.
            // So we set Times to 1 here.
            // We can also disable the switch when SwitchState == 1 for PAL3A,
            // but this way is simpler.
            if (ObjectInfo.Times == 0xFF)
            {
                ObjectInfo.Times = 1;
            }
            #endif

            if (ObjectInfo.IsNonBlocking == 0)
            {
                if (!(ObjectInfo.SwitchState == 1 && ObjectInfo.Parameters[0] == 1) &&
                    !IsDivineTreeMasterFlower())
                {
                    // Add collider to block player
                    _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
                }
            }

            #if PAL3
            // Add interaction indicator when switch times is greater than 0
            // and Parameter[1] is 0 (1 means the switch is not directly interactable)
            if (ObjectInfo.Times > 0 && ObjectInfo.Parameters[1] == 0)
            {
                Vector3 switchPosition = sceneGameObject.transform.position;
                _interactionIndicatorGameObject = new GameObject("Switch_Interaction_Indicator");
                _interactionIndicatorGameObject.transform.SetParent(sceneGameObject.transform, false);
                _interactionIndicatorGameObject.transform.position =
                    new Vector3(switchPosition.x, GetRendererBounds().max.y + 1f, switchPosition.z);
                (CvdFile cvdFile, ITextureResourceProvider textureProvider) =
                    resourceProvider.GetCvd(_interactionIndicatorModelPath);
                _interactionIndicatorRenderer = _interactionIndicatorGameObject.AddComponent<CvdModelRenderer>();
                _interactionIndicatorRenderer.Init(cvdFile,
                    resourceProvider.GetMaterialFactory(),
                    textureProvider,
                    tintColor);
                _interactionIndicatorRenderer.LoopAnimation();
            }
            #endif

            return sceneGameObject;
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return Activated &&
                   distance < MAX_INTERACTION_DISTANCE &&
                   ObjectInfo.Times > 0 &&
                   ObjectInfo.Parameters[1] is 0 or 2; // 0 means directly interactable,
                                                       // 2 means interactable but executing script only
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            var shouldResetCamera = false;

            #if PAL3
            if (ObjectInfo.Parameters[1] == 2) // 2 means interactable but executing script only
            {
                ExecuteScriptIfAny();
                yield break;
            }
            if (ctx.InitObjectId != ObjectInfo.Id && !IsFullyVisibleToCamera())
            {
                shouldResetCamera = true;
                yield return MoveCameraToLookAtPointAsync(
                    GetGameObject().transform.position,
                    ctx.PlayerActorGameObject);
                CameraFocusOnObject(ObjectInfo.Id);
            }
            #endif

            var currentSwitchState = ObjectInfo.SwitchState;

            ToggleAndSaveSwitchState();

            #if PAL3
            if (ObjectInfo.Times == 0 && _interactionIndicatorGameObject != null)
            {
                _interactionIndicatorRenderer.Dispose();
                Object.Destroy(_interactionIndicatorRenderer);
                Object.Destroy(_interactionIndicatorGameObject);
            }
            #endif

            if (ctx.InitObjectId == ObjectInfo.Id &&
                ObjectInfo.Parameters[1] == 0)
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
                yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true,
                    currentSwitchState == 0 ? 1f : -1f);

                // Remove collider to allow player to pass through
                if (ObjectInfo.Parameters[0] == 1 && _meshCollider != null)
                {
                    Object.Destroy(_meshCollider);
                }
            }

            #if PAL3
            ExecuteScriptIfAny();
            yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);
            #elif PAL3A
            if (ObjectInfo.LinkedObjectId != 0xFFFF)
            {
                SceneObject linkedObject = ctx.CurrentScene.GetSceneObject(ObjectInfo.LinkedObjectId);

                shouldResetCamera = true;

                yield return MoveCameraToLookAtPointAsync(
                    GameBoxInterpreter.ToUnityPosition(linkedObject.ObjectInfo.GameBoxPosition),
                    ctx.PlayerActorGameObject);

                if (!string.IsNullOrEmpty(linkedObject.ObjectInfo.SfxName))
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand(linkedObject.ObjectInfo.SfxName, 1));
                }

                yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);
                yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
                yield return new WaitForSeconds(1);
            }
            #endif

            #if PAL3
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

                var allActivatedSceneObjects = ctx.CurrentScene.GetAllActivatedSceneObjects();
                var allFlowerObjects = ctx.CurrentScene.GetAllSceneObjects().Where(
                    _ => allActivatedSceneObjects.Contains(_.Key) &&
                         _.Value.ObjectInfo.Type == ScnSceneObjectType.DivineTreeFlower);
                foreach (var flowerObject in allFlowerObjects)
                {
                    // Re-activate all flowers in current scene to refresh their state
                    ctx.CurrentScene.DeactivateSceneObject(flowerObject.Key);
                    ctx.CurrentScene.ActivateSceneObject(flowerObject.Key);
                }
            }
            #endif

            if (shouldResetCamera)
            {
                ResetCamera();
            }
        }

        private bool IsDivineTreeMasterFlower()
        {
            return ObjectInfo.Parameters[2] == 1 &&
                   ObjectInfo.Id == 22 &&
                   SceneInfo.Is("m16", "4");
        }

        public override void Deactivate()
        {
            if (_meshCollider != null)
            {
                Object.Destroy(_meshCollider);
            }

            #if PAL3
            if (_interactionIndicatorRenderer != null)
            {
                _interactionIndicatorRenderer.Dispose();
                Object.Destroy(_interactionIndicatorRenderer);
            }

            if (_interactionIndicatorGameObject != null)
            {
                Object.Destroy(_interactionIndicatorGameObject);
            }
            #endif

            base.Deactivate();
        }
    }
}