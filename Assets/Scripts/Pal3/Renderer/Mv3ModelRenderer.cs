// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Threading;
    using Core.DataLoader;
    using Core.DataReader.Mv3;
    using Core.GameBox;
    using Core.Renderer;
    using Core.Utils;
    using Dev;
    using UnityEngine;

    /// <summary>
    /// MV3(.mv3) model renderer
    /// </summary>
    public class Mv3ModelRenderer : MonoBehaviour
    {
        public event EventHandler<int> AnimationLoopPointReached;

        private const string MV3_ANIMATION_HOLD_EVENT_NAME = "hold";
        private const string MV3_MODEL_DEFAULT_TEXTURE_EXTENSION = ".tga";
        private const float TIME_TO_TICK_SCALE = 5000f;

        private readonly int _mainTexturePropertyId = Shader.PropertyToID("_MainTex");
        private readonly int _cutoffPropertyId = Shader.PropertyToID("_Cutoff");
        private readonly int _tintColorPropertyId = Shader.PropertyToID("_TintColor");
        private Shader _standardNoShadowShader;

        private ITextureResourceProvider _textureProvider;
        private VertexAnimationKeyFrame[] _keyFrames;
        private Texture2D _texture;
        private Material _material;
        private GameBoxMaterial _gbMaterial;
        private Mv3AnimationEvent[] _events;
        private bool _textureHasAlphaChannel;
        private Color _tintColor;
        private GameObject _meshObject;
        private Coroutine _animation;
        private string _animationName;
        private CancellationTokenSource _animationCts;

        private bool _isActionInHoldState;
        private uint _actionHoldingTick;

        private uint[] _frameTicks;
        private uint _duration;
        private RenderMeshComponent _renderMeshComponent;
        private WaitForSeconds _animationDelay;

        public void Init(Mv3Mesh mv3Mesh,
            Mv3Material material,
            Mv3AnimationEvent[] events,
            VertexAnimationKeyFrame[] keyFrames,
            uint duration,
            ITextureResourceProvider textureProvider,
            Color tintColor)
        {
            _tintColor = tintColor;
            _gbMaterial = material.Material;
            _textureProvider = textureProvider;
            _events = events;
            _keyFrames = keyFrames;
            _duration = duration;
            _texture = textureProvider.GetTexture(material.TextureNames[0], out var hasAlphaChannel);
            _textureHasAlphaChannel = hasAlphaChannel;
            _animationName = mv3Mesh.Name;
            _standardNoShadowShader = Shader.Find("Pal3/StandardNoShadow");
        }

        public void PlayAnimation(int loopCount = -1, float fps = -1f)
        {
            if (_keyFrames == null || _keyFrames.Length == 0)
            {
                throw new Exception("Animation not initialized.");
            }

            StopAnimation();

            if (_renderMeshComponent == null)
            {
                _frameTicks = _keyFrames.Select(f => f.Tick).ToArray();
                _meshObject = new GameObject(_animationName);

                // Attach BlendFlag and GameBoxMaterial to the GameObject for better debuggability
                #if UNITY_EDITOR
                var materialInfoPresenter = _meshObject.AddComponent<MaterialInfoPresenter>();
                materialInfoPresenter.blendFlag = (uint) (_textureHasAlphaChannel ? 1 : 0);
                materialInfoPresenter.material = _gbMaterial;
                #endif

                var meshRenderer = _meshObject.AddComponent<StaticMeshRenderer>();

                _material = new Material(_standardNoShadowShader);
                _material.SetTexture(_mainTexturePropertyId, _texture);

                var cutoff = _textureHasAlphaChannel ? 0.3f : 0f;
                if (cutoff > Mathf.Epsilon)
                {
                    _material.SetFloat(_cutoffPropertyId, cutoff);
                }

                _material.SetColor(_tintColorPropertyId, _tintColor);

                var meshDataBuffer = new MeshDataBuffer
                {
                    VertexBuffer = new Vector3[_keyFrames[0].Vertices.Length]
                };

                var normals = Array.Empty<Vector3>();

                var renderMesh = meshRenderer.Render(ref _keyFrames[0].Vertices,
                    ref _keyFrames[0].Triangles,
                    ref normals,
                    ref _keyFrames[0].Uv,
                    ref _material,
                    true);

                //renderMesh.RecalculateNormals();
                //renderMesh.RecalculateTangents();

                _renderMeshComponent = new RenderMeshComponent
                {
                    Mesh = renderMesh,
                    MeshRenderer = meshRenderer,
                    MeshDataBuffer = meshDataBuffer
                };

                _meshObject.transform.SetParent(transform, false);
            }

            if (_animation != null) StopCoroutine(_animation);

            _animationCts = new CancellationTokenSource();
            _animationDelay = fps <= 0 ? null : new WaitForSeconds(1 / fps);
            _animation = StartCoroutine(PlayAnimationInternal(loopCount,
                _animationDelay,
                _animationCts.Token));
        }

        public void ChangeTexture(string textureName)
        {
            if (!textureName.Contains(".")) textureName += MV3_MODEL_DEFAULT_TEXTURE_EXTENSION;
            _texture = _textureProvider.GetTexture(textureName);
            _material.SetTexture(_mainTexturePropertyId, _texture);
        }

        public void StopAnimation()
        {
            if (_animation != null)
            {
                _animationCts.Cancel();
                StopCoroutine(_animation);
                _animation = null;
            }
        }

        public Bounds GetBounds()
        {
            return _renderMeshComponent.MeshRenderer.GetRendererBounds();
        }

        public Bounds GetLocalBounds()
        {
            return _renderMeshComponent.MeshRenderer.GetMeshBounds();
        }

        public bool IsActionInHoldState()
        {
            return _isActionInHoldState;
        }

        public void ResumeAction()
        {
            StartCoroutine(ResumeActionInternal(_animationDelay));
        }

        private IEnumerator ResumeActionInternal(WaitForSeconds animationDelay)
        {
            if (!_isActionInHoldState) yield break;
            _animationCts = new CancellationTokenSource();
            yield return PlayOneTimeAnimationInternal(_actionHoldingTick, _duration, animationDelay, _animationCts.Token);
            _isActionInHoldState = false;
            _actionHoldingTick = 0;
            AnimationLoopPointReached?.Invoke(this, -2);
        }

        private IEnumerator PlayAnimationInternal(int loopCount,
            WaitForSeconds animationDelay,
            CancellationToken cancellationToken)
        {
            uint startTick = 0;

            if (loopCount == -1)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    yield return PlayOneTimeAnimationInternal(startTick, _duration, animationDelay, cancellationToken);
                    AnimationLoopPointReached?.Invoke(this, loopCount);
                }
            }
            else if (loopCount > 0)
            {
                while (!cancellationToken.IsCancellationRequested && --loopCount >= 0)
                {
                    yield return PlayOneTimeAnimationInternal(startTick, _duration, animationDelay, cancellationToken);
                    AnimationLoopPointReached?.Invoke(this, loopCount);
                }
            }
            else if (loopCount == -2)
            {
                _actionHoldingTick = _events.LastOrDefault(e =>
                    e.Name.Equals(MV3_ANIMATION_HOLD_EVENT_NAME, StringComparison.OrdinalIgnoreCase)).Tick;
                if (_actionHoldingTick != 0)
                {
                    yield return PlayOneTimeAnimationInternal(startTick, _actionHoldingTick, animationDelay, cancellationToken);
                    _isActionInHoldState = true;
                }
                else
                {
                    yield return PlayOneTimeAnimationInternal(startTick, _duration, animationDelay, cancellationToken);
                }
                AnimationLoopPointReached?.Invoke(this, loopCount);
            }
        }

        private IEnumerator PlayOneTimeAnimationInternal(uint startTick,
            uint endTick,
            WaitForSeconds animationDelay,
            CancellationToken cancellationToken)
        {
            var startTime = Time.timeSinceLevelLoad;
            var numOfFrames = _frameTicks.Length;

            while (!cancellationToken.IsCancellationRequested)
            {
                var tick = ((Time.timeSinceLevelLoad - startTime) * TIME_TO_TICK_SCALE
                                  + startTick);

                if (tick >= endTick)
                {
                    yield break;
                }

                if (_renderMeshComponent.MeshRenderer.IsVisible())
                {
                    var currentFrameIndex = Utility.GetFloorIndex(_frameTicks, (uint)tick);
                    var currentFrameTick = _frameTicks[currentFrameIndex];
                    var nextFrameIndex = currentFrameIndex < numOfFrames - 1 ? currentFrameIndex + 1 : 0;
                    var nextFrameTick = nextFrameIndex == 0 ? endTick : _frameTicks[nextFrameIndex];

                    var influence = (tick - currentFrameTick) / (nextFrameTick - currentFrameTick);

                    var vertices = _renderMeshComponent.MeshDataBuffer.VertexBuffer;
                    for (var i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = Vector3.Lerp(_keyFrames[currentFrameIndex].Vertices[i],
                            _keyFrames[nextFrameIndex].Vertices[i], influence);
                    }

                    _renderMeshComponent.Mesh.vertices = vertices;
                    _renderMeshComponent.Mesh.RecalculateBounds();
                }

                yield return animationDelay;
            }
        }

        private void OnDisable()
        {
            Dispose();
        }

        private void Dispose()
        {
            StopAnimation();

            if (_renderMeshComponent != null)
            {
                Destroy(_renderMeshComponent.Mesh);
                Destroy(_renderMeshComponent.MeshRenderer);
            }

            if (_meshObject != null)
            {
                Destroy(_meshObject);
            }
        }
    }
}