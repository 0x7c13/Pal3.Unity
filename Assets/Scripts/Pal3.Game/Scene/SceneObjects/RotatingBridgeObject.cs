// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Game.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Abstraction;
    using Engine.Animation;
    using Engine.Extensions;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;
    using Quaternion = UnityEngine.Quaternion;

    [ScnSceneObject(SceneObjectType.RotatingBridge)]
    public sealed class RotatingBridgeObject : SceneObject
    {
        private float ROTATION_ANIMATION_DURATION = 3.5f;

        private StandingPlatformController _platformController;

        public RotatingBridgeObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            Bounds bounds = GetMeshBounds();

            if (SceneInfo.Is("m17", "3") &&
                ObjectInfo.Name.Equals("_d.pol", StringComparison.OrdinalIgnoreCase))
            {
                bounds = new Bounds
                {
                    center = new Vector3(0f, -0.5f, 0f),
                    size = new Vector3(3.5f, 1.2f, 19f),
                };
            }
            else if (SceneInfo.IsCity("m19") &&
                     ObjectInfo.Name.Equals("_a.pol", StringComparison.OrdinalIgnoreCase))
            {
                bounds = new Bounds
                {
                    center = new Vector3(0f, -0.4f, 0f),
                    size = new Vector3(3.5f, 1f, 23f),
                };
            }

            _platformController = sceneObjectGameEntity.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex);

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            ITransform bridgeObjectTransform = GetGameEntity().Transform;

            yield return MoveCameraToLookAtPointAsync(
                bridgeObjectTransform.Position,
                ctx.PlayerActorGameEntity.Transform);

            CameraFocusOnObject(ObjectInfo.Id);

            Vector3 eulerAngles = bridgeObjectTransform.EulerAngles;
            var targetYRotation = (eulerAngles.y  + 90f) % 360f;
            var targetRotation = new Vector3(eulerAngles.x, targetYRotation, eulerAngles.z);

            PlaySfx("wg004");

            yield return bridgeObjectTransform.RotateAsync(Quaternion.Euler(targetRotation),
                ROTATION_ANIMATION_DURATION, AnimationCurveType.Sine);

            SaveCurrentYRotation();

            ResetCamera();
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