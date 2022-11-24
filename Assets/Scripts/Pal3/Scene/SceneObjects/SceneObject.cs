// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.IO;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Pol;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Data;
    using Dev;
    using Effect;
    using MetaData;
    using Renderer;
    using UnityEngine;

    public struct InteractionContext
    {
        public int ActorId;
        public Vector2Int ActorTilePosition;
        public float DistanceToActor;
    }
    
    public abstract class SceneObject
    {
        public ScnObjectInfo Info;
        public GraphicsEffect GraphicsEffect { get; }
        public SceneObjectModelType ModelType { get; }

        internal bool Activated;
        
        private IEffect _effectComponent;
        private GameObject _sceneObjectGameObject;
        private readonly string _modelFilePath;

        protected SceneObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo, bool hasModel = true)
        {
            Info = objectInfo;

            _modelFilePath = hasModel && !string.IsNullOrEmpty(objectInfo.Name) ?
                GetModelFilePath(objectInfo, sceneInfo) : string.Empty;

            ModelType = SceneObjectModelTypeResolver.GetType(Path.GetFileName(_modelFilePath));
            GraphicsEffect = GetEffectType(objectInfo);
        }

        private GraphicsEffect GetEffectType(ScnObjectInfo objectInfo)  
        {
            if (!objectInfo.Name.StartsWith('+')) return GraphicsEffect.None;
            
            if (objectInfo.Parameters[1] == 1 && ModelType == SceneObjectModelType.CvdModel)
            {
                // Dead object
                return GraphicsEffect.None;
            }

            return EffectTypeResolver.GetEffectByNameAndType(objectInfo.Name, objectInfo.EffectModelType);
        }

        private string GetModelFilePath(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
        {
            var separator = CpkConstants.DirectorySeparator;
            var modelFilePath = string.Empty;

            if (objectInfo.Name.StartsWith('_'))
            {
                modelFilePath = $"{sceneInfo.CityName}{CpkConstants.FileExtension}{separator}" +
                                $"{sceneInfo.Model}{separator}{objectInfo.Name}";
            }
            else if (objectInfo.Name.StartsWith('+'))
            {
                // Special vfx effect.
            }
            else if (!objectInfo.Name.Contains('.'))
            {
                modelFilePath = $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}item" +
                                $"{separator}{objectInfo.Name}{separator}{objectInfo.Name}.pol";
            }
            else
            {
                modelFilePath = $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}object" +
                                $"{separator}{objectInfo.Name}";
            }

            return modelFilePath;
        }

        public virtual GameObject Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (Activated) return _sceneObjectGameObject;
            
            _sceneObjectGameObject = new GameObject($"Object_{Info.Id}_{Info.Type}");

            // Attach SceneObjectInfo to the GameObject for better debuggability
            #if UNITY_EDITOR
            var infoPresenter = _sceneObjectGameObject.AddComponent<SceneObjectInfoPresenter>();
            infoPresenter.sceneObjectInfo = Info;
            #endif

            if (ModelType == SceneObjectModelType.PolModel)
            {
                (PolFile PolFile, ITextureResourceProvider TextureProvider) poly = resourceProvider.GetPol(_modelFilePath);
                var modelRenderer = _sceneObjectGameObject.AddComponent<PolyModelRenderer>();
                modelRenderer.Render(poly.PolFile,
                    resourceProvider.GetMaterialFactory(),
                    poly.TextureProvider,
                    tintColor);
            }
            else if (ModelType == SceneObjectModelType.CvdModel)
            {
                (CvdFile CvdFile, ITextureResourceProvider TextureProvider) cvd = resourceProvider.GetCvd(_modelFilePath);
                var modelRenderer = _sceneObjectGameObject.AddComponent<CvdModelRenderer>();

                var initTime = 0f;
                
                // Some switches are on by default.
                if (Info.Type == ScnSceneObjectType.Switch &&
                    Info.Parameters[0] == 1 &&
                    Info.Parameters[1] == 1)
                {
                    initTime = cvd.CvdFile.AnimationDuration;
                }

                modelRenderer.Init(cvd.CvdFile,
                    resourceProvider.GetMaterialFactory(),
                    cvd.TextureProvider,
                    tintColor,
                    initTime);

                if (Info.Type == ScnSceneObjectType.General)
                {
                    modelRenderer.LoopAnimation();
                }
            }

            _sceneObjectGameObject.transform.position = GameBoxInterpreter.ToUnityPosition(Info.GameBoxPosition);
            #if PAL3
            _sceneObjectGameObject.transform.rotation =
                Quaternion.Euler(Info.XRotation, -Info.YRotation, 0f);
            #elif PAL3A
            _sceneObjectGameObject.transform.rotation =
                Quaternion.Euler(Info.XRotation, -Info.YRotation, Info.ZRotation);
            #endif

            if (GraphicsEffect != GraphicsEffect.None &&
                EffectTypeResolver.GetEffectComponentType(GraphicsEffect) is {} effectComponentType)
            {
                _effectComponent = _sceneObjectGameObject.AddComponent(effectComponentType) as IEffect;
                #if PAL3
                var effectParameter = Info.EffectModelType;
                #elif PAL3A
                var effectParameter = (uint)Info.Parameters[5];
                #endif
                Debug.Log($"Adding {GraphicsEffect} [{effectParameter}] effect for scene object {Info.Id}");
                _effectComponent!.Init(resourceProvider, effectParameter);   
            }

            Activated = true;
            return _sceneObjectGameObject;
        }

        public GameObject GetGameObject()
        {
            return _sceneObjectGameObject;
        }
        
        public virtual bool IsInteractable(InteractionContext ctx)
        {
            return false;
        }

        public virtual void Interact()
        {
            // Do nothing
        }
        
        /// <summary>
        /// Should be called after the child class has finished its own deactivation.
        /// </summary>
        public virtual void Deactivate()
        {
            if (!Activated) return;
            
            Activated = false;

            if (_effectComponent != null)
            {
                _effectComponent.Dispose();
                _effectComponent = null;
            }

            if (_sceneObjectGameObject != null)
            {
                Object.Destroy(_sceneObjectGameObject);
            }
        }
    }
}