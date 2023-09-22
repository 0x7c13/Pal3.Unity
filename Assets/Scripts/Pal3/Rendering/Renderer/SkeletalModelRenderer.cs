// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Rendering.Renderer
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
    using Engine.DataLoader;
    using Engine.Extensions;
    using Engine.Renderer;
    using Material;
    using Rendering;
    using UnityEngine;
    using Color = UnityEngine.Color;

    internal class Bone
    {
        public string Name { get; }
        public GameObject GameObject { get; }
        public BoneNode BoneNode { get; }

        public Matrix4x4 BindPoseModelToBoneSpace { get; set; }
        public Matrix4x4 CurrentPoseToModelMatrix { get; set; }

        public MovBoneAnimationTrack? AnimationTrack { get; private set; }

        public uint[] FrameTicks { get; private set; }

        public Bone(string name, GameObject gameObject, BoneNode boneNode)
        {
            Name = name;
            GameObject = gameObject;
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

            FrameTicks = new uint[track.KeyFrames.Length];

            for (var i = 0; i < track.KeyFrames.Length; i++)
            {
                FrameTicks[i] = track.KeyFrames[i].KeySeconds.SecondsToGameBoxTick();
            }
        }

        public void DisposeAnimationTrack()
        {
            AnimationTrack = null;
            FrameTicks = null;
            CurrentPoseToModelMatrix = Matrix4x4.identity;
        }
    }

    /// <summary>
    /// Skeletal animation model renderer
    /// MSH(.msh) + MOV(.mov)
    /// </summary>
    public class SkeletalModelRenderer : MonoBehaviour, IDisposable
    {
        private IMaterialFactory _materialFactory;
        private Material[][] _materials;

        private MshFile _mshFile;
        private MovFile _movFile;

        private (string textureName, Texture2D texture) _mainTexture;
        private Texture2D _texture;
        private Color _tintColor;

        private readonly Dictionary<int, Bone> _bones = new ();

        private GameObject _rootBoneObject;
        private GameObject[] _meshObjects;
        private RenderMeshComponent[] _renderMeshComponents;

        private Coroutine _animation;
        private CancellationTokenSource _animationCts;

        private int[][] _indexBuffer;
        private Vector3[][] _vertexBuffer;

        public void Init(MshFile mshFile,
            MtlFile mtlFile,
            IMaterialFactory materialFactory,
            ITextureResourceProvider textureProvider,
            Color? tintColor = default)
        {
            Dispose();

            _mshFile = mshFile;
            _materialFactory = materialFactory;
            _tintColor = tintColor ?? Color.white;

            // All .mtl files in PAL3 contain only one material,
            // and there is only one texture in the material
            // so we only need to load the first texture from the
            // first material to use as the main texture for the whole model
            string textureName = mtlFile.Materials[0].TextureFileNames[0];
            _mainTexture = (mtlFile.Materials[0].TextureFileNames[0], textureProvider.GetTexture(textureName));

            SetupBone(_mshFile.RootBoneNode, parentBone: null);
            RenderMesh();
        }

        public void StartAnimation(MovFile movFile, int loopCount = -1)
        {
            if (_renderMeshComponents == null)
            {
                throw new Exception("Animation model not initialized.");
            }

            PauseAnimation();
            SetupAnimationTrack(movFile);

            _animationCts = new CancellationTokenSource();
            _animation = StartCoroutine(PlayAnimationInternalAsync(loopCount,
                _animationCts.Token));
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
            var startTime = Time.timeSinceLevelLoad;

            while (!cancellationToken.IsCancellationRequested)
            {
                float seconds = Time.timeSinceLevelLoad - startTime;

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
            if (_renderMeshComponents == null) return false;

            foreach (RenderMeshComponent renderMeshComponent in _renderMeshComponents)
            {
                if (renderMeshComponent.MeshRenderer.IsVisible()) return true;
            }

            return false;
        }

        private void UpdateBone(Bone bone, uint tick)
        {
            GameObject boneGo = bone.GameObject;
            MovBoneAnimationTrack? track = bone.AnimationTrack;
            BoneNode boneNode = bone.BoneNode;

            if (track != null)
            {
                int currentFrameIndex = CoreUtility.GetFloorIndex(bone.FrameTicks, tick);
                uint currentFrameTick = bone.FrameTicks[currentFrameIndex];
                var nextFrameIndex = currentFrameIndex < bone.FrameTicks.Length - 1 ? currentFrameIndex + 1 : 0;
                uint nextFrameTick = nextFrameIndex == 0 ? _movFile.Duration.SecondsToGameBoxTick() : bone.FrameTicks[nextFrameIndex];

                var influence = (float)(tick - currentFrameTick) / (nextFrameTick - currentFrameTick);

                Vector3 localPosition = Vector3.Lerp(
                    track.Value.KeyFrames[currentFrameIndex].GameBoxTranslation.ToUnityPosition(),
                    track.Value.KeyFrames[nextFrameIndex].GameBoxTranslation.ToUnityPosition(),
                    influence);

                Quaternion localRotation = Quaternion.Slerp(
                    track.Value.KeyFrames[currentFrameIndex].GameBoxRotation.MshQuaternionToUnityQuaternion(),
                    track.Value.KeyFrames[nextFrameIndex].GameBoxRotation.MshQuaternionToUnityQuaternion(),
                    influence);

                boneGo.transform.SetLocalPositionAndRotation(localPosition, localRotation);

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
                MshMesh subMesh = _mshFile.SubMeshes[subMeshIndex];
                Mesh mesh = _renderMeshComponents[subMeshIndex].Mesh;
                mesh.SetVertices(BuildVerticesWithCurrentSkeleton(subMesh, subMeshIndex));
                mesh.RecalculateBounds();
            }
        }

        private void SetupBone(BoneNode boneNode, Bone parentBone)
        {
            GameObject boneGo = new GameObject($"{boneNode.Id}_{boneNode.Name}_{boneNode.Type}");

            boneGo.transform.SetParent(parentBone == null ? gameObject.transform : parentBone.GameObject.transform);
            boneGo.transform.SetLocalPositionAndRotation(boneNode.GameBoxTranslation.ToUnityPosition(),
                boneNode.GameBoxRotation.MshQuaternionToUnityQuaternion());

            Bone bone = new (boneNode.Name, boneGo, boneNode);

            if (parentBone == null)
            {
                _rootBoneObject = boneGo;
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

            _renderMeshComponents = new RenderMeshComponent[_mshFile.SubMeshes.Length];
            _meshObjects = new GameObject[_mshFile.SubMeshes.Length];
            _indexBuffer = new int[_mshFile.SubMeshes.Length][];
            _vertexBuffer = new Vector3[_mshFile.SubMeshes.Length][];

            for (var i = 0; i < _mshFile.SubMeshes.Length; i++)
            {
                MshMesh subMesh = _mshFile.SubMeshes[i];
                RenderSubMesh(subMesh, i);
            }
        }

        private void RenderSubMesh(MshMesh subMesh, int subMeshIndex)
        {
            _meshObjects[subMeshIndex] = new GameObject($"SubMesh_{subMeshIndex}");
            _meshObjects[subMeshIndex].transform.SetParent(gameObject.transform, false);

            _vertexBuffer[subMeshIndex] = new Vector3[subMesh.Vertices.Length];

            int numOfIndices = subMesh.Faces.Length * 3;
            int[] triangles = new int[numOfIndices];
            Vector2[] uvs1 = new Vector2[numOfIndices];
            int[] indexBuffer = new int[numOfIndices];

            var index = 0;
            foreach (PhyFace phyFace in subMesh.Faces)
            {
                for (var i = 0; i < phyFace.Indices.Length; i++)
                {
                    indexBuffer[index] = phyFace.Indices[i];
                    uvs1[index] = new Vector2(phyFace.U[i, 0], phyFace.V[i, 0]);
                    triangles[index] = index;
                    index++;
                }
            }

            _indexBuffer[subMeshIndex] = indexBuffer;

            Vector3[] vertices = new Vector3[numOfIndices];
            for (var i = 0; i < numOfIndices; i++)
            {
                vertices[i] = subMesh.Vertices[indexBuffer[i]].GameBoxPosition.ToUnityPosition();
            }

            Material[] materials = _materialFactory.CreateStandardMaterials(
                RendererType.Msh,
                _mainTexture,
                default,
                _tintColor,
                GameBoxBlendFlag.Opaque);

            var meshRenderer = _meshObjects[subMeshIndex].AddComponent<StaticMeshRenderer>();
            Mesh renderMesh = meshRenderer.Render(
                vertices,
                triangles.ToUnityTriangles(),
                normals: null,
                uvs1,
                secondaryTextureUvs: null,
                materials,
                true);

            renderMesh.RecalculateNormals();
            renderMesh.RecalculateTangents();
            renderMesh.RecalculateBounds();

            _renderMeshComponents[subMeshIndex] = new RenderMeshComponent
            {
                Mesh = renderMesh,
                MeshRenderer = meshRenderer,
                MeshDataBuffer = new MeshDataBuffer()
                {
                    VertexBuffer = vertices
                }
            };
        }

        private Vector3[] BuildVerticesWithCurrentSkeleton(MshMesh subMesh, int subMeshIndex)
        {
            var vertexBuffer = _vertexBuffer[subMeshIndex];

            for (var i = 0; i < subMesh.Vertices.Length; i++)
            {
                PhyVertex vert = subMesh.Vertices[i];

                int boneId = vert.BoneIds[0];
                Bone bone = _bones[boneId];

                Vector3 originalPosition = vert.GameBoxPosition.ToUnityPosition();
                Vector4 currentPosition = bone.CurrentPoseToModelMatrix * bone.BindPoseModelToBoneSpace *
                                          new Vector4(originalPosition.x, originalPosition.y, originalPosition.z, 1.0f);

                vertexBuffer[i] = new Vector3(
                    currentPosition.x / currentPosition.w,
                    currentPosition.y / currentPosition.w,
                    currentPosition.z / currentPosition.w);
            }

            Vector3[] vertices = _renderMeshComponents[subMeshIndex].MeshDataBuffer.VertexBuffer;

            var indexBuffer = _indexBuffer[subMeshIndex];
            for (var i = 0; i < indexBuffer.Length; i++)
            {
                vertices[i] = vertexBuffer[indexBuffer[i]];
            }

            return vertices;
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

        public bool IsVisible()
        {
            return _meshObjects != null;
        }

        private void OnDisable()
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
                    _materialFactory.ReturnToPool(renderMeshComponent.MeshRenderer.GetMaterials());
                    renderMeshComponent.Mesh.Destroy();
                    renderMeshComponent.MeshRenderer.Destroy();
                }

                _renderMeshComponents = null;
            }

            if (_meshObjects != null)
            {
                foreach (GameObject meshObject in _meshObjects)
                {
                    meshObject.Destroy();
                }

                _meshObjects = null;
            }

            if (_bones != null)
            {
                foreach (Bone bone in _bones.Values)
                {
                    bone.GameObject.Destroy();
                }

                _bones.Clear();
            }

            if (_rootBoneObject != null)
            {
                _rootBoneObject.Destroy();
                _rootBoneObject = null;
            }

            _indexBuffer = null;
            _vertexBuffer = null;
        }
    }
}