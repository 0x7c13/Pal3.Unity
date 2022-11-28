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
                Vector3 finalPosition = sceneGameObject.transform.position;
                float gameBoxYPosition = ObjectInfo.Parameters[0];
                // A small Y offset to ensure actor shadow is properly rendered
                finalPosition.y = GameBoxInterpreter.ToUnityYPosition(gameBoxYPosition) - 0.02f;
                sceneGameObject.transform.position = finalPosition;
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

            // Some tweaks to the bounds to fit better with the rendering model
            Vector3 tweakedBoundsCenter = bounds.center;
            tweakedBoundsCenter.y -= 0.4f;
            Vector3 tweakedBoundsSize = bounds.size;
            tweakedBoundsSize.x += 0.4f;
            tweakedBoundsSize.z += 0.4f;
            bounds.center = tweakedBoundsCenter;
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
            StartCoroutine(InteractInternal());
        }

        private IEnumerator InteractInternal()
        {
            _object.ToggleSwitchState();
            
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFocusOnSceneObjectCommand(_object.ObjectInfo.Id));
            
            Vector3 finalPosition = transform.position;
            float gameBoxYPosition = _object.ObjectInfo.Parameters[0];
            
            // A small Y offset to ensure actor shadow is properly rendered
            finalPosition.y = GameBoxInterpreter.ToUnityYPosition(gameBoxYPosition) - 0.02f;

            _object.PlaySfxIfAny();
            
            yield return AnimationHelper.MoveTransform(gameObject.transform,
                finalPosition,
                LIFTING_ANIMATION_DURATION,
                AnimationCurveType.Sine);
            
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFreeCommand(1));
        }
    }
}