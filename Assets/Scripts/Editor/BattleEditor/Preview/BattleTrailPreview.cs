// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using Battle;
    using ResourceBrowser;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class BattleTrailPreview : IResourcePreviewFrameEffect, IDisposable
    {
        private readonly BattleTrailState _state = new();
        private readonly RenderTexture[] _history = new RenderTexture[BattleTrailState.Capacity];
        private RenderTexture _output;
        private Material _material;
        private bool _disposed;

        public bool Enabled
        {
            get; set;
        }

        public bool Suspended
        {
            get; set;
        }

        private bool Applies => Enabled && !Suspended;

        public Rect Viewport => Applies ? new Rect(0, 0, 1, .75f) : new Rect(0, 0, 1, 1);

        public Vector2Int TargetSize(int requestedWidth, int requestedHeight) => Applies ? new Vector2Int(256, 256) : new Vector2Int(requestedWidth, requestedHeight);

        public void Advance(double scaledSeconds, double absoluteSeconds) => _state.Advance(scaledSeconds);

        public RenderTexture Composite(RenderTexture scene, int requestedWidth, int requestedHeight)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BattleTrailPreview));
            if (!Applies)
                return scene;
            RenderTexture previous = RenderTexture.active;
            try
            {
                EnsureResources(requestedWidth, requestedHeight);
                Graphics.Blit(scene, _history[_state.CurrentTexture]);
                RenderTexture.active = _output;
                GL.Clear(false, true, Color.black);
                for (int order = 0; order < BattleTrailState.Capacity; order++)
                {
                    int alpha = BattleTrailState.AlphaAt(order);
                    _material.SetVector("_Diffuse", new Vector4((alpha / 2) / 255f, (alpha / 2) / 255f, (alpha / 2) / 255f, alpha / 255f));
                    Graphics.Blit(_history[_state.TextureAt(order)], _output, _material);
                }
            }
            finally
            {
                RenderTexture.active = previous;
            }
            return _output;
        }

        private void EnsureResources(int width, int height)
        {
            if (_material == null)
            {
                Shader shader = Shader.Find("Hidden/Pal3/BattleTrail");
                if (shader == null || !shader.isSupported)
                    throw new InvalidOperationException("The original battle trail shader is unavailable.");
                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            for (int index = 0; index < _history.Length; index++)
                if (_history[index] == null)
                {
                    _history[index] = Create(256, 256);
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = _history[index];
                    GL.Clear(false, true, Color.black);
                    RenderTexture.active = previous;
                }
            if (_output != null && _output.width == width && _output.height == height)
                return;
            Release(_output);
            _output = Create(width, height);
        }

        private static RenderTexture Create(int width, int height)
        {
            var texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.Create();
            return texture;
        }

        private static void Release(RenderTexture texture)
        {
            if (texture == null)
                return;
            texture.Release();
            Object.DestroyImmediate(texture);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Release(_output);
            _output = null;
            for (int index = 0; index < _history.Length; index++)
            {
                Release(_history[index]);
                _history[index] = null;
            }
            if (_material != null)
                Object.DestroyImmediate(_material);
            _material = null;
        }
    }
}
