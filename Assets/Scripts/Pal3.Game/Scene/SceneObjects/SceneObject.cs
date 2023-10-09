// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Camera;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Pol;
    using Core.DataReader.Scn;
    using Core.Primitives;
    using Core.Utilities;
    using Data;
    using Dev.Presenters;
    using Effect;
    using Engine.Abstraction;
    using Engine.Animation;
    using Engine.Coroutine;
    using Engine.DataLoader;
    using Engine.Extensions;
    using Engine.Logging;
    using Engine.Renderer;
    using Engine.Services;
    using Rendering.Material;
    using Rendering.Renderer;
    using Script;
    using Script.Waiter;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Quaternion = UnityEngine.Quaternion;
    using Vector3 = UnityEngine.Vector3;

    public struct InteractionContext
    {
        public Guid CorrelationId;
        public int InitObjectId;
        public Scene CurrentScene;
        public IGameEntity PlayerActorGameEntity;
        public bool StartedByPlayer;
    }

    public abstract class SceneObject
    {
        internal const ushort INVALID_SCENE_OBJECT_ID = 0xFFFF;
        internal const byte INFINITE_TIMES_COUNT = 0xFF;

        public ScnObjectInfo ObjectInfo;
        public ScnSceneInfo SceneInfo;
        public GraphicsEffectType GraphicsEffectType { get; }
        public SceneObjectModelType ModelType { get; }

        internal bool IsActivated;
        internal string ModelFileVirtualPath;

        private IEffect _effectComponent;
        private IGameEntity _sceneObjectGameEntity;

        private PolyModelRenderer _polyModelRenderer;
        private CvdModelRenderer _cvdModelRenderer;

        private readonly Lazy<ITransform> _cameraTransformLazy =
            new (() => ServiceLocator.Instance.Get<CameraManager>().GetCameraTransform());
        private ITransform CameraTransform => _cameraTransformLazy.Value;

        protected SceneObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo, bool hasModel = true)
        {
            ObjectInfo = objectInfo;
            SceneInfo = sceneInfo;

            ModelFileVirtualPath = hasModel && !string.IsNullOrEmpty(objectInfo.Name) ?
                GetModelFilePath(objectInfo, sceneInfo) : string.Empty;

            ModelType = SceneObjectModelTypeResolver.GetType(
                CoreUtility.GetFileName(ModelFileVirtualPath, CpkConstants.DirectorySeparatorChar));

            GraphicsEffectType = GetEffectType(objectInfo);
        }

        private GraphicsEffectType GetEffectType(ScnObjectInfo objectInfo)
        {
            if (!objectInfo.Name.StartsWith('+')) return GraphicsEffectType.None;

            if (objectInfo.Parameters[1] == 1 && ModelType == SceneObjectModelType.CvdModel)
            {
                // Dead object
                return GraphicsEffectType.None;
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

        public virtual IGameEntity Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (IsActivated) return _sceneObjectGameEntity;

            _sceneObjectGameEntity = new GameEntity($"Object_{ObjectInfo.Id}_{ObjectInfo.Type}");

            // Attach SceneObjectInfo to the GameEntity for better debuggability
            #if UNITY_EDITOR
            var infoPresenter = _sceneObjectGameEntity.AddComponent<SceneObjectInfoPresenter>();
            infoPresenter.sceneObjectInfo = ObjectInfo;
            #endif

            IMaterialFactory materialFactory = resourceProvider.GetMaterialFactory();

            if (ModelType == SceneObjectModelType.PolModel)
            {
                PolFile polFile = resourceProvider.GetGameResourceFile<PolFile>(ModelFileVirtualPath);
                ITextureResourceProvider textureProvider = resourceProvider.CreateTextureResourceProvider(
                    CoreUtility.GetDirectoryName(ModelFileVirtualPath, CpkConstants.DirectorySeparatorChar));
                _polyModelRenderer = _sceneObjectGameEntity.AddComponent<PolyModelRenderer>();
                _polyModelRenderer.Render(polFile,
                    textureProvider,
                    materialFactory,
                    isStaticObject: false,
                    tintColor);
            }
            else if (ModelType == SceneObjectModelType.CvdModel)
            {
                CvdFile cvdFile = resourceProvider.GetGameResourceFile<CvdFile>(ModelFileVirtualPath);
                ITextureResourceProvider textureProvider = resourceProvider.CreateTextureResourceProvider(
                    CoreUtility.GetDirectoryName(ModelFileVirtualPath, CpkConstants.DirectorySeparatorChar));
                _cvdModelRenderer = _sceneObjectGameEntity.AddComponent<CvdModelRenderer>();
                _cvdModelRenderer.Init(cvdFile,
                    textureProvider,
                    materialFactory,
                    tintColor);

                if (ObjectInfo.Type == SceneObjectType.General)
                {
                    _cvdModelRenderer.LoopAnimation();
                }
            }

            Vector3 newPosition = ObjectInfo.GameBoxPosition.ToUnityPosition();
            #if PAL3
            Quaternion newRotation = new GameBoxVector3(
                    ObjectInfo.GameBoxXRotation,
                    ObjectInfo.GameBoxYRotation,
                    0f).ToUnityQuaternion();
            #elif PAL3A
            Quaternion newRotation = new GameBoxVector3(
                    ObjectInfo.GameBoxXRotation,
                    ObjectInfo.GameBoxYRotation,
                    ObjectInfo.GameBoxZRotation).ToUnityQuaternion();
            #endif

            _sceneObjectGameEntity.Transform.SetPositionAndRotation(newPosition, newRotation);

            if (GraphicsEffectType != GraphicsEffectType.None &&
                EffectTypeResolver.GetEffectComponentType(GraphicsEffectType) is {} effectComponentType)
            {
                _effectComponent = _sceneObjectGameEntity.AddComponent(effectComponentType) as IEffect;
                #if PAL3
                var effectParameter = ObjectInfo.EffectModelType;
                #elif PAL3A
                var effectParameter = (uint)ObjectInfo.Parameters[5];
                #endif
                EngineLogger.Log($"Adding {GraphicsEffectType} [{effectParameter}] effect for scene object {ObjectInfo.Id}");
                _effectComponent!.Init(resourceProvider, effectParameter);
            }

            IsActivated = true;
            return _sceneObjectGameEntity;
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
                center = ObjectInfo.GameBoxPosition.ToUnityPosition(),
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

        public IGameEntity GetGameEntity()
        {
            return _sceneObjectGameEntity;
        }

        public abstract bool IsDirectlyInteractable(float distance);

        public abstract bool ShouldGoToCutsceneWhenInteractionStarted();

        public abstract IEnumerator InteractAsync(InteractionContext ctx);

        /// <summary>
        /// Should be called after the child class has finished its own deactivation.
        /// </summary>
        public virtual void Deactivate()
        {
            if (!IsActivated) return;

            IsActivated = false;

            if (_effectComponent != null)
            {
                _effectComponent.Dispose();
                _effectComponent = null;
            }

            if (_polyModelRenderer != null)
            {
                _polyModelRenderer.Dispose();
                _polyModelRenderer.Destroy();
                _polyModelRenderer = null;
            }

            if (_cvdModelRenderer != null)
            {
                _cvdModelRenderer.Dispose();
                _cvdModelRenderer.Destroy();
                _cvdModelRenderer = null;
            }

            if (_sceneObjectGameEntity != null)
            {
                _sceneObjectGameEntity.Destroy();
                _sceneObjectGameEntity = null;
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
                yield return CoroutineYieldInstruction.WaitForSeconds(0.5f);
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
                        yield return CoroutineYieldInstruction.WaitForSeconds(1);
                        ResetCamera(); // Make sure camera is looking at the player before moving
                        yield return null; // Wait for one frame to make sure camera is reset

                        yield return MoveCameraToLookAtPointAsync(
                            nextLinkedObject.ObjectInfo.GameBoxPosition.ToUnityPosition(),
                            ctx.PlayerActorGameEntity.Transform);

                        yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, nextLinkedObjectId);
                        yield return CoroutineYieldInstruction.WaitForSeconds(1);

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
                    _sceneObjectGameEntity.Transform.Position.ToGameBoxPosition().ToUnityPosition(scale: 1f)));
        }

        internal void SaveCurrentYRotation()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new SceneSaveGlobalObjectYRotationCommand(SceneInfo.CityName,
                    SceneInfo.SceneName,
                    ObjectInfo.Id,
                    _sceneObjectGameEntity.Transform.EulerAngles.y.ToGameBoxYEulerAngle()));
        }

        internal IEnumerator MoveCameraToLookAtPointAsync(
            Vector3 position,
            ITransform playerActorTransform)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new CameraFollowPlayerCommand(0));
            Vector3 offset = position - playerActorTransform.Position;
            Vector3 cameraCurrentPosition = CameraTransform.Position;
            Vector3 targetPosition = cameraCurrentPosition + offset;
            yield return CameraTransform.MoveAsync(targetPosition, 2f, AnimationCurveType.Sine);
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
            if (_sceneObjectGameEntity != null)
            {
                foreach (StaticMeshRenderer renderer in
                         _sceneObjectGameEntity.GetComponentsInChildren<StaticMeshRenderer>())
                {
                    if (!renderer.IsVisible) return false;
                }

                return true;
            }

            return false;
        }

        #endregion
    }
}