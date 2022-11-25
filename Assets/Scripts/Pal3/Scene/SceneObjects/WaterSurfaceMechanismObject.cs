// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Data;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.WaterSurfaceMechanism)]
    public class WaterSurfaceMechanismObject : SceneObject
    {
        private WaterSurfaceMechanismObjectController _surfaceMechanismObjectController;
        
        public WaterSurfaceMechanismObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }
        
        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            _surfaceMechanismObjectController = sceneGameObject.AddComponent<WaterSurfaceMechanismObjectController>();
            _surfaceMechanismObjectController.Init(this);
            return sceneGameObject;
        }
        
        public override void Interact(bool triggerredByPlayer)
        {
            if (!IsInteractableBasedOnTimesCount()) return;

            if (_surfaceMechanismObjectController != null)
            {
                _surfaceMechanismObjectController.Interact();
            }
        }

        public override void Deactivate()
        {
            if (_surfaceMechanismObjectController != null)
            {
                Object.Destroy(_surfaceMechanismObjectController);
            }
            
            base.Deactivate();
        }
    }

    internal class WaterSurfaceMechanismObjectController : MonoBehaviour
    {
        private const float WATER_ANIMATION_DURATION = 4f;
        
        private WaterSurfaceMechanismObject _surfaceMechanismObject;
        
        public void Init(WaterSurfaceMechanismObject surfaceMechanismObject)
        {
            _surfaceMechanismObject = surfaceMechanismObject;
        }
        
        public void Interact()
        {
            Vector3 finalPosition = gameObject.transform.position;
            finalPosition.y = GameBoxInterpreter.ToUnityYPosition(_surfaceMechanismObject.Info.Parameters[0]);
            
            StartCoroutine(AnimationHelper.MoveTransform(gameObject.transform,
                finalPosition,
                WATER_ANIMATION_DURATION,
                AnimationCurveType.Sine));
        }
    }
}