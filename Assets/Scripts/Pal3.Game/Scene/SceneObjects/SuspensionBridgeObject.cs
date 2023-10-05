// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Extensions;
    using Rendering.Renderer;
    using UnityEngine;

    [ScnSceneObject(SceneObjectType.SuspensionBridge)]
    public sealed class SuspensionBridgeObject : SceneObject
    {
        private StandingPlatformController _platformController;

        public SuspensionBridgeObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameObject();
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

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            var shouldResetCamera = false;
            if (!IsFullyVisibleToCamera())
            {
                shouldResetCamera = true;
                yield return MoveCameraToLookAtPointAsync(GetGameObject().transform.position,
                    ctx.PlayerActorGameObject);
                CameraFocusOnObject(ObjectInfo.Id);
            }

            PlaySfxIfAny();

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);
            }

            EnableStandingPlatform();

            if (shouldResetCamera)
            {
                ResetCamera();
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
            _platformController.Init(bounds, ObjectInfo.LayerIndex);
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                _platformController.Destroy();
                _platformController = null;
            }

            base.Deactivate();
        }
    }
}

#endif