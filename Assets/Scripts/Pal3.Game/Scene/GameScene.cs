// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
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
    using Engine.Services;
    using Engine.Utilities;
    using Rendering.Toon;
    using Rendering.Material;
    using Rendering.Renderer;
    using SceneObjects;
    using Settings;
    using State;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;
    using Color = Core.Primitives.Color;

    public sealed class GameScene : SceneBase,
        ICommandExecutor<SceneActivateObjectCommand>,
        ICommandExecutor<SceneMoveObjectCommand>,
        ICommandExecutor<SceneOpenDoorCommand>,
        #if PAL3A
        ICommandExecutor<FengYaSongCommand>,
        ICommandExecutor<SceneCloseDoorCommand>,
        ICommandExecutor<SceneEnableFogCommand>,
        #endif
        ICommandExecutor<ActorActivateCommand>,
        ICommandExecutor<ActorLookAtActorCommand>
    {
        private const float SCENE_CVD_ANIMATION_DEFAULT_TIMESCALE = .2f;

        private IGameEntity _parent;
        private IGameEntity _mesh;

        private Light _mainLight;
        private readonly Dictionary<int, Light> _fireLights = new();
        private Light _shadowedFire;
        private GameSettings _lightingSettings;
        private UnityEngine.Camera _lightingCamera;
        private ToonCameraLighting _cameraLighting;
        private readonly Plane[] _fireFrustum = new Plane[6];
        private bool _isNight;
        private bool _enhancedFire;
        private float _nextFireUpdate;

        private readonly List<IGameEntity> _navMeshEntities = new ();

        private readonly HashSet<int> _activatedSceneObjects = new ();
        private readonly Dictionary<int, IGameEntity> _actorEntities = new ();

        private HashSet<int> _sceneObjectIdsToNotLoadFromSaveState;

        private GameResourceProvider _resourceProvider;
        private ISceneObjectFactory _sceneObjectFactory;
        private SceneStateManager _sceneStateManager;
        private bool _usesToonRendering;
        private IGameEntity _cameraEntity;
        private SkyBoxRenderer _skyBoxRenderer;
        private IMaterialManager _materialManager;

        protected override void OnEnableGameEntity()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        protected override void OnDisableGameEntity()
        {
            SetLightingActive(false);

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

            foreach (KeyValuePair<int, IGameEntity> actor in _actorEntities)
            {
                actor.Value.Destroy();
            }

            if (_skyBoxRenderer != null)
            {
                _skyBoxRenderer.Destroy();
            }
        }

        public void Init(GameResourceProvider resourceProvider,
            ISceneObjectFactory sceneObjectFactory,
            SceneStateManager sceneStateManager,
            bool usesToonRendering,
            IGameEntity cameraEntity,
            HashSet<int> sceneObjectIdsToNotLoadFromSaveState)
        {
            _resourceProvider = resourceProvider;
            _sceneObjectFactory = sceneObjectFactory;
            _sceneStateManager = sceneStateManager;
            _usesToonRendering = usesToonRendering;
            _cameraEntity = cameraEntity;
            _sceneObjectIdsToNotLoadFromSaveState = sceneObjectIdsToNotLoadFromSaveState;
            _materialManager = resourceProvider.GetMaterialManager();
            if (usesToonRendering)
            {
                _lightingSettings = ServiceLocator.Instance.Get<GameSettings>();
                _lightingCamera = ((GameObject)cameraEntity.NativeObject).GetComponent<UnityEngine.Camera>();
                _cameraLighting = _lightingCamera.GetComponent<ToonCameraLighting>();
            }
        }

        public void Load(ScnFile scnFile, IGameEntity parent)
        {
            _parent = parent;

            Stopwatch timer = Stopwatch.StartNew();

            base.Init(_resourceProvider,
                scnFile,
                _sceneObjectFactory,
                _sceneStateManager,
                _sceneObjectIdsToNotLoadFromSaveState);

            EngineLogger.Log($"Scene data initialized in {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            _isNight = IsNightScene();
            _enhancedFire = _isNight && _lightingSettings?.ToonNightFireBoost == true;
            Color actorTintColor = Color.White;
            if (_isNight)
            {
                actorTintColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            timer.Restart();
            RenderMesh();
            EngineLogger.Log($"Mesh initialized in {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            RenderSkyBox();
            SetupNavMesh();
            if (_usesToonRendering)
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
            return SceneObjects.GetValueOrDefault(id);
        }

        public HashSet<int> GetAllActivatedSceneObjects()
        {
            return _activatedSceneObjects;
        }

        public Dictionary<int, SceneObject> GetAllSceneObjects()
        {
            return SceneObjects;
        }

        public GameActor GetActor(int id)
        {
            return Actors.GetValueOrDefault(id);
        }

        public Dictionary<int, GameActor> GetAllActors()
        {
            return Actors;
        }

        public IGameEntity GetActorGameEntity(int id)
        {
            return _actorEntities.GetValueOrDefault(id);
        }

        public Dictionary<int, IGameEntity> GetAllActorGameEntities()
        {
            return _actorEntities;
        }

        public bool IsPositionInsideJumpableArea(int layerIndex, Vector2Int tilePosition)
        {
            foreach (int objectId in _activatedSceneObjects)
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

            PolyModelRenderer polyMeshRenderer = _mesh.AddComponent<PolyModelRenderer>();
            polyMeshRenderer.Render(ScenePolyMesh.PolFile,
                ScenePolyMesh.TextureProvider,
                _materialManager,
                isStaticObject: true,
                forceOpaqueWater: _usesToonRendering && HasOpaqueToonWater(ScnFile.SceneInfo));

            if (SceneCvdMesh != null)
            {
                CvdModelRenderer cvdMeshRenderer = _mesh.AddComponent<CvdModelRenderer>();
                cvdMeshRenderer.Init(SceneCvdMesh.Value.CvdFile,
                    SceneCvdMesh.Value.TextureProvider,
                    _materialManager);
                cvdMeshRenderer.LoopAnimation(SCENE_CVD_ANIMATION_DEFAULT_TIMESCALE);
            }
        }

        private static bool HasOpaqueToonWater(ScnSceneInfo sceneInfo)
        {
            #if PAL3
            return sceneInfo.Is("q17", "q17");
            #elif PAL3A
            return sceneInfo.IsCity("m07") || sceneInfo.IsCity("m12");
            #else
            return false;
            #endif
        }

        private void RenderSkyBox()
        {
            if (ScnFile.SceneInfo.SkyBox == 0) return;
            _skyBoxRenderer = _cameraEntity.AddComponent<SkyBoxRenderer>();
            ITexture2D[] skyBoxTextures = _resourceProvider.GetSkyBoxTextures((int) ScnFile.SceneInfo.SkyBox);
            IMaterial skyboxMaterial = _materialManager.CreateSkyboxMaterial(
                skyBoxTextures[0],
                skyBoxTextures[1],
                skyBoxTextures[2],
                skyBoxTextures[3],
                skyBoxTextures[4],
                skyBoxTextures[5]);
            _skyBoxRenderer.Render(_cameraEntity, skyboxMaterial);
        }

        private void SetupNavMesh()
        {
            if (UnityEngineUtility.IsHandheldDevice())
            {
                // We only enable joystick/gamepad control for the gameplay on mobile
                // devices so there is no need for setting up nav mesh.
                return;
            }

            for (int i = 0; i < NavFile.Layers.Length; i++)
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
            bool isNight = IsNightScene();
            bool isIndoors = ScnFile.SceneInfo.SceneType == SceneType.InDoor;
            Quaternion rotation = Quaternion.Euler(isIndoors ? 112f : 58f, isIndoors ? -20f : -30f, 0f);
            Color color = isNight ? new Color(0.68f, 0.77f, 1f) : new Color(1f, 0.94f, 0.86f);
            float intensity = isNight ? 0.7f : isIndoors ? 0.8f : 1f;

            string city = ScnFile.SceneInfo.CityName;
            var key = (city.ToLowerInvariant(), ScnFile.SceneInfo.SceneName.ToLowerInvariant());
            if (LightingConstants.MainLightColorInfoGlobal.TryGetValue(city, out Color cityColor))
                color = cityColor;
            if (LightingConstants.MainLightColorInfo.TryGetValue(key, out Color sceneColor))
                color = sceneColor;
            if (LightingConstants.MainLightRotationInfo.TryGetValue(key, out Quaternion sceneRotation))
                rotation = sceneRotation;

            _mainLight = ToonLighting.CreateKey(transform, rotation, color.ToUnityColor(), intensity);
            RenderSettings.sun = _mainLight;
            _cameraLighting?.SetNightScene(_isNight && _fireLights.Count > 0 ? this : null);
        }

        internal void SetLightingActive(bool active)
        {
            if (!active)
            {
                _cameraLighting?.ClearNightScene(this);
                SetFireShadow(null);
            }
            if (_mainLight == null) return;

            _mainLight.enabled = active;
            if (active)
            {
                RenderSettings.sun = _mainLight;
                _cameraLighting?.SetNightScene(_isNight && _fireLights.Count > 0 ? this : null);
            }
            else
            {
                if (RenderSettings.sun == _mainLight) RenderSettings.sun = null;
            }

            foreach (Light light in _fireLights.Values)
            {
                if (light != null) light.enabled = active;
            }

            _nextFireUpdate = 0f;
        }

        private void LateUpdate()
        {
            if (!_isNight || _fireLights.Count == 0 || _mainLight == null || !_mainLight.enabled) return;
            float now = Time.unscaledTime;
            if (now < _nextFireUpdate) return;
            _nextFireUpdate = now + 0.2f;

            bool enhanced = _lightingSettings.ToonNightFireBoost;
            if (enhanced != _enhancedFire)
            {
                _enhancedFire = enhanced;
                foreach (Light light in _fireLights.Values)
                {
                    if (light != null) ToonLighting.ConfigureFireFill(light, enhanced);
                }
            }

            SetFireShadow(_lightingSettings.ToonNightFireShadows ? SelectFireShadow() : null);
        }

        private Light SelectFireShadow()
        {
            GeometryUtility.CalculateFrustumPlanes(_lightingCamera, _fireFrustum);
            Transform cameraTransform = _lightingCamera.transform;
            Vector3 cameraPosition = cameraTransform.position;
            Vector3 forward = cameraTransform.forward;
            float shadowDistance = UniversalRenderPipeline.asset.shadowDistance;
            Light candidate = null;
            float bestScore = float.PositiveInfinity;
            foreach (Light light in _fireLights.Values)
            {
                if (light == null || !light.isActiveAndEnabled) continue;

                Vector3 position = light.transform.position;
                if (!IsFireInView(position, light.range) ||
                    (position - cameraPosition).sqrMagnitude > Mathf.Pow(shadowDistance + light.range, 2f))
                    continue;

                // Prefer coverage near the middle of the shot, including fires just beyond the frame.
                Vector3 toLight = position - cameraPosition;
                float depth = Vector3.Dot(toLight, forward);
                float lateral = (toLight - depth * forward).sqrMagnitude;
                float score = lateral / Mathf.Max(depth * depth, 1f) + Mathf.Abs(depth) * 0.005f;
                if (light == _shadowedFire) score *= 0.8f;
                if (score >= bestScore) continue;

                bestScore = score;
                candidate = light;
            }
            return candidate;
        }

        private bool IsFireInView(Vector3 position, float range)
        {
            foreach (Plane plane in _fireFrustum)
            {
                if (plane.GetDistanceToPoint(position) < -range) return false;
            }
            return true;
        }

        private void SetFireShadow(Light light)
        {
            if (_shadowedFire == light) return;
            if (_shadowedFire != null) _shadowedFire.shadows = LightShadows.None;
            _shadowedFire = light;
            if (light != null) light.shadows = LightShadows.Soft;
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
            foreach (GameActor actorObject in Actors.Values)
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

            if (_usesToonRendering)
            {
                if (sceneObject.GraphicsEffectType == GraphicsEffectType.Fire &&
                    sceneObjectGameEntity.GetComponent<FireEffect>() is { } fireEffect &&
                    !fireEffect.EffectGameEntity.IsNullOrDisposed())
                {
                    var yOffset = EffectConstants.FireEffectInfo[fireEffect.FireEffectType].lightSourceYOffset;
                    var parent = (Transform)fireEffect.EffectGameEntity.Transform.NativeObject;
                    var light = ToonLighting.CreateFireFill(parent, yOffset, _enhancedFire);
                    light.enabled = _mainLight.enabled;
                    _fireLights.Add(id, light);
                    if (_isNight && light.enabled) _cameraLighting?.SetNightScene(this);
                    _nextFireUpdate = 0f;
                }
            }

            _activatedSceneObjects.Add(sceneObject.ObjectInfo.Id);
        }

        public void DeactivateSceneObject(int id)
        {
            if (!_activatedSceneObjects.Contains(id)) return;
            _activatedSceneObjects.Remove(id);

            if (_fireLights.Remove(id, out Light light))
            {
                if (light == _shadowedFire) SetFireShadow(null);
                if (light != null) light.enabled = false;
                if (_fireLights.Count == 0) _cameraLighting?.ClearNightScene(this);
                _nextFireUpdate = 0f;
            }

            SceneObjects[id].Deactivate();
        }

        private void CreateActorObject(GameActor actor, Color tintColor, Tilemap tileMap)
        {
            if (_actorEntities.ContainsKey(actor.Id)) return;

            IGameEntity actorGameEntity = ActorFactory.CreateActorGameEntity(_resourceProvider,
                actor,
                tileMap,
                isDropShadowEnabled: !_usesToonRendering,
                tintColor,
                ActorMovementMaxYDifferential,
                ActorMovementMaxYDifferentialCrossLayer,
                ActorMovementMaxYDifferentialCrossPlatform,
                GetAllActiveActorBlockingTilePositions);

            actorGameEntity.SetParent(_parent, worldPositionStays: false);
            _actorEntities[actor.Id] = actorGameEntity;
        }

        /// <summary>
        /// Get all tile positions blocked by the active actors in current scene.
        /// </summary>
        private HashSet<Vector2Int> GetAllActiveActorBlockingTilePositions(int layerIndex, int[] excludeActorIds)
        {
            Dictionary<int, IGameEntity> allActors = GetAllActorGameEntities();

            HashSet<Vector2Int> actorTiles = new HashSet<Vector2Int>();
            foreach ((int id, IGameEntity actor) in allActors)
            {
                if (excludeActorIds.Contains(id)) continue;
                if (actor.GetComponent<ActorController>().IsActive)
                {
                    ActorMovementController actorMovementController = actor.GetComponent<ActorMovementController>();
                    if (actorMovementController.GetCurrentLayerIndex() != layerIndex) continue;
                    Vector2Int tilePosition = actorMovementController.GetTilePosition();
                    actorTiles.Add(tilePosition);
                }
            }

            HashSet<Vector2Int> obstacles = new();
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
            if (!_actorEntities.TryGetValue(command.ActorId, out IGameEntity actorGameEntity)) return;
            actorGameEntity.GetComponent<ActorController>().IsActive = command.IsActive == 1;
        }

        public void Execute(ActorLookAtActorCommand command)
        {
            if (!_actorEntities.ContainsKey(command.ActorId) ||
                !_actorEntities.ContainsKey(command.LookAtActorId)) return;

            if (command.ActorId == command.LookAtActorId)
            {
                EngineLogger.LogWarning($"Actor {command.ActorId} cannot look at itself");
                return;
            }

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

                GameBoxVector3 gameBoxPositionOffset = new(
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
                Pal3.Instance.Execute(
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
                        Pal3.Instance.Execute(new ActorActivateCommand((int)actorId, 0));
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
                            Pal3.Instance.Execute(new ActorActivateCommand((int)actorId, 0));
                        }
                    }

                    IGameEntity leiYuanGeActorGameEntity = GetActorGameEntity((int)PlayerActorId.LeiYuanGe);
                    Vector3 leiYuanGePosition = leiYuanGeActorGameEntity.Transform.Position;
                    float leiYuanGeHeight = leiYuanGeActorGameEntity.GetComponent<ActorActionController>()
                        .GetActorHeight();

                    // Height adjustment based on bird action type
                    float yOffset = command.ActionType == 0 ? -0.23f : 0.23f;

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