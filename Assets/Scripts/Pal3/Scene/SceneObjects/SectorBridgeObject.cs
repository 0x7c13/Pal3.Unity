// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using Renderer;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.SectorBridge)]
    public sealed class SectorBridgeObject : SceneObject
    {
        private StandingPlatformController _standingPlatformController;

        public SectorBridgeObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            if (ObjectInfo.SwitchState == 1)
            {
                if (ModelType == SceneObjectModelType.CvdModel)
                {
                    CvdModelRenderer cvdModelRenderer = GetCvdModelRenderer();
                    cvdModelRenderer.SetCurrentTime(cvdModelRenderer.GetDefaultAnimationDuration());
                }

                _standingPlatformController = sceneGameObject.AddComponent<StandingPlatformController>();
                _standingPlatformController.Init(GetMeshBounds(), ObjectInfo.LayerIndex);
            }

            return sceneGameObject;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (ObjectInfo.SwitchState == 1) yield break;

            FlipAndSaveSwitchState();

            PlaySfxIfAny();

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);
            }

            _standingPlatformController = GetGameObject().AddComponent<StandingPlatformController>();
            _standingPlatformController.Init(GetMeshBounds(), ObjectInfo.LayerIndex);
        }

        public override void Deactivate()
        {
            if (_standingPlatformController != null)
            {
                Object.Destroy(_standingPlatformController);
                _standingPlatformController = null;
            }

            base.Deactivate();
        }
    }
}

#endif