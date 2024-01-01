// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Game.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Animation;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Rendering.Renderer;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.RoadElevatorOrBlocker)]
    public sealed class RoadElevatorOrBlockerObject : SceneObject
    {
        private StandingPlatformController _platformController;
        private SceneObjectMeshCollider _meshCollider;

        public RoadElevatorOrBlockerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            Bounds bounds = GetMeshBounds();
            bounds.size += new Vector3(0.6f, 0.2f, 0.6f); // Extend the bounds a little bit

            // This is a blocker object
            if (ObjectInfo.Parameters[4] == 1 &&
                ObjectInfo.Parameters[5] == 1)
            {
                if (ObjectInfo.SwitchState == 1 && ModelType == SceneObjectModelType.CvdModel)
                {
                    CvdModelRenderer cvdModelRenderer = GetCvdModelRenderer();
                    cvdModelRenderer.SetCurrentTime(cvdModelRenderer.GetDefaultAnimationDuration());
                    _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
                }
            }
            else // This is a road elevator object
            {
                // Add a standing platform controller so that the player can stand on the floor
                _platformController = sceneObjectGameEntity.AddComponent<StandingPlatformController>();
                _platformController.Init(bounds, ObjectInfo.LayerIndex);

                // Add a mesh collider to block the player from walking into the object
                _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
                _meshCollider.Init(new Vector3(-0.3f, -0.8f, -0.3f));
            }

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            PlaySfxIfAny();

            IGameEntity floorEntity = GetGameEntity();

            // This is a blocker object
            if (ObjectInfo.Parameters[4] == 1 &&
                ObjectInfo.Parameters[5] == 1)
            {
                FlipAndSaveSwitchState();

                yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);

                if (ObjectInfo.Parameters[2] != 0)
                {
                    ITransform transform = floorEntity.Transform;
                    float yOffset = ((float)ObjectInfo.Parameters[2]).ToUnityDistance();
                    Vector3 finalPosition = transform.Position + new Vector3(0f, -yOffset, 0f);
                    yield return transform.MoveAsync(finalPosition, 0.5f);
                    SaveCurrentPosition();
                }

                if (_meshCollider == null)
                {
                    _meshCollider = floorEntity.AddComponent<SceneObjectMeshCollider>();
                }
            }
            else // This is an elevator floor object
            {
                Vector3 currentPosition = floorEntity.Transform.Position;

                float lowerYPosition =
                    ((float)ObjectInfo.Parameters[0] * ObjectInfo.Parameters[4]).ToUnityYPosition();
                float upperYPosition =
                    ((float)ObjectInfo.Parameters[2] * ObjectInfo.Parameters[5]).ToUnityYPosition();

                if (SceneInfo.Is("m05", "3"))
                {
                    lowerYPosition = 0f;
                }

                float finalYPosition = lowerYPosition;

                if (MathF.Abs(currentPosition.y - lowerYPosition) < MathF.Abs(currentPosition.y - upperYPosition))
                {
                    finalYPosition = upperYPosition;
                }

                Vector3 toPosition = new Vector3(currentPosition.x, finalYPosition, currentPosition.z);

                var duration = 2f;

                if (SceneInfo.Is("m06", "2"))
                {
                    duration = 0.3f;
                }

                yield return floorEntity.Transform.MoveAsync(toPosition, duration);

                SaveCurrentPosition();
            }
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                _platformController.Destroy();
                _platformController = null;
            }

            if (_meshCollider != null)
            {
                _meshCollider.Destroy();
                _meshCollider = null;
            }

            base.Deactivate();
        }
    }
}

#endif