// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Services;
    using Data;
    using Renderer;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.LiftingMechanism)]
    public class LiftingMechanismObject : SceneObject
    {
        private LiftingMechanismObjectController _objectController;
        private StandingPlatformController _platformController;

        public LiftingMechanismObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
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
                var yOffset = finalYPosition - position.y;
                position.y = finalYPosition;
                sceneGameObject.transform.position = position;

                // Set Y position of the object on the platform
                if (ObjectInfo.Parameters[2] != 0)
                {
                    // TODO: impl
                }
            }
            
            _objectController = sceneGameObject.AddComponent<LiftingMechanismObjectController>();
            _objectController.Init(this);

            Bounds bounds = new Bounds();
            if (sceneGameObject.GetComponent<CvdModelRenderer>() is { } cvdModelRenderer)
            {
                bounds = cvdModelRenderer.GetMeshBounds();
            }
            else if (sceneGameObject.GetComponent<PolyModelRenderer>() is { } polyModelRenderer)
            {
                bounds = polyModelRenderer.GetMeshBounds();
            }

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
        
        public override void Interact(bool triggerredByPlayer)
        {
            if (!IsInteractableBasedOnTimesCount()) return;

            if (_objectController != null)
            {
                _objectController.Interact();
            }
        }

        public override void Deactivate()
        {
            if (_objectController != null)
            {
                Object.Destroy(_objectController);
            }
            
            if (_platformController != null)
            {
                Object.Destroy(_platformController);
            }
            
            base.Deactivate();
        }
    }

    internal class LiftingMechanismObjectController : MonoBehaviour
    {
        private const float LIFTING_ANIMATION_DURATION = 2.5f;
        
        private LiftingMechanismObject _object;
        
        public void Init(LiftingMechanismObject liftingMechanismObject)
        {
            _object = liftingMechanismObject;
        }
        
        public void Interact()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerEnableInputCommand(0));
            StartCoroutine(InteractInternal());
        }

        private IEnumerator InteractInternal()
        {
            _object.ToggleSwitchState();
            
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFocusOnSceneObjectCommand(_object.ObjectInfo.Id));
            
            Vector3 position = transform.position;
            float gameBoxYPosition = _object.ObjectInfo.Parameters[0];
            var finalYPosition = GameBoxInterpreter.ToUnityYPosition(gameBoxYPosition);
            var yOffset = finalYPosition - position.y;

            _object.PlaySfxIfAny();

            var hasObjectOnPlatform = false;
            GameObject objectOnThePlatform = null;
            Vector3 objectOnThePlatformOriginalPosition = Vector3.zero;
                
            // Set Y position of the object on the platform
            if (_object.ObjectInfo.Parameters[2] != 0)
            {
                hasObjectOnPlatform = true;
                objectOnThePlatform = ServiceLocator.Instance.Get<SceneManager>()
                    .GetCurrentScene()
                    .GetSceneObject(_object.ObjectInfo.Parameters[2])
                    .GetGameObject();
                objectOnThePlatformOriginalPosition = objectOnThePlatform.transform.position;
            }
            
            yield return AnimationHelper.EnumerateValue(0f, yOffset, LIFTING_ANIMATION_DURATION, AnimationCurveType.Sine,
                offset =>
                {
                    transform.position = new Vector3(position.x, position.y + offset, position.z);

                    if (hasObjectOnPlatform)
                    {
                        objectOnThePlatform.transform.position = new Vector3(
                            objectOnThePlatformOriginalPosition.x,
                            objectOnThePlatformOriginalPosition.y + offset,
                            objectOnThePlatformOriginalPosition.z);
                    }
                });
            
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFreeCommand(1));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new PlayerEnableInputCommand(1));
        }
    }
}