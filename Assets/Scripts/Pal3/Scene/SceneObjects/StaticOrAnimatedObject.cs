// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using Core.DataReader.Scn;
    using Core.Renderer;
    using Data;
    using Effect;
    using MetaData;
    using Renderer;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.StaticOrAnimated)]
    public class StaticOrAnimatedObject : SceneObject
    {
        public GraphicsEffect GraphicsEffect { get; } = GraphicsEffect.None;

        public StaticOrAnimatedObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            if (objectInfo.Parameters[1] == 1 &&
                ModelTypeResolver.GetType(objectInfo.Name) == ModelType.CvdModel)
            {
                //Debug.Log("Dead object.");
            }
            else
            {
                GraphicsEffect = EffectTypeResolver.GetEffectByName(objectInfo.Name);
            }
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, GameObject parent, Color tintColor)
        {
            var sceneGameObject = base.Activate(resourceProvider, parent, tintColor);
            sceneGameObject.AddComponent<StaticOrAnimatedObjectController>().Init(resourceProvider, this);
            return sceneGameObject;
        }
    }

    public class StaticOrAnimatedObjectController : MonoBehaviour
    {
        private StaticOrAnimatedObject _sceneObject;
        private Component _effectComponent;

        public void Init(GameResourceProvider resourceProvider, StaticOrAnimatedObject sceneObject)
        {
            _sceneObject = sceneObject;
            if (sceneObject.GraphicsEffect == GraphicsEffect.None) return;

            var effectComponentType = EffectTypeResolver.GetEffectComponentType(sceneObject.GraphicsEffect);
            _effectComponent = gameObject.AddComponent(effectComponentType);
            (_effectComponent as IEffect)!.Init(resourceProvider, sceneObject.Info.EffectModelType);
        }

        private void OnDisable()
        {
            Destroy(_effectComponent);
        }
    }
}