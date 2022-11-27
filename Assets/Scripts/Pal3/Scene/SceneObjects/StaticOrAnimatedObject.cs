// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using Renderer;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.StaticOrAnimated)]
    public class StaticOrAnimatedObject : SceneObject
    {
        public StaticOrAnimatedObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // Should block the player if Parameters[0] is 0
            if (ObjectInfo.Parameters[0] == 0)
            {
                sceneGameObject.AddComponent<SceneObjectMeshCollider>();
            }
            
            sceneGameObject.AddComponent<StaticOrAnimatedObjectController>().Init(this);
            return sceneGameObject;
        }

        public override void Interact(bool triggerredByPlayer)
        {
            if (Activated && ModelType == SceneObjectModelType.CvdModel)
            {
                GetCvdModelRenderer().StartOneTimeAnimation(true);
            }
        }
    }

    internal class StaticOrAnimatedObjectController : MonoBehaviour
    {
        private StaticOrAnimatedObject _sceneObject;
        private Component _effectComponent;
        private float _initYPosition;
        
        public void Init(StaticOrAnimatedObject sceneObject)
        {
            _sceneObject = sceneObject;
        }
        
        private void Start()
        {
            _initYPosition = transform.localPosition.y;

            // Randomly play animation if Parameters[1] == 0 for Cvd modeled objects.
            if (_sceneObject.ObjectInfo.Parameters[1] == 0)
            {
                if (gameObject.GetComponent<CvdModelRenderer>() is {} cvdModelRenderer)
                {
                    StartCoroutine(PlayAnimationRandomly(cvdModelRenderer));
                }
            }
        }

        // Play animation with random wait time.
        private IEnumerator PlayAnimationRandomly(CvdModelRenderer cvdModelRenderer)
        {
            while (isActiveAndEnabled)
            {
                yield return new WaitForSeconds(Random.Range(0.5f, 3.5f));
                yield return cvdModelRenderer.PlayOneTimeAnimation(true);
            }
        }

        void LateUpdate()
        {
            // Parameters[2] describes animated object's default animation.
            // 0 means no animation. 1 means the object is animated up and down (sine curve).
            // 2 means the object is animated with constant rotation.
            if (_sceneObject.ObjectInfo.Parameters[2] == 1)
            {
                Transform currentTransform = transform;
                Vector3 currentPosition = currentTransform.localPosition;
                transform.localPosition = new Vector3(currentPosition.x,
                    _initYPosition + Mathf.Sin(Time.time) / 6f,
                    currentPosition.z);
            }
            else if (_sceneObject.ObjectInfo.Parameters[2] == 2)
            {
                Transform currentTransform = transform;
                transform.RotateAround(currentTransform.position,
                    currentTransform.up, 
                    Time.deltaTime * 80f);
            }
        }

        private void OnDisable()
        {
            Destroy(_effectComponent);
        }
    }
}