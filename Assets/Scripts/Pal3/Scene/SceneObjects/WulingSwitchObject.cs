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
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Data;
    using MetaData;
    using Renderer;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.WuLingSwitch)]
    public sealed class WuLingSwitchObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        private SceneObjectMeshCollider _meshCollider;

        public WuLingSwitchObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
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

            if (ObjectInfo.IsNonBlocking == 0)
            {
                if (!(ObjectInfo.SwitchState == 1 && ObjectInfo.Parameters[0] == 1))
                {
                    // Add collider to block player
                    _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
                }
            }

            return sceneGameObject;
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return Activated &&
                   distance < MAX_INTERACTION_DISTANCE &&
                   ObjectInfo is {SwitchState: 0, Times: > 0} &&
                   ObjectInfo.Parameters[1] is 0 or 2; // 0 means directly interactable,
                                                       // 2 means interactable but executing script only
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;
            if (ObjectInfo.SwitchState == 1) yield break;

            var shouldResetCamera = false;

            // TODO: Implement WuLing interaction logic for this switch
            // Can only toggle this switch if ObjectInfo.WuLing matches current player actor's WuLing.

            ToggleAndSaveSwitchState();

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
                yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);

                // Remove collider to allow player to pass through
                if (ObjectInfo.Parameters[0] == 1 && _meshCollider != null)
                {
                    Object.Destroy(_meshCollider);
                }
            }

            // Disable associated effect object if any
            if (ObjectInfo.EffectModelType != 0)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new SceneActivateObjectCommand((int)ObjectInfo.EffectModelType, 0));
            }

            // Interact with linked object if any
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
            }

            base.Deactivate();
        }
    }
}

#endif