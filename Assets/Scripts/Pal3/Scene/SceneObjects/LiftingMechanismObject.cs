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
        private LiftingMechanismObjectController _liftingMechanismObjectController;
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
            
            _liftingMechanismObjectController = sceneGameObject.AddComponent<LiftingMechanismObjectController>();
            _liftingMechanismObjectController.Init(this);

            Bounds bounds = new Bounds();
            if (sceneGameObject.GetComponent<CvdModelRenderer>() is { } cvdModelRenderer)
            {
                bounds = cvdModelRenderer.GetMeshBounds();
            }
            else if (sceneGameObject.GetComponent<PolyModelRenderer>() is { } polyModelRenderer)
            {
                bounds = polyModelRenderer.GetMeshBounds();
            }
            
            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.SetBounds(bounds, ObjectInfo.LayerIndex);

            return sceneGameObject;
        }
        
        public override void Interact(bool triggerredByPlayer)
        {
            if (!IsInteractableBasedOnTimesCount()) return;

            if (_liftingMechanismObjectController != null)
            {
                _liftingMechanismObjectController.Interact();
            }
        }

        public override void Deactivate()
        {
            if (_liftingMechanismObjectController != null)
            {
                Object.Destroy(_liftingMechanismObjectController);
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
        
        private LiftingMechanismObject _liftingMechanismObject;
        
        public void Init(LiftingMechanismObject liftingMechanismObject)
        {
            _liftingMechanismObject = liftingMechanismObject;
        }
        
        public void Interact()
        {
            StartCoroutine(InteractInternal());
        }

        private IEnumerator InteractInternal()
        {
            _liftingMechanismObject.ToggleSwitchState();
            
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFocusOnSceneObjectCommand(_liftingMechanismObject.ObjectInfo.Id));
            
            Vector3 finalPosition = transform.position;
            float gameBoxYPosition = _liftingMechanismObject.ObjectInfo.Parameters[0];
            
            // A small Y offset to ensure actor shadow is properly rendered
            finalPosition.y = GameBoxInterpreter.ToUnityYPosition(gameBoxYPosition) - 0.02f;

            _liftingMechanismObject.PlaySfxIfAny();
            
            yield return AnimationHelper.MoveTransform(gameObject.transform,
                finalPosition,
                LIFTING_ANIMATION_DURATION,
                AnimationCurveType.Sine);
            
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFreeCommand(1));
        }
    }
}