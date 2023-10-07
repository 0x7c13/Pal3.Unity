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
    using Engine.Renderer;
    using Material;
    using Rendering;
    using UnityEngine;
    using Color = Core.Primitives.Color;

    /// <summary>
    /// MV3(.mv3) model renderer
    /// </summary>
    public class Mv3ModelRenderer : GameEntityScript, IDisposable
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
        private IGameEntity[] _meshEntities;
        private Color _tintColor;
        private string[] _animationName;
        private CancellationTokenSource _animationCts;

        private bool _isActionInHoldState;
        private uint _actionHoldingTick;

        private Mv3Mesh[] _meshes;
        private int _meshCount;
        private uint[][] _frameTicks;
        private uint _totalGameBoxTicks;
        private RenderMeshComponent[] _renderMeshComponents;
        private WaitForSeconds _animationDelay;

        // Tag node
        private Mv3TagNode[] _tagNodesInfo;
        private uint[][] _tagNodeFrameTicks;
        private IGameEntity[] _tagNodes;

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

            _renderMeshComponents = new RenderMeshComponent[_meshCount];
            _gbMaterials = new GameBoxMaterial[_meshCount];
            _textures = new Texture2D[_meshCount];
            _textureHasAlphaChannel = new bool[_meshCount];
            _materials = new Material[_meshCount][];
            _animationName = new string[_meshCount];
            _frameTicks = new uint[_meshCount][];
            _meshEntities = new IGameEntity[_meshCount];
            _tagNodesInfo = mv3File.TagNodes;
            _tagNodeFrameTicks = new uint[_tagNodesInfo.Length][];

            for (var i = 0; i < _tagNodesInfo.Length; i++)
            {
                var tagFrames = _tagNodesInfo[i].TagFrames;
                int tagFramesCount = tagFrames.Length;
                var ticksArray = new uint[tagFramesCount];

                for (int j = 0; j < tagFramesCount; j++)
                {
                    ticksArray[j] = tagFrames[j].GameBoxTick;
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
                _tagNodes = new IGameEntity[_tagNodesInfo.Length];

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
                ticksArray[i] = keyFrames[i].GameBoxTick;
            }

            _frameTicks[index] = ticksArray;

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

            var meshDataBuffer = new MeshDataBuffer
            {
                VertexBuffer = new Vector3[mv3Mesh.KeyFrames[0].GameBoxVertices.Length],
                NormalBuffer = mv3Mesh.GameBoxNormals.ToUnityNormals(),
            };

            Mesh renderMesh = meshRenderer.Render(
                mv3Mesh.KeyFrames[0].GameBoxVertices.ToUnityPositions(UnityPrimitivesConvertor.GameBoxMv3UnitToUnityUnit),
                mv3Mesh.GameBoxTriangles.ToUnityTriangles(),
                meshDataBuffer.NormalBuffer,
                mv3Mesh.Uvs.ToUnityVector2s(),
                mv3Mesh.Uvs.ToUnityVector2s(),
                _materials[index],
                true);

            renderMesh.RecalculateTangents();

            _renderMeshComponents[index] = new RenderMeshComponent
            {
                Mesh = renderMesh,
                MeshRenderer = meshRenderer,
                MeshDataBuffer = meshDataBuffer
            };

            _meshEntities[index].SetParent(GameEntity, worldPositionStays: false);
        }

        public void StartAnimation(int loopCount = -1, float fps = -1f)
        {
            if (_renderMeshComponents == null)
            {
                throw new Exception("Animation model not initialized");
            }

            PauseAnimation();

            _animationCts = new CancellationTokenSource();
            _animationDelay = fps <= 0 ? null : new WaitForSeconds(1 / fps);
            StartCoroutine(PlayAnimationInternalAsync(loopCount,
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
            if (_animationCts is {IsCancellationRequested: false})
            {
                _animationCts.Cancel();
            }
        }

        public bool IsVisible()
        {
            return _meshEntities != null;
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
            StartCoroutine(ResumeActionInternalAsync(_animationDelay));
        }

        private IEnumerator ResumeActionInternalAsync(WaitForSeconds animationDelay)
        {
            if (!_isActionInHoldState) yield break;
            _animationCts = new CancellationTokenSource();
            yield return PlayOneTimeAnimationInternalAsync(_actionHoldingTick, _totalGameBoxTicks, animationDelay, _animationCts.Token);
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
                    yield return PlayOneTimeAnimationInternalAsync(startTick, _totalGameBoxTicks, animationDelay, cancellationToken);
                    AnimationLoopPointReached?.Invoke(this, loopCount);
                }
            }
            else if (loopCount > 0)
            {
                while (!cancellationToken.IsCancellationRequested && --loopCount >= 0)
                {
                    yield return PlayOneTimeAnimationInternalAsync(startTick, _totalGameBoxTicks, animationDelay, cancellationToken);
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
                    yield return PlayOneTimeAnimationInternalAsync(startTick, _totalGameBoxTicks, animationDelay, cancellationToken);
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
            WaitForSeconds animationDelay,
            CancellationToken cancellationToken)
        {
            var startTime = Time.timeSinceLevelLoad;

            while (!cancellationToken.IsCancellationRequested)
            {
                uint tick = (Time.timeSinceLevelLoad - startTime).SecondsToGameBoxTick() + startTick;

                if (tick >= endTick)
                {
                    yield break;
                }

                if (!IsVisibleToCamera())
                {
                    yield return animationDelay;
                    continue;
                }

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

                if (_tagNodes != null)
                {
                    for (var i = 0; i < _tagNodes.Length; i++)
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
                }

                yield return animationDelay;
            }
        }

        public bool IsVisibleToCamera()
        {
            if (_renderMeshComponents == null) return false;

            foreach (RenderMeshComponent renderMeshComponent in _renderMeshComponents)
            {
                if (renderMeshComponent.MeshRenderer.IsVisible()) return true;
            }

            return false;
        }

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

        public void Dispose()
        {
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

                _renderMeshComponents = null;
            }

            if (_meshEntities != null)
            {
                foreach (IGameEntity meshEntity in _meshEntities)
                {
                    meshEntity?.Destroy();
                }

                _meshEntities = null;
            }

            if (_tagNodes != null)
            {
                foreach (IGameEntity tagNode in _tagNodes)
                {
                    tagNode?.Destroy();
                }

                _tagNodes = null;
            }
        }
    }
}