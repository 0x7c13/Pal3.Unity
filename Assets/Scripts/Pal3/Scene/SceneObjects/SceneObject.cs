// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.IO;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
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
        public ScnObjectInfo ObjectInfo;
        public ScnSceneInfo SceneInfo;
        public GraphicsEffect GraphicsEffect { get; }
        public SceneObjectModelType ModelType { get; }

        internal bool Activated;
        
        private IEffect _effectComponent;
        private GameObject _sceneObjectGameObject;
        private readonly string _modelFilePath;
        
        private PolyModelRenderer _polyModelRenderer;
        private CvdModelRenderer _cvdModelRenderer;

        protected SceneObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo, bool hasModel = true)
        {
            ObjectInfo = objectInfo;
            SceneInfo = sceneInfo;
            
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
            
            _sceneObjectGameObject = new GameObject($"Object_{ObjectInfo.Id}_{ObjectInfo.Type}");

            // Attach SceneObjectInfo to the GameObject for better debuggability
            #if UNITY_EDITOR
            var infoPresenter = _sceneObjectGameObject.AddComponent<SceneObjectInfoPresenter>();
            infoPresenter.sceneObjectInfo = ObjectInfo;
            #endif

            if (ModelType == SceneObjectModelType.PolModel)
            {
                (PolFile PolFile, ITextureResourceProvider TextureProvider) poly = resourceProvider.GetPol(_modelFilePath);
                _polyModelRenderer = _sceneObjectGameObject.AddComponent<PolyModelRenderer>();
                _polyModelRenderer.Render(poly.PolFile,
                    resourceProvider.GetMaterialFactory(),
                    poly.TextureProvider,
                    tintColor);
            }
            else if (ModelType == SceneObjectModelType.CvdModel)
            {
                (CvdFile CvdFile, ITextureResourceProvider TextureProvider) cvd = resourceProvider.GetCvd(_modelFilePath);
                _cvdModelRenderer = _sceneObjectGameObject.AddComponent<CvdModelRenderer>();

                var initTime = 0f;
                
                if (ObjectInfo.Type == ScnSceneObjectType.Switch)
                {
                    // Some switches are on by default.
                    if (ObjectInfo.Parameters[0] == 1 &&
                        ObjectInfo.Parameters[1] == 1)
                    {
                        initTime = cvd.CvdFile.AnimationDuration;
                    }

                    if (ObjectInfo.SwitchState == 1)
                    {
                        initTime = cvd.CvdFile.AnimationDuration;
                    }
                }

                _cvdModelRenderer.Init(cvd.CvdFile,
                    resourceProvider.GetMaterialFactory(),
                    cvd.TextureProvider,
                    tintColor,
                    initTime);

                if (ObjectInfo.Type == ScnSceneObjectType.General)
                {
                    _cvdModelRenderer.LoopAnimation();
                }
            }

            _sceneObjectGameObject.transform.position = GameBoxInterpreter.ToUnityPosition(ObjectInfo.GameBoxPosition);
            #if PAL3
            _sceneObjectGameObject.transform.rotation =
                Quaternion.Euler(ObjectInfo.XRotation, -ObjectInfo.YRotation, 0f);
            #elif PAL3A
            _sceneObjectGameObject.transform.rotation =
                Quaternion.Euler(ObjectInfo.XRotation, -ObjectInfo.YRotation, ObjectInfo.ZRotation);
            #endif

            if (GraphicsEffect != GraphicsEffect.None &&
                EffectTypeResolver.GetEffectComponentType(GraphicsEffect) is {} effectComponentType)
            {
                _effectComponent = _sceneObjectGameObject.AddComponent(effectComponentType) as IEffect;
                #if PAL3
                var effectParameter = ObjectInfo.EffectModelType;
                #elif PAL3A
                var effectParameter = (uint)ObjectInfo.Parameters[5];
                #endif
                Debug.Log($"Adding {GraphicsEffect} [{effectParameter}] effect for scene object {ObjectInfo.Id}");
                _effectComponent!.Init(resourceProvider, effectParameter);   
            }

            Activated = true;
            return _sceneObjectGameObject;
        }

        public GameObject GetGameObject()
        {
            return _sceneObjectGameObject;
        }
        
        public CvdModelRenderer GetCvdModelRenderer()
        {
            return _cvdModelRenderer;
        }
        
        public PolyModelRenderer GetPolyModelRenderer()
        {
            return _polyModelRenderer;
        }
        
        public virtual bool IsInteractable(InteractionContext ctx)
        {
            return false;
        }

        public virtual void Interact(bool triggerredByPlayer)
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
            
            if (_polyModelRenderer != null)
            {
                _polyModelRenderer.Dispose();
                _polyModelRenderer = null;
            }
            
            if (_cvdModelRenderer != null)
            {
                _cvdModelRenderer.Dispose();
                _cvdModelRenderer = null;
            }

            if (_sceneObjectGameObject != null)
            {
                Object.Destroy(_sceneObjectGameObject);
            }
        }
        
        #region Internal helpper methods
        internal bool IsInteractableBasedOnTimesCount()
        {
            switch (ObjectInfo.Times)
            {
                case 0xFF:
                    return true;
                case <= 0:
                    return false;
                default:
                    ObjectInfo.Times--;
                    CommandDispatcher<ICommand>.Instance.Dispatch(
                        new SceneChangeGlobalObjectTimesCountCommand(SceneInfo.CityName,
                            SceneInfo.SceneName,
                            ObjectInfo.Id,
                            ObjectInfo.Times));
                    return true;
            }
        }
        
        internal void ExecuteScriptIfAny()
        {
            if (ObjectInfo.ScriptId != ScriptConstants.InvalidScriptId)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunCommand((int)ObjectInfo.ScriptId));   
            }
        }

        internal void PlaySfxIfAny()
        {
            if (!string.IsNullOrEmpty(ObjectInfo.SfxName))
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand(ObjectInfo.SfxName, 1));
            }
        }

        internal void ChangeActivationState(bool isActivated)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new SceneActivateObjectCommand(ObjectInfo.Id, isActivated ? 1 : 0));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new SceneChangeGlobalObjectActivationStateCommand(SceneInfo.CityName,
                    SceneInfo.SceneName,
                    ObjectInfo.Id,
                    isActivated ? 1 : 0));
        }

        internal void ToggleSwitchState()
        {
            ObjectInfo.SwitchState = ObjectInfo.SwitchState == 0 ? (byte) 1 : (byte) 0;
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new SceneChangeGlobalObjectSwitchStateCommand(SceneInfo.CityName,
                    SceneInfo.SceneName,
                    ObjectInfo.Id,
                    ObjectInfo.SwitchState));
        }
        
        internal void ChangeLinkedObjectActivationStateIfAny(bool isActivated)
        {
            if (ObjectInfo.LinkedObjectId != 0xFFFF)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new SceneActivateObjectCommand(ObjectInfo.LinkedObjectId, isActivated ? 1 : 0));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new SceneChangeGlobalObjectActivationStateCommand(SceneInfo.CityName,
                        SceneInfo.SceneName,
                        ObjectInfo.LinkedObjectId,
                        isActivated ? 1 : 0));
            }
        }

        internal void InteractWithLinkedObjectIfAny()
        {
            if (ObjectInfo.LinkedObjectId != 0xFFFF)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new PlayerInteractWithObjectCommand(ObjectInfo.LinkedObjectId));
            }
        }
        #endregion
    }
}