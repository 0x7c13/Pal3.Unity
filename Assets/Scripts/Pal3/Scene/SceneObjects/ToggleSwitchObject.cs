// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Data;
    using MetaData;
    using Renderer;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.ToggleSwitch)]
    public sealed class ToggleSwitchObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 3f;

        private SceneObjectMeshCollider _meshCollider;

        public ToggleSwitchObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            if (ObjectInfo.SwitchState == 1 && ModelType == SceneObjectModelType.CvdModel)
            {
                CvdModelRenderer cvdModelRenderer = GetCvdModelRenderer();
                cvdModelRenderer.SetCurrentTime(cvdModelRenderer.GetDefaultAnimationDuration());
            }

            // Add collider to block player
            _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();

            return sceneGameObject;
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return Activated &&
                   distance < MAX_INTERACTION_DISTANCE &&
                   ObjectInfo.Times > 0;
        }

        // TODO: Implement WuLing interaction logic for this switch
        // Can only toggle this switch if ObjectInfo.WuLing matches current player actor's WuLing.
        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (ObjectInfo is { Times: INFINITE_TIMES_COUNT, CanOnlyBeTriggeredOnce: 1 })
            {
                ObjectInfo.Times = 1;
            }

            if (!IsInteractableBasedOnTimesCount()) yield break;

            FlipAndSaveSwitchState();

            if (ctx.StartedByPlayer && ctx.InitObjectId == ObjectInfo.Id)
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
                yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);
            }
            else if (ObjectInfo.Parameters[1] == 180) // Special case for M12-2
            {
                Transform objectTransform = GetGameObject().transform;
                Quaternion rotation = objectTransform.rotation;
                Quaternion targetRotation = rotation * Quaternion.Euler(0, 180, 0);

                yield return objectTransform.RotateAsync(targetRotation, 3f, AnimationCurveType.Sine);

                SaveCurrentYRotation();
            }

            // Disable associated effect object if any
            if (ObjectInfo.EffectModelType != 0)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new SceneActivateObjectCommand((int)ObjectInfo.EffectModelType, 0));
            }

            var shouldResetCamera = false;

            // Interact with linked object if any
            if (ObjectInfo.LinkedObjectId != INVALID_SCENE_OBJECT_ID)
            {
                SceneObject linkedObject = ctx.CurrentScene.GetSceneObject(ObjectInfo.LinkedObjectId);
                Vector3 linkedObjectGameBoxPosition = linkedObject.ObjectInfo.GameBoxPosition;

                shouldResetCamera = true;

                if (!SceneInfo.Is("m06", "2") &&
                    linkedObject.ObjectInfo.Type
                        is not ScnSceneObjectType.LiftingPlatform
                        and not ScnSceneObjectType.MovableCarrier
                        and not ScnSceneObjectType.WaterSurface)
                {
                    yield return MoveCameraToLookAtPointAsync(
                        GameBoxInterpreter.ToUnityPosition(linkedObjectGameBoxPosition),
                        ctx.PlayerActorGameObject);
                }

                if (ObjectInfo.Parameters[1] == 1)
                {
                    yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);
                    yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
                }
                else
                {
                    ExecuteScriptIfAny();
                    if (SceneInfo.Is("m06", "2"))
                    {
                        yield return new WaitForSeconds(1);
                    }
                    yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);
                }

                yield return new WaitForSeconds(1);
            }
            else
            {
                yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
            }

            if (shouldResetCamera)
            {
                ResetCamera();
            }
        }

        public override void Deactivate()
        {
            if (_meshCollider != null)
            {
                Object.Destroy(_meshCollider);
                _meshCollider = null;
            }

            base.Deactivate();
        }
    }
}

#endif