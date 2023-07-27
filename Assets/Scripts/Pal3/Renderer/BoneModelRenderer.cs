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
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading;
    using Core.DataLoader;
    using Core.GameBox;

    internal class Joint
    {
        public string Name;
        public GameObject GameObject;
        public BoneNode BoneNode;
        public MovBoneAnimationTrack? AnimationTrack;

        public Matrix4x4 BindPoseModelToBoneSpace = Matrix4x4.identity;
        public Matrix4x4 CurrentPoseToModelMatrix = Matrix4x4.identity;

        public bool IsRoot()
        {
            return BoneNode.ParentId < 0;
        }

        public void Clear()
        {
            AnimationTrack = null;
            BindPoseModelToBoneSpace = Matrix4x4.identity;
            CurrentPoseToModelMatrix = Matrix4x4.identity;
        }
    }

    /// <summary>
    /// Bone model renderer
    /// MSH(.msh) + MOV(.mov)
    /// </summary>
    public class BoneModelRenderer : MonoBehaviour, IDisposable
    {
        private IMaterialFactory _materialFactory;
        private Material[][] _materials;

        private ITextureResourceProvider _textureProvider;
        private MshFile _mshFile;
        private MovFile _movFile;

        private string _textureName;
        private Texture2D _texture;
        private Color _tintColor;

        private readonly Dictionary<int, Joint> _joints = new ();

        private Material[] _skinningMaterials;
        private Mesh[] _skinningMeshes;

        private Coroutine _animation;
        private CancellationTokenSource _animationCts;

        public void Init(MshFile mshFile,
            ITextureResourceProvider textureProvider,
            IMaterialFactory materialFactory,
            string textureName,
            Color? tintColor = default) // TODO: Read texture name from <actorName>.mtl file
        {
            _mshFile = mshFile;
            _textureProvider = textureProvider;
            _materialFactory = materialFactory;
            _textureName = textureName;
            _texture = textureProvider.GetTexture(_textureName);
            _tintColor = tintColor ?? Color.white;

            RenderSkeleton();
            RenderMesh();
        }

        public void StartAnimation(MovFile mov, int loopCount = -1)
        {
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
            // Clear all existing joint tracks
            foreach (var joint in _joints)
            {
                joint.Value.Clear();
            }

            _movFile = movFile;

            // Bind joint tracks from mov file
            foreach (MovBoneAnimationTrack track in _movFile.BoneAnimationTracks)
            {
                if (_joints.TryGetValue(track.BoneId, out Joint joint))
                {
                     joint.AnimationTrack = track;
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

                UpdateJoint(_joints[0], tick);
                UpdateSkinning();

                yield return null;
            }
        }

        private void UpdateJoint(Joint joint, uint tick)
        {
            GameObject go = joint.GameObject;
            MovBoneAnimationTrack? track = joint.AnimationTrack;
            BoneNode bone = joint.BoneNode;

            if (track != null)
            {
                int index = GetKeyIndex(track.Value, tick);
                Vector3 localPosition = track.Value.KeyFrames[index].Translation;
                Quaternion localRotation = track.Value.KeyFrames[index].Rotation;

                go.transform.localPosition = localPosition;
                go.transform.localRotation = localRotation;

                Matrix4x4 curPoseToModelMatrix = Matrix4x4.Rotate(localRotation) * Matrix4x4.Translate(localPosition);

                if (!joint.IsRoot())
                {
                    Joint parentJoint = _joints[bone.ParentId];
                    Matrix4x4 parentCurPoseToModelMatrix = parentJoint.CurrentPoseToModelMatrix;
                    curPoseToModelMatrix = parentCurPoseToModelMatrix * curPoseToModelMatrix;
                }

                joint.CurrentPoseToModelMatrix = curPoseToModelMatrix;
            }

            // Update children
            for (var i = 0; i < bone.Children.Length; i++)
            {
                Joint childJoint = _joints[bone.Children[i].Id];
                UpdateJoint(childJoint, tick);
            }
        }

        private int GetKeyIndex(MovBoneAnimationTrack track, uint tick)
        {
            int index = 0;

            for (;index < track.KeyFrames.Length; index++)
            {
                if (tick < track.KeyFrames[index].Tick)
                {
                    break;
                }
            }

            if (index == 0 || index >= track.KeyFrames.Length)
            {
                index = track.KeyFrames.Length - 1;
            }

            return index;
        }

        /*
        void UpdateSkinning()
        {
            // Here we should pass bones matrix to uniform
            List<Matrix4x4> boneMatrixArray = new List<Matrix4x4>();
            for (int i = 0; i < MAX_BONES_CNT; i++)
            {
                if (_jointDict.ContainsKey(i))
                {
                    // Here we should get the joint , and calc the matrix
                    var joint = _jointDict[i];
                    boneMatrixArray.Add(joint.skinningMatrix);
                }
                else
                {
                    boneMatrixArray.Add(Matrix4x4.identity);
                }
            }
            foreach (var material in _skinningMaterials)
            {
                material.SetMatrixArray(Shader.PropertyToID("_boneMatrixArray"), boneMatrixArray);
            }
        }
        */

        void UpdateSkinning()
        {
            for (int subMeshIndex = 0; subMeshIndex < _skinningMeshes.Length; subMeshIndex++)
            {
                MshMesh subMesh = _mshFile.SubMeshes[subMeshIndex];
                Mesh mesh = _skinningMeshes[subMeshIndex];
                mesh.SetVertices(BuildVertsWithSkeleton(subMesh));
            }
        }

        private void RenderSkeleton()
        {
            RenderBone(_mshFile.RootBoneNode, gameObject, null);
        }

        private void RenderBone(BoneNode bone, GameObject parent, Joint parentJoint)
        {
            GameObject renderNode = new GameObject();
            renderNode.name = $"[bone][name]{bone.Name} [id]{bone.Id}";
            renderNode.transform.SetParent(parent.transform);

            // display gizmo
            RenderBoneGizmo(renderNode);

            // self pos rotation
            renderNode.transform.localPosition = bone.Translation;
            renderNode.transform.localRotation = bone.Rotation;

            // hold joint to dict
            var joint = new Joint
            {
                GameObject = renderNode,
                BoneNode = bone,
                AnimationTrack = null,
                BindPoseModelToBoneSpace = Matrix4x4.identity
            };

            if (parentJoint != null)
            {
                _joints.Add(bone.Id, joint);
                var transMatrix = Matrix4x4.Translate(bone.Translation);
                var rotMatrix = Matrix4x4.Rotate(bone.Rotation);
                joint.BindPoseModelToBoneSpace = Matrix4x4.Inverse(transMatrix)
                                                 * Matrix4x4.Inverse(rotMatrix)
                                                 * parentJoint.BindPoseModelToBoneSpace;
            }

            // children
            for (int i = 0; i < bone.Children.Length; i++)
            {
                BoneNode subBone = bone.Children[i];
                RenderBone(subBone, renderNode,joint);
            }
        }

        private void RenderBoneGizmo(GameObject boneRenderNode)
        {
            var meshFilter = boneRenderNode.AddComponent<MeshFilter>();
            var meshRenderer = boneRenderNode.AddComponent<MeshRenderer>();

            var mesh = new Mesh();
            mesh.SetVertices(BuildBoneGizmoMesh());
            mesh.SetTriangles(BuildBoneGizmoTriangle(), 0);
            meshFilter.sharedMesh = mesh;

            // material
            Material material = _materialFactory.CreateGizmoMaterial();
            if (material != null)
            {
                meshRenderer.sharedMaterial = material;
                material.renderQueue = 5000; // at last
            }
        }

        private void RenderMesh()
        {
            if (_mshFile.SubMeshes.Length > 0)
            {
                _skinningMaterials = new Material[_mshFile.SubMeshes.Length];
                _skinningMeshes = new Mesh[_mshFile.SubMeshes.Length];
            }

            for (int i = 0; i < _mshFile.SubMeshes.Length; i++)
            {
                MshMesh subMesh = _mshFile.SubMeshes[i];
                RenderSubMesh(subMesh, i);
            }
        }

        private void RenderSubMesh(MshMesh subMesh, int subMeshIndex)
        {
            GameObject subMeshNode = new GameObject($"[submesh] {subMeshIndex}");
            subMeshNode.transform.SetParent(gameObject.transform);

            var meshRenderer = subMeshNode.AddComponent<MeshRenderer>();
            var meshFilter = subMeshNode.AddComponent<MeshFilter>();

            var mesh = new Mesh();
            mesh.MarkDynamic();

            mesh.SetVertices(subMesh.Vertices.Select(v => v.Position).ToArray());
            mesh.SetTriangles(CalculateTriangles(subMesh), 0);

            /*
            mesh.SetUVs(1, BuildBoneIds(subMesh));
            mesh.SetUVs(2, BuildBoneWeights(subMesh));
            */

            //mesh.SetUVs(1,BuildColors(subMesh));

            Vector2[] uvs = new Vector2[subMesh.Vertices.Length];

            foreach (PhyFace phyFace in subMesh.Faces)
            {
                for (int i = 0; i < phyFace.Indices.Length; i++)
                {
                    uvs[phyFace.Indices[i]] = new Vector2(
                        phyFace.U[i, 0],
                        phyFace.V[i, 0]);
                }
            }

            mesh.SetUVs(0, uvs);
            meshFilter.sharedMesh = mesh;

            Material[] materials = _materialFactory.CreateStandardMaterials(
                RendererType.Msh,
                (_textureName, _texture),
                default,
                _tintColor,
                GameBoxBlendFlag.Opaque);

            meshRenderer.sharedMaterial = materials[0];
            _skinningMaterials[subMeshIndex] = materials[0];

            // hold mesh
            _skinningMeshes[subMeshIndex] = mesh;
        }

        private Vector3[] BuildVertsWithSkeleton(MshMesh subMesh)
        {
            Vector3[] verts = new Vector3[subMesh.Vertices.Length];

            for (var i = 0; i < subMesh.Vertices.Length; i++)
            {
                PhyVertex vert = subMesh.Vertices[i];

                int boneId = vert.BoneIds[0];
                Joint joint = _joints[boneId];

                Vector4 homoPos = new Vector4(vert.Position.x, vert.Position.y, vert.Position.z, 1.0f);
                Vector4 posInBone = joint.BindPoseModelToBoneSpace * homoPos;
                Vector4 resultPos = joint.CurrentPoseToModelMatrix * posInBone;

                verts[i] = new Vector3(resultPos.x/resultPos.w, resultPos.y/resultPos.w, resultPos.z/resultPos.w);
            }
            return verts;
        }

        private int[] CalculateTriangles(MshMesh subMesh)
        {
            int[] triangles = new int[subMesh.Faces.Length * 3];

            int index = 0;
            for (int i = 0; i < subMesh.Faces.Length; i++)
            {
                triangles[index++] = subMesh.Faces[i].Indices[0];
                triangles[index++] = subMesh.Faces[i].Indices[1];
                triangles[index++] = subMesh.Faces[i].Indices[2];
            }

            GameBoxInterpreter.ToUnityTriangles(triangles);
            return triangles;
        }

        private List<Vector4> BuildBoneWeights(MshMesh subMesh)
        {
            List<Vector4> weights = new List<Vector4>();
            for (int i = 0; i < subMesh.Vertices.Length; i++)
            {
                var vert = subMesh.Vertices[i];

                float weightSum = vert.Weights[0] +  vert.Weights[1] + vert.Weights[2]+vert.Weights[3];
                Vector4 weightsNormalize = new Vector4(vert.Weights[0],vert.Weights[1],vert.Weights[2],vert.Weights[3]);
                weightsNormalize = weightsNormalize / weightSum;
                var weight = weightsNormalize;
                weights.Add(weight);
            }
            //Debug.Log("weights:" + weights);
            return weights;
        }

        private List<Vector4> BuildColors(MshMesh subMesh)
        {
            List<Vector4> ids = new List<Vector4>();
            for (int i = 0; i < subMesh.Vertices.Length; i++)
            {
                var color = new Vector4(0.0f,1.0f,0.0f,1.0f);
                if (i == 0)
                {
                    color = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
                }
                ids.Add(color);
            }

            return ids;
        }

        private List<Vector4> BuildBoneIds(MshMesh subMesh)
        {
            List<Vector4> ids = new List<Vector4>();
            for (int i = 0; i < subMesh.Vertices.Length; i++)
            {
                var vert = subMesh.Vertices[i];
                var boneIds = new Vector4(vert.BoneIds[0], vert.BoneIds[1], vert.BoneIds[2], vert.BoneIds[3]);
                ids.Add(boneIds);
            }

            return ids;
        }

        private Vector3[] BuildBoneGizmoMesh()
        {
            const float size = 0.02f;
            Vector3[] verts = new Vector3[8];
            verts[0] = new Vector3(-size, -size, -size);
            verts[1] = new Vector3(-size, size, -size);
            verts[2] = new Vector3(size, -size, -size);
            verts[3] = new Vector3(size, size, -size);

            verts[4] = new Vector3(-size, -size, size);
            verts[5] = new Vector3(-size, size, size);
            verts[6] = new Vector3(size, -size, size);
            verts[7] = new Vector3(size, -size, size);

            return verts;
        }

        private int[] BuildBoneGizmoTriangle()
        {
            int[] indices =
            {
                0, 1, 2, 2, 1, 3,
                4, 5, 6, 6, 5, 7,

                4, 5, 0, 0, 5, 1,
                2, 4, 6, 6, 4, 7,

                1, 5, 3, 3, 5, 7,
                0, 4, 2, 2, 4, 6,
            };
            return indices;
        }

        private void OnDisable()
        {
            Dispose();
        }

        public void Dispose()
        {
            PauseAnimation();
        }
    }
}