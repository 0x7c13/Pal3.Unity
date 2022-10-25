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
    using Core.DataReader.Lgt;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Renderer;
    using Core.Utils;
    using Data;
    using Dev;
    using MetaData;
    using Renderer;
    using SceneObjects;
    using UnityEngine;
    using UnityEngine.Rendering;
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

        private Camera _mainCamera;
        private SkyBoxRenderer _skyBoxRenderer;

        private GameObject _parent;
        private GameObject _mesh;
        private List<GameObject> _lights = new();
        
        private readonly List<GameObject> _navMeshLayers = new ();
        private readonly Dictionary<int, MeshCollider[]> _meshColliders = new ();

        private readonly Dictionary<byte, GameObject> _activatedSceneObjects = new ();
        private readonly Dictionary<byte, GameObject> _actorObjects = new ();

        private GameResourceProvider _resourceProvider;
        private Tilemap _tilemap;

        private bool _isMainLightInitialized;

        public void Init(GameResourceProvider resourceProvider, Camera mainCamera)
        {
            _resourceProvider = resourceProvider;
            _mainCamera = mainCamera;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

            Destroy(_mesh);
            
            foreach (GameObject lightSource in _lights)
            {
                Destroy(lightSource);
            }

            foreach (var meshColliders in _meshColliders.Values)
            {
                foreach (MeshCollider meshCollider in meshColliders)
                {
                    Destroy(meshCollider.sharedMesh);
                    Destroy(meshCollider);
                }
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
            CreateLightSources();
            SetupEnvironmentLight();
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

        public Dictionary<int, MeshCollider[]> GetMeshColliders()
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

                var vertices = NavFile.FaceLayers[i].Vertices
                    .Select(v => GameBoxInterpreter.ToUnityVertex(new Vector3(v.x, v.y, v.z),
                        GameBoxInterpreter.GameBoxUnitToUnityUnit)).ToArray();

                var meshCollider = navMesh.AddComponent<MeshCollider>();
                meshCollider.convex = false;
                meshCollider.sharedMesh = new Mesh()
                {
                    vertices = vertices,
                    triangles = GameBoxInterpreter.ToUnityTriangles(NavFile.FaceLayers[i].Triangles)
                };

                /*
                 * There are some cases where the nav mesh is pointing downwards instead
                 * of upwards. I am not sure why is that but for now, let's generate
                 * two nav meshes just to be safe (one facing up, one facing down).
                 * TODO: Calculate the normal to see if mesh is facing downwards? Or a blacklist?
                 */
                var meshColliderInverse = navMesh.AddComponent<MeshCollider>();
                meshColliderInverse.convex = false;
                meshColliderInverse.sharedMesh = new Mesh()
                {
                    vertices = vertices,
                    triangles = NavFile.FaceLayers[i].Triangles
                };

                _meshColliders[i] = new [] { meshCollider, meshColliderInverse };

                _navMeshLayers.Add(navMesh);
            }
        }

        private void CreateLightSources()
        {
            if (LgtFile == null) return;

            _lights = new List<GameObject>();

            foreach (LightNode lightNode in LgtFile.LightNodes)
            {
                var lightSource = new GameObject($"LightSource_{lightNode.LightType}");
                lightSource.transform.SetParent(_parent.transform);

                // Attach LightNode to the GameObject for better debuggability
                #if UNITY_EDITOR
                var materialInfoPresenter = lightSource.AddComponent<LightSourceInfoPresenter>();
                materialInfoPresenter.LightNode = lightNode;
                #endif

                switch (lightNode.LightType)
                {
                    // case GameBoxLightType.Omni:
                    //     lightSource.transform.position = GameBoxInterpreter.ToUnityPosition(lightNode.WorldMatrix.MultiplyPoint(Vector3.zero));
                    //     break;
                    case GameBoxLightType.Spot:
                    {
                        lightSource.transform.position = GameBoxInterpreter.ToUnityPosition(lightNode.WorldMatrix.MultiplyPoint(Vector3.zero));
                        float w = Mathf.Sqrt(1.0f + lightNode.WorldMatrix.m00 + lightNode.WorldMatrix.m11 + lightNode.WorldMatrix.m22) / 2.0f;
                        lightSource.transform.rotation = GameBoxInterpreter.LgtQuaternionToUnityQuaternion(new GameBoxQuaternion()
                        {
                            X = (lightNode.WorldMatrix.m21 - lightNode.WorldMatrix.m12) / (4.0f * w),
                            Y = (lightNode.WorldMatrix.m02 - lightNode.WorldMatrix.m20) / (4.0f * w),
                            Z = (lightNode.WorldMatrix.m10 - lightNode.WorldMatrix.m01) / (4.0f * w),
                            W = w,
                        });
                        var lightComponent = lightSource.AddComponent<Light>();
                        lightComponent.type = LightType.Directional;
                        lightComponent.color = lightNode.LightColor;
                        lightComponent.intensity = ScnFile.SceneInfo.SceneType == ScnSceneType.StoryB ? 0.8f : 1f;
                        lightComponent.range = 500f;
                        lightComponent.shadows = LightShadows.Soft;
                        _isMainLightInitialized = true;
                        RenderSettings.sun = lightComponent;
                        break;
                    }
                    // case GameBoxLightType.Directional:
                    // {
                    //     float w = Mathf.Sqrt(1.0f + lightNode.WorldMatrix.m00 + lightNode.WorldMatrix.m11 + lightNode.WorldMatrix.m22) / 2.0f;
                    //     lightSource.transform.rotation = GameBoxInterpreter.LgtQuaternionToUnityQuaternion(new GameBoxQuaternion()
                    //     {
                    //         X = (lightNode.WorldMatrix.m21 - lightNode.WorldMatrix.m12) / (4.0f * w),
                    //         Y = (lightNode.WorldMatrix.m02 - lightNode.WorldMatrix.m20) / (4.0f * w),
                    //         Z = (lightNode.WorldMatrix.m10 - lightNode.WorldMatrix.m01) / (4.0f * w),
                    //         W = w,
                    //     });
                    //     break;
                    // }
                }

                _lights.Add(lightSource);
            }
        }

        private void SetupEnvironmentLight()
        {
            if (!_isMainLightInitialized)
            {
                var lightSource = new GameObject($"LightSource_Default");
                lightSource.transform.SetParent(_parent.transform);
                lightSource.transform.rotation = Quaternion.Euler(70f, 0f, 0f);

                var lightComponent = lightSource.AddComponent<Light>();
                
                #if PAL3
                lightComponent.color = IsNightScene() ?
                    new Color(100f / 255f, 100f / 255f, 100f / 255f) :
                    new Color(200f / 255f, 190f / 255f, 180f / 255f);
                #elif PAL3A
                lightComponent.color = IsNightScene() ?
                    new Color(60f / 255f, 60f / 255f, 100f / 255f) :
                    new Color(200f / 255f, 200f / 255f, 200f / 255f);
                #endif
                
                lightComponent.type = LightType.Directional;
                lightComponent.intensity = ScnFile.SceneInfo.SceneType == ScnSceneType.StoryB ? 0.8f : 1f;
                lightComponent.range = 500f;
                lightComponent.shadows = LightShadows.Soft;

                RenderSettings.sun = lightComponent;
            }
            
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.ambientLight = IsNightScene() ?
                new Color( 60f/ 255f, 70f / 255f, 100f / 255f) :
                new Color(200f / 255f, 200f / 255f, 180f / 255f);
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
            sceneObjectGameObject.transform.SetParent(_parent.transform);
            _activatedSceneObjects[sceneObject.Info.Id] = sceneObjectGameObject;
        }

        private void DisposeSceneObject(byte id)
        {
            if (!_activatedSceneObjects.ContainsKey(id)) return;
            _activatedSceneObjects.Remove(id, out GameObject sceneObject);
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