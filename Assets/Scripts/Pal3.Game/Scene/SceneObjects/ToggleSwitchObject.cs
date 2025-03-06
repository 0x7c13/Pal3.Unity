// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

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
    using Engine.Animation;
    using Engine.Core.Abstraction;
    using Engine.Coroutine;
    using Engine.Extensions;
    using Rendering.Renderer;

    using Color = Core.Primitives.Color;
    using Quaternion = UnityEngine.Quaternion;

    [ScnSceneObject(SceneObjectType.ToggleSwitch)]
    public sealed class ToggleSwitchObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 3f;

        private SceneObjectMeshCollider _meshCollider;

        public ToggleSwitchObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
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

            // Add collider to block player
            _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return IsActivated &&
                   distance < MAX_INTERACTION_DISTANCE &&
                   ObjectInfo.Times > 0;
        }

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        // TODO: Implement element interaction logic for this switch
        // Can only toggle this switch if ObjectInfo.ElementType matches current player actor's element type.
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
                Pal3.Instance.Execute(new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
                Pal3.Instance.Execute(new PlayerActorLookAtSceneObjectCommand(ObjectInfo.Id));
                Pal3.Instance.Execute(new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                        ActorConstants.ActionToNameMap[ActorActionType.Check], 1));
            }

            PlaySfxIfAny();

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);
            }
            else if (ObjectInfo.Parameters[1] == 180) // Special case for M12-2
            {
                ITransform objectTransform = GetGameEntity().Transform;
                Quaternion rotation = objectTransform.Rotation;
                Quaternion targetRotation = rotation * Quaternion.Euler(0, 180, 0);

                yield return objectTransform.RotateAsync(targetRotation, 3f, AnimationCurveType.Sine);

                SaveCurrentYRotation();
            }

            // Disable associated effect object if any
            if (ObjectInfo.EffectModelType != 0)
            {
                Pal3.Instance.Execute(new SceneActivateObjectCommand((int)ObjectInfo.EffectModelType, 0));
            }

            bool shouldResetCamera = false;

            // Interact with linked object if any
            if (ObjectInfo.LinkedObjectId != INVALID_SCENE_OBJECT_ID)
            {
                SceneObject linkedObject = ctx.CurrentScene.GetSceneObject(ObjectInfo.LinkedObjectId);
                GameBoxVector3 linkedObjectGameBoxPosition = linkedObject.ObjectInfo.GameBoxPosition;

                shouldResetCamera = true;

                if (!SceneInfo.Is("m06", "2") &&
                    linkedObject.ObjectInfo.Type
                        is not SceneObjectType.LiftingPlatform
                        and not SceneObjectType.MovableCarrier
                        and not SceneObjectType.WaterSurface)
                {
                    yield return MoveCameraToLookAtPointAsync(
                        linkedObjectGameBoxPosition.ToUnityPosition(),
                        ctx.PlayerActorGameEntity.Transform);
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
                        yield return CoroutineYieldInstruction.WaitForSeconds(1);
                    }
                    yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);
                }

                yield return CoroutineYieldInstruction.WaitForSeconds(1);
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
                _meshCollider.Destroy();
                _meshCollider = null;
            }

            base.Deactivate();
        }
    }
}

#endif