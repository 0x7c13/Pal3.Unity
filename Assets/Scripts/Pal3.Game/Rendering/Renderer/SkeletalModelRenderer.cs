// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Renderer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using Core.DataReader.Msh;
    using Core.DataReader.Mov;
    using Core.DataReader.Mtl;
    using Core.Primitives;
    using Core.Utilities;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.DataLoader;
    using Engine.Extensions;
    using Engine.Logging;
    using Engine.Renderer;
    using Engine.Services;
    using Material;
    using Rendering;
    using UnityEngine;
    using Color = Core.Primitives.Color;

    internal sealed class Bone
    {
        public string Name { get; }
        public IGameEntity GameEntity { get; }
        public BoneNode BoneNode { get; }

        public Matrix4x4 BindPoseModelToBoneSpace { get; set; }
        public Matrix4x4 CurrentPoseToModelMatrix { get; set; }

        public MovBoneAnimationTrack? AnimationTrack { get; private set; }

        public uint[] FrameTicks { get; private set; }

        public Bone(string name, IGameEntity gameEntity, BoneNode boneNode)
        {
            Name = name;
            GameEntity = gameEntity;
            BoneNode = boneNode;
            BindPoseModelToBoneSpace = Matrix4x4.identity;
            CurrentPoseToModelMatrix = Matrix4x4.identity;
        }

        public bool IsRoot()
        {
            return BoneNode.ParentId == -1;
        }

        public void SetAnimationTrack(MovBoneAnimationTrack track)
        {
            AnimationTrack = track;

            if (FrameTicks == null || FrameTicks.Length != track.KeyFrames.Length)
            {
                FrameTicks = new uint[track.KeyFrames.Length];
            }

            for (var i = 0; i < track.KeyFrames.Length; i++)
            {
                FrameTicks[i] = track.KeyFrames[i].KeySeconds.SecondsToGameBoxTick();
            }
        }

        public void DisposeAnimationTrack()
        {
            AnimationTrack = null;
            CurrentPoseToModelMatrix = Matrix4x4.identity;
        }
    }

    /// <summary>
    /// Skeletal animation model renderer
    /// MSH(.msh) + MOV(.mov)
    /// </summary>
    public sealed class SkeletalModelRenderer : GameEntityScript, IDisposable
    {
        private IMaterialFactory _materialFactory;
        private Material[][] _materials;

        private MshFile _mshFile;
        private MovFile _movFile;

        private (string textureName, Texture2D texture) _mainTexture;
        private Texture2D _texture;
        private Color _tintColor;

        private readonly Dictionary<int, Bone> _bones = new ();

        private IGameEntity _rootBoneEntity;
        private IGameEntity[] _meshEntities;
        private RenderMeshComponent[] _renderMeshComponents;

        private CancellationTokenSource _animationCts;

        private int[][] _indexBuffer;
        private Vector3[][] _allVerticesBuffer;

        public bool IsInitialized { get; private set; }

        protected override void OnEnableGameEntity()
        {
        }

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

        public void Init(MshFile mshFile,
            MtlFile mtlFile,
            IMaterialFactory materialFactory,
            ITextureResourceProvider textureProvider,
            Color? tintColor = default)
        {
            Dispose();

            _mshFile = mshFile;
            _materialFactory = materialFactory;
            _tintColor = tintColor ?? Color.White;

            // All .mtl files in PAL3 contain only one material,
            // and there is only one texture in the material
            // so we only need to load the first texture from the
            // first material to use as the main texture for the whole model
            string textureName = mtlFile.Materials[0].TextureFileNames[0];
            _mainTexture = (mtlFile.Materials[0].TextureFileNames[0], textureProvider.GetTexture(textureName));

            SetupBone(_mshFile.RootBoneNode, parentBone: null);
            RenderMesh();

            IsInitialized = true;
        }

        public void StartAnimation(MovFile movFile, int loopCount = -1)
        {
            if (!IsInitialized)
            {
                throw new Exception("Animation model not initialized");
            }

            PauseAnimation();
            SetupAnimationTrack(movFile);

            _animationCts = new CancellationTokenSource();
            StartCoroutine(PlayAnimationInternalAsync(loopCount, _animationCts.Token));
        }

        public void PauseAnimation()
        {
            if (_animationCts is {IsCancellationRequested: false})
            {
                _animationCts.Cancel();
            }
        }

        private void SetupAnimationTrack(MovFile movFile)
        {
            // Reset all existing bone animation tracks
            foreach (Bone bone in _bones.Values)
            {
                bone.DisposeAnimationTrack();
            }

            _movFile = movFile;

            // Bind animation tracks from mov file
            foreach (MovBoneAnimationTrack track in _movFile.BoneAnimationTracks)
            {
                if (_bones.TryGetValue(track.BoneId, out Bone bonne))
                {
                    bonne.SetAnimationTrack(track);
                }
            }
        }

        private IEnumerator PlayAnimationInternalAsync(int loopCount,
            CancellationToken cancellationToken)
        {
            if (loopCount == -1) // Infinite loop until cancelled
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    yield return PlayOneTimeAnimationInternalAsync(cancellationToken);
                }
            }
            else if (loopCount > 0)
            {
                while (!cancellationToken.IsCancellationRequested && --loopCount >= 0)
                {
                    yield return PlayOneTimeAnimationInternalAsync(cancellationToken);
                }
            }
        }

        private IEnumerator PlayOneTimeAnimationInternalAsync(CancellationToken cancellationToken)
        {
            var startTime = GameTimeProvider.Instance.TimeSinceStartup;

            while (!cancellationToken.IsCancellationRequested)
            {
                float seconds = (float)(GameTimeProvider.Instance.TimeSinceStartup - startTime);

                if (seconds >= _movFile.Duration)
                {
                    yield break;
                }

                if (IsVisibleToCamera())
                {
                    uint tick = seconds.SecondsToGameBoxTick();
                    UpdateBone(_bones[0], tick);
                    UpdateSkinning();
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

        private void UpdateBone(Bone bone, uint tick)
        {
            IGameEntity boneEntity = bone.GameEntity;
            MovBoneAnimationTrack? track = bone.AnimationTrack;
            BoneNode boneNode = bone.BoneNode;

            if (track != null)
            {
                int currentFrameIndex = CoreUtility.GetFloorIndex(bone.FrameTicks, tick);
                uint currentFrameTick = bone.FrameTicks[currentFrameIndex];
                var nextFrameIndex = currentFrameIndex < bone.FrameTicks.Length - 1 ? currentFrameIndex + 1 : 0;
                uint nextFrameTick = bone.FrameTicks[nextFrameIndex];

                float influence = 1f;
                if (nextFrameTick != currentFrameTick)
                {
                    influence = (float)(tick - currentFrameTick) / (nextFrameTick - currentFrameTick);
                }

                Vector3 localPosition = Vector3.Lerp(
                    track.Value.KeyFrames[currentFrameIndex].GameBoxTranslation.ToUnityPosition(),
                    track.Value.KeyFrames[nextFrameIndex].GameBoxTranslation.ToUnityPosition(),
                    influence);

                Quaternion localRotation = Quaternion.Slerp(
                    track.Value.KeyFrames[currentFrameIndex].GameBoxRotation.MshQuaternionToUnityQuaternion(),
                    track.Value.KeyFrames[nextFrameIndex].GameBoxRotation.MshQuaternionToUnityQuaternion(),
                    influence);

                boneEntity.Transform.SetLocalPositionAndRotation(localPosition, localRotation);

                Matrix4x4 curPoseToModelMatrix = Matrix4x4.Translate(localPosition) * Matrix4x4.Rotate(localRotation);

                if (!bone.IsRoot())
                {
                    curPoseToModelMatrix = _bones[boneNode.ParentId].CurrentPoseToModelMatrix * curPoseToModelMatrix;
                }

                bone.CurrentPoseToModelMatrix = curPoseToModelMatrix;
            }

            // Update children
            for (var i = 0; i < boneNode.Children.Length; i++)
            {
                Bone childBone = _bones[boneNode.Children[i].Id];
                UpdateBone(childBone, tick);
            }
        }

        private void UpdateSkinning()
        {
            for (int subMeshIndex = 0; subMeshIndex < _renderMeshComponents.Length; subMeshIndex++)
            {
                UpdateVerticesWithCurrentSkeleton(subMeshIndex);
                Mesh mesh = _renderMeshComponents[subMeshIndex].Mesh;
                mesh.SetVertices(_renderMeshComponents[subMeshIndex].MeshDataBuffer.VertexBuffer);
                mesh.RecalculateBounds();
            }
        }

        private void SetupBone(BoneNode boneNode, Bone parentBone)
        {
            IGameEntity boneEntity = GameEntityFactory.Create($"{boneNode.Id}_{boneNode.Name}_{boneNode.Type}",
                parentBone == null ? GameEntity : parentBone.GameEntity, worldPositionStays: true);

            boneEntity.Transform.SetLocalPositionAndRotation(boneNode.GameBoxTranslation.ToUnityPosition(),
                boneNode.GameBoxRotation.MshQuaternionToUnityQuaternion());

            Bone bone = new (boneNode.Name, boneEntity, boneNode);

            if (parentBone == null)
            {
                _rootBoneEntity = boneEntity;
            }
            else
            {
                _bones.Add(boneNode.Id, bone);
                Matrix4x4 translationMatrix = Matrix4x4.Translate(boneNode.GameBoxTranslation.ToUnityPosition());
                Matrix4x4 rotationMatrix = Matrix4x4.Rotate(boneNode.GameBoxRotation.MshQuaternionToUnityQuaternion());
                bone.BindPoseModelToBoneSpace = Matrix4x4.Inverse(rotationMatrix)
                                                * Matrix4x4.Inverse(translationMatrix)
                                                * parentBone.BindPoseModelToBoneSpace;
            }

            // Render child bones
            foreach (BoneNode subBone in boneNode.Children)
            {
                SetupBone(subBone, bone);
            }
        }

        private void RenderMesh()
        {
            if (_mshFile.SubMeshes.Length == 0) return;

            if (_renderMeshComponents == null ||
                _renderMeshComponents.Length != _mshFile.SubMeshes.Length)
            {
                _renderMeshComponents = new RenderMeshComponent[_mshFile.SubMeshes.Length];
                _meshEntities = new IGameEntity[_mshFile.SubMeshes.Length];
                _indexBuffer = new int[_mshFile.SubMeshes.Length][];
                _allVerticesBuffer = new Vector3[_mshFile.SubMeshes.Length][];
            }

            for (var i = 0; i < _mshFile.SubMeshes.Length; i++)
            {
                MshMesh subMesh = _mshFile.SubMeshes[i];
                RenderSubMesh(subMesh, i);
            }
        }

        private void RenderSubMesh(MshMesh subMesh, int subMeshIndex)
        {
            _meshEntities[subMeshIndex] = GameEntityFactory.Create($"SubMesh_{subMeshIndex}",
                GameEntity, worldPositionStays: false);

            int numOfIndices = subMesh.Faces.Length * 3;

            RenderMeshComponent renderMeshComponent = _renderMeshComponents[subMeshIndex] ??= new RenderMeshComponent();

            renderMeshComponent.MeshDataBuffer ??= new MeshDataBuffer();
            renderMeshComponent.MeshDataBuffer.AllocateOrResizeBuffers(
                vertexBufferSize: numOfIndices,
                normalBufferSize: 0, // Skeletal model does not have normals, we will recalculate them later
                uvBufferSize: numOfIndices,
                triangleBufferSize: numOfIndices);

            if (_indexBuffer[subMeshIndex] == null ||
                _indexBuffer[subMeshIndex].Length != numOfIndices)
            {
                _indexBuffer[subMeshIndex] = new int[numOfIndices];
            }

            if (_allVerticesBuffer[subMeshIndex] == null ||
                _allVerticesBuffer[subMeshIndex].Length != subMesh.Vertices.Length)
            {
                _allVerticesBuffer[subMeshIndex] = new Vector3[subMesh.Vertices.Length];
            }

            var index = 0;
            foreach (PhyFace phyFace in subMesh.Faces)
            {
                for (var i = 0; i < phyFace.Indices.Length; i++)
                {
                    _indexBuffer[subMeshIndex][index] = phyFace.Indices[i];
                    renderMeshComponent.MeshDataBuffer.UvBuffer[index].x = phyFace.U[i, 0];
                    renderMeshComponent.MeshDataBuffer.UvBuffer[index].y = phyFace.V[i, 0];
                    renderMeshComponent.MeshDataBuffer.TriangleBuffer[index] = index;
                    index++;
                }
            }

            renderMeshComponent.MeshDataBuffer.TriangleBuffer.ToUnityTrianglesInPlace();

            for (var i = 0; i < numOfIndices; i++)
            {
                renderMeshComponent.MeshDataBuffer.VertexBuffer[i] =
                    subMesh.Vertices[_indexBuffer[subMeshIndex][i]].GameBoxPosition.ToUnityPosition();
            }

            Material[] materials = _materialFactory.CreateStandardMaterials(
                RendererType.Msh,
                mainTexture: _mainTexture,
                shadowTexture: default,
                tintColor: _tintColor,
                blendFlag: GameBoxBlendFlag.Opaque);

            var meshRenderer = _meshEntities[subMeshIndex].AddComponent<StaticMeshRenderer>();
            Mesh renderMesh = meshRenderer.Render(
                vertices: renderMeshComponent.MeshDataBuffer.VertexBuffer,
                triangles: renderMeshComponent.MeshDataBuffer.TriangleBuffer,
                normals: default,
                mainTextureUvs: (channel: 0, uvs: renderMeshComponent.MeshDataBuffer.UvBuffer),
                secondaryTextureUvs: default, // Skeletal model does not have secondary texture
                materials: materials,
                isDynamic: true);

            renderMesh.RecalculateNormals();
            renderMesh.RecalculateTangents();
            renderMesh.RecalculateBounds();

            renderMeshComponent.Mesh = renderMesh;
            renderMeshComponent.MeshRenderer = meshRenderer;
        }

        private void UpdateVerticesWithCurrentSkeleton(int subMeshIndex)
        {
            MshMesh subMesh = _mshFile.SubMeshes[subMeshIndex];
            Vector3[] allVertices = _allVerticesBuffer[subMeshIndex];

            for (var i = 0; i < subMesh.Vertices.Length; i++)
            {
                PhyVertex vert = subMesh.Vertices[i];

                int boneId = vert.BoneIds[0];
                Bone bone = _bones[boneId];

                Vector3 originalPosition = vert.GameBoxPosition.ToUnityPosition();
                Vector4 currentPosition = bone.CurrentPoseToModelMatrix * bone.BindPoseModelToBoneSpace *
                                          new Vector4(originalPosition.x, originalPosition.y, originalPosition.z, 1.0f);

                allVertices[i].x = currentPosition.x / currentPosition.w;
                allVertices[i].y = currentPosition.y / currentPosition.w;
                allVertices[i].z = currentPosition.z / currentPosition.w;
            }

            for (var i = 0; i < _indexBuffer[subMeshIndex].Length; i++)
            {
                _renderMeshComponents[subMeshIndex].MeshDataBuffer.VertexBuffer[i] =
                    allVertices[_indexBuffer[subMeshIndex][i]];
            }
        }

        public Bounds GetRendererBounds()
        {
            if (!IsInitialized)
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
            if (!IsInitialized)
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

        public void Dispose()
        {
            if (!IsInitialized) return;

            IsInitialized = false;

            PauseAnimation();

            if (_renderMeshComponents != null)
            {
                foreach (RenderMeshComponent renderMeshComponent in _renderMeshComponents)
                {
                    _materialFactory.ReturnToPool(renderMeshComponent.MeshRenderer.GetMaterials());
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

            if (_bones != null)
            {
                foreach (Bone bone in _bones.Values)
                {
                    bone.GameEntity?.Destroy();
                }

                _bones.Clear();
            }

            if (_rootBoneEntity != null)
            {
                _rootBoneEntity.Destroy();
                _rootBoneEntity = null;
            }
        }
    }
}