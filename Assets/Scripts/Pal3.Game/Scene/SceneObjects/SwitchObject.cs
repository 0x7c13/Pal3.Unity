// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using System.Linq;
    using Command;
    using Command.Extensions;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Scn;
    using Core.Utilities;
    using Data;
    using Engine.Abstraction;
    using Engine.DataLoader;
    using Engine.Extensions;
    using Rendering.Renderer;

    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;
    
    [ScnSceneObject(SceneObjectType.Switch)]
    public sealed class SwitchObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 3f;

        private SceneObjectMeshCollider _meshCollider;

        private const string INTERACTION_INDICATOR_MODEL_FILE_NAME = "g03.cvd";

        private CvdModelRenderer _interactionIndicatorRenderer;
        private IGameEntity _interactionIndicatorGameEntity;

        public SwitchObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            if (ObjectInfo.SwitchState == 1 && ModelType == SceneObjectModelType.CvdModel)
            {
                CvdModelRenderer cvdModelRenderer = GetCvdModelRenderer();
                cvdModelRenderer.SetCurrentTime(cvdModelRenderer.GetDefaultAnimationDuration());
            }

            if (ObjectInfo.IsNonBlocking == 0)
            {
                if (!(ObjectInfo.SwitchState == 1 && ObjectInfo.Parameters[0] == 1) &&
                    !IsDivineTreeMasterFlower())
                {
                    // Add collider to block player
                    _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
                }
            }

            // Add interaction indicator when switch times is greater than 0
            // and Parameter[1] is 0 (1 means the switch is not directly interactable)
            if (ObjectInfo.Times > 0 && ObjectInfo.Parameters[1] == 0)
            {
                string interactionIndicatorModelPath = FileConstants.GetGameObjectModelFileVirtualPath(
                    INTERACTION_INDICATOR_MODEL_FILE_NAME);

                Vector3 switchPosition = sceneObjectGameEntity.Transform.Position;
                _interactionIndicatorGameEntity = new GameEntity("Switch_Interaction_Indicator");
                _interactionIndicatorGameEntity.SetParent(sceneObjectGameEntity, worldPositionStays: false);
                _interactionIndicatorGameEntity.Transform.Position =
                    new Vector3(switchPosition.x, GetRendererBounds().max.y + 1f, switchPosition.z);

                CvdFile cvdFile = resourceProvider.GetGameResourceFile<CvdFile>(interactionIndicatorModelPath);
                ITextureResourceProvider textureProvider = resourceProvider.CreateTextureResourceProvider(
                    CoreUtility.GetDirectoryName(interactionIndicatorModelPath, CpkConstants.DirectorySeparatorChar));
                _interactionIndicatorRenderer = _interactionIndicatorGameEntity.AddComponent<CvdModelRenderer>();
                _interactionIndicatorRenderer.Init(cvdFile,
                    textureProvider,
                    resourceProvider.GetMaterialFactory(),
                    tintColor);
                _interactionIndicatorRenderer.LoopAnimation();
            }

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return IsActivated &&
                   distance < MAX_INTERACTION_DISTANCE &&
                   ObjectInfo.Times > 0 &&
                   ObjectInfo.Parameters[1] is 0 or 2; // 0 means directly interactable,
                                                       // 2 means interactable but executing script only
        }

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            var shouldResetCamera = false;

            if (ObjectInfo.Parameters[1] == 2) // 2 means interactable but executing script only
            {
                ExecuteScriptIfAny();
                yield break;
            }

            if (ctx.InitObjectId != ObjectInfo.Id && !IsFullyVisibleToCamera())
            {
                shouldResetCamera = true;
                yield return MoveCameraToLookAtPointAsync(
                    GetGameEntity().Transform.Position,
                    ctx.PlayerActorGameEntity.Transform);
                CameraFocusOnObject(ObjectInfo.Id);
            }

            var switchStateBeforeInteraction = ObjectInfo.SwitchState;

            FlipAndSaveSwitchState();

            if (ObjectInfo.Times == 0 && _interactionIndicatorGameEntity != null)
            {
                _interactionIndicatorRenderer.Dispose();
                _interactionIndicatorRenderer.Destroy();
                _interactionIndicatorRenderer = null;

                _interactionIndicatorGameEntity.Destroy();
                _interactionIndicatorGameEntity = null;
            }

            if (ctx.StartedByPlayer &&
                ctx.InitObjectId == ObjectInfo.Id &&
                ObjectInfo.Parameters[1] == 0)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new PlayerActorLookAtSceneObjectCommand(ObjectInfo.Id));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                        ActorConstants.ActionToNameMap[ActorActionType.Check], 1));
            }

            PlaySfxIfAny();


            if (ModelType == SceneObjectModelType.CvdModel)
            {
                yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true,
                    switchStateBeforeInteraction == 0 ? 1f : -1f);

                // Remove collider to allow player to pass through
                if (ObjectInfo.Parameters[0] == 1 && _meshCollider != null)
                {
                    _meshCollider.Destroy();
                }
            }

            ExecuteScriptIfAny();
            yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);

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
                         _.Value.ObjectInfo.Type == SceneObjectType.DivineTreeFlower);
                foreach (var flowerObject in allFlowerObjects)
                {
                    // Re-activate all flowers in current scene to refresh their state
                    ctx.CurrentScene.DeactivateSceneObject(flowerObject.Key);
                    ctx.CurrentScene.ActivateSceneObject(flowerObject.Key);
                }
            }

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
                _meshCollider.Destroy();
                _meshCollider = null;
            }

            if (_interactionIndicatorRenderer != null)
            {
                _interactionIndicatorRenderer.Dispose();
                _interactionIndicatorRenderer.Destroy();
                _interactionIndicatorRenderer = null;
            }

            if (_interactionIndicatorGameEntity != null)
            {
                _interactionIndicatorGameEntity.Destroy();
                _interactionIndicatorGameEntity = null;
            }

            base.Deactivate();
        }
    }
}

#endif