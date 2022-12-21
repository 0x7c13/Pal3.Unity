// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.Collidable)]
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
                _triggerController.SetupCollider(GetMeshBounds(), ObjectInfo.IsNonBlocking == 1);
                _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;
            }
            else
            {
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

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (ObjectInfo.SwitchState == 1) yield break;

            if (!IsInteractableBasedOnTimesCount()) yield break;

            ToggleAndSaveSwitchState();

            PlaySfxIfAny();

            yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);

            yield return ActivateOrInteractWithLinkedObjectIfAnyAsync(ctx);

            yield return ExecuteScriptAndWaitForFinishIfAnyAsync();

            // Reset collider since bounds may change after animation
            _triggerController.SetupCollider(GetMeshBounds(), ObjectInfo.IsNonBlocking == 1);
        }

        public override void Deactivate()
        {
            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                Object.Destroy(_triggerController);
            }

            if (_meshCollider != null)
            {
                Object.Destroy(_meshCollider);
            }

            base.Deactivate();
        }
    }
}