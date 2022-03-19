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

                _material = new Material(Shader.Find("Pal3/StandardNoShadow"));
                _material.SetTexture(Shader.PropertyToID("_MainTex"), _texture);

                var cutoff = _textureHasAlphaChannel ? 0.3f : 0f;
                if (cutoff > Mathf.Epsilon)
                {
                    _material.SetFloat(Shader.PropertyToID("_Cutoff"), cutoff);
                }

                _material.SetColor(Shader.PropertyToID("_TintColor"), _tintColor);

                var meshDataBuffer = new MeshDataBuffer
                {
                    VertexBuffer = new Vector3[_keyFrames[0].Vertices.Length]
                };

                var renderMesh = meshRenderer.Render( _keyFrames[0].Vertices,
                    _keyFrames[0].Triangles,
                    Array.Empty<Vector3>(),
                    _keyFrames[0].Uv,
                    _material,
                    true);

                meshRenderer.RecalculateBoundsNormalsAndTangents();

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
            _material.SetTexture(Shader.PropertyToID("_MainTex"), _texture);
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

                    var meshDataBuffer = _renderMeshComponent.MeshDataBuffer;
                    for (var i = 0; i < meshDataBuffer.VertexBuffer.Length; i++)
                    {
                        meshDataBuffer.VertexBuffer[i] = Vector3.Lerp(_keyFrames[currentFrameIndex].Vertices[i],
                            _keyFrames[nextFrameIndex].Vertices[i], influence);
                    }

                    _renderMeshComponent.Mesh.vertices = meshDataBuffer.VertexBuffer;
                    //_meshRenderer.RecalculateBoundsNormalsAndTangents();
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