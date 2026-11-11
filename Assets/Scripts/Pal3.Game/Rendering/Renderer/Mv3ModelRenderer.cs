// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
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
    using Dev.Presenters;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Extensions;
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
        public event EventHandler<string> AnimationEventTriggered;

        private const string MV3_ANIMATION_HOLD_EVENT_NAME = "hold";
        private const string MV3_MODEL_DEFAULT_TEXTURE_EXTENSION = ".tga";

        private ITextureResourceProvider _textureProvider;
        private IMaterialManager _materialManager;
        private Color _tintColor;

        private Mv3AnimationEvent[] _events;
        private uint _totalGameBoxTicks;
        private Mv3Mesh[] _meshes;
        private int _meshCount;

        // Mv3 mesh data
        private RenderMeshComponent[] _renderMeshComponents;
        private GameBoxMaterial[] _gbMaterials;
        private ITexture2D[] _textures;
        private bool[] _textureHasAlphaChannel;
        private IMaterial[][] _materials;
        private Vector3[][] _sourceNormals;
        private string[] _animationName;
        private uint[][] _frameTicks;
        private IGameEntity[] _meshEntities;

        // Tag node
        private PolFile _tagNodePolFile;
        private Mv3TagNode[] _tagNodesInfo;
        private uint[][] _tagNodeFrameTicks;
        private IGameEntity[] _tagNodes;

        // State
        public bool IsInitialized { get; private set; }
        private bool _isActionInHoldState;
        private uint _actionHoldingTick;
        private int _lastCheckedTick = -1; // Track the last checked tick
        private CancellationTokenSource _animationCts;

        protected override void OnEnableGameEntity()
        {
        }

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

        public void Init(Mv3File mv3File,
            IMaterialManager materialManager,
            ITextureResourceProvider textureProvider,
            Color? tintColor = default,
            PolFile tagNodePolFile = default,
            ITextureResourceProvider tagNodeTextureProvider = default,
            Color? tagNodeTintColor = default)
        {
            Dispose();

            _materialManager = materialManager;
            _textureProvider = textureProvider;
            _tintColor = tintColor ?? Color.White;
            _tagNodePolFile = tagNodePolFile;

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
                _textures = new ITexture2D[_meshCount];
                _textureHasAlphaChannel = new bool[_meshCount];
                _materials = new IMaterial[_meshCount][];
                _animationName = new string[_meshCount];
                _frameTicks = new uint[_meshCount][];
                _meshEntities = new IGameEntity[_meshCount];
            }

            if (_materialManager.UsesToonRendering && (_sourceNormals == null || _sourceNormals.Length != _meshCount))
            {
                _sourceNormals = new Vector3[_meshCount][];
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

            for (int i = 0; i < _tagNodesInfo.Length; i++)
            {
                Mv3TagFrame[] tagFrames = _tagNodesInfo[i].TagFrames;
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

            for (int i = 0; i < _meshCount; i++)
            {
                Mv3Mesh mesh = mv3File.Meshes[i];
                int materialId = mesh.Attributes[0].MaterialId;
                GameBoxMaterial material = mv3File.Materials[materialId];

                InitSubMeshes(i, ref mesh, ref material);
            }

            if (_tagNodePolFile != null)
            {
                for (int i = 0; i < _tagNodesInfo.Length; i++)
                {
                    if (_tagNodesInfo[i].Name.Equals("tag_weapon3", StringComparison.OrdinalIgnoreCase))
                    {
                        _tagNodes[i] = null;
                        continue;
                    }

                    _tagNodes[i] = GameEntityFactory.Create(_tagNodePolFile.NodeDescriptions[0].Name,
                        GameEntity, worldPositionStays: true);

                    PolyModelRenderer tagNodeRenderer = _tagNodes[i].AddComponent<PolyModelRenderer>();
                    tagNodeRenderer.Render(_tagNodePolFile,
                        tagNodeTextureProvider,
                        _materialManager,
                        isStaticObject: false,
                        tagNodeTintColor);

                    _tagNodes[i].Transform.SetLocalPositionAndRotation(
                        mv3File.TagNodes[i].TagFrames[0].GameBoxPosition
                            .ToUnityPosition(UnityPrimitivesConvertor.GameBoxMv3UnitToUnityUnit),
                        mv3File.TagNodes[i].TagFrames[0].GameBoxRotation
                            .Mv3QuaternionToUnityQuaternion());
                }
            }

            IsInitialized = true;
        }

        private void InitSubMeshes(int index,
            ref Mv3Mesh mv3Mesh,
            ref GameBoxMaterial material)
        {
            string textureName = material.TextureFileNames[0];

            _gbMaterials[index] = material;
            _textures[index] = _textureProvider.GetTexture(textureName, out bool hasAlphaChannel);
            _textureHasAlphaChannel[index] = hasAlphaChannel;
            _animationName[index] = mv3Mesh.Name;

            Mv3AnimationKeyFrame[] keyFrames = mv3Mesh.KeyFrames;
            int keyFramesCount = keyFrames.Length;

            if (_frameTicks[index] == null || _frameTicks[index].Length != keyFramesCount)
            {
                _frameTicks[index] = new uint[keyFramesCount];
            }

            for (int i = 0; i < keyFramesCount; i++)
            {
                _frameTicks[index][i] = keyFrames[i].GameBoxTick;
            }

            _meshEntities[index] = GameEntityFactory.Create(mv3Mesh.Name,
                GameEntity, worldPositionStays: false);

            // Attach BlendFlag and GameBoxMaterial to the GameEntity for better debuggability
            #if UNITY_EDITOR
            MaterialInfoPresenter materialInfoPresenter = _meshEntities[index].AddComponent<MaterialInfoPresenter>();
            materialInfoPresenter.blendFlag = _textureHasAlphaChannel[index] ?
                GameBoxBlendFlag.AlphaBlend :
                GameBoxBlendFlag.Opaque;
            materialInfoPresenter.material = _gbMaterials[index];
            #endif

            StaticMeshRenderer meshRenderer = _meshEntities[index].AddComponent<StaticMeshRenderer>();

            _materials[index] = CreateMaterials(textureName, _textures[index], hasAlphaChannel);

            _renderMeshComponents[index] ??= new RenderMeshComponent();
            _renderMeshComponents[index].MeshDataBuffer ??= new MeshDataBuffer();

            MeshDataBuffer buffer = _renderMeshComponents[index].MeshDataBuffer;
            bool usesToon = _materialManager.UsesToonRendering;
            int vertexCount = mv3Mesh.KeyFrames[0].GameBoxVertices.Length;
            buffer.AllocateOrResizeBuffers(vertexBufferSize: vertexCount,
                    normalBufferSize: usesToon ? vertexCount : 0,
                    uvBufferSize: mv3Mesh.Uvs.Length,
                    triangleBufferSize: mv3Mesh.GameBoxTriangles.Length);

            mv3Mesh.KeyFrames[0].GameBoxVertices.ToUnityPositions(
                buffer.VertexBuffer, UnityPrimitivesConvertor.GameBoxMv3UnitToUnityUnit);
            mv3Mesh.GameBoxTriangles.ToUnityTriangles(buffer.TriangleBuffer);
            mv3Mesh.Uvs.ToUnityVector2s(buffer.UvBuffer);

            if (usesToon)
            {
                if (_sourceNormals[index]?.Length != mv3Mesh.NormalCount)
                {
                    _sourceNormals[index] = new Vector3[mv3Mesh.NormalCount];
                }
                buffer.RebuildNormalsBySourceVertex(mv3Mesh.NormalIndices, _sourceNormals[index]);
            }

            Mesh renderMesh = meshRenderer.Render(
                vertices: buffer.VertexBuffer,
                triangles: buffer.TriangleBuffer,
                normals: usesToon ? buffer.NormalBuffer : null,
                mainTextureUvs: (channel: 0, uvs: buffer.UvBuffer),
                secondaryTextureUvs: default, // MV3 models don't have secondary texture
                materials: _materials[index],
                isDynamic: true);

            _renderMeshComponents[index].Mesh = renderMesh;
            _renderMeshComponents[index].MeshRenderer = meshRenderer;
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

        private IMaterial[] CreateMaterials(string textureName, ITexture2D texture, bool hasAlphaChannel)
        {
            IMaterial[] materials = _materialManager.CreateStandardMaterials(
                RendererType.Mv3,
                mainTexture: (textureName, texture),
                bakedLightmap: default,
                tintColor: _tintColor,
                blendFlag: hasAlphaChannel ? GameBoxBlendFlag.AlphaBlend : GameBoxBlendFlag.Opaque);
            ApplyTextureScale(materials, textureName);
            return materials;
        }

        private void ApplyTextureScale(IMaterial[] materials, string textureName)
        {
            #if PAL3A
            string texturePath = _textureProvider.GetTexturePath(textureName);
            float scaleY = Dev.TexturePatcher.TextureFileHasWrongTiling(texturePath) ? -1f : 1f;
            foreach (IMaterial material in materials)
            {
                material.SetMainTextureScale(1f, scaleY);
            }
            #endif
        }

        public void ChangeTexture(string textureName)
        {
            if (!IsInitialized || _meshCount == 0) return;
            if (!textureName.Contains(".")) textureName += MV3_MODEL_DEFAULT_TEXTURE_EXTENSION;

            ITexture2D texture = _textureProvider.GetTexture(textureName, out bool hasAlphaChannel);
            if (texture == null) return;

            // ActorChangeTexture targets the first sub-mesh and all of its material passes.
            IMaterial[] materials = _materials[0];
            if (_textureHasAlphaChannel[0] != hasAlphaChannel)
            {
                IMaterial[] replacement = CreateMaterials(textureName, texture, hasAlphaChannel);
                _renderMeshComponents[0].MeshRenderer.SetMaterials(replacement);
                _materials[0] = replacement;
                ReturnMaterials(materials);
            }
            else
            {
                foreach (IMaterial material in materials)
                {
                    material.SetMainTexture(texture);
                }
                ApplyTextureScale(materials, textureName);
            }

            _textures[0] = texture;
            _textureHasAlphaChannel[0] = hasAlphaChannel;
        }

        private void ReturnMaterials(IMaterial[] materials)
        {
            #if PAL3A
            foreach (IMaterial material in materials)
            {
                material.SetMainTextureScale(1f, 1f);
            }
            #endif

            _materialManager.ReturnToPool(materials);
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
            for (int i = 1; i < _renderMeshComponents.Length; i++)
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
            for (int i = 1; i < _renderMeshComponents.Length; i++)
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
            CancellationToken cancellationToken = _animationCts.Token;
            yield return PlayOneTimeAnimationInternalAsync(_actionHoldingTick,
                _totalGameBoxTicks, cancellationToken);
            _isActionInHoldState = false;
            _actionHoldingTick = 0;

            if (!cancellationToken.IsCancellationRequested)
            {
                AnimationLoopPointReached?.Invoke(this, -2);
            }
        }

        private IEnumerator PlayAnimationInternalAsync(int loopCount,
            CancellationToken cancellationToken)
        {
            const uint startTick = 0;

            switch (loopCount)
            {
                // Infinite loop until cancelled
                case -1:
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        yield return PlayOneTimeAnimationInternalAsync(startTick, _totalGameBoxTicks, cancellationToken);
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            AnimationLoopPointReached?.Invoke(this, loopCount);
                        }
                    }

                    break;
                }
                // Finite loop
                case > 0:
                {
                    while (!cancellationToken.IsCancellationRequested && --loopCount >= 0)
                    {
                        yield return PlayOneTimeAnimationInternalAsync(startTick, _totalGameBoxTicks, cancellationToken);
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            AnimationLoopPointReached?.Invoke(this, loopCount);
                        }
                    }

                    break;
                }
                // Play until action holding point
                case -2:
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

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        AnimationLoopPointReached?.Invoke(this, loopCount);
                    }

                    break;
                }
            }
        }

        private bool TryGetLastHoldingTick(out uint holdingTick)
        {
            for (int i = _events.Length - 1; i >= 0; i--)
            {
                Mv3AnimationEvent currentEvent = _events[i];
                if (currentEvent.Name.Equals(MV3_ANIMATION_HOLD_EVENT_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    holdingTick = currentEvent.GameBoxTick;
                    return true;
                }
            }

            holdingTick = 0;
            return false;
        }

        private void CheckForAnimationEvents(uint currentTick)
        {
            foreach (Mv3AnimationEvent animationEvent in _events)
            {
                if (animationEvent.GameBoxTick > _lastCheckedTick && animationEvent.GameBoxTick <= currentTick)
                {
                    AnimationEventTriggered?.Invoke(this, animationEvent.Name);
                    break;
                }
            }
            
            _lastCheckedTick = (int)currentTick;
        }

        private IEnumerator PlayOneTimeAnimationInternalAsync(uint startTick,
            uint endTick,
            CancellationToken cancellationToken)
        {
            double startTime = GameTimeProvider.Instance.TimeSinceStartup;
            _lastCheckedTick = -1;

            CheckForAnimationEvents(startTick); // In case the startTick is an event tick

            while (!cancellationToken.IsCancellationRequested)
            {
                uint tick = ((float)(GameTimeProvider.Instance.TimeSinceStartup - startTime)).SecondsToGameBoxTick() + startTick;

                // Trigger animation events
                CheckForAnimationEvents(tick);
                
                if (tick >= endTick)
                {
                    yield break;
                }

                if (!IsVisibleToCamera())
                {
                    yield return null;
                    continue;
                }

                if (cancellationToken.IsCancellationRequested) yield break;

                // Animates the mesh
                for (int i = 0; i < _meshCount; i++)
                {
                    RenderMeshComponent meshComponent = _renderMeshComponents[i];

                    uint[] frameTicks = _frameTicks[i];

                    int currentFrameIndex = CoreUtility.GetFloorIndex(frameTicks, tick);
                    uint currentFrameTick = _frameTicks[i][currentFrameIndex];
                    int nextFrameIndex = currentFrameIndex < frameTicks.Length - 1 ? currentFrameIndex + 1 : 0;
                    uint nextFrameTick = nextFrameIndex == 0 ? endTick : _frameTicks[i][nextFrameIndex];

                    float influence = (float)(tick - currentFrameTick) / (nextFrameTick - currentFrameTick);

                    Vector3[] vertices = meshComponent.MeshDataBuffer.VertexBuffer;
                    for (int j = 0; j < vertices.Length; j++)
                    {
                        vertices[j] = Vector3.Lerp(
                            _meshes[i].KeyFrames[currentFrameIndex].GameBoxVertices[j]
                                .ToUnityPosition(UnityPrimitivesConvertor.GameBoxMv3UnitToUnityUnit),
                            _meshes[i].KeyFrames[nextFrameIndex].GameBoxVertices[j]
                                .ToUnityPosition(UnityPrimitivesConvertor.GameBoxMv3UnitToUnityUnit), influence);
                    }

                    meshComponent.Mesh.SetVertices(vertices);
                    if (_materialManager.UsesToonRendering)
                    {
                        meshComponent.MeshDataBuffer.RebuildNormalsBySourceVertex(_meshes[i].NormalIndices, _sourceNormals[i]);
                        meshComponent.Mesh.SetNormals(meshComponent.MeshDataBuffer.NormalBuffer);
                    }
                    meshComponent.Mesh.RecalculateBounds();
                }

                // Animates the tag nodes
                if (_tagNodePolFile != null)
                {
                    for (int i = 0; i < _tagNodesInfo.Length; i++)
                    {
                        if (_tagNodes[i] == null) continue;

                        uint[] frameTicks = _tagNodeFrameTicks[i];

                        int currentFrameIndex = CoreUtility.GetFloorIndex(frameTicks, tick);
                        uint currentFrameTick = _tagNodeFrameTicks[i][currentFrameIndex];
                        int nextFrameIndex = currentFrameIndex < frameTicks.Length - 1 ? currentFrameIndex + 1 : 0;
                        uint nextFrameTick = nextFrameIndex == 0 ? endTick : _tagNodeFrameTicks[i][nextFrameIndex];

                        float influence = (float)(tick - currentFrameTick) / (nextFrameTick - currentFrameTick);

                        Vector3 position = Vector3.Lerp(
                            _tagNodesInfo[i].TagFrames[currentFrameIndex].GameBoxPosition.ToUnityPosition(),
                            _tagNodesInfo[i].TagFrames[nextFrameIndex].GameBoxPosition.ToUnityPosition(), influence);
                        Quaternion rotation = Quaternion.Slerp(
                            _tagNodesInfo[i].TagFrames[currentFrameIndex].GameBoxRotation.Mv3QuaternionToUnityQuaternion(),
                            _tagNodesInfo[i].TagFrames[nextFrameIndex].GameBoxRotation.Mv3QuaternionToUnityQuaternion(), influence);

                        _tagNodes[i].Transform.SetLocalPositionAndRotation(position, rotation);
                    }
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
                    if (renderMeshComponent?.MeshRenderer == null) continue;
                    renderMeshComponent.MeshRenderer.Dispose();
                    renderMeshComponent.MeshRenderer.Destroy();
                }
            }

            for (int i = 0; i < _materials.Length; i++)
            {
                IMaterial[] materials = _materials[i];
                if (materials == null) continue;

                ReturnMaterials(materials);
                _materials[i] = null;
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
                    tagNode?.Destroy(); // tagNode can be null
                }
            }

            _isActionInHoldState = false;
            _actionHoldingTick = 0;
        }
    }
}