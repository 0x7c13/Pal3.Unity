// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
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
    using Core.GameBox;
    using Data;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.LiftingPlatform)]
    public sealed class LiftingPlatformObject : SceneObject
    {
        private const float LIFTING_ANIMATION_DURATION = 2.5f;

        private StandingPlatformController _platformController;

        public LiftingPlatformObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

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
            Vector3 tweakedBoundsSize = bounds.size;
            tweakedBoundsSize.x += 0.5f;
            tweakedBoundsSize.y += 0.2f;
            tweakedBoundsSize.z += 0.5f;
            bounds.size = tweakedBoundsSize;

            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.SetBounds(bounds, ObjectInfo.LayerIndex);

            return sceneGameObject;
        }

        public override IEnumerator Interact(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            var shouldResetCamera = false;
            if (!IsVisibleToCamera())
            {
                shouldResetCamera = true;
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new CameraFocusOnSceneObjectCommand(ObjectInfo.Id));
            }

            GameObject liftingMechanismGameObject = GetGameObject();
            Vector3 position = liftingMechanismGameObject.transform.position;
            float gameBoxYPosition = ObjectInfo.Parameters[0];

            var finalYPosition = GameBoxInterpreter.ToUnityYPosition(ObjectInfo.SwitchState == 0 ?
                gameBoxYPosition :
                ObjectInfo.GameBoxPosition.y);
            var yOffset = finalYPosition - position.y;

            ToggleAndSaveSwitchState();
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

            yield return AnimationHelper.EnumerateValue(0f, yOffset, LIFTING_ANIMATION_DURATION, AnimationCurveType.Sine,
                offset =>
                {
                    liftingMechanismGameObject.transform.position =
                        new Vector3(position.x, position.y + offset, position.z);

                    if (hasObjectOnPlatform)
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

            if (shouldResetCamera)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFreeCommand(1));
            }
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