// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
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
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Rendering.Renderer;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.SuspensionBridge)]
    public sealed class SuspensionBridgeObject : SceneObject
    {
        private StandingPlatformController _platformController;

        public SuspensionBridgeObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            // Set to final position if the bridge is already activated
            if (ObjectInfo.Times == 0)
            {
                CvdModelRenderer cvdModelRenderer = GetCvdModelRenderer();
                cvdModelRenderer.SetCurrentTime(cvdModelRenderer.GetDefaultAnimationDuration());
                EnableStandingPlatform();
            }

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            bool shouldResetCamera = false;
            if (!IsFullyVisibleToCamera())
            {
                shouldResetCamera = true;
                yield return MoveCameraToLookAtPointAsync(
                    GetGameEntity().Transform.Position,
                    ctx.PlayerActorGameEntity.Transform);
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
            Bounds bounds = new()
            {
                center = new Vector3(0f, -0.2f, 1.4f),
                size = new Vector3(4.5f, 0.7f, 6f),
            };

            _platformController = GetGameEntity().GetOrAddComponent<StandingPlatformController>();
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