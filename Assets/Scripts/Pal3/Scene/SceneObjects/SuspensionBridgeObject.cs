// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Data;
    using Renderer;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.SuspensionBridge)]
    public class SuspensionBridgeObject : SceneObject
    {
        private StandingPlatformController _platformController;

        public SuspensionBridgeObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // Set to final position if the bridge is already activated
            if (ObjectInfo.Times == 0)
            {
                CvdModelRenderer cvdModelRenderer = GetCvdModelRenderer();
                cvdModelRenderer.SetCurrentTime(cvdModelRenderer.GetDefaultAnimationDuration());
                EnableStandingPlatform();
            }

            return sceneGameObject;
        }

        public override IEnumerator Interact(bool triggerredByPlayer)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            PlaySfxIfAny();

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                // SuspensionBridge is always triggered by other switch type objects
                // so we don't wait for the animation to finish thus we don't call
                // IEnumerator version of the cvd animation playing method here.
                GetCvdModelRenderer().StartOneTimeAnimation(true, EnableStandingPlatform);
            }
        }

        private void EnableStandingPlatform()
        {
            // _e.cvd
            var bounds = new Bounds
            {
                center = new Vector3(0f, -0.2f, 1.4f),
                size = new Vector3(4.5f, 0.7f, 6f),
            };

            _platformController = GetGameObject().GetOrAddComponent<StandingPlatformController>();
            _platformController.SetBounds(bounds, ObjectInfo.LayerIndex);
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                Object.Destroy(_platformController);
            }

            base.Deactivate();
        }
    }
}