// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Actor;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Renderer;
    using Core.Utils;
    using Data;
    using Effect;
    using MetaData;
    using Renderer;
    using SceneObjects;
    using State;
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    public class Scene : SceneBase,
        ICommandExecutor<SceneActivateObjectCommand>,
        ICommandExecutor<SceneMoveObjectCommand>,
        ICommandExecutor<SceneOpenDoorCommand>,
        #if PAL3A
        ICommandExecutor<SceneActivateObject2Command>,
        ICommandExecutor<FengYaSongCommand>,
        ICommandExecutor<SceneCloseDoorCommand>,
        #endif
        ICommandExecutor<ActorActivateCommand>,
        ICommandExecutor<ActorLookAtActorCommand>
    {
        private const float SCENE_CVD_ANIMATION_DEFAULT_TIMESCALE = .2f;
        private const int MAX_NUM_OF_POINT_LIGHTS_WITH_SHADOWS = 3;

        private static int _lightCullingMask;

        private GameObject _parent;
        private GameObject _mesh;

        private Light _mainLight;
        private readonly List<Light> _pointLights = new();

        private readonly List<GameObject> _navMeshLayers = new ();
        private readonly Dictionary<int, MeshCollider> _meshColliders = new ();

        private readonly HashSet<int> _activatedSceneObjects = new ();
        private readonly Dictionary<int, GameObject> _actorObjects = new ();

        private HashSet<int> _sceneObjectIdsToNotLoadFromSaveState;

        private GameResourceProvider _resourceProvider;
        private SceneStateManager _sceneStateManager;
        private bool _isLightingEnabled;
        private Camera _mainCamera;
        private SkyBoxRenderer _skyBoxRenderer;
        private IMaterialFactory _materialFactory;

        private Tilemap _tilemap;

        public void Init(GameResourceProvider resourceProvider,
            SceneStateManager sceneStateManager,
            bool isLightingEnabled,
            Camera mainCamera,
            HashSet<int> sceneObjectIdsToNotLoadFromSaveState)
        {
            _resourceProvider = resourceProvider;
            _sceneStateManager = sceneStateManager;
            _isLightingEnabled = isLightingEnabled;
            _mainCamera = mainCamera;
            _sceneObjectIdsToNotLoadFromSaveState = sceneObjectIdsToNotLoadFromSaveState;
            _lightCullingMask = (1 << LayerMask.NameToLayer("Default")) |
                                (1 << LayerMask.NameToLayer("VFX"));
            _materialFactory = resourceProvider.GetMaterialFactory();
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

            Destroy(_mesh);

            foreach (MeshCollider meshCollider in _meshColliders.Values)
            {
                Destroy(meshCollider.sharedMesh);
                Destroy(meshCollider);
            }

            foreach (GameObject navMeshLayer in _navMeshLayers)
            {
                Destroy(navMeshLayer);
            }

            foreach (int sceneObjectId in _activatedSceneObjects)
            {
                SceneObjects[sceneObjectId].Deactivate();
            }

            foreach (var actor in _actorObjects)
            {
                Destroy(actor.Value);
            }

            if (_skyBoxRenderer != null)
            {
                Destroy(_skyBoxRenderer);
            }
        }

        public void Load(ScnFile scnFile, GameObject parent)
        {
            _parent = parent;

            var timer = new Stopwatch();
            timer.Start();

            base.Init(_resourceProvider,
                scnFile,
                _sceneStateManager,
                _sceneObjectIdsToNotLoadFromSaveState);
            //Debug.LogError($"InitTotal: {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            _tilemap = new Tilemap(NavFile);
            Color actorTintColor = Color.white;
            if (IsNightScene())
            {
                actorTintColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            timer.Restart();
            RenderMesh();
            //Debug.LogError($"RenderMesh: {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            RenderSkyBox();
            SetupNavMesh();
            if (_isLightingEnabled)
            {
                SetupEnvironmentLighting();
            }
            //Debug.LogError($"SkyBox+NavMesh+Lights: {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            InitActorObjects(actorTintColor, _tilemap);
            //Debug.LogError($"CreateActors: {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            InitSceneObjects();
            //Debug.LogError($"ActivateSceneObjects: {timer.ElapsedMilliseconds} ms");
            timer.Stop();
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
            return SceneObjects.ContainsKey(id) ? SceneObjects[id] : null;
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
            return Actors.ContainsKey(id) ? Actors[id] : null;
        }

        public Dictionary<int, Actor> GetAllActors()
        {
            return Actors;
        }

        public GameObject GetActorGameObject(int id)
        {
            return _actorObjects.ContainsKey(id) ? _actorObjects[id] : null;
        }

        public Dictionary<int, GameObject> GetAllActorGameObjects()
        {
            return _actorObjects;
        }

        public Dictionary<int, MeshCollider> GetMeshColliders()
        {
            return _meshColliders;
        }

        public bool IsPositionInsideJumpableArea(int layerIndex, Vector2Int tilePosition)
        {
            foreach (var objectId in _activatedSceneObjects)
            {
                SceneObject sceneObject = SceneObjects[objectId];
                if (sceneObject.ObjectInfo.Type == ScnSceneObjectType.JumpableArea &&
                    layerIndex == sceneObject.ObjectInfo.LayerIndex &&
                    GameBoxInterpreter.IsPointInsideRect(sceneObject.ObjectInfo.TileMapTriggerRect,
                        tilePosition))
                {
                    return true;
                }
            }

            return false;
        }

        private void RenderMesh()
        {
            // Render mesh
            _mesh = new GameObject($"Mesh_{ScnFile.SceneInfo.CityName}_{ScnFile.SceneInfo.SceneName}");
            var polyMeshRenderer = _mesh.AddComponent<PolyModelRenderer>();
            _mesh.transform.SetParent(_parent.transform, false);

            polyMeshRenderer.Render(ScenePolyMesh.PolFile,
                ScenePolyMesh.TextureProvider,
                _materialFactory,
                Color.white,
                IsWaterSurfaceOpaque());

            if (SceneCvdMesh != null)
            {
                var cvdMeshRenderer = _mesh.AddComponent<CvdModelRenderer>();
                cvdMeshRenderer.Init(SceneCvdMesh.Value.CvdFile,
                    _materialFactory,
                    SceneCvdMesh.Value.TextureProvider);
                cvdMeshRenderer.LoopAnimation(SCENE_CVD_ANIMATION_DEFAULT_TIMESCALE);
            }
        }

        private void RenderSkyBox()
        {
            if (ScnFile.SceneInfo.SkyBox == 0) return;
            _skyBoxRenderer = _mainCamera.gameObject.AddComponent<SkyBoxRenderer>();
            Texture2D[] skyBoxTextures = _resourceProvider.GetSkyBoxTextures((int) ScnFile.SceneInfo.SkyBox);
            _skyBoxRenderer.Render(_mainCamera,
                skyBoxTextures[0],
                skyBoxTextures[1],
                skyBoxTextures[2],
                skyBoxTextures[3],
                skyBoxTextures[4],
                skyBoxTextures[5]);
        }

        private void SetupNavMesh()
        {
            if (Utility.IsHandheldDevice())
            {
                // We only enable joystick/gamepad control for the gameplay on mobile
                // devices so there is no need for setting up nav mesh.
                return;
            }

            int raycastOnlyLayer = LayerMask.NameToLayer("RaycastOnly");

            for (var i = 0; i < NavFile.FaceLayers.Length; i++)
            {
                var navMesh = new GameObject($"NavMesh_Layer_{i}")
                {
                    layer = raycastOnlyLayer
                };
                navMesh.transform.SetParent(_parent.transform, false);

                var meshCollider = navMesh.AddComponent<MeshCollider>();
                meshCollider.convex = false;
                meshCollider.sharedMesh = new Mesh()
                {
                    vertices = NavFile.FaceLayers[i].Vertices,
                    triangles = NavFile.FaceLayers[i].Triangles,
                };

                _meshColliders[i] = meshCollider;
                _navMeshLayers.Add(navMesh);
            }
        }

        private void SetupEnvironmentLighting()
        {
            Vector3 mainLightPosition = new Vector3(0, 20f, 0);
            Quaternion mainLightRotation = ScnFile.SceneInfo.SceneType == ScnSceneType.InDoor ?
                    Quaternion.Euler(120f, -20f, 0f) :
                    Quaternion.Euler(70f, -30f, 0f);

            if (ScnFile.SceneInfo.SceneType == ScnSceneType.InDoor && IsNightScene())
            {
                mainLightRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            // Most in-door scenes have a single spot light source where we can find in the LGT file,
            // which can be used as the main light source for the scene.
            // if (ScnFile.SceneInfo.SceneType == ScnSceneType.InDoor &&
            //     LgtFile.LightNodes.FirstOrDefault(_ => _.LightType == GameBoxLightType.Spot) is var mainLight)
            // {
            //     float w = Mathf.Sqrt(1.0f + mainLight.WorldMatrix.m00 + mainLight.WorldMatrix.m11 + mainLight.WorldMatrix.m22) / 2.0f;
            //     mainLightRotation = GameBoxInterpreter.LgtQuaternionToUnityQuaternion(new GameBoxQuaternion()
            //     {
            //         X = (mainLight.WorldMatrix.m21 - mainLight.WorldMatrix.m12) / (4.0f * w),
            //         Y = (mainLight.WorldMatrix.m02 - mainLight.WorldMatrix.m20) / (4.0f * w),
            //         Z = (mainLight.WorldMatrix.m10 - mainLight.WorldMatrix.m01) / (4.0f * w),
            //         W = w,
            //     });
            // }

            var mainLightGo = new GameObject($"LightSource_Main");
            mainLightGo.transform.SetParent(_parent.transform, false);
            mainLightGo.transform.position = mainLightPosition;
            mainLightGo.transform.rotation = mainLightRotation;

            _mainLight = mainLightGo.AddComponent<Light>();
            _mainLight.type = LightType.Directional;
            _mainLight.range = 500f;
            _mainLight.shadows = LightShadows.Soft;
            _mainLight.cullingMask = _lightCullingMask;
            RenderSettings.sun = _mainLight;

            #if PAL3
            _mainLight.color = IsNightScene() ?
                new Color(60f / 255f, 80f / 255f, 170f / 255f) :
                new Color(220f / 255f, 210f / 255f, 200f / 255f);
            _mainLight.intensity = (IsNightScene() || ScnFile.SceneInfo.SceneType == ScnSceneType.InDoor) ? 0.75f : 1f;
            #elif PAL3A
            _mainLight.color = IsNightScene() ?
                new Color(60f / 255f, 80f / 255f, 170f / 255f) :
                new Color(200f / 255f, 200f / 255f, 200f / 255f);
            _mainLight.intensity = (IsNightScene() || ScnFile.SceneInfo.SceneType == ScnSceneType.InDoor) ? 0.65f : 1f;
            #endif

            // Ambient light
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.ambientLight = IsNightScene() ?
                new Color( 90f/ 255f, 100f / 255f, 130f / 255f) :
                new Color(200f / 255f, 200f / 255f, 180f / 255f);

            // Apply lighting override
            var key = (ScnFile.SceneInfo.CityName.ToLower(), ScnFile.SceneInfo.SceneName.ToLower());
            if (LightingConstants.MainLightColorInfoGlobal.ContainsKey(ScnFile.SceneInfo.CityName))
            {
                _mainLight.color = LightingConstants.MainLightColorInfoGlobal[ScnFile.SceneInfo.CityName];
            }
            if (LightingConstants.MainLightColorInfo.ContainsKey(key))
            {
                _mainLight.color = LightingConstants.MainLightColorInfo[key];
            }
            if (LightingConstants.MainLightRotationInfo.ContainsKey(key))
            {
                _mainLight.transform.rotation = LightingConstants.MainLightRotationInfo[key];
            }
        }

        private void AddPointLight(Transform parent, float yOffset)
        {
            // Add a point light to the fire fx
            var lightSource = new GameObject($"LightSource_Point");
            lightSource.transform.SetParent(parent, false);
            lightSource.transform.localPosition = new Vector3(0f, yOffset, 0f);

            var lightComponent = lightSource.AddComponent<Light>();
            lightComponent.color = new Color(220f / 255f, 145f / 255f, 105f / 255f);
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

            Color tintColor = Color.white;
            if (IsNightScene())
            {
                tintColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            }

            GameObject sceneObjectGameObject = sceneObject.Activate(_resourceProvider, tintColor);
            sceneObjectGameObject.transform.SetParent(_parent.transform, false);

            if (_isLightingEnabled)
            {
                if (sceneObject.GraphicsEffect == GraphicsEffect.Fire &&
                    sceneObjectGameObject.GetComponent<FireEffect>() is { } fireEffect &&
                    fireEffect.EffectGameObject != null)
                {
                    var yOffset = EffectConstants.FireEffectInfo[fireEffect.FireEffectType].lightSourceYOffset;
                    AddPointLight(fireEffect.EffectGameObject.transform, yOffset);
                }
            }

            _activatedSceneObjects.Add(sceneObject.ObjectInfo.Id);
        }

        public void DeactivateSceneObject(int id)
        {
            if (!_activatedSceneObjects.Contains(id)) return;
            _activatedSceneObjects.Remove(id);

            if (_isLightingEnabled &&
                SceneObjects[id].GetGameObject().GetComponentInChildren<Light>() is {type: LightType.Point} pointLight)
            {
                _pointLights.Remove(pointLight);
                StripPointLightShadowsIfNecessary();
            }

            SceneObjects[id].Deactivate();
        }

        private void CreateActorObject(Actor actor, Color tintColor, Tilemap tileMap)
        {
            if (_actorObjects.ContainsKey(actor.Info.Id)) return;
            GameObject actorGameObject = ActorFactory.CreateActorGameObject(_resourceProvider,
                actor,
                tileMap,
                isDropShadowEnabled: !_isLightingEnabled,
                tintColor,
                GetAllActiveActorBlockingTilePositions);
            actorGameObject.transform.SetParent(_parent.transform, false);
            _actorObjects[actor.Info.Id] = actorGameObject;
        }

        private void ActivateActorObject(int id, bool isActive)
        {
            if (!_actorObjects.ContainsKey(id)) return;

            GameObject actorGameObject = _actorObjects[id];
            var actorController = actorGameObject.GetComponent<ActorController>();
            actorController.IsActive = isActive;
        }

        /// <summary>
        /// Get all tile positions blocked by the active actors in current scene.
        /// </summary>
        private HashSet<Vector2Int> GetAllActiveActorBlockingTilePositions(int layerIndex, int[] excludeActorIds)
        {
            var allActors = GetAllActorGameObjects();

            var actorTiles = new HashSet<Vector2Int>();
            foreach ((var id, GameObject actor) in allActors)
            {
                if (excludeActorIds.Contains(id)) continue;
                if (actor.GetComponent<ActorController>().IsActive)
                {
                    var actorMovementController = actor.GetComponent<ActorMovementController>();
                    if (actorMovementController.GetCurrentLayerIndex() != layerIndex) continue;
                    Vector2Int tilePosition = _tilemap.GetTilePosition(actorMovementController.GetWorldPosition(), layerIndex);
                    actorTiles.Add(tilePosition);
                }
            }

            var obstacles = new HashSet<Vector2Int>();
            foreach (Vector2Int actorTile in actorTiles)
            {
                obstacles.Add(actorTile);

                // Mark 8 tiles right next to the actor tile as obstacles
                foreach (Direction direction in DirectionUtils.AllDirections)
                {
                    Vector2Int neighbourTile = actorTile + DirectionUtils.ToVector2Int(direction);
                    if (_tilemap.IsTilePositionInsideTileMap(neighbourTile, layerIndex))
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
            if (!_actorObjects.ContainsKey(command.ActorId)) return;
            ActivateActorObject(command.ActorId, command.IsActive == 1);
        }

        public void Execute(ActorLookAtActorCommand command)
        {
            if (!_actorObjects.ContainsKey(command.ActorId) ||
                !_actorObjects.ContainsKey(command.LookAtActorId)) return;

            Transform actorTransform = _actorObjects[command.ActorId].transform;
            Transform lookAtActorTransform = _actorObjects[command.LookAtActorId].transform;
            Vector3 lookAtActorPosition = lookAtActorTransform.position;

            actorTransform.LookAt(new Vector3(
                lookAtActorPosition.x,
                actorTransform.position.y,
                lookAtActorPosition.z));
        }

        public void Execute(SceneOpenDoorCommand command)
        {
            if (_activatedSceneObjects.Contains(command.ObjectId))
            {
                GameObject sceneObjectGo = SceneObjects[command.ObjectId].GetGameObject();
                if (sceneObjectGo.GetComponent<CvdModelRenderer>() is { } cvdMeshRenderer)
                {
                    cvdMeshRenderer.StartOneTimeAnimation(true);
                }
            }
            else
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.ObjectId}.");
            }
        }

        public void Execute(SceneMoveObjectCommand command)
        {
            if (_activatedSceneObjects.Contains(command.ObjectId))
            {
                SceneObject sceneObject = SceneObjects[command.ObjectId];

                var gameBoxPositionOffset = new Vector3(
                    command.GameBoxXOffset,
                    command.GameBoxYOffset,
                    command.GameBoxZOffset);

                Vector3 originalPosition = ScnFile.ObjectInfos.First(_ => _.Id == command.ObjectId).GameBoxPosition;

                // There are some objects that moved by scene scripts right after scene load which are persisted by
                // SceneStateManager. Next time when the scene is loaded, SceneStateManager will set these objects to
                // their final position. Thus we need to ignore these particular SceneMoveObject command issued by
                // scene script. We are doing this by checking if the object current position is the same as the
                // original position + SceneMoveObjectCommand offset.
                // Example: The climbable object in PAL3 scene m08 3.
                if (sceneObject.ObjectInfo.GameBoxPosition != originalPosition &&
                    Vector3.Distance(originalPosition + gameBoxPositionOffset, sceneObject.ObjectInfo.GameBoxPosition) < 0.01f)
                {
                    // Don't do anything since this object has already been moved by the SceneStateManager
                    Debug.LogWarning($"Won't move object {command.ObjectId} since it has already been " +
                                   $"moved by the SceneStateManager on scene load.");
                    return;
                }

                GameObject sceneObjectGo = sceneObject.GetGameObject();
                Vector3 offset = GameBoxInterpreter.ToUnityPosition(gameBoxPositionOffset);
                Vector3 toPosition = sceneObjectGo.transform.position + offset;
                StartCoroutine(AnimationHelper.MoveTransformAsync(sceneObjectGo.transform, toPosition, command.Duration));

                // Save the new position since it is moved by the script
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new SceneSaveGlobalObjectPositionCommand(ScnFile.SceneInfo.CityName,
                        ScnFile.SceneInfo.SceneName,
                        command.ObjectId,
                        GameBoxInterpreter.ToGameBoxPosition(toPosition)));
            }
            else
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.ObjectId}.");
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
                GameObject sceneObjectGo = SceneObjects[command.ObjectId].GetGameObject();
                if (sceneObjectGo.GetComponent<CvdModelRenderer>() is { } cvdMeshRenderer)
                {
                    cvdMeshRenderer.SetCurrentTime(cvdMeshRenderer.GetDefaultAnimationDuration());
                    cvdMeshRenderer.StartOneTimeAnimation(true, timeScale: -1f);
                }
            }
            else
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.ObjectId}.");
            }
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

                    GameObject leiYuanGeActorGameObject = GetActorGameObject((int)PlayerActorId.LeiYuanGe);
                    Vector3 leiYuanGePosition = leiYuanGeActorGameObject.transform.position;
                    float leiYuanGeHeight = leiYuanGeActorGameObject.GetComponent<ActorActionController>()
                        .GetActorHeight();

                    // Height adjustment based on bird action type
                    var yOffset = command.ActionType == 0 ? -0.23f : 0.23f;

                    GameObject birdActorGameObject = GetActorGameObject((int)activeBirdActorId);
                    birdActorGameObject.transform.position = new Vector3(leiYuanGePosition.x,
                        leiYuanGePosition.y + leiYuanGeHeight + yOffset,
                        leiYuanGePosition.z);
                    birdActorGameObject.transform.forward = leiYuanGeActorGameObject.transform.forward;
                    birdActorGameObject.GetComponent<ActorController>().IsActive = true;
                    birdActorGameObject.GetComponent<ActorActionController>()
                        .PerformAction(command.ActionType == 0 ? ActorActionType.Stand : ActorActionType.Walk);
                    break;
                }
            }
        }
        #endif
    }
}