// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Core.GameBox;
    using Core.Services;
    using Data;
    using State;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.LiftingPlatform)]
    public sealed class LiftingPlatformObject : SceneObject
    {
        private const float LIFTING_ANIMATION_DURATION = 2.5f;

        private StandingPlatformController _platformController;
        private SceneObjectMeshCollider _meshCollider;

        public LiftingPlatformObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            #if PAL3A
            if (!string.IsNullOrEmpty(ObjectInfo.DependentSceneName))
            {
                var sceneStateManager = ServiceLocator.Instance.Get<SceneStateManager>();
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
                Vector3 position = sceneGameObject.transform.position;
                float gameBoxYPosition = ObjectInfo.Parameters[0];
                var finalYPosition = GameBoxInterpreter.ToUnityYPosition(gameBoxYPosition);
                position.y = finalYPosition;
                sceneGameObject.transform.position = position;
            }

            Bounds bounds = GetMeshBounds();
            // Some tweaks to the bounds to make sure actor won't stuck
            // near the edge of the platform
            bounds.size += new Vector3(0.6f, 0.2f, 0.6f);

            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex);

            #if PAL3A
            if (bounds.size.y > 1f)
            {
                // Add a mesh collider to block the player from walking into the object
                _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
                _meshCollider.Init(new Vector3(-0.3f, -0.8f, -0.3f));
            }
            #endif

            return sceneGameObject;
        }

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

            GameObject liftingMechanismGameObject = GetGameObject();
            Vector3 position = liftingMechanismGameObject.transform.position;

            yield return MoveCameraToLookAtPointAsync(
                position,
                ctx.PlayerActorGameObject);
            CameraFocusOnObject(ObjectInfo.Id);

            float gameBoxYPosition = ObjectInfo.Parameters[0];

            var finalYPosition = GameBoxInterpreter.ToUnityYPosition(ObjectInfo.SwitchState == 0 ?
                gameBoxYPosition :
                ObjectInfo.GameBoxPosition.y);
            var yOffset = finalYPosition - position.y;

            FlipAndSaveSwitchState();

            PlaySfxIfAny();

            var hasObjectOnPlatform = false;
            GameObject objectOnThePlatform = null;
            Vector3 objectOnThePlatformOriginalPosition = Vector3.zero;

            // Set Y position of the object on the platform
            if (ObjectInfo.Parameters[2] != 0)
            {
                objectOnThePlatform = ctx.CurrentScene.GetSceneObject(ObjectInfo.Parameters[2]).GetGameObject();
                if (objectOnThePlatform != null)
                {
                    hasObjectOnPlatform = true;
                    objectOnThePlatformOriginalPosition = objectOnThePlatform.transform.position;
                }
            }

            yield return CoreAnimation.EnumerateValueAsync(0f, yOffset, LIFTING_ANIMATION_DURATION,
                AnimationCurveType.Sine, offset =>
                {
                    liftingMechanismGameObject.transform.position =
                        new Vector3(position.x, position.y + offset, position.z);

                    if (hasObjectOnPlatform && objectOnThePlatform != null)
                    {
                        objectOnThePlatform.transform.position = new Vector3(
                            objectOnThePlatformOriginalPosition.x,
                            objectOnThePlatformOriginalPosition.y + offset,
                            objectOnThePlatformOriginalPosition.z);
                    }
                });

            // Save position of the object on the platform if any
            if (hasObjectOnPlatform)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new SceneSaveGlobalObjectPositionCommand(SceneInfo.CityName,
                        SceneInfo.SceneName,
                        ObjectInfo.Parameters[2],
                        GameBoxInterpreter.ToGameBoxPosition(objectOnThePlatform.transform.position)));
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