// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Data;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.RotatingBridge)]
    public class RotatingBridgeObject : SceneObject
    {
        private float ROTATION_ANIMATION_DURATION = 3.5f;

        private StandingPlatformController _platformController;

        public RotatingBridgeObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            Bounds bounds = GetPolyModelRenderer().GetMeshBounds();

            if (ObjectInfo.Name.Equals("_d.pol", StringComparison.OrdinalIgnoreCase))
            {
                bounds = new Bounds
                {
                    center = new Vector3(0f, -0.5f, 0f),
                    size = new Vector3(3.5f, 1.2f, 19f),
                };
            }
            else if (ObjectInfo.Name.Equals("_a.pol", StringComparison.OrdinalIgnoreCase))
            {
                bounds = new Bounds
                {
                    center = new Vector3(0f, -0.4f, 0f),
                    size = new Vector3(3.5f, 1f, 23f),
                };
            }

            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.SetBounds(bounds, ObjectInfo.LayerIndex);

            return sceneGameObject;
        }

        public override IEnumerator Interact(InteractionContext ctx)
        {
            yield return MoveCameraToLookAtObjectAndFocus(ctx.PlayerActorGameObject);

            GameObject bridgeObject = GetGameObject();
            Vector3 eulerAngles = bridgeObject.transform.rotation.eulerAngles;
            var targetYRotation = (eulerAngles.y  + 90f) % 360f;
            var targetRotation = new Vector3(eulerAngles.x, targetYRotation, eulerAngles.z);

            PlaySfx("wg004");

            yield return AnimationHelper.RotateTransform(bridgeObject.transform,
                Quaternion.Euler(targetRotation), ROTATION_ANIMATION_DURATION, AnimationCurveType.Sine);

            SaveYRotation();

            ResetCamera();
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