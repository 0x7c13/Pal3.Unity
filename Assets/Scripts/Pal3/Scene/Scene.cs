// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
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
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    public class Scene : SceneBase,
        ICommandExecutor<SceneActivateObjectCommand>,
        ICommandExecutor<PlayerInteractWithObjectCommand>,
        ICommandExecutor<SceneMoveObjectCommand>,
        ICommandExecutor<SceneOpenDoorCommand>,
        #if PAL3A
        ICommandExecutor<FengYaSongCommand>,
        ICommandExecutor<SceneCloseDoorCommand>,
        #endif
        ICommandExecutor<ActorActivateCommand>,
        ICommandExecutor<ActorLookAtActorCommand>
    {
        private const float SCENE_CVD_ANIMATION_DEFAULT_TIMESCALE = .2f;
        private const int MAX_NUM_OF_POINT_LIGHTS_WITH_SHADOWS = 3;

        private static int _lightCullingMask;
        
        private Camera _mainCamera;
        private SkyBoxRenderer _skyBoxRenderer;

        private GameObject _parent;
        private GameObject _mesh;
        
        private Light _mainLight;
        private readonly List<Light> _pointLights = new();
        
        private readonly List<GameObject> _navMeshLayers = new ();
        private readonly Dictionary<int, MeshCollider> _meshColliders = new ();

        private readonly Dictionary<byte, GameObject> _activatedSceneObjects = new ();
        private readonly Dictionary<byte, GameObject> _actorObjects = new ();

        private GameResourceProvider _resourceProvider;
        private Tilemap _tilemap;

        public void Init(GameResourceProvider resourceProvider, Camera mainCamera)
        {
            _resourceProvider = resourceProvider;
            _mainCamera = mainCamera;
            _lightCullingMask = (1 << LayerMask.NameToLayer("Default")) |
                                (1 << LayerMask.NameToLayer("VFX"));
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

            foreach (GameObject sceneObject in _activatedSceneObjects.Values)
            {
                Destroy(sceneObject);
            }

            foreach (var actor in _actorObjects)
            {
                Destroy(actor.Value);
            }

            if (_skyBoxRenderer != null) Destroy(_skyBoxRenderer);
        }

        public void Load(ScnFile scnFile, GameObject parent)
        {
            _parent = parent;

            var timer = new Stopwatch();
            timer.Start();
            base.Init(_resourceProvider, scnFile);
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
            #if RTX_ON
            SetupEnvironmentLighting();
            #endif
            //Debug.LogError($"SkyBox+NavMesh+Lights: {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            CreateActorObjects(actorTintColor, _tilemap);
            //Debug.LogError($"CreateActors: {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            ActivateSceneObjects();
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

        public SceneObject GetSceneObject(byte id)
        {
            return SceneObjects.ContainsKey(id) ? SceneObjects[id] : null;
        }

        public GameObject GetSceneObjectGameObject(byte id)
        {
            return _activatedSceneObjects.ContainsKey(id) ? _activatedSceneObjects[id] : null;
        }

        public Dictionary<byte, GameObject> GetAllActivatedSceneObjects()
        {
            return _activatedSceneObjects;
        }
        
        public Dictionary<byte, SceneObject> GetAllSceneObjects()
        {
            return SceneObjects;
        }

        public Actor GetActor(byte id)
        {
            return Actors.ContainsKey(id) ? Actors[id] : null;
        }

        public Dictionary<byte, Actor> GetAllActors()
        {
            return Actors;
        }

        public GameObject GetActorGameObject(byte id)
        {
            return _actorObjects.ContainsKey(id) ? _actorObjects[id] : null;
        }

        public Dictionary<byte, GameObject> GetAllActorGameObjects()
        {
            return _actorObjects;
        }

        public Dictionary<int, MeshCollider> GetMeshColliders()
        {
            return _meshColliders;
        }

        private void RenderMesh()
        {
            // Render mesh
            _mesh = new GameObject($"Mesh_{ScnFile.SceneInfo.Name}");
            var polyMeshRenderer = _mesh.AddComponent<PolyModelRenderer>();
            _mesh.transform.SetParent(_parent.transform);

            polyMeshRenderer.Render(ScenePolyMesh.PolFile,
                _resourceProvider.GetMaterialFactory(),
                ScenePolyMesh.TextureProvider,
                Color.white);

            if (SceneCvdMesh != null)
            {
                var cvdMeshRenderer = _mesh.AddComponent<CvdModelRenderer>();
                cvdMeshRenderer.Init(SceneCvdMesh.Value.CvdFile,
                    _resourceProvider.GetMaterialFactory(),
                    SceneCvdMesh.Value.TextureProvider,
                    Color.white,
                    0f);
                cvdMeshRenderer.PlayAnimation(SCENE_CVD_ANIMATION_DEFAULT_TIMESCALE);
            }
        }

        private void RenderSkyBox()
        {
            if (ScnFile.SceneInfo.SkyBox == 0) return;
            _skyBoxRenderer = _mainCamera.gameObject.AddComponent<SkyBoxRenderer>();
            _skyBoxRenderer.Render(_resourceProvider.GetSkyBoxTextures((int)ScnFile.SceneInfo.SkyBox));
        }

        private void SetupNavMesh()
        {
            if (Utility.IsHandheldDevice())
            {
                // We only enable joystick/gamepad control for the gameplay on mobile
                // devices so there is no need for setting up nav mesh.
                return;
            }

            for (var i = 0; i < NavFile.FaceLayers.Length; i++)
            {
                var navMesh = new GameObject($"NavMesh_Layer_{i}")
                {
                    layer = LayerMask.NameToLayer("RaycastOnly")
                };
                navMesh.transform.SetParent(_parent.transform);
                
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
            Quaternion mainLightRotation = ScnFile.SceneInfo.SceneType == ScnSceneType.StoryB ?
                    Quaternion.Euler(120f, -20f, 0f) :
                    Quaternion.Euler(70f, 0f, 0f);

            if (ScnFile.SceneInfo.SceneType == ScnSceneType.StoryB && IsNightScene())
            {
                mainLightRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            
            // Most in-door scenes have a single spot light source where we can find in the LGT file,
            // which can be used as the main light source for the scene.
            // if (ScnFile.SceneInfo.SceneType == ScnSceneType.StoryB &&
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
            mainLightGo.transform.SetParent(_parent.transform);
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
                new Color(100f / 255f, 100f / 255f, 100f / 255f) :
                new Color(200f / 255f, 190f / 255f, 180f / 255f);
            _mainLight.intensity = ScnFile.SceneInfo.SceneType == ScnSceneType.StoryB ? 0.75f : 1f;
            #elif PAL3A
            _mainLight.color = IsNightScene() ?
                new Color(60f / 255f, 60f / 255f, 100f / 255f) :
                new Color(200f / 255f, 200f / 255f, 200f / 255f);
            _mainLight.intensity = ScnFile.SceneInfo.SceneType == ScnSceneType.StoryB ? 0.65f : 1f;
            #endif
            
            // Ambient light
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.ambientLight = IsNightScene() ?
                new Color( 60f/ 255f, 70f / 255f, 100f / 255f) :
                new Color(200f / 255f, 200f / 255f, 180f / 255f);
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
            var disableShadows = _pointLights.Count > MAX_NUM_OF_POINT_LIGHTS_WITH_SHADOWS;
            
            foreach (Light pointLight in _pointLights)
            {
                pointLight.shadows = disableShadows ? LightShadows.None : LightShadows.Soft;
            }
        }

        private void ActivateSceneObjects()
        {
            foreach (SceneObject sceneObject in SceneObjects.Values.Where(s => s.Info.Active == 1))
            {
                ActivateSceneObject(sceneObject);
            }
        }

        private void CreateActorObjects(Color tintColor, Tilemap tilemap)
        {
            foreach (Actor actorObject in Actors.Values)
            {
                if (actorObject.AnimationFileType == ActorAnimationFileType.Mv3)
                {
                    CreateActorObject(actorObject, tintColor, tilemap);
                }
            }
        }

        private void ActivateSceneObject(SceneObject sceneObject)
        {
            if (_activatedSceneObjects.ContainsKey(sceneObject.Info.Id)) return;

            Color tintColor = Color.white;
            if (IsNightScene())
            {
                tintColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            }

            GameObject sceneObjectGameObject = sceneObject.Activate(_resourceProvider, tintColor);

            #if RTX_ON
            if (sceneObject.GraphicsEffect == GraphicsEffect.Fire &&
                sceneObjectGameObject.GetComponent<FireEffect>() is { } fireEffect &&
                fireEffect.EffectGameObject != null)
            {
                var yOffset = EffectConstants.FireEffectInfo[fireEffect.FireEffectType].lightSourceYOffset;
                AddPointLight(fireEffect.EffectGameObject.transform, yOffset);
            }
            #endif

            sceneObjectGameObject.transform.SetParent(_parent.transform);
            _activatedSceneObjects[sceneObject.Info.Id] = sceneObjectGameObject;
        }

        private void DisposeSceneObject(byte id)
        {
            if (!_activatedSceneObjects.ContainsKey(id)) return;
            _activatedSceneObjects.Remove(id, out GameObject sceneObject);
            
            #if RTX_ON
            if (sceneObject.GetComponentInChildren<Light>() is {type: LightType.Point} pointLight)
            {
                _pointLights.Remove(pointLight);
                StripPointLightShadowsIfNecessary();
            }
            #endif
            
            Destroy(sceneObject);
        }

        private void CreateActorObject(Actor actor, Color tintColor, Tilemap tileMap)
        {
            if (_actorObjects.ContainsKey(actor.Info.Id)) return;
            GameObject actorGameObject = ActorFactory.CreateActorGameObject(_resourceProvider,
                actor, tileMap, tintColor, GetAllActiveActorBlockingTilePositions);
            actorGameObject.transform.SetParent(_parent.transform, false);
            _actorObjects[actor.Info.Id] = actorGameObject;
        }

        private void ActivateActorObject(byte id, bool isActive)
        {
            if (!_actorObjects.ContainsKey(id)) return;

            GameObject actorGameObject = _actorObjects[id];
            var actorController = actorGameObject.GetComponent<ActorController>();
            actorController.IsActive = isActive;
        }

        /// <summary>
        /// Get all tile positions blocked by the active actors in current scene.
        /// </summary>
        private HashSet<Vector2Int> GetAllActiveActorBlockingTilePositions(int layerIndex, byte[] excludeActorIds)
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
                foreach (Direction direction in Enum.GetValues(typeof(Direction)).Cast<Direction>())
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

        public void Execute(SceneActivateObjectCommand command)
        {
            if (!SceneObjects.ContainsKey((byte)command.ObjectId)) return;

            SceneObject sceneObject = SceneObjects[(byte)command.ObjectId];

            if (command.IsActive == 1)
            {
                ActivateSceneObject(sceneObject);
            }
            else
            {
                DisposeSceneObject(sceneObject.Info.Id);
            }
        }

        public void Execute(ActorActivateCommand command)
        {
            if (command.ActorId == ActorConstants.PlayerActorVirtualID) return;
            var actorId = (byte) command.ActorId;
            if (!_actorObjects.ContainsKey(actorId)) return;
            ActivateActorObject(actorId, command.IsActive == 1);
        }

        public void Execute(ActorLookAtActorCommand command)
        {
            if (!_actorObjects.ContainsKey((byte)command.ActorId) ||
                !_actorObjects.ContainsKey((byte)command.LookAtActorId)) return;

            Transform actorTransform = _actorObjects[(byte)command.ActorId].transform;
            Transform lookAtActorTransform = _actorObjects[(byte)command.LookAtActorId].transform;
            Vector3 lookAtActorPosition = lookAtActorTransform.position;

            actorTransform.LookAt(new Vector3(
                lookAtActorPosition.x,
                actorTransform.position.y,
                lookAtActorPosition.z));
        }

        public void Execute(PlayerInteractWithObjectCommand command)
        {
            if (_activatedSceneObjects.ContainsKey((byte) command.SceneObjectId))
            {
                GameObject sceneObject = _activatedSceneObjects[(byte) command.SceneObjectId];
                if (sceneObject.GetComponent<CvdModelRenderer>() is { } cvdMeshRenderer)
                {
                    cvdMeshRenderer.PlayAnimation(timeScale: 1, loopCount: 1);
                }
            }
            else
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.SceneObjectId}.");
            }
        }

        public void Execute(SceneOpenDoorCommand command)
        {
            if (_activatedSceneObjects.ContainsKey((byte) command.ObjectId))
            {
                GameObject sceneObject = _activatedSceneObjects[(byte) command.ObjectId];
                if (sceneObject.GetComponent<CvdModelRenderer>() is { } cvdMeshRenderer)
                {
                    cvdMeshRenderer.PlayAnimation(timeScale: 1, loopCount: 1);
                }
            }
            else
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.ObjectId}.");
            }
        }
        
        #if PAL3A
        public void Execute(SceneCloseDoorCommand command)
        {
            if (_activatedSceneObjects.ContainsKey((byte) command.ObjectId))
            {
                GameObject sceneObject = _activatedSceneObjects[(byte) command.ObjectId];
                if (sceneObject.GetComponent<CvdModelRenderer>() is { } cvdMeshRenderer)
                {
                    cvdMeshRenderer.PlayAnimation(timeScale: -1, loopCount: 1);
                }
            }
            else
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.ObjectId}.");
            }
        }
        #endif

        public void Execute(SceneMoveObjectCommand command)
        {
            if (_activatedSceneObjects.ContainsKey((byte) command.ObjectId))
            {
                GameObject sceneObject = _activatedSceneObjects[(byte) command.ObjectId];
                Vector3 offset = GameBoxInterpreter.ToUnityPosition(
                    new Vector3(command.XOffset, command.YOffset, command.ZOffset));
                Vector3 toPosition = sceneObject.transform.position + offset;
                StartCoroutine(AnimationHelper.MoveTransform(sceneObject.transform, toPosition, command.Duration));
            }
            else
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.ObjectId}.");
            }
        }

        #if PAL3A
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
                            CommandDispatcher<ICommand>.Instance.Dispatch(new ActorActivateCommand((byte)actorId, 0));   
                        }
                    }

                    GameObject leiYuanGeActorGameObject = GetActorGameObject((byte)PlayerActorId.LeiYuanGe);
                    Vector3 leiYuanGeHeadPosition = leiYuanGeActorGameObject.GetComponent<ActorActionController>()
                        .GetActorHeadWorldPosition();

                    var yOffset = command.ActionType == 0 ? -0.23f : 0.23f;  // Height adjustment
                
                    GameObject birdActorGameObject = GetActorGameObject((byte)activeBirdActorId);
                    birdActorGameObject.transform.position = new Vector3(leiYuanGeHeadPosition.x,
                        leiYuanGeHeadPosition.y + yOffset,
                        leiYuanGeHeadPosition.z);
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