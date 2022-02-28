// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using System.Collections.Generic;
    using Core.DataLoader;
    using Core.DataReader.Cvd;
    using Core.GameBox;
    using Core.Renderer;
    using Core.Utils;
    using UnityEngine;

    /// <summary>
    /// CVD(.cvd) file renderer
    /// </summary>
    public class CvdMeshRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, Texture2D> _textureCache = new ();
        private Color _tintColor;

        public void Render(CvdFile cvdFile, ITextureResourceProvider textureProvider, Color tintColor)
        {
            _tintColor = tintColor;

            foreach (var node in cvdFile.RootNodes)
            {
                BuildTextureCache(node, textureProvider, _textureCache);
            }

            var root = new GameObject("Cvd Mesh");

            for (var i = 0; i < cvdFile.RootNodes.Length; i++)
            {
                RenderMeshInternal(
                    i,
                    cvdFile.RootNodes[i],
                    _textureCache,
                    root);
            }

            root.transform.SetParent(gameObject.transform, false);
        }

        private void BuildTextureCache(CvdGeometryNode node,
            ITextureResourceProvider textureProvider,
            Dictionary<string, Texture2D> textureCache)
        {
            foreach (var meshSection in node.Mesh.MeshSections)
            {
                if (string.IsNullOrEmpty(meshSection.TextureName)) continue;
                if (textureCache.ContainsKey(meshSection.TextureName)) continue;
                var texture2D = textureProvider.GetTexture(meshSection.TextureName);
                if (texture2D != null) textureCache[meshSection.TextureName] = texture2D;
            }

            foreach (var childNode in node.Children)
            {
                BuildTextureCache(childNode, textureProvider, textureCache);
            }
        }

        private void RenderMeshInternal(
            int meshIndex,
            CvdGeometryNode node,
            Dictionary<string, Texture2D> textureCache,
            GameObject parent)
        {
            // Debug.Log($"{node.Mesh.Frames.Length} " +
            //                  $"{node.PositionKeyInfos.Length} " +
            //                  $"{node.RotationKeyInfos.Length} " +
            //                  $"{node.ScaleKeyInfos.Length}");

            var frameIndex = 0;
            var frameVertices = node.Mesh.Frames[frameIndex];

            var meshGameObject = new GameObject($"{meshIndex}");
            meshGameObject.transform.SetParent(parent.transform, false);

            var position = GetPosition(node.PositionKeyInfos[frameIndex]);
            var rotation = GetRotation(node.RotationKeyInfos[frameIndex]);
            var (scale, scaleRotation) = GetScale(node.ScaleKeyInfos[frameIndex]);

            var scalePreRotation = GameBoxInterpreter.CvdQuaternionToUnityQuaternion(scaleRotation);
            var scaleInverseRotation = Quaternion.Inverse(scalePreRotation);

            for (var i = 0; i < node.Mesh.MeshSections.Length; i++)
            {
                var meshSection = node.Mesh.MeshSections[i];

                var vertices = new List<Vector3>();
                var normals = new List<Vector3>();
                var uv = new List<Vector2>();

                var indexBuffer = new List<int>();
                var triangles = GenerateTriangles(meshSection, indexBuffer);

                GameBoxInterpreter.ToUnityTriangles(triangles);

                for (var j = 0; j < indexBuffer.Count; j++)
                {
                    vertices.Add(GameBoxInterpreter.CvdPositionToUnityPosition(
                        frameVertices[indexBuffer[j]].Position));
                    normals.Add(frameVertices[indexBuffer[j]].Normal);
                    uv.Add(frameVertices[indexBuffer[j]].Uv);
                }

                if (string.IsNullOrEmpty(meshSection.TextureName) ||
                    !textureCache.ContainsKey(meshSection.TextureName)) continue;

                var meshSectionGameObject = new GameObject($"{i}");
                var meshRenderer = meshSectionGameObject.AddComponent<StaticMeshRenderer>();

                var material = new Material(Shader.Find("Pal3/StandardNoShadow"));
                material.SetTexture(Shader.PropertyToID("_MainTex"), textureCache[meshSection.TextureName]);
                var cutoff = (meshSection.BlendFlag == 1) ? 0.3f : 0f;
                if (cutoff > Mathf.Epsilon)
                {
                    material.SetFloat(Shader.PropertyToID("_Cutoff"), cutoff);
                }
                material.SetColor(Shader.PropertyToID("_TintColor"), _tintColor);

                meshRenderer.Render(vertices.ToArray(),
                    triangles.ToArray(),
                    normals.ToArray(),
                    uv.ToArray(),
                    material);

                meshRenderer.ApplyMatrix(
                      Matrix4x4.Translate(GameBoxInterpreter.CvdPositionToUnityPosition(position))
                    * Matrix4x4.Scale(new Vector3(node.Scale, node.Scale, node.Scale))
                    * Matrix4x4.Rotate(GameBoxInterpreter.CvdQuaternionToUnityQuaternion(rotation))
                    * Matrix4x4.Rotate(scalePreRotation)
                    * Matrix4x4.Scale(GameBoxInterpreter.CvdScaleToUnityScale(scale))
                    * Matrix4x4.Rotate(scaleInverseRotation));

                meshSectionGameObject.transform.SetParent(meshGameObject.transform, false);
            }

            for (var i = 0; i < node.Children.Length; i++)
            {
                RenderMeshInternal(i, node.Children[i], textureCache, meshGameObject);
            }
        }

        private List<int> GenerateTriangles(CvdMeshSection meshSection, List<int> indexBuffer)
        {
            var triangles = new List<int>();
            var indexMap = new Dictionary<int, int>();

            for (var j = 0; j < meshSection.Triangles.Length; j++)
            {
                var indices = new[]
                {
                    meshSection.Triangles[j].x,
                    meshSection.Triangles[j].y,
                    meshSection.Triangles[j].z
                };

                for (var k = 0; k < 3; k++)
                {
                    if (indexMap.ContainsKey(indices[k]))
                    {
                        triangles.Add(indexMap[indices[k]]);
                    }
                    else
                    {
                        var index = indexBuffer.Count;
                        indexBuffer.Add(indices[k]);
                        indexMap[indices[k]] = index;
                        triangles.Add(index);
                    }
                }
            }

            return triangles;
        }

        public Vector3 GetPosition((CvdAnimationKeyType KeyType, byte[] Data) nodePositionInfo)
        {
            Vector3 position = Vector3.zero;

            switch (nodePositionInfo.KeyType)
            {
                case CvdAnimationKeyType.Tcb:
                {
                    var positionKey = Utility.ReadStruct<CvdTcbVector3Key>(nodePositionInfo.Data);
                    position = positionKey.Value;
                    break;
                }
                case CvdAnimationKeyType.Bezier:
                {
                    var positionKey = Utility.ReadStruct<CvdBezierVector3Key>(nodePositionInfo.Data);
                    position = positionKey.Value;
                    break;
                }
                case CvdAnimationKeyType.Linear:
                {
                    var positionKey = Utility.ReadStruct<CvdLinearVector3Key>(nodePositionInfo.Data);
                    position = positionKey.Value;
                    break;
                }
            }

            return position;
        }

        public (Vector3 Scale, GameBoxQuaternion Rotation) GetScale(
            (CvdAnimationKeyType KeyType, byte[] Data) nodeScaleInfo)
        {
            switch (nodeScaleInfo.KeyType)
            {
                case CvdAnimationKeyType.Tcb:
                {
                    var scaleKey = Utility.ReadStruct<CvdTcbScaleKey>(nodeScaleInfo.Data);
                    return new(scaleKey.Value, scaleKey.Rotation);
                }
                case CvdAnimationKeyType.Bezier:
                {
                    var scaleKey = Utility.ReadStruct<CvdBezierScaleKey>(nodeScaleInfo.Data);
                    return new(scaleKey.Value, scaleKey.Rotation);
                }
                case CvdAnimationKeyType.Linear:
                {
                    var scaleKey = Utility.ReadStruct<CvdLinearScaleKey>(nodeScaleInfo.Data);
                    return new(scaleKey.Value, scaleKey.Rotation);
                }
            }

            return new (new Vector3(1f, 1f, 1f), new GameBoxQuaternion());
        }

        public GameBoxQuaternion GetRotation((CvdAnimationKeyType KeyType, byte[] Data) nodeRotationInfo)
        {
            GameBoxQuaternion rotation = default;

            switch (nodeRotationInfo.KeyType)
            {
                case CvdAnimationKeyType.Tcb:
                {
                    var rotationKey = Utility.ReadStruct<CvdTcbRotationKey>(nodeRotationInfo.Data);
                    // TODO: calculate TCB rotation based on Vector3 and Angle
                    break;
                }
                case CvdAnimationKeyType.Bezier:
                {
                    var rotationKey = Utility.ReadStruct<CvdBezierRotationKey>(nodeRotationInfo.Data);
                    rotation = rotationKey.Value;
                    break;
                }
                case CvdAnimationKeyType.Linear:
                {
                    var rotationKey = Utility.ReadStruct<CvdLinearRotationKey>(nodeRotationInfo.Data);
                    rotation = rotationKey.Value;
                    break;
                }
            }

            return rotation;
        }

        private void OnDisable()
        {
            Dispose();
        }

        private void Dispose()
        {
            foreach (var meshRenderer in GetComponentsInChildren<StaticMeshRenderer>())
            {
                Destroy(meshRenderer.gameObject);
            }
        }
    }
}