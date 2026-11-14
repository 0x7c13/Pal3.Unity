// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.DataReader.Cvd;
    using Core.DataReader.Mov;
    using Core.DataReader.Msh;
    using Core.DataReader.Mtl;
    using Core.DataReader.Mv3;
    using Core.DataReader.Pol;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using Game.Data;
    using Game.Rendering.Material;
    using Game.Rendering.Renderer;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    public sealed class ResourceModelPreview : IDisposable
    {
        private readonly ResourceCatalog _catalog;
        private readonly Scene _scene;
        private readonly Camera _camera;
        private readonly GameMaterialManager _materials;
        private readonly Dictionary<string, (ITexture2D texture, bool alpha)> _textures = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<IDisposable> _renderers = new();
        private readonly List<Action<float>> _samplers = new();
        private RenderTexture _target;
        private Vector2 _orbit = new(145, 20);
        private Vector3 _center;
        private float _distance = 5;
        private Bounds? _fallbackBounds;

        public GameObject Root
        {
            get;
        }

        public float Duration
        {
            get; private set;
        }

        public string Statistics
        {
            get; private set;
        }

        public Dictionary<string, bool> Dependencies { get; } = new(StringComparer.OrdinalIgnoreCase);

        public ResourceModelPreview(ResourceCatalog catalog, bool toon)
        {
            _catalog = catalog;
            _scene = EditorSceneManager.NewPreviewScene();
            Root = NewObject("Resource preview");
            var cameraObject = NewObject("Preview camera");
            _camera = cameraObject.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.scene = _scene;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f);
            _camera.fieldOfView = 40;
            _camera.nearClipPlane = 0.01f;
            _camera.farClipPlane = 10000;
            var light = NewObject("Preview light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(45, -35, 0);
            _materials = new GameMaterialManager(new UnityMaterialFactory(), toon);
        }

        private GameObject NewObject(string name, Transform parent = null)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(go, _scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }

        public void Load(ResourceEntry entry, object decoded)
        {
            GameObject go = NewObject(entry.Name, Root.transform);
            var textures = new ModelTextures(this, DirectoryOf(entry.Path));
            switch (decoded)
            {
                case PolFile pol:
                    var poly = go.AddComponent<PolyModelRenderer>();
                    _renderers.Add(poly);
                    poly.Render(pol, textures, _materials, isStaticObject: true,
                        textureFactory: new UnityTextureFactory(), animateWater: false);
                    break;
                case CvdFile cvd:
                    var cvdRenderer = go.AddComponent<CvdModelRenderer>();
                    _renderers.Add(cvdRenderer);
                    cvdRenderer.Init(cvd, textures, _materials);
                    Duration = Mathf.Max(Duration, cvd.AnimationDuration);
                    _samplers.Add(t => cvdRenderer.SetCurrentTime(Mathf.Clamp(t, 0, cvd.AnimationDuration)));
                    break;
                case Mv3File mv3:
                    var mv3Renderer = go.AddComponent<Mv3ModelRenderer>();
                    _renderers.Add(mv3Renderer);
                    mv3Renderer.Init(mv3, _materials, textures);
                    Duration = Mathf.Max(Duration, (float)mv3.TotalGameBoxTicks / UnityPrimitivesConvertor.GameBoxTicksPerSecond);
                    _samplers.Add(mv3Renderer.SampleAnimation);
                    break;
                case MshFile:
                case MovFile:
                    string directory = DirectoryOf(entry.Path);
                    string folder = FileName(directory);
                    string mshPath = decoded is MshFile ? entry.Path : Path.ChangeExtension(entry.Path, ".msh");
                    if (_catalog.Find(mshPath) == null)
                        mshPath = CombinePath(directory, folder + ".msh");
                    string mtlPath = Path.ChangeExtension(mshPath, ".mtl");
                    if (_catalog.Find(mtlPath) == null)
                        mtlPath = CombinePath(directory, folder + ".mtl");
                    var msh = decoded as MshFile ?? (MshFile)ReadDependency(mshPath);
                    var mtl = (MtlFile)ReadDependency(mtlPath);
                    var skeletal = go.AddComponent<SkeletalModelRenderer>();
                    _renderers.Add(skeletal);
                    skeletal.Init(msh, mtl, _materials, new ModelTextures(this, DirectoryOf(mtlPath)));
                    if (decoded is MovFile mov)
                    {
                        Duration = mov.Duration;
                        _samplers.Add(t => skeletal.SampleAnimation(mov, t));
                    }
                    break;
                default:
                    throw new NotSupportedException("No model preview for " + entry.Extension);
            }
            foreach (Transform child in Root.GetComponentsInChildren<Transform>(true))
                child.gameObject.hideFlags = HideFlags.HideAndDontSave;
            Sample(0);
            Frame();
            MeshFilter[] meshes = Root.GetComponentsInChildren<MeshFilter>();
            Statistics = $"{meshes.Length} meshes · {meshes.Sum(m => m.sharedMesh != null ? m.sharedMesh.vertexCount : 0):N0} vertices · " +
                $"{Root.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Distinct().Count()} materials";
        }

        private object ReadDependency(string path)
        {
            ResourceEntry entry = _catalog.Find(path);
            Dependencies[path] = entry != null;
            if (entry == null)
                throw new FileNotFoundException("Missing model dependency: " + path);
            return ResourceFormats.Read(entry, _catalog.Read(entry), _catalog.Codepage);
        }

        public static string DirectoryOf(string path)
        {
            path = path.Replace('/', '\\');
            int slash = path.LastIndexOf('\\');
            return slash < 0 ? "" : path[..slash];
        }

        public static string FileName(string path) => path.Replace('/', '\\').Split('\\')[^1];

        public static string CombinePath(string directory, string name) => string.IsNullOrEmpty(directory) ? name : directory + "\\" + name;

        private sealed class ModelTextures : ITextureResourceProvider
        {
            private readonly ResourceModelPreview _owner;
            private readonly string _directory;

            public ModelTextures(ResourceModelPreview owner, string directory)
            {
                _owner = owner;
                _directory = directory;
            }

            public string GetTexturePath(string name) => CombinePath(_directory, name);

            public ITexture2D GetTexture(string name) => GetTexture(name, out _);

            public ITexture2D GetTexture(string name, out bool hasAlphaChannel)
            {
                if (string.IsNullOrEmpty(name))
                {
                    hasAlphaChannel = false;
                    return new UnityTexture2D(Texture2D.whiteTexture);
                }
                string original = GetTexturePath(name);
                string dds = Path.ChangeExtension(original, ".dds");
                ResourceEntry entry = _owner._catalog.Find(dds) ?? _owner._catalog.Find(original);
                string path = entry?.Path ?? original;
                _owner.Dependencies[path] = entry != null;
                if (entry == null)
                {
                    hasAlphaChannel = false;
                    return new UnityTexture2D(Texture2D.whiteTexture);
                }
                if (!_owner._textures.TryGetValue(path, out var cached))
                {
                    byte[] bytes = ResourceImageData.Prepare(entry.Extension, _owner._catalog.Read(entry));
                    ITexture2D texture;
                    bool alpha;
                    if (entry.Extension is ".png" or ".jpg" or ".jpeg")
                    {
                        var native = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (!native.LoadImage(bytes))
                        {
                            Object.DestroyImmediate(native);
                            throw new InvalidDataException("Invalid image: " + path);
                        }
                        texture = new UnityTexture2D(native);
                        alpha = entry.Extension == ".png";
                    }
                    else
                    {
                        using ITextureDataSource source = new TextureLoaderFactory().GetTextureLoader(entry.Extension).Load(bytes, out alpha);
                        texture = new UnityTextureFactory().CreateTexture(source, TextureCreateOptions.Readable);
                    }
                    var nativeTexture = (Texture2D)texture.NativeObject;
                    nativeTexture.name = path;
                    nativeTexture.hideFlags = HideFlags.HideAndDontSave;
                    nativeTexture.wrapMode = TextureWrapMode.Repeat;
                    _owner._textures[path] = cached = (texture, alpha);
                }
                hasAlphaChannel = cached.alpha;
                return cached.texture;
            }
        }

        public void Sample(float time)
        {
            foreach (Action<float> sample in _samplers)
                sample(time);
        }

        public void Frame()
        {
            Renderer[] renderers = Root.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : (_fallbackBounds ?? new Bounds(Vector3.zero, Vector3.one));
            foreach (Renderer renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            _center = bounds.center;
            _distance = Mathf.Max(0.5f, bounds.extents.magnitude / Mathf.Sin(_camera.fieldOfView * Mathf.Deg2Rad / 2) * 1.2f);
        }

        public void FramePoints(IEnumerable<Vector3> points)
        {
            Vector3[] positions = points.ToArray();
            var bounds = new Bounds(positions.Length > 0 ? positions[0] : Vector3.zero, Vector3.one);
            foreach (Vector3 point in positions)
                bounds.Encapsulate(point);
            _fallbackBounds = bounds;
            Frame();
        }

        public bool Project(Vector3 world, Rect view, out Vector2 screen)
        {
            Vector3 point = _camera.WorldToViewportPoint(world);
            screen = new Vector2(view.x + point.x * view.width, view.y + (1 - point.y) * view.height);
            return point.z > 0;
        }

        public void Draw(Rect rect)
        {
            Event e = Event.current;
            if (rect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDrag)
                {
                    if (e.button == 2 || e.shift)
                        _center += (_camera.transform.right * -e.delta.x + _camera.transform.up * e.delta.y) * (_distance / 600);
                    else if (e.button == 0)
                        _orbit += e.delta * new Vector2(0.5f, -0.5f);
                    e.Use();
                }
                if (e.type == EventType.ScrollWheel)
                {
                    _distance = Mathf.Clamp(_distance * Mathf.Exp(e.delta.y * 0.06f), 0.05f, 5000);
                    e.Use();
                }
                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F)
                {
                    Frame();
                    e.Use();
                }
            }
            if (e.type != EventType.Repaint || rect.width < 2 || rect.height < 2)
                return;
            GUI.DrawTexture(rect, Render(Mathf.CeilToInt(rect.width), Mathf.CeilToInt(rect.height)), ScaleMode.StretchToFill, false);
        }

        public RenderTexture Render(int width, int height)
        {
            EnsureTarget(width, height);
            _camera.transform.position = _center + Quaternion.Euler(_orbit.y, _orbit.x, 0) * new Vector3(0, 0, -_distance);
            _camera.transform.LookAt(_center);
            _camera.aspect = (float)width / height;
            RenderPipeline.SubmitRenderRequest(_camera, new UniversalRenderPipeline.SingleCameraRequest { destination = _target });
            return _target;
        }

        private void EnsureTarget(int width, int height)
        {
            if (_target != null && _target.width == width && _target.height == height)
                return;
            if (_target != null)
            {
                _target.Release();
                Object.DestroyImmediate(_target);
            }
            _target = new RenderTexture(width, height, 24) { hideFlags = HideFlags.HideAndDontSave };
            _target.Create();
        }

        public void Dispose()
        {
            // Capture partially initialized resources too: runtime renderers may not yet be IsInitialized.
            Mesh[] meshes = Root != null ? Root.GetComponentsInChildren<MeshFilter>().Select(m => m.sharedMesh).Where(m => m != null).Distinct().ToArray() : Array.Empty<Mesh>();
            Material[] materials = Root != null ? Root.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToArray() : Array.Empty<Material>();
            foreach (IDisposable renderer in _renderers)
                renderer.Dispose();
            _renderers.Clear();
            _samplers.Clear();
            if (Root != null)
                Object.DestroyImmediate(Root);
            _materials.Dispose();
            foreach (Mesh mesh in meshes)
                if (mesh != null)
                    Object.DestroyImmediate(mesh);
            foreach (Material material in materials)
                if (material != null)
                    Object.DestroyImmediate(material);
            foreach (var texture in _textures.Values)
                Object.DestroyImmediate((Texture2D)texture.texture.NativeObject);
            _textures.Clear();
            if (_target != null)
            {
                _target.Release();
                Object.DestroyImmediate(_target);
                _target = null;
            }
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
        }
    }
}
