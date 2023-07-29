// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using System;
    using System.Collections;
    using System.Threading;
    using Core.DataLoader;
    using Core.DataReader.Mv3;
    using Core.DataReader.Pol;
    using Core.GameBox;
    using Core.Renderer;
    using Core.Utils;
    using Dev;
    using UnityEngine;

    /// <summary>
    /// MV3(.mv3) model renderer
    /// </summary>
    public class Mv3ModelRenderer : MonoBehaviour, IDisposable
    {
        public event EventHandler<int> AnimationLoopPointReached;

        private const string MV3_ANIMATION_HOLD_EVENT_NAME = "hold";
        private const string MV3_MODEL_DEFAULT_TEXTURE_EXTENSION = ".tga";

        private ITextureResourceProvider _textureProvider;
        private IMaterialFactory _materialFactory;
        private Texture2D[] _textures;
        private Material[][] _materials;
        private GameBoxMaterial[] _gbMaterials;
        private Mv3AnimationEvent[] _events;
        private bool[] _textureHasAlphaChannel;
        private GameObject[] _meshObjects;
        private Coroutine _animation;
        private Color _tintColor;
        private string[] _animationName;
        private CancellationTokenSource _animationCts;

        private bool _isActionInHoldState;
        private uint _actionHoldingTick;

        private Mv3Mesh[] _meshes;
        private int _meshCount;
        private uint[][] _frameTicks;
        private uint _duration;
        private RenderMeshComponent[] _renderMeshComponents;
        private WaitForSeconds _animationDelay;

        // Tag node
        private Mv3TagNode[] _tagNodesInfo;
        private uint[][] _tagNodeFrameTicks;
        private GameObject[] _tagNodes;

        public void Init(Mv3File mv3File,
            IMaterialFactory materialFactory,
            ITextureResourceProvider textureProvider,
            Color? tintColor = default,
            PolFile tagNodePolFile = default,
            ITextureResourceProvider tagNodeTextureProvider = default,
            Color? tagNodeTintColor = default)
        {
            Dispose();

            _materialFactory = materialFactory;
            _textureProvider = textureProvider;
            _tintColor = tintColor ?? Color.white;

            _events = mv3File.AnimationEvents;
            _duration = mv3File.Duration;
            _meshes = mv3File.Meshes;
            _meshCount = mv3File.Meshes.Length;

            _renderMeshComponents = new RenderMeshComponent[_meshCount];
            _gbMaterials = new GameBoxMaterial[_meshCount];
            _textures = new Texture2D[_meshCount];
            _textureHasAlphaChannel = new bool[_meshCount];
            _materials = new Material[_meshCount][];
            _animationName = new string[_meshCount];
            _frameTicks = new uint[_meshCount][];
            _meshObjects = new GameObject[_meshCount];
            _tagNodesInfo = mv3File.TagNodes;
            _tagNodeFrameTicks = new uint[_tagNodesInfo.Length][];

            for (var i = 0; i < _tagNodesInfo.Length; i++)
            {
                var tagFrames = _tagNodesInfo[i].TagFrames;
                int tagFramesCount = tagFrames.Length;
                var ticksArray = new uint[tagFramesCount];

                for (int j = 0; j < tagFramesCount; j++)
                {
                    ticksArray[j] = tagFrames[j].Tick;
                }

                _tagNodeFrameTicks[i] = ticksArray;
            }

            for (var i = 0; i < _meshCount; i++)
            {
                Mv3Mesh mesh = mv3File.Meshes[i];
                var materialId = mesh.Attributes[0].MaterialId;
                GameBoxMaterial material = mv3File.Materials[materialId];

                InitSubMeshes(i, ref mesh, ref material);
            }

            if (tagNodePolFile != null && _tagNodesInfo is {Length: > 0})
            {
                _tagNodes = new GameObject[_tagNodesInfo.Length];

                for (var i = 0; i < _tagNodesInfo.Length; i++)
                {
                    if (_tagNodesInfo[i].Name.Equals("tag_weapon3", StringComparison.OrdinalIgnoreCase))
                    {
                        _tagNodes[i] = null;
                        continue;
                    }

                    _tagNodes[i] = new GameObject(tagNodePolFile.NodeDescriptions[0].Name);
                    var tagNodeRenderer = _tagNodes[i].AddComponent<PolyModelRenderer>();
                    tagNodeRenderer.Render(tagNodePolFile,
                        tagNodeTextureProvider,
                        _materialFactory,
                        tagNodeTintColor);

                    _tagNodes[i].transform.SetParent(transform, true);
                    _tagNodes[i].transform.localPosition = mv3File.TagNodes[i].TagFrames[0].Position;
                    _tagNodes[i].transform.localRotation = mv3File.TagNodes[i].TagFrames[0].Rotation;
                }
            }
        }

        private void InitSubMeshes(int index,
            ref Mv3Mesh mv3Mesh,
            ref GameBoxMaterial material)
        {
            var textureName = material.TextureFileNames[0];

            _gbMaterials[index] = material;
            _textures[index] = _textureProvider.GetTexture(textureName, out var hasAlphaChannel);
            _textureHasAlphaChannel[index]= hasAlphaChannel;
            _animationName[index] = mv3Mesh.Name;

            var keyFrames = mv3Mesh.KeyFrames;
            int keyFramesCount = keyFrames.Length;
            var ticksArray = new uint[keyFramesCount];

            for (int i = 0; i < keyFramesCount; i++)
            {
                ticksArray[i] = keyFrames[i].Tick;
            }

            _frameTicks[index] = ticksArray;

            _meshObjects[index] = new GameObject(mv3Mesh.Name);

            // Attach BlendFlag and GameBoxMaterial to the GameObject for better debuggability
            #if UNITY_EDITOR
            var materialInfoPresenter = _meshObjects[index].AddComponent<MaterialInfoPresenter>();
            materialInfoPresenter.blendFlag = _textureHasAlphaChannel[index] ?
                GameBoxBlendFlag.AlphaBlend :
                GameBoxBlendFlag.Opaque;
            materialInfoPresenter.material = _gbMaterials[index];
            #endif

            var meshRenderer = _meshObjects[index].AddComponent<StaticMeshRenderer>();

            Material[] materials = _materialFactory.CreateStandardMaterials(
                RendererType.Mv3,
                (textureName, _textures[index]),
                shadowTexture: (null, null), // MV3 models don't have shadow textures
                _tintColor,
                _textureHasAlphaChannel[index] ? GameBoxBlendFlag.AlphaBlend : GameBoxBlendFlag.Opaque);

            _materials[index] = materials;

            #if PAL3A
            // Apply PAL3A texture scaling/tiling fix
            var texturePath = _textureProvider.GetTexturePath(textureName);
            if (Pal3AMv3TextureTilingIssue.KnownTextureFiles.Contains(texturePath))
            {
                _materials[index][0].mainTextureScale = new Vector2(1.0f, -1.0f);
            }
            #endif

            var meshDataBuffer = new MeshDataBuffer
            {
                VertexBuffer = new Vector3[mv3Mesh.KeyFrames[0].Vertices.Length],
                NormalBuffer = mv3Mesh.Normals,
            };

            Mesh renderMesh = meshRenderer.Render(ref mv3Mesh.KeyFrames[0].Vertices,
                ref mv3Mesh.Triangles,
                ref meshDataBuffer.NormalBuffer,
                ref mv3Mesh.Uvs,
                ref mv3Mesh.Uvs,
                ref _materials[index],
                true);

            renderMesh.RecalculateTangents();

            _renderMeshComponents[index] = new RenderMeshComponent
            {
                Mesh = renderMesh,
                MeshRenderer = meshRenderer,
                MeshDataBuffer = meshDataBuffer
            };

            _meshObjects[index].transform.SetParent(transform, false);
        }

        public void StartAnimation(int loopCount = -1, float fps = -1f)
        {
            if (_renderMeshComponents == null)
            {
                throw new Exception("Animation model not initialized.");
            }

            PauseAnimation();

            _animationCts = new CancellationTokenSource();
            _animationDelay = fps <= 0 ? null : new WaitForSeconds(1 / fps);
            _animation = StartCoroutine(PlayAnimationInternalAsync(loopCount,
                _animationDelay,
                _animationCts.Token));
        }

        public void ChangeTexture(string textureName)
        {
            if (!textureName.Contains(".")) textureName += MV3_MODEL_DEFAULT_TEXTURE_EXTENSION;

            _textures[0] = _textureProvider.GetTexture(textureName, out var hasAlphaChannel);
            _textureHasAlphaChannel[0] = hasAlphaChannel;

            // Change the texture for the first sub-mesh only
            _materialFactory.UpdateMaterial(_materials[0][0], _textures[0], _textureHasAlphaChannel[0] ?
                GameBoxBlendFlag.AlphaBlend :
                GameBoxBlendFlag.Opaque);
        }

        public void PauseAnimation()
        {
            if (_animation != null)
            {
                _animationCts.Cancel();
                StopCoroutine(_animation);
                _animation = null;
            }
        }

        public void Dispose()
        {
            PauseAnimation();

            if (_renderMeshComponents != null)
            {
                foreach (RenderMeshComponent renderMeshComponent in _renderMeshComponents)
                {
                    Destroy(renderMeshComponent.Mesh);
                    Destroy(renderMeshComponent.MeshRenderer);
                }

                _renderMeshComponents = null;
            }

            if (_meshObjects != null)
            {
                foreach (GameObject meshObject in _meshObjects)
                {
                    Destroy(meshObject);
                }

                _meshObjects = null;
            }

            if (_tagNodes != null)
            {
                foreach (GameObject tagNode in _tagNodes)
                {
                    Destroy(tagNode);
                }

                _tagNodes = null;
            }
        }

        public bool IsVisible()
        {
            return _meshObjects != null;
        }

        public Bounds GetRendererBounds()
        {
            if (_renderMeshComponents.Length == 0)
            {
                return new Bounds(transform.position, Vector3.one);
            }
            Bounds bounds = _renderMeshComponents[0].MeshRenderer.GetRendererBounds();
            for (var i = 1; i < _renderMeshComponents.Length; i++)
            {
                bounds.Encapsulate(_renderMeshComponents[i].MeshRenderer.GetRendererBounds());
            }
            return bounds;
        }

        public Bounds GetMeshBounds()
        {
            if (_renderMeshComponents.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }
            Bounds bounds = _renderMeshComponents[0].MeshRenderer.GetMeshBounds();
            for (var i = 1; i < _renderMeshComponents.Length; i++)
            {
                bounds.Encapsulate(_renderMeshComponents[i].MeshRenderer.GetMeshBounds());
            }
            return bounds;
        }

        public bool IsActionInHoldState()
        {
            return _isActionInHoldState;
        }

        public void ResumeAction()
        {
            StartCoroutine(ResumeActionInternalAsync(_animationDelay));
        }

        private IEnumerator ResumeActionInternalAsync(WaitForSeconds animationDelay)
        {
            if (!_isActionInHoldState) yield break;
            _animationCts = new CancellationTokenSource();
            yield return PlayOneTimeAnimationInternalAsync(_actionHoldingTick, _duration, animationDelay, _animationCts.Token);
            _isActionInHoldState = false;
            _actionHoldingTick = 0;
            AnimationLoopPointReached?.Invoke(this, -2);
        }

        private IEnumerator PlayAnimationInternalAsync(int loopCount,
            WaitForSeconds animationDelay,
            CancellationToken cancellationToken)
        {
            uint startTick = 0;

            if (loopCount == -1) // Infinite loop until cancelled
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    yield return PlayOneTimeAnimationInternalAsync(startTick, _duration, animationDelay, cancellationToken);
                    AnimationLoopPointReached?.Invoke(this, loopCount);
                }
            }
            else if (loopCount > 0)
            {
                while (!cancellationToken.IsCancellationRequested && --loopCount >= 0)
                {
                    yield return PlayOneTimeAnimationInternalAsync(startTick, _duration, animationDelay, cancellationToken);
                    AnimationLoopPointReached?.Invoke(this, loopCount);
                }
            }
            else if (loopCount == -2) // Play until action holding point
            {
                if (TryGetLastHoldingTick(out uint holdingTick))
                {
                    _actionHoldingTick = holdingTick;
                    yield return PlayOneTimeAnimationInternalAsync(startTick, _actionHoldingTick, animationDelay, cancellationToken);
                    _isActionInHoldState = true;
                }
                else
                {
                    yield return PlayOneTimeAnimationInternalAsync(startTick, _duration, animationDelay, cancellationToken);
                }
                AnimationLoopPointReached?.Invoke(this, loopCount);
            }
        }

        private bool TryGetLastHoldingTick(out uint holdingTick)
        {
            for (int i = _events.Length - 1; i >= 0; i--)
            {
                var currentEvent = _events[i];
                if (currentEvent.Name.Equals(MV3_ANIMATION_HOLD_EVENT_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    holdingTick = currentEvent.Tick;
                    return true;
                }
            }

            holdingTick = 0;
            return false;
        }

        private IEnumerator PlayOneTimeAnimationInternalAsync(uint startTick,
            uint endTick,
            WaitForSeconds animationDelay,
            CancellationToken cancellationToken)
        {
            var startTime = Time.timeSinceLevelLoad;

            while (!cancellationToken.IsCancellationRequested)
            {
                uint tick = GameBoxInterpreter.SecondsToTick(Time.timeSinceLevelLoad - startTime) + startTick;

                if (tick >= endTick)
                {
                    yield break;
                }

                for (var i = 0; i < _meshCount; i++)
                {
                    RenderMeshComponent meshComponent = _renderMeshComponents[i];
                    if (!meshComponent.MeshRenderer.IsVisible()) continue;

                    var frameTicks = _frameTicks[i];

                    var currentFrameIndex = Utility.GetFloorIndex(frameTicks, tick);
                    var currentFrameTick = _frameTicks[i][currentFrameIndex];
                    var nextFrameIndex = currentFrameIndex < frameTicks.Length - 1 ? currentFrameIndex + 1 : 0;
                    var nextFrameTick = nextFrameIndex == 0 ? endTick : _frameTicks[i][nextFrameIndex];

                    var influence = (float)(tick - currentFrameTick) / (nextFrameTick - currentFrameTick);

                    var vertices = meshComponent.MeshDataBuffer.VertexBuffer;
                    for (var j = 0; j < vertices.Length; j++)
                    {
                        vertices[j] = Vector3.Lerp(_meshes[i].KeyFrames[currentFrameIndex].Vertices[j],
                            _meshes[i].KeyFrames[nextFrameIndex].Vertices[j], influence);
                    }

                    meshComponent.Mesh.SetVertices(vertices);
                    meshComponent.Mesh.RecalculateBounds();
                }

                if (_tagNodes != null)
                {
                    for (var i = 0; i < _tagNodes.Length; i++)
                    {
                        if (_tagNodes[i] == null) continue;

                        var frameTicks = _tagNodeFrameTicks[i];

                        var currentFrameIndex = Utility.GetFloorIndex(frameTicks, (uint) tick);
                        var currentFrameTick = _tagNodeFrameTicks[i][currentFrameIndex];
                        var nextFrameIndex = currentFrameIndex < frameTicks.Length - 1 ? currentFrameIndex + 1 : 0;
                        var nextFrameTick = nextFrameIndex == 0 ? endTick : _tagNodeFrameTicks[i][nextFrameIndex];

                        var influence = (float)(tick - currentFrameTick) / (nextFrameTick - currentFrameTick);

                        Vector3 position = Vector3.Lerp(_tagNodesInfo[i].TagFrames[currentFrameIndex].Position,
                            _tagNodesInfo[i].TagFrames[nextFrameIndex].Position, influence);
                        Quaternion rotation = Quaternion.Lerp(_tagNodesInfo[i].TagFrames[currentFrameIndex].Rotation,
                            _tagNodesInfo[i].TagFrames[nextFrameIndex].Rotation, influence);

                        _tagNodes[i].transform.localPosition = position;
                        _tagNodes[i].transform.localRotation = rotation;
                    }
                }

                yield return animationDelay;
            }
        }

        private void OnDisable()
        {
            Dispose();
        }
    }
}