// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using System;
    using System.Collections;
    using Core.DataReader.Msh;
    using Core.DataReader.Mov;
    using UnityEngine;
    using System.Collections.Generic;
    using System.Threading;
    using Core.DataLoader;
    using Core.GameBox;
    using Core.Renderer;
    using Core.Utils;

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

            for (int i = 0; i < track.KeyFrames.Length; i++)
            {
                FrameTicks[i] = track.KeyFrames[i].Tick;
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

        private string _textureName;
        private Texture2D _texture;
        private Color _tintColor;

        private readonly Dictionary<int, Bone> _bones = new ();

        private GameObject[] _meshObjects;
        private RenderMeshComponent[] _renderMeshComponents;

        private Coroutine _animation;
        private CancellationTokenSource _animationCts;

        private readonly Dictionary<int, List<int>> _indexBuffer = new();
        private readonly Dictionary<int, Vector3[]> _vertexBuffer = new();

        public void Init(MshFile mshFile,
            IMaterialFactory materialFactory,
            ITextureResourceProvider textureProvider,
            string textureName,
            Color? tintColor = default) // TODO: Read texture name from <actorName>.mtl file
        {
            Dispose();

            _mshFile = mshFile;
            _materialFactory = materialFactory;
            _textureName = textureName;
            _texture = textureProvider.GetTexture(_textureName);
            _tintColor = tintColor ?? Color.white;

            SetupBone(_mshFile.RootBoneNode, parentBone: null);
            RenderMesh();
        }

        public void StartAnimation(MovFile mov, int loopCount = -1)
        {
            if (_renderMeshComponents == null)
            {
                throw new Exception("Animation model not initialized.");
            }

            PauseAnimation();
            BindJointTrack(mov);

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

        private void BindJointTrack(MovFile movFile)
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
                uint tick = GameBoxInterpreter.SecondsToTick(Time.timeSinceLevelLoad - startTime);

                if (tick >= _movFile.Duration)
                {
                    yield break;
                }

                UpdateBone(_bones[0], tick);
                UpdateSkinning();

                yield return null;
            }
        }

        private void UpdateBone(Bone bone, uint tick)
        {
            GameObject boneGo = bone.GameObject;
            MovBoneAnimationTrack? track = bone.AnimationTrack;
            BoneNode boneNode = bone.BoneNode;

            if (track != null)
            {
                int currentFrameIndex = Utility.GetFloorIndex(bone.FrameTicks, tick);

                Vector3 localPosition = track.Value.KeyFrames[currentFrameIndex].Translation;
                Quaternion localRotation = track.Value.KeyFrames[currentFrameIndex].Rotation;

                boneGo.transform.localPosition = localPosition;
                boneGo.transform.localRotation = localRotation;

                Matrix4x4 curPoseToModelMatrix = Matrix4x4.Translate(localPosition) * Matrix4x4.Rotate(localRotation);

                if (!bone.IsRoot())
                {
                    Bone parentBone = _bones[boneNode.ParentId];
                    Matrix4x4 parentCurPoseToModelMatrix = parentBone.CurrentPoseToModelMatrix;
                    curPoseToModelMatrix = parentCurPoseToModelMatrix * curPoseToModelMatrix;
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

            boneGo.transform.localPosition = boneNode.Translation;
            boneGo.transform.localRotation = boneNode.Rotation;

            Bone bone = new (boneNode.Name, boneGo, boneNode);

            if (parentBone != null)
            {
                _bones.Add(boneNode.Id, bone);
                Matrix4x4 translationMatrix = Matrix4x4.Translate(boneNode.Translation);
                Matrix4x4 rotationMatrix = Matrix4x4.Rotate(boneNode.Rotation);
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

            for (int i = 0; i < _mshFile.SubMeshes.Length; i++)
            {
                MshMesh subMesh = _mshFile.SubMeshes[i];
                RenderSubMesh(subMesh, i);
            }
        }

        private void RenderSubMesh(MshMesh subMesh, int subMeshIndex)
        {
            _meshObjects[subMeshIndex] = new GameObject($"SubMesh_{subMeshIndex}");
            _meshObjects[subMeshIndex].transform.SetParent(gameObject.transform, false);

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            Vector3[] normals = null;
            List<Vector2> uvs = new List<Vector2>();

            _indexBuffer[subMeshIndex] = new List<int>();
            _vertexBuffer[subMeshIndex] = new Vector3[subMesh.Vertices.Length];

            var index = 0;
            foreach (PhyFace phyFace in subMesh.Faces)
            {
                for (var i = 0; i < phyFace.Indices.Length; i++)
                {
                    _indexBuffer[subMeshIndex].Add(phyFace.Indices[i]);
                    uvs.Add(new Vector2(phyFace.U[i, 0], phyFace.V[i, 0]));
                    triangles.Add(index++);
                }
            }

            GameBoxInterpreter.ToUnityTriangles(triangles);

            for (int i = 0; i < _indexBuffer[subMeshIndex].Count; i++)
            {
                vertices.Add(subMesh.Vertices[_indexBuffer[subMeshIndex][i]].Position);
            }

            Material[] materials = _materialFactory.CreateStandardMaterials(
                RendererType.Msh,
                (_textureName, _texture),
                default,
                _tintColor,
                GameBoxBlendFlag.Opaque);

            Vector3[] verts = vertices.ToArray();
            int[] tris = triangles.ToArray();
            Vector2[] uvsArray = uvs.ToArray();

            var meshRenderer = _meshObjects[subMeshIndex].AddComponent<StaticMeshRenderer>();
            Mesh renderMesh = meshRenderer.Render(
                ref verts,
                ref tris,
                ref normals,
                ref uvsArray,
                ref uvsArray,
                ref materials,
                true);

            _renderMeshComponents[subMeshIndex] = new RenderMeshComponent
            {
                Mesh = renderMesh,
                MeshRenderer = meshRenderer,
                MeshDataBuffer = new MeshDataBuffer()
                {
                    VertexBuffer = verts
                }
            };
        }

        private Vector3[] BuildVerticesWithCurrentSkeleton(MshMesh subMesh, int subMeshIndex)
        {
            for (var i = 0; i < subMesh.Vertices.Length; i++)
            {
                PhyVertex vert = subMesh.Vertices[i];

                int boneId = vert.BoneIds[0];
                Bone bone = _bones[boneId];

                Vector4 originalPosition = new Vector4(vert.Position.x, vert.Position.y, vert.Position.z, 1.0f);
                Vector4 currentPosition = bone.CurrentPoseToModelMatrix * bone.BindPoseModelToBoneSpace * originalPosition;

                _vertexBuffer[subMeshIndex][i] = new Vector3(
                    currentPosition.x / currentPosition.w,
                    currentPosition.y / currentPosition.w,
                    currentPosition.z / currentPosition.w);
            }

            Vector3[] vertices = _renderMeshComponents[subMeshIndex].Mesh.vertices;

            for (int i = 0; i < _indexBuffer[subMeshIndex].Count; i++)
            {
                vertices[i] = _vertexBuffer[subMeshIndex][_indexBuffer[subMeshIndex][i]];
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

            if (_bones != null)
            {
                foreach (Bone bone in _bones.Values)
                {
                    Destroy(bone.GameObject);
                }

                _bones.Clear();
            }

            _indexBuffer.Clear();
            _vertexBuffer.Clear();
        }
    }
}