// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Actor;
    using Actor.Controllers;
    using Command;
    using Command.Extensions;
    using Constants;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Core.Primitives;
    using Data;
    using Effect;
    using Engine.Animation;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using Engine.Logging;
    using Engine.Navigation;
    using Engine.Renderer;
    using Engine.Utilities;
    using Rendering.Material;
    using Rendering.Renderer;
    using SceneObjects;
    using State;
    using UnityEngine;
    using Color = Core.Primitives.Color;

    public class Scene : SceneBase,
        ICommandExecutor<SceneActivateObjectCommand>,
        ICommandExecutor<SceneMoveObjectCommand>,
        ICommandExecutor<SceneOpenDoorCommand>,
        #if PAL3A
        ICommandExecutor<SceneActivateObject2Command>,
        ICommandExecutor<FengYaSongCommand>,
        ICommandExecutor<SceneCloseDoorCommand>,
        ICommandExecutor<SceneEnableFogCommand>,
        #endif
        ICommandExecutor<ActorActivateCommand>,
        ICommandExecutor<ActorLookAtActorCommand>
    {
        private const float SCENE_CVD_ANIMATION_DEFAULT_TIMESCALE = .2f;
        private const int MAX_NUM_OF_POINT_LIGHTS_WITH_SHADOWS = 3;

        private static int _lightCullingMask;

        private IGameEntity _parent;
        private IGameEntity _mesh;

        private Light _mainLight;
        private readonly List<Light> _pointLights = new();

        private readonly List<IGameEntity> _navMeshEntities = new ();

        private readonly HashSet<int> _activatedSceneObjects = new ();
        private readonly Dictionary<int, IGameEntity> _actorEntities = new ();

        private HashSet<int> _sceneObjectIdsToNotLoadFromSaveState;

        private GameResourceProvider _resourceProvider;
        private SceneStateManager _sceneStateManager;
        private bool _isLightingEnabled;
        private IGameEntity _cameraEntity;
        private SkyBoxRenderer _skyBoxRenderer;
        private IMaterialFactory _materialFactory;

        protected override void OnEnableGameEntity()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        protected override void OnDisableGameEntity()
        {
            // Remove fog when scene is disabled
            if (RenderSettings.fog) RenderSettings.fog = false;

            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

            _mesh.Destroy();

            foreach (IGameEntity navMeshEntity in _navMeshEntities)
            {
                navMeshEntity.Destroy();
            }

            foreach (int sceneObjectId in _activatedSceneObjects)
            {
                SceneObjects[sceneObjectId].Deactivate();
            }

            foreach (var actor in _actorEntities)
            {
                actor.Value.Destroy();
            }

            if (_skyBoxRenderer != null)
            {
                _skyBoxRenderer.Destroy();
            }
        }

        public void Init(GameResourceProvider resourceProvider,
            SceneStateManager sceneStateManager,
            bool isLightingEnabled,
            IGameEntity cameraEntity,
            HashSet<int> sceneObjectIdsToNotLoadFromSaveState)
        {
            _resourceProvider = resourceProvider;
            _sceneStateManager = sceneStateManager;
            _isLightingEnabled = isLightingEnabled;
            _cameraEntity = cameraEntity;
            _sceneObjectIdsToNotLoadFromSaveState = sceneObjectIdsToNotLoadFromSaveState;
            _lightCullingMask = (1 << LayerMask.NameToLayer("Default")) |
                                (1 << LayerMask.NameToLayer("VFX"));
            _materialFactory = resourceProvider.GetMaterialFactory();
        }

        public void Load(ScnFile scnFile, IGameEntity parent)
        {
            _parent = parent;

            Stopwatch timer = Stopwatch.StartNew();

            base.Init(_resourceProvider,
                scnFile,
                _sceneStateManager,
                _sceneObjectIdsToNotLoadFromSaveState);

            EngineLogger.Log($"Scene data initialized in {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            Color actorTintColor = Color.White;
            if (IsNightScene())
            {
                actorTintColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            timer.Restart();
            RenderMesh();
            EngineLogger.Log($"Mesh initialized in {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            RenderSkyBox();
            SetupNavMesh();
            if (_isLightingEnabled)
            {
                SetupEnvironmentLighting();
            }
            EngineLogger.Log($"SkyBox + NavMesh initialized in {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            InitActorObjects(actorTintColor, Tilemap);
            EngineLogger.Log($"Actors initialized in {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            InitSceneObjects();
            EngineLogger.Log($"Objects initialized in {timer.ElapsedMilliseconds} ms");
        }

        public ScnSceneInfo GetSceneInfo()
        {
            return ScnFile.SceneInfo;
        }

        public Tilemap GetTilemap()
        {
            return Tilemap;
        }

        public SceneObject GetSceneObject(int id)
        {
            return SceneObjects.TryGetValue(id, out SceneObject sceneObject) ? sceneObject : null;
        }

        public HashSet<int> GetAllActivatedSceneObjects()
        {
            return _activatedSceneObjects;
        }

        public Dictionary<int, SceneObject> GetAllSceneObjects()
        {
            return SceneObjects;
        }

        public Actor GetActor(int id)
        {
            return Actors.TryGetValue(id, out Actor actor) ? actor : null;
        }

        public Dictionary<int, Actor> GetAllActors()
        {
            return Actors;
        }

        public IGameEntity GetActorGameEntity(int id)
        {
            return _actorEntities.TryGetValue(id, out IGameEntity actorObject) ? actorObject : null;
        }

        public Dictionary<int, IGameEntity> GetAllActorGameEntities()
        {
            return _actorEntities;
        }

        public bool IsPositionInsideJumpableArea(int layerIndex, Vector2Int tilePosition)
        {
            foreach (var objectId in _activatedSceneObjects)
            {
                SceneObject sceneObject = SceneObjects[objectId];
                if (sceneObject.ObjectInfo.Type == SceneObjectType.JumpableArea &&
                    layerIndex == sceneObject.ObjectInfo.LayerIndex &&
                    sceneObject.ObjectInfo.TileMapTriggerRect.IsPointInsideRect(tilePosition.x, tilePosition.y))
                {
                    return true;
                }
            }

            return false;
        }

        private void RenderMesh()
        {
            // Render mesh
            _mesh = GameEntityFactory.Create($"Mesh_{ScnFile.SceneInfo.CityName}_{ScnFile.SceneInfo.SceneName}",
                _parent, worldPositionStays: false);
            _mesh.IsStatic = true; // Scene mesh is static

            var polyMeshRenderer = _mesh.AddComponent<PolyModelRenderer>();
            polyMeshRenderer.Render(ScenePolyMesh.PolFile,
                ScenePolyMesh.TextureProvider,
                _materialFactory,
                isStaticObject: true, // Scene mesh is static
                Color.White,
                IsWaterSurfaceOpaque());

            if (SceneCvdMesh != null)
            {
                var cvdMeshRenderer = _mesh.AddComponent<CvdModelRenderer>();
                cvdMeshRenderer.Init(SceneCvdMesh.Value.CvdFile,
                    SceneCvdMesh.Value.TextureProvider,
                    _materialFactory);
                cvdMeshRenderer.LoopAnimation(SCENE_CVD_ANIMATION_DEFAULT_TIMESCALE);
            }
        }

        private void RenderSkyBox()
        {
            if (ScnFile.SceneInfo.SkyBox == 0) return;
            _skyBoxRenderer = _cameraEntity.AddComponent<SkyBoxRenderer>();
            ITexture2D[] skyBoxTextures = _resourceProvider.GetSkyBoxTextures((int) ScnFile.SceneInfo.SkyBox);
            _skyBoxRenderer.Render(_cameraEntity,
                skyBoxTextures[0],
                skyBoxTextures[1],
                skyBoxTextures[2],
                skyBoxTextures[3],
                skyBoxTextures[4],
                skyBoxTextures[5]);
        }

        private void SetupNavMesh()
        {
            if (UnityEngineUtility.IsHandheldDevice())
            {
                // We only enable joystick/gamepad control for the gameplay on mobile
                // devices so there is no need for setting up nav mesh.
                return;
            }

            for (var i = 0; i < NavFile.Layers.Length; i++)
            {
                IGameEntity navMeshGameEntity = GameEntityFactory.Create($"NavMesh_Layer_{i}",
                    _parent, worldPositionStays: false);
                navMeshGameEntity.IsStatic = true; // NavMesh is static

                NavMesh navMesh = navMeshGameEntity.AddComponent<NavMesh>();
                navMesh.Init(layerIndex: i, NavFile.MeshData[i]);

                _navMeshEntities.Add(navMeshGameEntity);
            }
        }

        private void SetupEnvironmentLighting()
        {
            Vector3 mainLightPosition = new Vector3(0, 20f, 0);
            Quaternion mainLightRotation = ScnFile.SceneInfo.SceneType == SceneType.InDoor ?
                    Quaternion.Euler(120f, -20f, 0f) :
                    Quaternion.Euler(70f, -30f, 0f);

            if (ScnFile.SceneInfo.SceneType == SceneType.InDoor && IsNightScene())
            {
                mainLightRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            // Most in-door scenes have a single spot light source where we can find in the LGT file,
            // which can be used as the main light source for the scene.
            // if (ScnFile.SceneInfo.SceneType == ScnSceneType.InDoor &&
            //     LgtFile.LightNodes.FirstOrDefault(_ => _.LightType == GameBoxLightType.Spot) is var mainLight)
            // {
            //     float w = MathF.Sqrt(1.0f + mainLight.WorldMatrix.m00 + mainLight.WorldMatrix.m11 + mainLight.WorldMatrix.m22) / 2.0f;
            //     mainLightRotation = GameBoxInterpreter.LgtQuaternionToUnityQuaternion(new GameBoxQuaternion()
            //     {
            //         X = (mainLight.WorldMatrix.m21 - mainLight.WorldMatrix.m12) / (4.0f * w),
            //         Y = (mainLight.WorldMatrix.m02 - mainLight.WorldMatrix.m20) / (4.0f * w),
            //         Z = (mainLight.WorldMatrix.m10 - mainLight.WorldMatrix.m01) / (4.0f * w),
            //         W = w,
            //     });
            // }

            IGameEntity mainLightEntity = GameEntityFactory.Create($"LightSource_Main",
                _parent, worldPositionStays: false);
            mainLightEntity.Transform.SetPositionAndRotation(mainLightPosition, mainLightRotation);

            _mainLight = mainLightEntity.AddComponent<Light>();
            _mainLight.type = LightType.Directional;
            _mainLight.range = 500f;
            _mainLight.shadows = LightShadows.Soft;
            _mainLight.cullingMask = _lightCullingMask;
            RenderSettings.sun = _mainLight;

            #if PAL3
            _mainLight.color = IsNightScene() ?
                new Color(60f / 255f, 80f / 255f, 170f / 255f).ToUnityColor() :
                new Color(220f / 255f, 210f / 255f, 200f / 255f).ToUnityColor();
            _mainLight.intensity = (IsNightScene() || ScnFile.SceneInfo.SceneType == SceneType.InDoor) ? 0.75f : 0.9f;
            #elif PAL3A
            _mainLight.color = IsNightScene() ?
                new Color(60f / 255f, 80f / 255f, 170f / 255f).ToUnityColor() :
                new Color(200f / 255f, 200f / 255f, 200f / 255f).ToUnityColor();
            _mainLight.intensity = (IsNightScene() || ScnFile.SceneInfo.SceneType == SceneType.InDoor) ? 0.65f : 0.9f;
            #endif

            // Ambient light
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.ambientLight = IsNightScene() ?
                new Color( 90f/ 255f, 100f / 255f, 130f / 255f).ToUnityColor() :
                new Color(180f / 255f, 180f / 255f, 160f / 255f).ToUnityColor();

            // Apply lighting override
            var key = (ScnFile.SceneInfo.CityName.ToLower(), ScnFile.SceneInfo.SceneName.ToLower());
            if (LightingConstants.MainLightColorInfoGlobal.TryGetValue(ScnFile.SceneInfo.CityName, out Color globalMainLightColorOverride))
            {
                _mainLight.color = globalMainLightColorOverride.ToUnityColor();
            }
            if (LightingConstants.MainLightColorInfo.TryGetValue(key, out Color mainLightColorOverride))
            {
                _mainLight.color = mainLightColorOverride.ToUnityColor();
            }
            if (LightingConstants.MainLightRotationInfo.TryGetValue(key, out Quaternion mainLightRotationOverride))
            {
                mainLightEntity.Transform.Rotation = mainLightRotationOverride;
            }
        }

        private void AddPointLight(IGameEntity parent, float yOffset)
        {
            // Add a point light to the fire fx
            IGameEntity lightSource = GameEntityFactory.Create($"LightSource_Point",
                parent, worldPositionStays: false);
            lightSource.Transform.LocalPosition = new Vector3(0f, yOffset, 0f);

            var lightComponent = lightSource.AddComponent<Light>();
            lightComponent.color = new Color(220f / 255f, 145f / 255f, 105f / 255f).ToUnityColor();
            lightComponent.type = LightType.Point;
            lightComponent.intensity = IsNightScene() ? 1f : 1.2f;
            lightComponent.range = 50f;
            lightComponent.shadows = LightShadows.Soft;
            lightComponent.shadowNearPlane = 0.25f;
            lightComponent.cullingMask = _lightCullingMask;

            _pointLights.Add(lightComponent);
            StripPointLightShadowsIfNecessary();
        }

        private void StripPointLightShadowsIfNecessary()
        {
            var disableShadows = !IsNightScene() || _pointLights.Count > MAX_NUM_OF_POINT_LIGHTS_WITH_SHADOWS;

            foreach (Light pointLight in _pointLights)
            {
                pointLight.shadows = disableShadows ? LightShadows.None : LightShadows.Soft;
            }
        }

        private void InitSceneObjects()
        {
            foreach (SceneObject sceneObject in SceneObjects.Values)
            {
                if (!_sceneObjectIdsToNotLoadFromSaveState.Contains(sceneObject.ObjectInfo.Id) &&
                    _sceneStateManager.TryGetSceneObjectStateOverride(ScnFile.SceneInfo.CityName,
                        ScnFile.SceneInfo.SceneName,
                        sceneObject.ObjectInfo.Id,
                        out SceneObjectStateOverride state) && state.IsActivated.HasValue)
                {
                    if (state.IsActivated.Value)
                    {
                        ActivateSceneObject(sceneObject.ObjectInfo.Id);
                    }
                }
                else if (sceneObject.ObjectInfo.InitActive == 1)
                {
                    ActivateSceneObject(sceneObject.ObjectInfo.Id);
                }
            }
        }

        private void InitActorObjects(Color tintColor, Tilemap tilemap)
        {
            foreach (Actor actorObject in Actors.Values)
            {
                CreateActorObject(actorObject, tintColor, tilemap);
            }
        }

        public void ActivateSceneObject(int id)
        {
            if (_activatedSceneObjects.Contains(id)) return;
            SceneObject sceneObject = SceneObjects[id];

            Color tintColor = Color.White;
            if (IsNightScene())
            {
                tintColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            }

            IGameEntity sceneObjectGameEntity = sceneObject.Activate(_resourceProvider, tintColor);
            sceneObjectGameEntity.SetParent(_parent, worldPositionStays: false);

            if (_isLightingEnabled)
            {
                if (sceneObject.GraphicsEffectType == GraphicsEffectType.Fire &&
                    sceneObjectGameEntity.GetComponent<FireEffect>() is { } fireEffect &&
                    fireEffect.EffectGameEntity != null)
                {
                    var yOffset = EffectConstants.FireEffectInfo[fireEffect.FireEffectType].lightSourceYOffset;
                    AddPointLight(fireEffect.EffectGameEntity, yOffset);
                }
            }

            _activatedSceneObjects.Add(sceneObject.ObjectInfo.Id);
        }

        public void DeactivateSceneObject(int id)
        {
            if (!_activatedSceneObjects.Contains(id)) return;
            _activatedSceneObjects.Remove(id);

            if (_isLightingEnabled &&
                SceneObjects[id].GetGameEntity().GetComponentInChildren<Light>() is {type: LightType.Point} pointLight)
            {
                _pointLights.Remove(pointLight);
                StripPointLightShadowsIfNecessary();
            }

            SceneObjects[id].Deactivate();
        }

        private void CreateActorObject(Actor actor, Color tintColor, Tilemap tileMap)
        {
            if (_actorEntities.ContainsKey(actor.Id)) return;

            IGameEntity actorGameEntity = ActorFactory.CreateActorGameEntity(_resourceProvider,
                actor,
                tileMap,
                isDropShadowEnabled: !_isLightingEnabled,
                tintColor,
                ActorMovementMaxYDifferential,
                ActorMovementMaxYDifferentialCrossLayer,
                ActorMovementMaxYDifferentialCrossPlatform,
                GetAllActiveActorBlockingTilePositions);

            actorGameEntity.SetParent(_parent, worldPositionStays: false);
            _actorEntities[actor.Id] = actorGameEntity;
        }

        private void ActivateActorObject(int id, bool isActive)
        {
            if (!_actorEntities.ContainsKey(id)) return;

            IGameEntity actorGameEntity = _actorEntities[id];
            var actorController = actorGameEntity.GetComponent<ActorController>();
            actorController.IsActive = isActive;
        }

        /// <summary>
        /// Get all tile positions blocked by the active actors in current scene.
        /// </summary>
        private HashSet<Vector2Int> GetAllActiveActorBlockingTilePositions(int layerIndex, int[] excludeActorIds)
        {
            var allActors = GetAllActorGameEntities();

            var actorTiles = new HashSet<Vector2Int>();
            foreach ((var id, IGameEntity actor) in allActors)
            {
                if (excludeActorIds.Contains(id)) continue;
                if (actor.GetComponent<ActorController>().IsActive)
                {
                    var actorMovementController = actor.GetComponent<ActorMovementController>();
                    if (actorMovementController.GetCurrentLayerIndex() != layerIndex) continue;
                    Vector2Int tilePosition = actorMovementController.GetTilePosition();
                    actorTiles.Add(tilePosition);
                }
            }

            var obstacles = new HashSet<Vector2Int>();
            foreach (Vector2Int actorTile in actorTiles)
            {
                if (!Tilemap.IsTilePositionInsideTileMap(actorTile, layerIndex)) continue;

                obstacles.Add(actorTile);

                // Mark 8 tiles right next to the actor tile as obstacles
                foreach (Direction direction in DirectionUtils.AllDirections)
                {
                    Vector2Int neighbourTile = actorTile + DirectionUtils.ToVector2Int(direction);
                    if (Tilemap.IsTilePositionInsideTileMap(neighbourTile, layerIndex))
                    {
                        obstacles.Add(neighbourTile);
                    }
                }
            }

            return obstacles;
        }

        /// <summary>
        /// Opaque water surface looks better in some scenes.
        /// This method is to override the default transparent
        /// water surface if necessary when lit shader is used.
        /// </summary>
        /// <returns></returns>
        private bool IsWaterSurfaceOpaque()
        {
            if (_materialFactory.ShaderType == MaterialShaderType.Lit)
            {
                #if PAL3
                if (ScnFile.SceneInfo.Is("q17", "q17"))
                {
                    return true;
                }
                #elif PAL3A
                if (ScnFile.SceneInfo.IsCity("m07") ||
                    ScnFile.SceneInfo.IsCity("m12"))
                {
                    return true;
                }
                #endif
            }

            // Default to transparent water surface
            return false;
        }

        public void Execute(SceneActivateObjectCommand command)
        {
            if (!SceneObjects.ContainsKey(command.ObjectId)) return;

            if (command.IsActive == 1)
            {
                ActivateSceneObject(command.ObjectId);
            }
            else
            {
                DeactivateSceneObject(command.ObjectId);
            }

            // Save the activation state since it is activated/de-activated by the script
            _sceneStateManager.Execute(new SceneSaveGlobalObjectActivationStateCommand(
                ScnFile.SceneInfo.CityName,
                ScnFile.SceneInfo.SceneName,
                command.ObjectId,
                command.IsActive == 1));
        }

        public void Execute(ActorActivateCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID) return;
            if (!_actorEntities.ContainsKey(command.ActorId)) return;
            ActivateActorObject(command.ActorId, command.IsActive == 1);
        }

        public void Execute(ActorLookAtActorCommand command)
        {
            if (!_actorEntities.ContainsKey(command.ActorId) ||
                !_actorEntities.ContainsKey(command.LookAtActorId)) return;

            ITransform actorTransform = _actorEntities[command.ActorId].Transform;
            ITransform lookAtActorTransform = _actorEntities[command.LookAtActorId].Transform;
            Vector3 lookAtActorPosition = lookAtActorTransform.Position;

            actorTransform.LookAt(new Vector3(
                lookAtActorPosition.x,
                actorTransform.Position.y,
                lookAtActorPosition.z));
        }

        public void Execute(SceneOpenDoorCommand command)
        {
            if (_activatedSceneObjects.Contains(command.ObjectId))
            {
                IGameEntity sceneObjectEntity = SceneObjects[command.ObjectId].GetGameEntity();
                if (sceneObjectEntity.GetComponent<CvdModelRenderer>() is { } cvdMeshRenderer)
                {
                    cvdMeshRenderer.StartOneTimeAnimation(true);
                }
            }
            else
            {
                EngineLogger.LogError($"Scene object not found or not activated yet: {command.ObjectId}");
            }
        }

        public void Execute(SceneMoveObjectCommand command)
        {
            if (_activatedSceneObjects.Contains(command.ObjectId))
            {
                SceneObject sceneObject = SceneObjects[command.ObjectId];

                GameBoxVector3 gameBoxPositionOffset = new GameBoxVector3(
                    command.GameBoxXOffset,
                    command.GameBoxYOffset,
                    command.GameBoxZOffset);

                GameBoxVector3 originalPosition = ScnFile.ObjectInfos.First(_ => _.Id == command.ObjectId).GameBoxPosition;

                // There are some objects that moved by scene scripts right after scene load which are persisted by
                // SceneStateManager. Next time when the scene is loaded, SceneStateManager will set these objects to
                // their final position. Thus we need to ignore these particular SceneMoveObject command issued by
                // scene script. We are doing this by checking if the object current position is the same as the
                // original position + SceneMoveObjectCommand offset.
                // Example: The climbable object in PAL3 scene m08 3.
                if (sceneObject.ObjectInfo.GameBoxPosition != originalPosition &&
                    GameBoxVector3.Distance(originalPosition + gameBoxPositionOffset, sceneObject.ObjectInfo.GameBoxPosition) < 0.01f)
                {
                    // Don't do anything since this object has already been moved by the SceneStateManager
                    EngineLogger.LogWarning($"Won't move object {command.ObjectId} since it has already been " +
                                   "moved by the SceneStateManager on scene load");
                    return;
                }

                IGameEntity sceneObjectEntity = sceneObject.GetGameEntity();
                Vector3 offset = gameBoxPositionOffset.ToUnityPosition();
                Vector3 toPosition = sceneObjectEntity.Transform.Position + offset;
                StartCoroutine(sceneObjectEntity.Transform.MoveAsync(toPosition, command.Duration));

                // Save the new position since it is moved by the script
                GameBoxVector3 toGameBoxPosition = toPosition.ToGameBoxPosition();
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new SceneSaveGlobalObjectPositionCommand(ScnFile.SceneInfo.CityName,
                        ScnFile.SceneInfo.SceneName,
                        command.ObjectId,
                        toGameBoxPosition.X,
                        toGameBoxPosition.Y,
                        toGameBoxPosition.Z));
            }
            else
            {
                EngineLogger.LogError($"Scene object not found or not activated yet: {command.ObjectId}");
            }
        }

        #if PAL3A
        public void Execute(SceneActivateObject2Command command)
        {
            if (!_sceneStateManager.TryGetSceneObjectStateOverride(ScnFile.SceneInfo.CityName,
                    command.SceneName, command.ObjectId, out SceneObjectStateOverride state) ||
                !state.IsActivated.HasValue)
            {
                _sceneStateManager.Execute(new SceneSaveGlobalObjectActivationStateCommand(
                    ScnFile.SceneInfo.CityName,
                    command.SceneName,
                    command.ObjectId,
                    command.IsActive == 1));
            }
        }

        public void Execute(SceneCloseDoorCommand command)
        {
            if (_activatedSceneObjects.Contains(command.ObjectId))
            {
                IGameEntity sceneObjectEntity = SceneObjects[command.ObjectId].GetGameEntity();
                if (sceneObjectEntity.GetComponent<CvdModelRenderer>() is { } cvdMeshRenderer)
                {
                    cvdMeshRenderer.SetCurrentTime(cvdMeshRenderer.GetDefaultAnimationDuration());
                    cvdMeshRenderer.StartOneTimeAnimation(true, timeScale: -1f);
                }
            }
            else
            {
                EngineLogger.LogError($"Scene object not found or not activated yet: {command.ObjectId}");
            }
        }

        public void Execute(SceneEnableFogCommand command)
        {
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(
                command.Red / 255f,
                command.Green / 255f,
                command.Blue / 255f,
                command.Alpha).ToUnityColor();
            RenderSettings.fogStartDistance = command.StartDistance.ToUnityDistance();
            RenderSettings.fogEndDistance = command.EndDistance.ToUnityDistance();
            RenderSettings.fog = true;
        }

        public void Execute(FengYaSongCommand command)
        {
            switch (command.ModelType)
            {
                case 3:
                {
                    // Hide all existing birds
                    foreach (FengYaSongActorId actorId in Enum.GetValues(typeof(FengYaSongActorId)))
                    {
                        CommandDispatcher<ICommand>.Instance.Dispatch(new ActorActivateCommand((int)actorId, 0));
                    }
                    break;
                }
                case >= 0 and <= 2:
                {
                    FengYaSongActorId activeBirdActorId = command.ModelType switch
                    {
                        0 => FengYaSongActorId.Feng,
                        1 => FengYaSongActorId.Ya,
                        2 => FengYaSongActorId.Song,
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    // Hide all other birds
                    foreach (FengYaSongActorId actorId in Enum.GetValues(typeof(FengYaSongActorId)))
                    {
                        if (actorId != activeBirdActorId)
                        {
                            CommandDispatcher<ICommand>.Instance.Dispatch(new ActorActivateCommand((int)actorId, 0));
                        }
                    }

                    IGameEntity leiYuanGeActorGameEntity = GetActorGameEntity((int)PlayerActorId.LeiYuanGe);
                    Vector3 leiYuanGePosition = leiYuanGeActorGameEntity.Transform.Position;
                    float leiYuanGeHeight = leiYuanGeActorGameEntity.GetComponent<ActorActionController>()
                        .GetActorHeight();

                    // Height adjustment based on bird action type
                    var yOffset = command.ActionType == 0 ? -0.23f : 0.23f;

                    IGameEntity birdActorGameEntity = GetActorGameEntity((int)activeBirdActorId);
                    birdActorGameEntity.Transform.Position = new Vector3(leiYuanGePosition.x,
                        leiYuanGePosition.y + leiYuanGeHeight + yOffset,
                        leiYuanGePosition.z);
                    birdActorGameEntity.Transform.Forward = leiYuanGeActorGameEntity.Transform.Forward;
                    birdActorGameEntity.GetComponent<ActorController>().IsActive = true;
                    birdActorGameEntity.GetComponent<ActorActionController>()
                        .PerformAction(command.ActionType == 0 ? ActorActionType.Stand : ActorActionType.Walk);
                    break;
                }
            }
        }
        #endif
    }
}