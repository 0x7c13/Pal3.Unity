// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.Extensions;
    using Common;
    using Core.Command;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Core.Primitives;
    using Data;
    using Engine.Animation;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Services;
    using State;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.LiftingPlatform)]
    public sealed class LiftingPlatformObject : SceneObject
    {
        private const float LIFTING_ANIMATION_DURATION = 2.5f;

        private StandingPlatformController _platformController;
        private SceneObjectMeshCollider _meshCollider;

        public LiftingPlatformObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            #if PAL3A
            if (!string.IsNullOrEmpty(ObjectInfo.DependentSceneName))
            {
                SceneStateManager sceneStateManager = ServiceLocator.Instance.Get<SceneStateManager>();
                if (sceneStateManager.TryGetSceneObjectStateOverride(
                        SceneInfo.CityName,
                        ObjectInfo.DependentSceneName,
                        ObjectInfo.DependentObjectId,
                        out SceneObjectStateOverride stateOverride))
                {
                    if (stateOverride.SwitchState == 1)
                    {
                        ObjectInfo.SwitchState = 1;
                    }
                }
            }
            #endif

            // Set to final position if the platform is already activated
            if (ObjectInfo.SwitchState == 1)
            {
                Vector3 position = sceneObjectGameEntity.Transform.Position;
                float gameBoxYPosition = ObjectInfo.Parameters[0];
                float finalYPosition = gameBoxYPosition.ToUnityYPosition();
                position.y = finalYPosition;
                sceneObjectGameEntity.Transform.Position = position;
            }

            Bounds bounds = GetMeshBounds();
            // Some tweaks to the bounds to make sure actor won't stuck
            // near the edge of the platform
            bounds.size += new Vector3(0.6f, 0.2f, 0.6f);

            _platformController = sceneObjectGameEntity.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex);

            #if PAL3A
            if (bounds.size.y > 1f)
            {
                // Add a mesh collider to block the player from walking into the object
                _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
                _meshCollider.Init(new Vector3(-0.3f, -0.8f, -0.3f));
            }
            #endif

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            #if PAL3
            // Fix a bug in m22-4 where the script can interact with the platform
            // even though the platform is already activated with correct height.
            // This is to prevent the unwanted interaction triggered by the script
            // since the platform is not interactable in this scene by the player anyway.
            if (SceneInfo.Is("m22", "4"))
            {
                yield break;
            }
            #endif

            if (!IsInteractableBasedOnTimesCount()) yield break;

            ITransform liftingMechanismTransform = GetGameEntity().Transform;
            Vector3 position = liftingMechanismTransform.Position;

            yield return MoveCameraToLookAtPointAsync(
                position,
                ctx.PlayerActorGameEntity.Transform);

            CameraFocusOnObject(ObjectInfo.Id);

            float gameBoxYPosition = ObjectInfo.Parameters[0];

            float finalYPosition = (ObjectInfo.SwitchState == 0 ?
                gameBoxYPosition : ObjectInfo.GameBoxPosition.Y).ToUnityYPosition();
            float yOffset = finalYPosition - position.y;

            FlipAndSaveSwitchState();

            PlaySfxIfAny();

            bool hasObjectOnPlatform = false;
            IGameEntity objectOnThePlatform = null;
            Vector3 objectOnThePlatformOriginalPosition = Vector3.zero;

            // Set Y position of the object on the platform
            if (ObjectInfo.Parameters[2] != 0)
            {
                objectOnThePlatform = ctx.CurrentScene.GetSceneObject(ObjectInfo.Parameters[2]).GetGameEntity();
                if (objectOnThePlatform != null)
                {
                    hasObjectOnPlatform = true;
                    objectOnThePlatformOriginalPosition = objectOnThePlatform.Transform.Position;
                }
            }

            yield return CoreAnimation.EnumerateValueAsync(0f, yOffset, LIFTING_ANIMATION_DURATION,
                AnimationCurveType.Sine, offset =>
                {
                    liftingMechanismTransform.Position =
                        new Vector3(position.x, position.y + offset, position.z);

                    if (hasObjectOnPlatform && objectOnThePlatform != null)
                    {
                        objectOnThePlatform.Transform.Position = new Vector3(
                            objectOnThePlatformOriginalPosition.x,
                            objectOnThePlatformOriginalPosition.y + offset,
                            objectOnThePlatformOriginalPosition.z);
                    }
                });

            // Save position of the object on the platform if any
            if (hasObjectOnPlatform)
            {
                GameBoxVector3 objectGameBoxPosition = objectOnThePlatform.Transform.Position.ToGameBoxPosition();
                Pal3.Instance.Execute(new SceneSaveGlobalObjectPositionCommand(SceneInfo.CityName,
                        SceneInfo.SceneName,
                        ObjectInfo.Parameters[2],
                        objectGameBoxPosition.X,
                        objectGameBoxPosition.Y,
                        objectGameBoxPosition.Z));
            }

            yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);

            ResetCamera();
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