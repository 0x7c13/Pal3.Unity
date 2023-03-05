// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using System.IO;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Animation;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Pol;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Services;
    using Data;
    using Dev;
    using Effect;
    using MetaData;
    using Renderer;
    using Script;
    using Script.Waiter;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public struct InteractionContext
    {
        public Guid CorrelationId;
        public int InitObjectId;
        public Scene CurrentScene;
        public GameObject PlayerActorGameObject;
    }

    public abstract class SceneObject
    {
        public ScnObjectInfo ObjectInfo;
        public ScnSceneInfo SceneInfo;
        public GraphicsEffect GraphicsEffect { get; }
        public SceneObjectModelType ModelType { get; }

        internal bool Activated;
        internal string ModelFilePath;

        private IEffect _effectComponent;
        private GameObject _sceneObjectGameObject;

        private PolyModelRenderer _polyModelRenderer;
        private CvdModelRenderer _cvdModelRenderer;

        protected SceneObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo, bool hasModel = true)
        {
            ObjectInfo = objectInfo;
            SceneInfo = sceneInfo;

            ModelFilePath = hasModel && !string.IsNullOrEmpty(objectInfo.Name) ?
                GetModelFilePath(objectInfo, sceneInfo) : string.Empty;

            ModelType = SceneObjectModelTypeResolver.GetType(Path.GetFileName(ModelFilePath));
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
                (PolFile PolFile, ITextureResourceProvider TextureProvider) poly = resourceProvider.GetPol(ModelFilePath);
                _polyModelRenderer = _sceneObjectGameObject.AddComponent<PolyModelRenderer>();
                _polyModelRenderer.Render(poly.PolFile,
                    resourceProvider.GetMaterialFactory(),
                    poly.TextureProvider,
                    tintColor);
            }
            else if (ModelType == SceneObjectModelType.CvdModel)
            {
                (CvdFile CvdFile, ITextureResourceProvider TextureProvider) cvd = resourceProvider.GetCvd(ModelFilePath);
                _cvdModelRenderer = _sceneObjectGameObject.AddComponent<CvdModelRenderer>();

                var initTime = 0f;

                if (ObjectInfo.Type is ScnSceneObjectType.Switch
                        #if PAL3
                        or ScnSceneObjectType.Collidable
                        #endif
                        or ScnSceneObjectType.Shakeable
                    && ObjectInfo.SwitchState == 1)
                {
                    initTime = cvd.CvdFile.AnimationDuration;
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
            _sceneObjectGameObject.transform.rotation = GameBoxInterpreter.ToUnityRotation(
                new Vector3(ObjectInfo.GameBoxXRotation, ObjectInfo.GameBoxYRotation, 0f));
            #elif PAL3A
            _sceneObjectGameObject.transform.rotation = GameBoxInterpreter.ToUnityRotation(
                new Vector3(ObjectInfo.GameBoxXRotation, ObjectInfo.GameBoxYRotation, ObjectInfo.GameBoxZRotation));
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

        protected Bounds GetMeshBounds()
        {
            if (_polyModelRenderer != null)
            {
                return _polyModelRenderer.GetMeshBounds();
            }

            if (_cvdModelRenderer != null)
            {
                return _cvdModelRenderer.GetMeshBounds();
            }

            return new Bounds
            {
                center = Vector3.zero,
                size = Vector3.one
            };
        }

        public Bounds GetRendererBounds()
        {
            if (_polyModelRenderer != null)
            {
                return _polyModelRenderer.GetRendererBounds();
            }

            if (_cvdModelRenderer != null)
            {
                return _cvdModelRenderer.GetRendererBounds();
            }

            return new Bounds
            {
                center = GameBoxInterpreter.ToUnityPosition(ObjectInfo.GameBoxPosition),
                size = Vector3.one
            };
        }

        public CvdModelRenderer GetCvdModelRenderer()
        {
            return _cvdModelRenderer;
        }

        public PolyModelRenderer GetPolyModelRenderer()
        {
            return _polyModelRenderer;
        }

        public GameObject GetGameObject()
        {
            return _sceneObjectGameObject;
        }

        public virtual bool IsDirectlyInteractable(float distance)
        {
            return false;
        }

        public virtual bool ShouldGoToCutsceneWhenInteractionStarted()
        {
            return true;
        }

        public virtual IEnumerator InteractAsync(InteractionContext ctx)
        {
            // Do nothing
            yield break;
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

        internal void RequestForInteraction()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerInteractWithObjectCommand(ObjectInfo.Id));
        }

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
                        new SceneSaveGlobalObjectTimesCountCommand(
                            SceneInfo.CityName,
                            SceneInfo.SceneName,
                            ObjectInfo.Id,
                            ObjectInfo.Times));
                    return true;
            }
        }

        internal void ExecuteScriptIfAny()
        {
            if (ObjectInfo.ScriptId == ScriptConstants.InvalidScriptId) return;
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunCommand((int)ObjectInfo.ScriptId));
        }

        internal IEnumerator ExecuteScriptAndWaitForFinishIfAnyAsync()
        {
            if (ObjectInfo.ScriptId == ScriptConstants.InvalidScriptId) yield break;
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunCommand((int)ObjectInfo.ScriptId));
            yield return new WaitUntilScriptFinished(PalScriptType.Scene, ObjectInfo.ScriptId);
        }

        internal void PlaySfxIfAny()
        {
            if (!string.IsNullOrEmpty(ObjectInfo.SfxName))
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand(ObjectInfo.SfxName, 1));
            }
        }

        internal void PlaySfx(string sfxName, int loopCount = 1)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand(sfxName, loopCount));
        }

        internal void ChangeAndSaveActivationState(bool isActivated)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new SceneActivateObjectCommand(ObjectInfo.Id, isActivated ? 1 : 0));
            // Scene will receive this command and save its global activation state
            // so no need to dispatch a SceneSaveGlobalObjectActivationStateCommand here
        }

        internal void ToggleAndSaveSwitchState()
        {
            ObjectInfo.SwitchState = ObjectInfo.SwitchState == 0 ? (byte) 1 : (byte) 0;
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new SceneSaveGlobalObjectSwitchStateCommand(SceneInfo.CityName,
                    SceneInfo.SceneName,
                    ObjectInfo.Id,
                    ObjectInfo.SwitchState));
        }

        internal IEnumerator ActivateOrInteractWithObjectIfAnyAsync(InteractionContext ctx, ushort objectId)
        {
            if (objectId == 0xFFFF) yield break;

            Scene scene = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene();
            var allActivatedSceneObjects = scene.GetAllActivatedSceneObjects();

            if (!allActivatedSceneObjects.Contains(objectId))
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new SceneActivateObjectCommand(objectId, 1));
            }
            else
            {
                yield return scene.GetSceneObject(objectId).InteractAsync(ctx);
            }
        }

        internal void ChangeAndSaveNavLayerIndex(byte layerIndex)
        {
            ObjectInfo.LayerIndex = layerIndex;
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new SceneSaveGlobalObjectLayerIndexCommand(SceneInfo.CityName,
                    SceneInfo.SceneName,
                    ObjectInfo.Id,
                    layerIndex));
        }

        internal void SaveCurrentPosition()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new SceneSaveGlobalObjectPositionCommand(SceneInfo.CityName,
                    SceneInfo.SceneName,
                    ObjectInfo.Id,
                    GameBoxInterpreter.ToGameBoxPosition(_sceneObjectGameObject.transform.position)));
        }

        internal void SaveYRotation()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new SceneSaveGlobalObjectYRotationCommand(SceneInfo.CityName,
                    SceneInfo.SceneName,
                    ObjectInfo.Id,
                    GameBoxInterpreter.ToGameBoxYRotation(_sceneObjectGameObject.transform.rotation.eulerAngles.y)));
        }

        internal IEnumerator MoveCameraToLookAtPointAsync(
            Vector3 position,
            GameObject playerActorGameObject)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFreeCommand(0));

            Vector3 offset = position - playerActorGameObject.transform.position;
            Transform cameraTransform = Camera.main!.transform;
            Vector3 cameraCurrentPosition = cameraTransform.position;
            Vector3 targetPosition = cameraCurrentPosition + offset;
            yield return AnimationHelper.MoveTransformAsync(cameraTransform,
                targetPosition, 1.5f, AnimationCurveType.Sine);
        }

        internal void CameraFocusOnObject(int objectId)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFreeCommand(1));
            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFocusOnSceneObjectCommand(objectId));
        }

        internal void ResetCamera()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFreeCommand(1));
        }

        internal bool IsFullyVisibleToCamera()
        {
            if (_sceneObjectGameObject != null)
            {
                foreach (MeshRenderer renderer in
                         _sceneObjectGameObject.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!renderer.isVisible) return false;
                }

                return true;
            }

            return false;
        }

        #endregion
    }
}