// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using Renderer;
    using UnityEngine;
    using Object = UnityEngine.Object;

    #if PAL3
    [ScnSceneObject(ScnSceneObjectType.Collidable)]
    #endif
    [ScnSceneObject(ScnSceneObjectType.Shakeable)]
    public sealed class CollidableObject : SceneObject
    {
        private BoundsTriggerController _triggerController;
        private SceneObjectMeshCollider _meshCollider;

        public CollidableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // Don't add a trigger if the object is already collided (SwitchState sets to 1).
            if (ObjectInfo.SwitchState == 0)
            {
                _triggerController = sceneGameObject.AddComponent<BoundsTriggerController>();
                _triggerController.SetBounds(GetMeshBounds(), ObjectInfo.IsNonBlocking == 1);
                _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;
                _triggerController.OnPlayerActorExited += OnPlayerActorExited;
            }
            else if (ObjectInfo.SwitchState == 1)
            {
                if (ModelType == SceneObjectModelType.CvdModel)
                {
                    CvdModelRenderer cvdModelRenderer = GetCvdModelRenderer();
                    cvdModelRenderer.SetCurrentTime(cvdModelRenderer.GetDefaultAnimationDuration());
                }

                if (ObjectInfo.IsNonBlocking == 0)
                {
                    _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
                }
            }

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, GameObject playerGameObject)
        {
            if (ObjectInfo.SwitchState == 0)
            {
                RequestForInteraction();
            }
        }

        private void OnPlayerActorExited(object sender, GameObject playerGameObject)
        {
            if (ObjectInfo is {SwitchState: 1, IsNonBlocking: 0})
            {
                if (_meshCollider == null)
                {
                    _meshCollider = GetGameObject().AddComponent<SceneObjectMeshCollider>();
                }
            }
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (ObjectInfo.SwitchState == 1) yield break;

            if (!IsInteractableBasedOnTimesCount()) yield break;

            FlipAndSaveSwitchState();

            PlaySfxIfAny();

            yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);

            yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);

            yield return ExecuteScriptAndWaitForFinishIfAnyAsync();

            // Reset colliders since bounds may change after animation.
            // isTrigger should depend on the object's IsNonBlocking flag
            // but here we just set it to true to avoid the player from
            // getting stuck in the object after the animation.
            _triggerController.SetBounds(GetMeshBounds(), isTrigger: true);

            if (_meshCollider != null)
            {
                Object.Destroy(_meshCollider);
                _meshCollider = null;
            }
        }

        public override void Deactivate()
        {
            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                _triggerController.OnPlayerActorExited -= OnPlayerActorExited;
                Object.Destroy(_triggerController);
                _triggerController = null;
            }

            if (_meshCollider != null)
            {
                Object.Destroy(_meshCollider);
                _meshCollider = null;
            }

            base.Deactivate();
        }
    }
}