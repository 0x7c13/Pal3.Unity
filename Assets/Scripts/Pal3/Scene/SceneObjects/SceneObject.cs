// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
    using Core.Utils;
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
        public bool StartedByPlayer;
    }

    public abstract class SceneObject
    {
        internal const ushort INVALID_SCENE_OBJECT_ID = 0xFFFF;
        internal const byte INFINITE_TIMES_COUNT = 0xFF;

        public ScnObjectInfo ObjectInfo;
        public ScnSceneInfo SceneInfo;
        public GraphicsEffect GraphicsEffect { get; }
        public SceneObjectModelType ModelType { get; }

        internal bool Activated;
        internal string ModelFileVirtualPath;

        private IEffect _effectComponent;
        private GameObject _sceneObjectGameObject;

        private PolyModelRenderer _polyModelRenderer;
        private CvdModelRenderer _cvdModelRenderer;

        protected SceneObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo, bool hasModel = true)
        {
            ObjectInfo = objectInfo;
            SceneInfo = sceneInfo;

            ModelFileVirtualPath = hasModel && !string.IsNullOrEmpty(objectInfo.Name) ?
                GetModelFilePath(objectInfo, sceneInfo) : string.Empty;

            ModelType = SceneObjectModelTypeResolver.GetType(
                Utility.GetFileName(ModelFileVirtualPath, CpkConstants.DirectorySeparatorChar));

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
            var modelFilePath = string.Empty;

            if (objectInfo.Name.StartsWith('_')) // is object model in current scene folder
            {
                modelFilePath = FileConstants.GetGameObjectModelFileVirtualPath(sceneInfo, objectInfo.Name);
            }
            else if (objectInfo.Name.StartsWith('+'))
            {
                // Special vfx effect.
            }
            else if (!objectInfo.Name.Contains('.')) // is item name
            {
                modelFilePath = FileConstants.GetGameItemModelFileVirtualPath(objectInfo.Name);
            }
            else // is object model in game object folder
            {
                modelFilePath = FileConstants.GetGameObjectModelFileVirtualPath(objectInfo.Name);
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

            IMaterialFactory materialFactory = resourceProvider.GetMaterialFactory();

            if (ModelType == SceneObjectModelType.PolModel)
            {
                PolFile polFile = resourceProvider.GetGameResourceFile<PolFile>(ModelFileVirtualPath);
                ITextureResourceProvider textureProvider = resourceProvider.CreateTextureResourceProvider(
                    Utility.GetDirectoryName(ModelFileVirtualPath, CpkConstants.DirectorySeparatorChar));
                _polyModelRenderer = _sceneObjectGameObject.AddComponent<PolyModelRenderer>();
                _polyModelRenderer.Render(polFile,
                    textureProvider,
                    materialFactory,
                    tintColor);
            }
            else if (ModelType == SceneObjectModelType.CvdModel)
            {
                CvdFile cvdFile = resourceProvider.GetGameResourceFile<CvdFile>(ModelFileVirtualPath);
                ITextureResourceProvider textureProvider = resourceProvider.CreateTextureResourceProvider(
                    Utility.GetDirectoryName(ModelFileVirtualPath, CpkConstants.DirectorySeparatorChar));
                _cvdModelRenderer = _sceneObjectGameObject.AddComponent<CvdModelRenderer>();
                _cvdModelRenderer.Init(cvdFile,
                    textureProvider,
                    materialFactory,
                    tintColor);

                if (ObjectInfo.Type == ScnSceneObjectType.General)
                {
                    _cvdModelRenderer.LoopAnimation();
                }
            }

            Vector3 newPosition = GameBoxInterpreter.ToUnityPosition(ObjectInfo.GameBoxPosition);
            #if PAL3
            Quaternion newRotation = GameBoxInterpreter.ToUnityRotation(
                new Vector3(ObjectInfo.GameBoxXRotation, ObjectInfo.GameBoxYRotation, 0f));
            #elif PAL3A
            Quaternion newRotation = GameBoxInterpreter.ToUnityRotation(
                new Vector3(ObjectInfo.GameBoxXRotation, ObjectInfo.GameBoxYRotation, ObjectInfo.GameBoxZRotation));
            #endif

            _sceneObjectGameObject.transform.SetPositionAndRotation(newPosition, newRotation);

            if (GraphicsEffect != GraphicsEffect.None &&
                EffectTypeResolver.GetEffectComponentType(GraphicsEffect) is {} effectComponentType)
            {
                _effectComponent = _sceneObjectGameObject.AddComponent(effectComponentType) as IEffect;
                #if PAL3
                var effectParameter = ObjectInfo.EffectModelType;
                #elif PAL3A
                var effectParameter = (uint)ObjectInfo.Parameters[5];
                #endif
                Debug.Log($"[{nameof(SceneObject)}] Adding {GraphicsEffect} [{effectParameter}] effect for scene object {ObjectInfo.Id}");
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
                _sceneObjectGameObject = null;
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
                case INFINITE_TIMES_COUNT:
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

        internal void FlipAndSaveSwitchState()
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
            if (objectId == INVALID_SCENE_OBJECT_ID) yield break;

            var allActivatedSceneObjects = ctx.CurrentScene.GetAllActivatedSceneObjects();

            if (!allActivatedSceneObjects.Contains(objectId))
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new SceneActivateObjectCommand(objectId, 1));
            }
            else
            {
                yield return ctx.CurrentScene.GetSceneObject(objectId).InteractAsync(ctx);
            }

            #if PAL3
            SceneObject linkedObject = ctx.CurrentScene.GetSceneObject(objectId);
            ushort nextLinkedObjectId = linkedObject.ObjectInfo.LinkedObjectId;
            if (nextLinkedObjectId != INVALID_SCENE_OBJECT_ID)
            {
                yield return new WaitForSeconds(0.5f);
                yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, nextLinkedObjectId);
            }
            #elif PAL3A
            // PAL3A has additional activation logic for chained objects
            // When all objects linked to a child object are activated, the child object will be activated
            SceneObject linkedObject = ctx.CurrentScene.GetSceneObject(objectId);
            ushort nextLinkedObjectId = linkedObject.ObjectInfo.LinkedObjectId;
            if (nextLinkedObjectId != INVALID_SCENE_OBJECT_ID)
            {
                HashSet<int> activatedObjects = ctx.CurrentScene.GetAllActivatedSceneObjects();
                if (ctx.CurrentScene.GetAllSceneObjects()
                    .Where(_ => _.Value.ObjectInfo.LinkedObjectId == nextLinkedObjectId)
                    .All(_ => activatedObjects.Contains(_.Key)))
                {
                    SceneObject nextLinkedObject = ctx.CurrentScene.GetSceneObject(nextLinkedObjectId);

                    if (linkedObject.ObjectInfo.Type != nextLinkedObject.ObjectInfo.Type)
                    {
                        yield return new WaitForSeconds(1);
                        ResetCamera(); // Make sure camera is looking at the player before moving
                        yield return null; // Wait for one frame to make sure camera is reset

                        yield return MoveCameraToLookAtPointAsync(
                            GameBoxInterpreter.ToUnityPosition(nextLinkedObject.ObjectInfo.GameBoxPosition),
                            ctx.PlayerActorGameObject);

                        yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, nextLinkedObjectId);
                        yield return new WaitForSeconds(1);

                        ResetCamera(); // Reset again
                        yield return null; // Wait for one frame to make sure camera is reset
                    }
                    else
                    {
                        yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, nextLinkedObjectId);
                    }
                }
            }
            #endif
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

        internal void SaveCurrentYRotation()
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
            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFollowPlayerCommand(0));

            Vector3 offset = position - playerActorGameObject.transform.position;
            Transform cameraTransform = Camera.main!.transform;
            Vector3 cameraCurrentPosition = cameraTransform.position;
            Vector3 targetPosition = cameraCurrentPosition + offset;
            yield return cameraTransform.MoveAsync(targetPosition, 2f, AnimationCurveType.Sine);
        }

        internal void CameraFocusOnObject(int objectId)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFollowPlayerCommand(1));
            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFocusOnSceneObjectCommand(objectId));
        }

        internal void ResetCamera()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFollowPlayerCommand(1));
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