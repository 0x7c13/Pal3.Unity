// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Original
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.DataReader;
    using Core.DataReader.Data;
    using Core.DataReader.Cvd;
    using Core.DataReader.Par;
    using Core.FileSystem;
    using Data;
    using Engine.Core.Implementation;
    using Engine.Core.Abstraction;
    using UnityEngine;
    using UnityEngine.Rendering;

    public sealed class OriginalEffectResources : IDisposable
    {
        private readonly ICpkFileSystem _fileSystem;
        private readonly int _codepage;
        private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ParticleFile> _particles = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, byte[]> _particleBytes = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ParticleFile> _overrideParticles = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ParticleShapeFile> _shapes = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CvdFile> _models = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<(string, bool, bool, int), Material> _materials = new();
        private readonly Dictionary<int, string[]> _diagnostics = new();
        private readonly Dictionary<int, string[]> _overrideDiagnostics = new();
        private OriginalEffectOverrides _cachedOverrides;
        private string _cachedOverridesJson;
        private EffectDefinitionFile _definitions;

        public EffectDefinitionFile Definitions => _definitions ??= Read<EffectDefinitionFile>("cbdata\\flexeff.data", new EffectDefinitionFileReader());

        public OriginalEffectResources(ICpkFileSystem fileSystem, int codepage = 936)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _codepage = codepage;
        }

        public static string Normalize(string path)
        {
            path = path.Replace('/', '\\');
            return path.StartsWith("basedata.cpk\\", StringComparison.OrdinalIgnoreCase) ? path : "basedata.cpk\\" + path;
        }

        public byte[] Bytes(string path) => _fileSystem.ReadAllBytes(Normalize(path));

        public T Read<T>(string path, IFileReader<T> reader) => reader.Read(Bytes(path), _codepage);

        public ITextureResourceProvider ModelTextureProvider(string file) => new ModelTextures(this, file);

        private sealed class ModelTextures : ITextureResourceProvider
        {
            private readonly OriginalEffectResources _owner;
            private readonly string _file;

            public ModelTextures(OriginalEffectResources owner, string file)
            {
                _owner = owner;
                _file = file;
            }

            public string GetTexturePath(string name) => RelativeTo(_file, name);

            public ITexture2D GetTexture(string name) => GetTexture(name, out _);

            public ITexture2D GetTexture(string name, out bool hasAlphaChannel)
            {
                hasAlphaChannel = true;
                return new UnityTexture2D(_owner.Texture(GetTexturePath(name)));
            }
        }

        public ParticleFile Particle(string path, OriginalEffectOverrides edits = null)
        {
            path = Normalize(path);
            if (!_particles.TryGetValue(path, out ParticleFile file))
            {
                if (!_particleBytes.TryGetValue(path, out byte[] bytes))
                    _particleBytes[path] = bytes = Bytes(path);
                _particles[path] = file = ((IFileReader<ParticleFile>)new ParticleFileReader()).Read(bytes, _codepage);
            }
            if (edits != null && edits.HasParticleEdits(path))
            {
                RefreshOverrideCache(edits);
                if (!_overrideParticles.TryGetValue(path, out ParticleFile copy))
                {
                    copy = ((IFileReader<ParticleFile>)new ParticleFileReader()).Read(_particleBytes[path], _codepage);
                    edits.ApplyParticle(path, copy);
                    _overrideParticles[path] = copy;
                }
                return copy;
            }
            return file;
        }

        private void RefreshOverrideCache(OriginalEffectOverrides edits)
        {
            // Public override fields also support scripts, Undo and the native
            // Inspector. A content stamp catches those edits without relying on
            // every caller to manually invalidate a revision counter.
            string json = JsonUtility.ToJson(edits);
            if (ReferenceEquals(edits, _cachedOverrides) && json == _cachedOverridesJson)
                return;
            _cachedOverrides = edits;
            _cachedOverridesJson = json;
            _overrideDiagnostics.Clear();
            _overrideParticles.Clear();
        }

        public ParticleShapeFile Shape(string path)
        {
            if (!_shapes.TryGetValue(path, out ParticleShapeFile file))
                _shapes.Add(path, file = Read(path, new ParticleShapeFileReader()));
            return file;
        }

        public CvdFile Model(string path)
        {
            if (!_models.TryGetValue(path, out CvdFile file))
                _models.Add(path, file = Read(path, new CvdFileReader()));
            return file;
        }

        public static string RelativeTo(string file, string relative)
        {
            file = file.Replace('/', '\\');
            int slash = file.LastIndexOf('\\');
            return (slash < 0 ? "" : file[..(slash + 1)]) + relative.Replace('/', '\\');
        }

        public string ResolveTexture(string path)
        {
            path = Normalize(path);
            string dds = Path.ChangeExtension(path, ".dds");
            if (_fileSystem.FileExists(dds))
                return dds;
            if (_fileSystem.FileExists(path))
                return path;
            throw new FileNotFoundException($"Original effect texture not found: {path}", path);
        }

        public Texture2D Texture(string path)
        {
            path = ResolveTexture(path);
            if (_textures.TryGetValue(path, out Texture2D cached))
                return cached;
            Texture2D texture;
            if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(Bytes(path), true))
                {
                    Destroy(texture);
                    throw new InvalidDataException($"Invalid image {path}");
                }
            }
            else
            {
                var provider = new TextureProvider(_fileSystem, new TextureLoaderFactory(), new UnityTextureFactory(),
                    "basedata.cpk", usage: TextureUsage.Sprite);
                texture = provider.GetTexture(path["basedata.cpk\\".Length..])?.NativeObject as Texture2D;
                if (texture == null)
                    throw new InvalidDataException($"Could not load original effect texture {path}");
            }
            texture.name = path;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.hideFlags = HideFlags.DontSave;
            _textures.Add(path, texture);
            return texture;
        }

        public Material Material(string texture, bool additive, bool noZ, int alphaMode = 0)
        {
            var key = (texture, additive, noZ, alphaMode);
            if (_materials.TryGetValue(key, out Material cached))
                return cached;
            Shader shader = Shader.Find("Pal3/OriginalEffect");
            if (shader == null || !shader.isSupported)
                throw new InvalidOperationException("OriginalEffect URP shader is unavailable");
            var material = new Material(shader) { name = $"Original {texture}", hideFlags = HideFlags.DontSave };
            try
            {
                material.mainTexture = string.IsNullOrEmpty(texture) ? Texture2D.whiteTexture : Texture(texture);
            }
            catch
            {
                Destroy(material);
                throw;
            }
            material.SetInt("_SrcBlend", (int)(alphaMode == 1 ? BlendMode.One : BlendMode.SrcAlpha));
            material.SetInt("_DstBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetInt("_ZTest", (int)(noZ ? CompareFunction.Always : CompareFunction.LessEqual));
            material.SetFloat("_AlphaMode", alphaMode);
            _materials.Add(key, material);
            return material;
        }

        // Validate the complete callback closure before choosing this backend.
        public string[] Diagnose(int groupId, OriginalEffectOverrides edits = null)
        {
            if (edits != null)
                RefreshOverrideCache(edits);
            Dictionary<int, string[]> cache = edits == null ? _diagnostics : _overrideDiagnostics;
            if (cache.TryGetValue(groupId, out string[] cached))
                return cached;
            var errors = new List<string>();
            var visiting = new HashSet<int>();
            var done = new HashSet<int>();
            void ModelTextures(string path, CvdGeometryNode node)
            {
                if (node.IsGeometryNode)
                    foreach (CvdMeshSection section in node.Mesh.MeshSections)
                    {
                        string name = section.Material.TextureFileNames[0];
                        if (!string.IsNullOrEmpty(name))
                            ResolveTexture(RelativeTo(path, name));
                    }
                foreach (CvdGeometryNode child in node.Children)
                    ModelTextures(path, child);
            }
            void Visit(int id)
            {
                if (done.Contains(id))
                    return;
                if (!visiting.Add(id))
                {
                    errors.Add($"Recursive callback cycle at group {id}");
                    return;
                }
                if (id < 0 || id >= Definitions.Groups.Length)
                {
                    errors.Add($"Group {id} is outside flexeff");
                    visiting.Remove(id);
                    return;
                }
                EffectGroupObject[] objects = Definitions.Groups[id].Objects;
                for (int index = 0; index < objects.Length; index++)
                {
                    EffectGroupObject obj = objects[index];
                    Definitions.TryGetType(obj.TypeCrc, out EffectDefinition d);
                    try
                    {
                        EffectTask[] tasks = obj.Tasks;
                        if (!OriginalEffectOverrides.Resolve(edits, id, index, ref d, ref tasks))
                            continue;
                        string kind = EffectClass.Name(d.ClassNameCrc);
                        switch (kind)
                        {
                            case "callback":
                                foreach (EffectTask t in tasks)
                                    foreach (var child in t.Callbacks())
                                        Visit(child.groupId);
                                break;
                            case "audio":
                                break;
                            case "particle":
                                ParticleFile p = Particle(d.ParticleFileName, edits);
                                foreach (ParticleDescription pd in p.Particles)
                                    ResolveTexture(RelativeTo(d.ParticleFileName, pd.Texture));
                                foreach (ParticleEmitter e in p.Emitters)
                                    if (e.Shape == 2)
                                        Shape(RelativeTo(d.ParticleFileName, e.ShapePath));
                                break;
                            case "quad":
                            case "cylinder":
                            case "meteor":
                            case "shock":
                            case "grid":
                                ResolveTexture(d.TextureName);
                                break;
                            case "model":
                                OriginalModelNode.ValidateTasks(tasks);
                                if (!_fileSystem.FileExists(Normalize(d.TextureName)))
                                    throw new FileNotFoundException(d.TextureName);
                                if (!d.TextureName.EndsWith(".cvd", StringComparison.OrdinalIgnoreCase))
                                    throw new NotSupportedException("Original POL effect adapter is not implemented");
                                foreach (CvdGeometryNode node in Model(d.TextureName).RootNodes)
                                    ModelTextures(d.TextureName, node);
                                break;
                            default:
                                errors.Add($"Group {id}, {d.TypeName}: unsupported original class {kind}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Group {id}, {d.TypeName}: {ex.Message}");
                    }
                }
                visiting.Remove(id);
                done.Add(id);
            }
            Visit(groupId);
            string[] result = errors.ToArray();
            cache[groupId] = result;
            return result;
        }

        public IEnumerable<int> Preload(int groupId, OriginalEffectOverrides edits = null)
        {
            var seen = new HashSet<int>();
            var pending = new Stack<int>();
            pending.Push(groupId);
            while (pending.Count > 0)
            {
                int id = pending.Pop();
                if (!seen.Add(id))
                    continue;
                EffectGroupObject[] objects = Definitions.Groups[id].Objects;
                for (int index = 0; index < objects.Length; index++)
                {
                    EffectGroupObject obj = objects[index];
                    Definitions.TryGetType(obj.TypeCrc, out EffectDefinition d);
                    EffectTask[] tasks = obj.Tasks;
                    if (!OriginalEffectOverrides.Resolve(edits, id, index, ref d, ref tasks))
                        continue;
                    string kind = EffectClass.Name(d.ClassNameCrc);
                    if (kind == "callback")
                        foreach (EffectTask t in tasks)
                            foreach (var child in t.Callbacks())
                                pending.Push(child.groupId);
                    else if (kind == "particle")
                        foreach (ParticleDescription p in Particle(d.ParticleFileName, edits).Particles)
                            Texture(RelativeTo(d.ParticleFileName, p.Texture));
                    else if (kind != "audio" && kind != "model")
                        Texture(d.TextureName);
                    yield return id;
                }
            }
        }

        public void TrimUnusedRenderAssets(IEnumerable<GameObject> liveRoots)
        {
            var materials = new HashSet<Material>();
            var textures = new HashSet<Texture>();
            foreach (GameObject root in liveRoots)
            {
                if (root == null)
                    continue;
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material == null)
                            continue;
                        materials.Add(material);
                        if (material.mainTexture != null)
                            textures.Add(material.mainTexture);
                    }
            }
            foreach (var key in _materials.Where(p => !materials.Contains(p.Value)).Select(p => p.Key).ToArray())
            {
                Destroy(_materials[key]);
                _materials.Remove(key);
            }
            foreach (string key in _textures.Where(p => !textures.Contains(p.Value)).Select(p => p.Key).ToArray())
            {
                Destroy(_textures[key]);
                _textures.Remove(key);
            }
        }

        public void Dispose()
        {
            foreach (Material m in _materials.Values)
                Destroy(m);
            foreach (Texture2D t in _textures.Values)
                Destroy(t);
            _materials.Clear();
            _textures.Clear();
            _particles.Clear();
            _particleBytes.Clear();
            _overrideParticles.Clear();
            _shapes.Clear();
            _models.Clear();
            _diagnostics.Clear();
            _overrideDiagnostics.Clear();
            _cachedOverrides = null;
            _cachedOverridesJson = null;
        }

        public static void Destroy(UnityEngine.Object value)
        {
            if (value == null)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(value);
            else
                UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
