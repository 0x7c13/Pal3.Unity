// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Renderer
{
    using System;
    using System.Collections;
    using System.Threading;
    using Core.DataReader.Mv3;
    using Core.DataReader.Pol;
    using Core.Primitives;
    using Core.Utilities;
    using Dev;
    using Dev.Presenters;
    using Engine.Abstraction;
    using Engine.DataLoader;
    using Engine.Extensions;
    using Engine.Logging;
    using Engine.Renderer;
    using Engine.Services;
    using Material;
    using Rendering;
    using UnityEngine;
    using Color = Core.Primitives.Color;

    /// <summary>
    /// MV3(.mv3) model renderer
    /// </summary>
    public sealed class Mv3ModelRenderer : GameEntityScript, IDisposable
    {
        public event EventHandler<int> AnimationLoopPointReached;

        private const string MV3_ANIMATION_HOLD_EVENT_NAME = "hold";
        private const string MV3_MODEL_DEFAULT_TEXTURE_EXTENSION = ".tga";

        private ITextureResourceProvider _textureProvider;
        private IMaterialFactory _materialFactory;
        private Color _tintColor;

        private Mv3AnimationEvent[] _events;
        private uint _totalGameBoxTicks;
        private Mv3Mesh[] _meshes;
        private int _meshCount;

        // Mv3 mesh data
        private RenderMeshComponent[] _renderMeshComponents;
        private GameBoxMaterial[] _gbMaterials;
        private Texture2D[] _textures;
        private bool[] _textureHasAlphaChannel;
        private Material[][] _materials;
        private string[] _animationName;
        private uint[][] _frameTicks;
        private IGameEntity[] _meshEntities;

        // Tag node
        private Mv3TagNode[] _tagNodesInfo;
        private uint[][] _tagNodeFrameTicks;
        private IGameEntity[] _tagNodes;

        // State
        public bool IsInitialized { get; private set; }
        private bool _isActionInHoldState;
        private uint _actionHoldingTick;
        private CancellationTokenSource _animationCts;

        protected override void OnEnableGameEntity()
        {
        }

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

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
            _tintColor = tintColor ?? Color.White;

            _events = mv3File.AnimationEvents;
            _totalGameBoxTicks = mv3File.TotalGameBoxTicks;
            _meshes = mv3File.Meshes;
            _meshCount = mv3File.Meshes.Length;
            _tagNodesInfo = mv3File.TagNodes;

            // Only allocate memory when needed, reuse memory when possible
            if (_renderMeshComponents == null || _renderMeshComponents.Length != _meshCount)
            {
                _renderMeshComponents = new RenderMeshComponent[_meshCount];
                _gbMaterials = new GameBoxMaterial[_meshCount];
                _textures = new Texture2D[_meshCount];
                _textureHasAlphaChannel = new bool[_meshCount];
                _materials = new Material[_meshCount][];
                _animationName = new string[_meshCount];
                _frameTicks = new uint[_meshCount][];
                _meshEntities = new IGameEntity[_meshCount];
            }

            // Same for tag nodes
            if (_tagNodesInfo.Length > 0)
            {
                if (_tagNodeFrameTicks == null || _tagNodeFrameTicks.Length != _tagNodesInfo.Length)
                {
                    _tagNodeFrameTicks = new uint[_tagNodesInfo.Length][];
                }

                if (_tagNodes == null || _tagNodes.Length != _tagNodesInfo.Length)
                {
                    _tagNodes = new IGameEntity[_tagNodesInfo.Length];
                }
            }

            for (var i = 0; i < _tagNodesInfo.Length; i++)
            {
                var tagFrames = _tagNodesInfo[i].TagFrames;
                int tagFramesCount = tagFrames.Length;

                if (_tagNodeFrameTicks[i] == null || _tagNodeFrameTicks[i].Length != tagFramesCount)
                {
                    _tagNodeFrameTicks[i] = new uint[tagFramesCount];
                }

                for (int j = 0; j < tagFramesCount; j++)
                {
                    _tagNodeFrameTicks[i][j] = tagFrames[j].GameBoxTick;
                }
            }

            for (var i = 0; i < _meshCount; i++)
            {
                Mv3Mesh mesh = mv3File.Meshes[i];
                var materialId = mesh.Attributes[0].MaterialId;
                GameBoxMaterial material = mv3File.Materials[materialId];

                InitSubMeshes(i, ref mesh, ref material);
            }

            if (tagNodePolFile != null)
            {
                for (var i = 0; i < _tagNodesInfo.Length; i++)
                {
                    if (_tagNodesInfo[i].Name.Equals("tag_weapon3", StringComparison.OrdinalIgnoreCase))
                    {
                        _tagNodes[i] = null;
                        continue;
                    }

                    _tagNodes[i] = new GameEntity(tagNodePolFile.NodeDescriptions[0].Name);
                    var tagNodeRenderer = _tagNodes[i].AddComponent<PolyModelRenderer>();
                    tagNodeRenderer.Render(tagNodePolFile,
                        tagNodeTextureProvider,
                        _materialFactory,
                        isStaticObject: false,
                        tagNodeTintColor);

                    _tagNodes[i].SetParent(GameEntity, worldPositionStays: true);
                    _tagNodes[i].Transform.SetLocalPositionAndRotation(
                        mv3File.TagNodes[i].TagFrames[0].GameBoxPosition.ToUnityPosition(UnityPrimitivesConvertor.GameBoxMv3UnitToUnityUnit),
                        mv3File.TagNodes[i].TagFrames[0].GameBoxRotation.Mv3QuaternionToUnityQuaternion());
                }
            }

            IsInitialized = true;
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

            if (_frameTicks[index] == null || _frameTicks[index].Length != keyFramesCount)
            {
                _frameTicks[index] = new uint[keyFramesCount];
            }

            for (int i = 0; i < keyFramesCount; i++)
            {
                _frameTicks[index][i] = keyFrames[i].GameBoxTick;
            }

            _meshEntities[index] = new GameEntity(mv3Mesh.Name);

            // Attach BlendFlag and GameBoxMaterial to the GameEntity for better debuggability
            #if UNITY_EDITOR
            var materialInfoPresenter = _meshEntities[index].AddComponent<MaterialInfoPresenter>();
            materialInfoPresenter.blendFlag = _textureHasAlphaChannel[index] ?
                GameBoxBlendFlag.AlphaBlend :
                GameBoxBlendFlag.Opaque;
            materialInfoPresenter.material = _gbMaterials[index];
            #endif

            var meshRenderer = _meshEntities[index].AddComponent<StaticMeshRenderer>();

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
            if (TexturePatcher.TextureFileHasWrongTiling(texturePath))
            {
                _materials[index][0].mainTextureScale = new Vector2(1.0f, -1.0f);
            }
            #endif

            _renderMeshComponents[index] ??= new RenderMeshComponent();
            _renderMeshComponents[index].MeshDataBuffer ??= new MeshDataBuffer();

            if (_renderMeshComponents[index].MeshDataBuffer.VertexBuffer == null ||
                _renderMeshComponents[index].MeshDataBuffer.VertexBuffer.Length !=
                mv3Mesh.KeyFrames[0].GameBoxVertices.Length)
            {
                _renderMeshComponents[index].MeshDataBuffer.VertexBuffer = new Vector3[mv3Mesh.KeyFrames[0].GameBoxVertices.Length];
            }

            Mesh renderMesh = meshRenderer.Render(
                mv3Mesh.KeyFrames[0].GameBoxVertices.ToUnityPositions(UnityPrimitivesConvertor.GameBoxMv3UnitToUnityUnit),
                mv3Mesh.GameBoxTriangles.ToUnityTriangles(),
                mv3Mesh.GameBoxNormals.ToUnityNormals(),
                mv3Mesh.Uvs.ToUnityVector2s(),
                mv3Mesh.Uvs.ToUnityVector2s(),
                _materials[index],
                true);

            renderMesh.RecalculateTangents();

            _renderMeshComponents[index].Mesh = renderMesh;
            _renderMeshComponents[index].MeshRenderer = meshRenderer;

            _meshEntities[index].SetParent(GameEntity, worldPositionStays: false);
        }

        public void StartAnimation(int loopCount = -1)
        {
            if (!IsInitialized)
            {
                throw new Exception("Animation model not initialized");
            }

            PauseAnimation();

            _animationCts = new CancellationTokenSource();
            StartCoroutine(PlayAnimationInternalAsync(loopCount, _animationCts.Token));
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
            if (_animationCts is {IsCancellationRequested: false})
            {
                _animationCts.Cancel();
            }
        }

        public Bounds GetRendererBounds()
        {
            if (_renderMeshComponents.Length == 0)
            {
                return new Bounds(Transform.Position, Vector3.one);
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
            StartCoroutine(ResumeActionInternalAsync());
        }

        private IEnumerator ResumeActionInternalAsync()
        {
            if (!_isActionInHoldState) yield break;
            _animationCts = new CancellationTokenSource();
            yield return PlayOneTimeAnimationInternalAsync(_actionHoldingTick,
                _totalGameBoxTicks,
                _animationCts.Token);
            _isActionInHoldState = false;
            _actionHoldingTick = 0;
            AnimationLoopPointReached?.Invoke(this, -2);
        }

        private IEnumerator PlayAnimationInternalAsync(int loopCount,
            CancellationToken cancellationToken)
        {
            uint startTick = 0;

            if (loopCount == -1) // Infinite loop until cancelled
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    yield return PlayOneTimeAnimationInternalAsync(startTick, _totalGameBoxTicks, cancellationToken);
                    AnimationLoopPointReached?.Invoke(this, loopCount);
                }
            }
            else if (loopCount > 0)
            {
                while (!cancellationToken.IsCancellationRequested && --loopCount >= 0)
                {
                    yield return PlayOneTimeAnimationInternalAsync(startTick, _totalGameBoxTicks, cancellationToken);
                    AnimationLoopPointReached?.Invoke(this, loopCount);
                }
            }
            else if (loopCount == -2) // Play until action holding point
            {
                if (TryGetLastHoldingTick(out uint holdingTick))
                {
                    _actionHoldingTick = holdingTick;
                    yield return PlayOneTimeAnimationInternalAsync(startTick, _actionHoldingTick, cancellationToken);
                    _isActionInHoldState = true;
                }
                else
                {
                    yield return PlayOneTimeAnimationInternalAsync(startTick, _totalGameBoxTicks, cancellationToken);
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
                    holdingTick = currentEvent.GameBoxTick;
                    return true;
                }
            }

            holdingTick = 0;
            return false;
        }

        private IEnumerator PlayOneTimeAnimationInternalAsync(uint startTick,
            uint endTick,
            CancellationToken cancellationToken)
        {
            var startTime = GameTimeProvider.Instance.TimeSinceStartup;

            while (!cancellationToken.IsCancellationRequested)
            {
                uint tick = ((float)(GameTimeProvider.Instance.TimeSinceStartup - startTime)).SecondsToGameBoxTick() + startTick;

                if (tick >= endTick)
                {
                    yield break;
                }

                if (!IsVisibleToCamera())
                {
                    yield return null;
                    continue;
                }

                // Animates the mesh
                for (var i = 0; i < _meshCount; i++)
                {
                    RenderMeshComponent meshComponent = _renderMeshComponents[i];

                    var frameTicks = _frameTicks[i];

                    var currentFrameIndex = CoreUtility.GetFloorIndex(frameTicks, tick);
                    var currentFrameTick = _frameTicks[i][currentFrameIndex];
                    var nextFrameIndex = currentFrameIndex < frameTicks.Length - 1 ? currentFrameIndex + 1 : 0;
                    var nextFrameTick = nextFrameIndex == 0 ? endTick : _frameTicks[i][nextFrameIndex];

                    var influence = (float)(tick - currentFrameTick) / (nextFrameTick - currentFrameTick);

                    var vertices = meshComponent.MeshDataBuffer.VertexBuffer;
                    for (var j = 0; j < vertices.Length; j++)
                    {
                        vertices[j] = Vector3.Lerp(
                            _meshes[i].KeyFrames[currentFrameIndex].GameBoxVertices[j]
                                .ToUnityPosition(UnityPrimitivesConvertor.GameBoxMv3UnitToUnityUnit),
                            _meshes[i].KeyFrames[nextFrameIndex].GameBoxVertices[j]
                                .ToUnityPosition(UnityPrimitivesConvertor.GameBoxMv3UnitToUnityUnit), influence);
                    }

                    meshComponent.Mesh.SetVertices(vertices);
                    meshComponent.Mesh.RecalculateBounds();
                }

                // Animates the tag nodes
                for (var i = 0; i < _tagNodesInfo.Length; i++)
                {
                    if (_tagNodes[i] == null) continue;

                    var frameTicks = _tagNodeFrameTicks[i];

                    var currentFrameIndex = CoreUtility.GetFloorIndex(frameTicks, tick);
                    var currentFrameTick = _tagNodeFrameTicks[i][currentFrameIndex];
                    var nextFrameIndex = currentFrameIndex < frameTicks.Length - 1 ? currentFrameIndex + 1 : 0;
                    var nextFrameTick = nextFrameIndex == 0 ? endTick : _tagNodeFrameTicks[i][nextFrameIndex];

                    var influence = (float)(tick - currentFrameTick) / (nextFrameTick - currentFrameTick);

                    Vector3 position = Vector3.Lerp(
                        _tagNodesInfo[i].TagFrames[currentFrameIndex].GameBoxPosition.ToUnityPosition(),
                        _tagNodesInfo[i].TagFrames[nextFrameIndex].GameBoxPosition.ToUnityPosition(), influence);
                    Quaternion rotation = Quaternion.Slerp(
                        _tagNodesInfo[i].TagFrames[currentFrameIndex].GameBoxRotation.Mv3QuaternionToUnityQuaternion(),
                        _tagNodesInfo[i].TagFrames[nextFrameIndex].GameBoxRotation.Mv3QuaternionToUnityQuaternion(), influence);

                    _tagNodes[i].Transform.SetLocalPositionAndRotation(position, rotation);
                }

                yield return null;
            }
        }

        public bool IsVisibleToCamera()
        {
            if (!IsInitialized) return false;

            foreach (RenderMeshComponent renderMeshComponent in _renderMeshComponents)
            {
                if (renderMeshComponent.MeshRenderer.IsVisible) return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (!IsInitialized) return;

            IsInitialized = false;

            PauseAnimation();

            if (_renderMeshComponents != null)
            {
                foreach (RenderMeshComponent renderMeshComponent in _renderMeshComponents)
                {
                    var materials = renderMeshComponent.MeshRenderer.GetMaterials();
                    if (materials != null)
                    {
                        #if PAL3A
                        // Revert tiling fix before returning to pool
                        foreach (Material material in materials)
                        {
                            material.mainTextureScale = Vector2.one;
                        }
                        #endif
                        _materialFactory.ReturnToPool(materials);
                    }
                    renderMeshComponent.Mesh.Destroy();
                    renderMeshComponent.MeshRenderer.Dispose();
                    renderMeshComponent.MeshRenderer.Destroy();
                }
            }

            if (_meshEntities != null)
            {
                foreach (IGameEntity meshEntity in _meshEntities)
                {
                    meshEntity?.Destroy();
                }
            }

            if (_tagNodes != null)
            {
                foreach (IGameEntity tagNode in _tagNodes)
                {
                    tagNode?.Destroy();
                }
            }

            _isActionInHoldState = false;
            _actionHoldingTick = 0;
        }
    }
}