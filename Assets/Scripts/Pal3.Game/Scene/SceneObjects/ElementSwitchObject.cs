// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
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
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Coroutine;
    using Engine.Extensions;
    using Rendering.Renderer;

    using Color = Core.Primitives.Color;

    [ScnSceneObject(SceneObjectType.ElementSwitch)]
    public sealed class ElementSwitchObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 3f;

        private SceneObjectMeshCollider _meshCollider;

        public ElementSwitchObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
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
                if (!(ObjectInfo.SwitchState == 1 && ObjectInfo.Parameters[0] == 1))
                {
                    // Add collider to block player
                    _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
                }
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

            if (ctx.StartedByPlayer &&
                ctx.InitObjectId == ObjectInfo.Id)
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

                // Remove collider to allow player to pass through
                if (ObjectInfo.Parameters[0] == 1 && _meshCollider != null)
                {
                    _meshCollider.Destroy();
                    _meshCollider = null;
                }
            }

            // Disable associated effect object if any
            if (ObjectInfo.EffectModelType != 0)
            {
                Pal3.Instance.Execute(new SceneActivateObjectCommand((int)ObjectInfo.EffectModelType, 0));
            }

            var shouldResetCamera = false;

            // Interact with linked object if any
            if (ObjectInfo.LinkedObjectId != INVALID_SCENE_OBJECT_ID)
            {
                SceneObject linkedObject = ctx.CurrentScene.GetSceneObject(ObjectInfo.LinkedObjectId);

                shouldResetCamera = true;

                yield return MoveCameraToLookAtPointAsync(
                    linkedObject.ObjectInfo.GameBoxPosition.ToUnityPosition(),
                    ctx.PlayerActorGameEntity.Transform);

                if (!string.IsNullOrEmpty(linkedObject.ObjectInfo.SfxName))
                {
                    Pal3.Instance.Execute(new PlaySfxCommand(linkedObject.ObjectInfo.SfxName, 1));
                }

                yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);
                yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
                yield return CoroutineYieldInstruction.WaitForSeconds(1);
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