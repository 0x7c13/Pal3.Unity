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
    public class CollidableObject : SceneObject
    {
        private BoundsTriggerController _triggerController;

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
                _triggerController.SetupCollider(GetRendererBounds(), ObjectInfo.IsNonBlocking == 1);
                _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;
            }

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, GameObject playerGameObject)
        {
            Pal3.Instance.StartCoroutine(Interact(true));
        }

        public override IEnumerator Interact(bool triggerredByPlayer)
        {
            if (ObjectInfo.SwitchState == 1) yield break;

            if (!IsInteractableBasedOnTimesCount()) yield break;

            ToggleAndSaveSwitchState();

            PlaySfxIfAny();

            yield return GetCvdModelRenderer().PlayOneTimeAnimation(true);

            yield return ActivateOrInteractWithLinkedObjectIfAny();

            ExecuteScriptIfAny();

            // Reset collider since bounds may change after animation
            _triggerController.SetupCollider(GetRendererBounds(), ObjectInfo.IsNonBlocking == 1);
        }

        public override void Deactivate()
        {
            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                Object.Destroy(_triggerController);
            }

            base.Deactivate();
        }
    }
}