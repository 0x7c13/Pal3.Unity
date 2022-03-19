// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using Core.DataLoader;
    using Core.DataReader.Cvd;
    using Core.GameBox;
    using Core.Renderer;
    using Core.Utils;
    using Dev;
    using UnityEngine;

    /// <summary>
    /// CVD(.cvd) model renderer
    /// </summary>
    public class CvdModelRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, Texture2D> _textureCache = new ();
        private readonly List<(CvdGeometryNode, Dictionary<int, RenderMeshComponent>)> _renderers = new ();

        private Color _tintColor;

        private float _animationDuration;
        private Coroutine _animation;
        private CancellationTokenSource _animationCts;

        public void Init(CvdFile cvdFile, ITextureResourceProvider textureProvider, Color tintColor, float time)
        {
            _animationDuration = cvdFile.AnimationDuration;
            _tintColor = tintColor;

            foreach (var node in cvdFile.RootNodes)
            {
                BuildTextureCache(node, textureProvider, _textureCache);
            }

            var root = new GameObject("Cvd Mesh");

            for (var i = 0; i < cvdFile.RootNodes.Length; i++)
            {
                var hashKey = $"{i}";
                RenderMeshInternal(
                    time,
                    hashKey,
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

        private int GetFrameIndex(float[] keyTimes, float time)
        {
            int frameIndex = 0;

            if (keyTimes.Length == 1 ||
                time >= keyTimes[^1])
            {
                frameIndex = keyTimes.Length - 1;
            }
            else
            {
                frameIndex = Utility.GetFloorIndex(keyTimes, time);
            }

            return frameIndex;
        }

        private Matrix4x4 GetFrameMatrix(float time, CvdGeometryNode node)
        {
            var position = GetPosition(time, node.PositionKeyInfos);
            var rotation = GetRotation(time, node.RotationKeyInfos);
            var (scale, scaleRotation) = GetScale(time, node.ScaleKeyInfos);

            var scalePreRotation = GameBoxInterpreter.CvdQuaternionToUnityQuaternion(scaleRotation);
            var scaleInverseRotation = Quaternion.Inverse(scalePreRotation);

            return Matrix4x4.Translate(GameBoxInterpreter.CvdPositionToUnityPosition(position))
                   * Matrix4x4.Scale(new Vector3(node.Scale, node.Scale, node.Scale))
                   * Matrix4x4.Rotate(GameBoxInterpreter.CvdQuaternionToUnityQuaternion(rotation))
                   * Matrix4x4.Rotate(scalePreRotation)
                   * Matrix4x4.Scale(GameBoxInterpreter.CvdScaleToUnityScale(scale))
                   * Matrix4x4.Rotate(scaleInverseRotation);
        }

        private void RenderMeshInternal(
            float time,
            string meshName,
            CvdGeometryNode node,
            Dictionary<string, Texture2D> textureCache,
            GameObject parent)
        {
            var nodeMeshes = (node, new Dictionary<int, RenderMeshComponent>());

            var frameIndex = GetFrameIndex(node.Mesh.AnimationTimeKeys, time);
            var frameMatrix = GetFrameMatrix(time, node);

            var influence = 0f;
            if (time > Mathf.Epsilon && frameIndex + 1 < node.Mesh.AnimationTimeKeys.Length)
            {
                influence = (time - node.Mesh.AnimationTimeKeys[frameIndex]) /
                                (node.Mesh.AnimationTimeKeys[frameIndex + 1] -
                                 node.Mesh.AnimationTimeKeys[frameIndex]);
            }

            var meshObject = new GameObject(meshName);
            meshObject.transform.SetParent(parent.transform, false);

            for (var i = 0; i < node.Mesh.MeshSections.Length; i++)
            {
                var meshSection = node.Mesh.MeshSections[i];

                var sectionHashKey = $"{meshName}_{i}";

                var meshDataBuffer = new MeshDataBuffer
                {
                    VertexBuffer = new Vector3[meshSection.FrameVertices[frameIndex].Length],
                    NormalBuffer = new Vector3[meshSection.FrameVertices[frameIndex].Length],
                    UvBuffer = new Vector2[meshSection.FrameVertices[frameIndex].Length],
                };

                UpdateMeshDataBuffer(ref meshDataBuffer,
                    meshSection,
                    frameIndex,
                    influence,
                    frameMatrix);

                if (string.IsNullOrEmpty(meshSection.TextureName) ||
                    !textureCache.ContainsKey(meshSection.TextureName)) continue;

                var meshSectionObject = new GameObject($"{sectionHashKey}");

                // Attach BlendFlag and GameBoxMaterial to the GameObject for better debuggability
                #if UNITY_EDITOR
                var materialInfoPresenter = meshObject.AddComponent<MaterialInfoPresenter>();
                materialInfoPresenter.blendFlag = meshSection.BlendFlag;
                materialInfoPresenter.material = meshSection.Material;
                #endif

                var meshRenderer = meshSectionObject.AddComponent<StaticMeshRenderer>();

                var material = new Material(Shader.Find("Pal3/StandardNoShadow"));
                material.SetTexture(Shader.PropertyToID("_MainTex"), textureCache[meshSection.TextureName]);
                var cutoff = (meshSection.BlendFlag == 1) ? 0.3f : 0f;
                if (cutoff > Mathf.Epsilon)
                {
                    material.SetFloat(Shader.PropertyToID("_Cutoff"), cutoff);
                }

                material.SetColor(Shader.PropertyToID("_TintColor"), _tintColor);

                var renderMesh = meshRenderer.Render(meshDataBuffer.VertexBuffer,
                    GameBoxInterpreter.ToUnityTriangles(meshSection.Triangles),
                    meshDataBuffer.NormalBuffer,
                    meshDataBuffer.UvBuffer,
                    material,
                    true);

                meshRenderer.RecalculateBoundsNormalsAndTangents();

                nodeMeshes.Item2[i] = new RenderMeshComponent
                {
                    Mesh = renderMesh,
                    MeshRenderer = meshRenderer,
                    MeshDataBuffer = meshDataBuffer
                };

                meshSectionObject.transform.SetParent(meshObject.transform, false);
            }

            _renderers.Add(nodeMeshes);

            for (var i = 0; i < node.Children.Length; i++)
            {
                var childMeshName = $"{meshName}-{i}";
                RenderMeshInternal(time, childMeshName, node.Children[i], textureCache, meshObject);
            }
        }

        private void UpdateMeshDataBuffer(ref MeshDataBuffer meshDataBuffer,
            CvdMeshSection meshSection,
            int frameIndex,
            float influence,
            Matrix4x4 matrix)
        {
            var frameVertices = meshSection.FrameVertices[frameIndex];

            if (influence < Mathf.Epsilon)
            {
                for (var i = 0; i < frameVertices.Length; i++)
                {
                    meshDataBuffer.VertexBuffer[i] = matrix.MultiplyPoint3x4(
                        GameBoxInterpreter.CvdPositionToUnityPosition(frameVertices[i].Position));
                    meshDataBuffer.NormalBuffer[i] = GameBoxInterpreter.CvdPositionToUnityPosition(frameVertices[i].Normal);
                    meshDataBuffer.UvBuffer[i] = frameVertices[i].Uv;
                }
            }
            else
            {
                for (var i = 0; i < frameVertices.Length; i++)
                {
                    var toFrameVertices = meshSection.FrameVertices[frameIndex + 1];
                    var lerpPosition = Vector3.Lerp(frameVertices[i].Position, toFrameVertices[i].Position, influence);
                    meshDataBuffer.VertexBuffer[i] = matrix.MultiplyPoint3x4(GameBoxInterpreter.CvdPositionToUnityPosition(lerpPosition));
                    // Ignoring normals here since Unity can help us do the calculation.
                    var lerpUv = Vector2.Lerp(frameVertices[i].Uv, toFrameVertices[i].Uv, influence);
                    meshDataBuffer.UvBuffer[i] = lerpUv;
                }
            }
        }

        private void UpdateMesh(float time)
        {
            foreach (var (node, renderMeshComponents) in _renderers)
            {
                var frameIndex = GetFrameIndex(node.Mesh.AnimationTimeKeys, time);
                var frameMatrix = GetFrameMatrix(time, node);

                var influence = 0f;
                if (time > Mathf.Epsilon && frameIndex + 1 < node.Mesh.AnimationTimeKeys.Length)
                {
                    influence = (time - node.Mesh.AnimationTimeKeys[frameIndex]) /
                                (node.Mesh.AnimationTimeKeys[frameIndex + 1] -
                                 node.Mesh.AnimationTimeKeys[frameIndex]);
                }

                for (var i = 0; i < node.Mesh.MeshSections.Length; i++)
                {
                    if (!renderMeshComponents.ContainsKey(i)) continue;

                    var renderMeshComponent = renderMeshComponents[i];
                    if (!renderMeshComponent.MeshRenderer.IsVisible()) continue;

                    var meshSection = node.Mesh.MeshSections[i];

                    var meshDataBuffer = renderMeshComponent.MeshDataBuffer;
                    UpdateMeshDataBuffer(ref meshDataBuffer,
                        meshSection,
                        frameIndex,
                        influence,
                        frameMatrix);

                    renderMeshComponent.Mesh.vertices = meshDataBuffer.VertexBuffer;
                    renderMeshComponent.Mesh.uv = meshDataBuffer.UvBuffer;
                    renderMeshComponent.Mesh.RecalculateBounds();
                    renderMeshComponent.Mesh.RecalculateNormals();
                    renderMeshComponent.Mesh.RecalculateTangents();
                }
            }
        }

        public void PlayAnimation(float timeScale = 1f, int loopCount = -1, float fps = -1f)
        {
            if (_animationDuration < Mathf.Epsilon) return;

            if (_renderers.Count == 0)
            {
                throw new Exception("Animation not initialized.");
            }

            StopAnimation();

            if (_animation != null) StopCoroutine(_animation);

            _animationCts = new CancellationTokenSource();
            var animationDelay = fps <= 0 ? null : new WaitForSeconds(1 / fps);
            _animation = StartCoroutine(PlayAnimationInternal(timeScale,
                loopCount,
                animationDelay,
                _animationCts.Token));
        }

        private IEnumerator PlayAnimationInternal(float timeScale,
            int loopCount,
            WaitForSeconds animationDelay,
            CancellationToken cancellationToken)
        {
            if (loopCount == -1)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    yield return PlayOneTimeAnimationInternal(timeScale, animationDelay, cancellationToken);
                }
            }
            else if (loopCount > 0)
            {
                while (!cancellationToken.IsCancellationRequested && --loopCount >= 0)
                {
                    yield return PlayOneTimeAnimationInternal(timeScale, animationDelay, cancellationToken);
                }
            }
        }

        private IEnumerator PlayOneTimeAnimationInternal(float timeScale,
            WaitForSeconds animationDelay,
            CancellationToken cancellationToken)
        {
            var startTime = Time.timeSinceLevelLoad;

            while (!cancellationToken.IsCancellationRequested)
            {
                var currentTime = (Time.timeSinceLevelLoad - startTime) * timeScale;

                if (currentTime >= _animationDuration)
                {
                    yield break;
                }

                UpdateMesh(currentTime);

                yield return animationDelay;
            }
        }

        private Vector3 GetPosition(float time, CvdAnimationPositionKeyFrame[] nodePositionInfo)
        {
            if (nodePositionInfo.Length == 1) return nodePositionInfo[0].Position;

            // TODO: Use binary search and interpolate value based on curve type
            for (var i = 0; i < nodePositionInfo.Length - 1; i++)
            {
                var toKeyFrame = nodePositionInfo[i + 1];
                if (time < toKeyFrame.Time)
                {
                    var fromKeyFrame =  nodePositionInfo[i];
                    var influence = (time - fromKeyFrame.Time) /
                                    (toKeyFrame.Time - fromKeyFrame.Time);
                    return Vector3.Lerp(fromKeyFrame.Position, toKeyFrame.Position, influence);
                }
            }

            return nodePositionInfo[^1].Position;
        }

        private (Vector3 Scale, GameBoxQuaternion Rotation) GetScale(float time,
            CvdAnimationScaleKeyFrame[] nodeScaleInfo)
        {
            if (nodeScaleInfo.Length == 1)
            {
                var scaleKeyFrame = nodeScaleInfo[0];
                return (scaleKeyFrame.Scale, scaleKeyFrame.Rotation);
            }

            // TODO: Use binary search and interpolate value based on curve type
            for (var i = 0; i < nodeScaleInfo.Length - 1; i++)
            {
                var toKeyFrame = nodeScaleInfo[i + 1];
                if (time < toKeyFrame.Time)
                {
                    var fromKeyFrame = nodeScaleInfo[i];
                    var influence = (time - fromKeyFrame.Time) /
                                    (toKeyFrame.Time - fromKeyFrame.Time);
                    var calculatedRotation = Quaternion.Lerp(
                        new Quaternion(fromKeyFrame.Rotation.X,
                            fromKeyFrame.Rotation.Y,
                            fromKeyFrame.Rotation.Z,
                            fromKeyFrame.Rotation.W),
                        new Quaternion(toKeyFrame.Rotation.X,
                            toKeyFrame.Rotation.Y,
                            toKeyFrame.Rotation.Z,
                            toKeyFrame.Rotation.W),
                        influence);
                    return (Vector3.Lerp(fromKeyFrame.Scale, toKeyFrame.Scale, influence),
                        new GameBoxQuaternion()
                        {
                            X = calculatedRotation.x,
                            Y = calculatedRotation.y,
                            Z = calculatedRotation.z,
                            W = calculatedRotation.w,
                        });
                }
            }

            var lastKeyFrame = nodeScaleInfo[^1];
            return (lastKeyFrame.Scale, lastKeyFrame.Rotation);
        }

        private GameBoxQuaternion GetRotation(float time,
            CvdAnimationRotationKeyFrame[] nodeRotationInfo)
        {
            if (nodeRotationInfo.Length == 1) return nodeRotationInfo[0].Rotation;

            // TODO: Use binary search and interpolate value based on curve type
            for (var i = 0; i < nodeRotationInfo.Length - 1; i++)
            {
                var toKeyFrame = nodeRotationInfo[i + 1];
                if (time < toKeyFrame.Time)
                {
                    var fromKeyFrame = nodeRotationInfo[i];
                    var influence = (time - fromKeyFrame.Time) /
                                    (toKeyFrame.Time - fromKeyFrame.Time);
                    var calculatedRotation = Quaternion.Lerp(
                        new Quaternion(fromKeyFrame.Rotation.X,
                            fromKeyFrame.Rotation.Y,
                            fromKeyFrame.Rotation.Z,
                            fromKeyFrame.Rotation.W),
                        new Quaternion(toKeyFrame.Rotation.X,
                            toKeyFrame.Rotation.Y,
                            toKeyFrame.Rotation.Z,
                            toKeyFrame.Rotation.W),
                        influence);
                    return new GameBoxQuaternion()
                            {
                                X = calculatedRotation.x,
                                Y = calculatedRotation.y,
                                Z = calculatedRotation.z,
                                W = calculatedRotation.w,
                            };
                }
            }

            return nodeRotationInfo[^1].Rotation;
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

        private void OnDisable()
        {
            Dispose();
        }

        private void Dispose()
        {
            StopAnimation();
            foreach (var meshRenderer in GetComponentsInChildren<StaticMeshRenderer>())
            {
                Destroy(meshRenderer.gameObject);
            }
        }
    }
}