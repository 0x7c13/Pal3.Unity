// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    using System.Collections.Generic;
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
    using Renderer;
    using SceneObjects;
    using UnityEngine;

    public class Scene : SceneBase,
        ICommandExecutor<SceneActivateObjectCommand>,
        ICommandExecutor<PlayerInteractWithObjectCommand>,
        ICommandExecutor<SceneMoveObjectCommand>,
        ICommandExecutor<ActorActivateCommand>,
        ICommandExecutor<ActorLookAtActorCommand>
    {
        private Camera _mainCamera;
        private SkyBoxRenderer _skyBoxRenderer;

        private GameObject _parent;
        private GameObject _mesh;
        private readonly List<GameObject> _navMeshLayers = new ();
        private readonly Dictionary<int, MeshCollider[]> _meshColliders = new ();

        private readonly Dictionary<byte, GameObject> _activatedSceneObjects = new ();
        private readonly Dictionary<byte, GameObject> _actorObjects = new ();

        private GameResourceProvider _resourceProvider;

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

            foreach (var meshColliders in _meshColliders.Values)
            {
                foreach (var meshCollider in meshColliders)
                {
                    Destroy(meshCollider.sharedMesh);
                    Destroy(meshCollider);
                }
            }

            foreach (var navMeshLayer in _navMeshLayers)
            {
                Destroy(navMeshLayer);
            }

            foreach (var sceneObject in _activatedSceneObjects.Values)
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

            base.Init(_resourceProvider, scnFile);

            RenderMesh();
            RenderSkyBox();
            SetupNavMesh();
            CreateActorObjects();
            ActivateSceneObjects();
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

        public Actor GetActor(byte id)
        {
            return Actors.ContainsKey(id) ? Actors[id] : null;
        }

        public GameObject GetActorGameObject(byte id)
        {
            return _actorObjects.ContainsKey(id) ? _actorObjects[id] : null;
        }

        public Dictionary<byte, GameObject> GetAllActors()
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
            var meshRenderer = _mesh.AddComponent<PolyStaticMeshRenderer>();
            _mesh.transform.SetParent(_parent.transform);
            meshRenderer.Render(ScenePolyMesh.PolFile, ScenePolyMesh.TextureProvider);
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
                // We only enable joystick control for the gameplay on mobile
                // devices so there is no need for setting up nav mesh.
                return;
            }

            for (var i = 0; i < NavFile.FaceLayers.Length; i++)
            {
                var navMesh = new GameObject($"Nav Mesh Layer {i}");

                /*
                 * There are some cases where the nav mesh is pointing downwards instead
                 * of upwards. I am not sure why is that but for now, let's generate
                 * two nav meshes just to be safe (one face up, one face down).
                 * TODO: Calculate the normal to see if mesh is facing downwards?
                 */

                var vertices = NavFile.FaceLayers[i].Vertices
                    .Select(v => GameBoxInterpreter.ToUnityVertex(new Vector3(v.x, v.y, v.z),
                        GameBoxInterpreter.GameBoxUnitToUnityUnit)).ToArray();

                var meshColliderFaceUp = navMesh.AddComponent<MeshCollider>();
                navMesh.transform.SetParent(_parent.transform);

                meshColliderFaceUp.convex = false;
                meshColliderFaceUp.sharedMesh = new Mesh()
                {
                    vertices = vertices,
                    triangles = GameBoxInterpreter.ToUnityTriangles(NavFile.FaceLayers[i].Triangles)
                };

                var meshColliderFaceDown = navMesh.AddComponent<MeshCollider>();

                meshColliderFaceDown.convex = false;
                meshColliderFaceDown.sharedMesh = new Mesh()
                {
                    vertices = vertices,
                    triangles = NavFile.FaceLayers[i].Triangles
                };

                _meshColliders[i] = new []{ meshColliderFaceUp, meshColliderFaceDown};
                _navMeshLayers.Add(navMesh);
            }
        }

        private void ActivateSceneObjects()
        {
            foreach (var sceneObject in SceneObjects.Values.Where(s => s.Info.Active == 1))
            {
                ActivateSceneObject(sceneObject);
            }
        }

        private void CreateActorObjects()
        {
            foreach (var actorObject in Actors.Values)
            {
                if (actorObject.AnimationFileType == ActorAnimationFileType.Mv3)
                {
                    CreateActorObject(actorObject);
                }
            }
        }

        private void ActivateSceneObject(SceneObject sceneObject)
        {
            if (_activatedSceneObjects.ContainsKey(sceneObject.Info.Id)) return;

            var tintColor = Color.white;
            if (ScnFile.SceneInfo.LightMap == 1)
            {
                tintColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            }

            _activatedSceneObjects[sceneObject.Info.Id] = sceneObject.Activate(_resourceProvider, _parent, tintColor);
        }

        private void DisposeSceneObject(byte id)
        {
            if (!_activatedSceneObjects.ContainsKey(id)) return;
            _activatedSceneObjects.Remove(id, out var sceneObject);
            Destroy(sceneObject);
        }

        private void CreateActorObject(Actor actor)
        {
            if (_actorObjects.ContainsKey(actor.Info.Id)) return;

            var tintColor = Color.white;
            if (ScnFile.SceneInfo.LightMap == 1)
            {
                tintColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            _actorObjects[actor.Info.Id] = ActorFactory.CreateActorGameObject(_resourceProvider, actor,
                _parent, new Tilemap(NavFile), tintColor);
        }

        private void ActivateActorObject(byte id, bool isActive)
        {
            if (!_actorObjects.ContainsKey(id)) return;

            var actorGameObject = _actorObjects[id];
            var actorController = actorGameObject.GetComponent<ActorController>();
            actorController.IsActive = isActive;
        }

        public void Execute(SceneActivateObjectCommand command)
        {
            if (!SceneObjects.ContainsKey((byte)command.ObjectId)) return;

            var sceneObject = SceneObjects[(byte)command.ObjectId];

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
            var actorId = (byte) command.ActorId;
            if (!_actorObjects.ContainsKey(actorId)) return;
            ActivateActorObject(actorId, command.IsActive == 1);
        }

        public void Execute(ActorLookAtActorCommand command)
        {
            if (!_actorObjects.ContainsKey((byte)command.ActorId) ||
                !_actorObjects.ContainsKey((byte)command.LookAtActorId)) return;

            var actorTransform = _actorObjects[(byte)command.ActorId].transform;
            var lookAtActorTransform = _actorObjects[(byte)command.LookAtActorId].transform;
            var lookAtActorPosition = lookAtActorTransform.position;

            actorTransform.LookAt(new Vector3(
                lookAtActorPosition.x,
                actorTransform.position.y,
                lookAtActorPosition.z));
        }

        public void Execute(PlayerInteractWithObjectCommand command)
        {
            if (SceneObjects.ContainsKey((byte) command.SceneObjectId))
            {
                var sceneObject = SceneObjects[(byte) command.SceneObjectId];
            }
            else
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.SceneObjectId}.");
            }
        }

        public void Execute(SceneMoveObjectCommand command)
        {
            if (_activatedSceneObjects.ContainsKey((byte) command.ObjectId))
            {
                var sceneObject = _activatedSceneObjects[(byte) command.ObjectId];
                var offset = GameBoxInterpreter.ToUnityPosition(
                    new Vector3(command.XOffset, command.YOffset, command.ZOffset));
                var toPosition = sceneObject.transform.position + offset;
                StartCoroutine(AnimationHelper.MoveTransform(sceneObject.transform, toPosition, command.Duration));
            }
            else
            {
                Debug.LogError($"Scene object not found or not activated yet: {command.ObjectId}.");
            }
        }
    }
}