// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if PAL3A
namespace Pal3.Game.Effect
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Primitives;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using State;
    using UnityEngine;
    using UnityEngine.Rendering;

    public sealed class SkyboxEffectManager : IDisposable,
        ICommandExecutor<SkyboxAddEffectCommand>, ICommandExecutor<SkyboxRemoveEffectCommand>,
        ICommandExecutor<SceneLeavingCurrentSceneNotification>,
        ICommandExecutor<GameStateChangedNotification>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const float BoxSize = 4500f; // Original SkyBox initialization at 0x5272AD.
        private readonly GameResourceProvider _resources;
        private readonly Transform _camera;
        private readonly Dictionary<string, ITexture2D[]> _sequences = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<Effect> _effects = new();
        private readonly Material _material;
        private readonly MaterialPropertyBlock _properties = new();
        private GameState _state = GameState.UI;
        private readonly int _textureId = Shader.PropertyToID("_MainTex");
        private sealed class Effect
        {
            public string Sequence;
            public ITexture2D[] Frames;
            public GameObject Object;
            public Mesh Mesh;
            public MeshRenderer Renderer;
            public float Time, Fps, Interval, Delay;
            public int Frame = -1;
        }

        public int EffectCount => _effects.Count;
        public SkyboxEffectManager(GameResourceProvider resources, Transform camera)
        {
            _resources = resources;
            _camera = camera;
            _material = new Material(Resources.Load<Shader>("Shaders/SkyboxEffect"));
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            Clear();
            UnityEngine.Object.Destroy(_material);
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(SkyboxAddEffectCommand command)
        {
            int face = command.Face < 0 ? UnityEngine.Random.Range(0, 6) : command.Face;
            if (face > 5 || string.IsNullOrWhiteSpace(command.Sequence) ||
                !float.IsFinite(command.X) || !float.IsFinite(command.Y) ||
                !float.IsFinite(command.Scale) || !float.IsFinite(command.Interval) ||
                !float.IsFinite(command.InitialDelay) || !float.IsFinite(command.Fps) || command.Fps <= 0)
                throw new ArgumentException("Invalid skybox effect parameters");
            string sequence = command.Sequence;
            if (!_sequences.TryGetValue(sequence, out var frames))
            {
                int separator = sequence.LastIndexOf('_');
                if (separator < 1 || !int.TryParse(sequence[(separator + 1)..], out int count) || count < 1 || count > 1024)
                    throw new ArgumentException("Invalid skybox sequence name");
                var provider = _resources.CreateTextureResourceProvider(
                    FileConstants.EffectScnFolderVirtualPath + "skybox\\effect\\" + sequence, useCache: false);
                frames = new ITexture2D[count];
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        string name = sequence[..separator] + (i + 1).ToString("D4") + ".tga";
                        frames[i] = provider.GetTexture(name) ?? throw new FileNotFoundException(name);
                    }
                }
                catch { foreach (var frame in frames) frame?.Destroy(); throw; }
                _sequences.Add(sequence, frames);
            }
            float scale = command.Scale > 0 ? command.Scale : 1;
            float width = Mathf.Min(BoxSize, frames[0].Width * scale), height = Mathf.Min(BoxSize, frames[0].Height * scale);
            var go = new GameObject("SkyboxEffect_" + sequence);
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            var mesh = new Mesh { name = "SkyboxEffectQuad" };
            mesh.vertices = Vertices(face, command.X, command.Y, width, height);
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            _effects.Add(new Effect
            {
                Sequence = sequence,
                Frames = frames,
                Object = go,
                Mesh = mesh,
                Renderer = renderer,
                Fps = command.Fps,
                Interval = Mathf.Max(0, command.Interval),
                Delay = Mathf.Max(0, command.InitialDelay)
            });
        }

        // Vertex order follows original 0x527E5C. Coordinates are relative to the camera.
        internal static Vector3[] Vertices(int face, float x, float y, float width, float height)
        {
            float h = BoxSize * 0.5f, a = h + x, b = h + y;
            GameBoxVector3[] points = face switch
            {
                0 => new[] { new GameBoxVector3(-h, b - height, a), new(-h, b - height, a - width), new(-h, b, a - width), new(-h, b, a) },
                1 => new[] { new GameBoxVector3(a, b - height, h), new(a - width, b - height, h), new(a - width, b, h), new(a, b, h) },
                2 => new[] { new GameBoxVector3(h, b - height, a - width), new(h, b - height, a), new(h, b, a), new(h, b, a - width) },
                3 => new[] { new GameBoxVector3(a - width, b - height, -h), new(a, b - height, -h), new(a, b, -h), new(a - width, b, -h) },
                4 => new[] { new GameBoxVector3(a, h, b - height), new(a, h, b), new(a - width, h, b), new(a - width, h, b - height) },
                _ => new[] { new GameBoxVector3(a - width, -h, b - height), new(a - width, -h, b), new(a, -h, b), new(a, -h, b - height) }
            };
            return points.Select(p => p.ToUnityPosition()).ToArray();
        }

        public void Update(float deltaTime)
        {
            foreach (var effect in _effects)
            {
                effect.Object.transform.position = _camera.position;
                bool visible = _state != GameState.Combat && _state != GameState.VideoPlaying;
                if (_state == GameState.Gameplay || _state == GameState.Cutscene)
                    effect.Time += deltaTime;
                float elapsed = effect.Time - effect.Delay;
                float duration = effect.Frames.Length / effect.Fps;
                float phase = elapsed >= 0 ? elapsed % (duration + effect.Interval) : -1;
                int frame = phase >= 0 && phase < duration ? Mathf.Min(effect.Frames.Length - 1, (int)(phase * effect.Fps)) : -1;
                effect.Renderer.enabled = visible && frame >= 0;
                if (effect.Frame == frame)
                    continue;
                effect.Frame = frame;
                if (frame < 0)
                    continue;
                _properties.SetTexture(_textureId, effect.Frames[frame].NativeObject as Texture);
                effect.Renderer.SetPropertyBlock(_properties);
            }
        }

        public void Execute(SkyboxRemoveEffectCommand command)
        {
            foreach (var effect in _effects.Where(e => string.Equals(e.Sequence, command.Sequence, StringComparison.OrdinalIgnoreCase)).ToArray())
            {
                UnityEngine.Object.Destroy(effect.Object);
                UnityEngine.Object.Destroy(effect.Mesh);
                _effects.Remove(effect);
            }
            if (_sequences.Remove(command.Sequence, out var frames))
            {
                foreach (var frame in frames) frame.Destroy();
            }
        }

        private void Clear()
        {
            foreach (var effect in _effects)
            {
                UnityEngine.Object.Destroy(effect.Object);
                UnityEngine.Object.Destroy(effect.Mesh);
            }
            _effects.Clear();
            foreach (var frames in _sequences.Values)
            {
                foreach (var frame in frames) frame.Destroy();
            }
            _sequences.Clear();
        }

        public void Execute(GameStateChangedNotification command)
        {
            _state = command.NewState;
        }

        public void Execute(SceneLeavingCurrentSceneNotification command) => Clear();
        public void Execute(ResetGameStateCommand command) => Clear();
    }
}
#endif
