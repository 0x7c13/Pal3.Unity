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
    using Core.DataLoader;
    using Core.DataReader.Cvd;
    using Core.Renderer;
    using Core.Utils;
    using Dev;
    using Material;
    using UnityEngine;

    /// <summary>
    /// CVD(.cvd) model renderer
    /// </summary>
    public class CvdModelRenderer : MonoBehaviour, IDisposable
    {
        private readonly Dictionary<string, Texture2D> _textureCache = new ();
        private readonly List<(CvdGeometryNode, Dictionary<int, RenderMeshComponent>)> _renderers = new ();

        private Color _tintColor;

        private float _currentTime;
        private float _animationDuration;
        private CancellationTokenSource _animationCts = new ();

        private IMaterialFactory _materialFactory;

        public void Init(CvdFile cvdFile,
            ITextureResourceProvider textureProvider,
            IMaterialFactory materialFactory,
            Color? tintColor = default,
            float initTime = 0f)
        {
            _materialFactory = materialFactory;
            _animationDuration = cvdFile.AnimationDuration;
            _tintColor = tintColor ?? Color.white;
            _currentTime = initTime;

            foreach (CvdGeometryNode node in cvdFile.RootNodes)
            {
                BuildTextureCache(node, textureProvider, _textureCache);
            }

            var root = new GameObject("Cvd Mesh");

            for (var i = 0; i < cvdFile.RootNodes.Length; i++)
            {
                var hashKey = $"{i}";
                RenderMeshInternal(
                    initTime,
                    hashKey,
                    cvdFile.RootNodes[i],
                    _textureCache,
                    root);
            }

            root.transform.SetParent(gameObject.transform, false);
        }

        public float GetCurrentTime()
        {
            return _currentTime;
        }

        public void SetCurrentTime(float time)
        {
            UpdateMesh(time);
        }

        public float GetDefaultAnimationDuration(float timeScale = 1f)
        {
            if (timeScale == 0f) return 0f;
            return _animationDuration / Mathf.Abs(timeScale);
        }

        public Bounds GetRendererBounds()
        {
            var boundsInitialized = false;
            var bounds = new Bounds(transform.position, Vector3.one);

            foreach ((CvdGeometryNode node, Dictionary<int, RenderMeshComponent> meshComponents) in _renderers)
            {
                if (!node.IsGeometryNode) continue;

                foreach(RenderMeshComponent meshComponent in meshComponents.Values)
                {
                    Bounds rendererBounds = meshComponent.MeshRenderer.GetRendererBounds();
                    if (!boundsInitialized)
                    {
                        bounds = rendererBounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(rendererBounds);
                    }
                }
            }

            return bounds;
        }

        public Bounds GetMeshBounds()
        {
            var boundsInitialized = false;
            var bounds = new Bounds(Vector3.zero, Vector3.one);

            foreach ((CvdGeometryNode node, Dictionary<int, RenderMeshComponent> meshComponents) in _renderers)
            {
                if (!node.IsGeometryNode) continue;

                foreach(RenderMeshComponent meshComponent in meshComponents.Values)
                {
                    Bounds meshBounds = meshComponent.MeshRenderer.GetMeshBounds();
                    if (!boundsInitialized)
                    {
                        bounds = meshBounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(meshBounds);
                    }
                }
            }

            return bounds;
        }

        private void BuildTextureCache(CvdGeometryNode node,
            ITextureResourceProvider textureProvider,
            Dictionary<string, Texture2D> textureCache)
        {
            if (node.IsGeometryNode)
            {
                foreach (CvdMeshSection meshSection in node.Mesh.MeshSections)
                {
                    string textureName = meshSection.Material.TextureFileNames[0];
                    if (string.IsNullOrEmpty(textureName)) continue;
                    if (textureCache.ContainsKey(textureName)) continue;
                    Texture2D texture2D = textureProvider.GetTexture(textureName);
                    if (texture2D != null) textureCache[textureName] = texture2D;
                }
            }

            foreach (CvdGeometryNode childNode in node.Children)
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
            Vector3 position = GetPosition(time, node.PositionKeyInfos);
            Quaternion rotation = GetRotation(time, node.RotationKeyInfos);
            (Vector3 scale, Quaternion scaleRotation) = GetScale(time, node.ScaleKeyInfos);

            Quaternion scalePreRotation = scaleRotation;
            Quaternion scaleInverseRotation = Quaternion.Inverse(scalePreRotation);

            return Matrix4x4.Translate(position)
                   * Matrix4x4.Scale(new Vector3(node.Scale, node.Scale, node.Scale))
                   * Matrix4x4.Rotate(rotation)
                   * Matrix4x4.Rotate(scalePreRotation)
                   * Matrix4x4.Scale(scale)
                   * Matrix4x4.Rotate(scaleInverseRotation);
        }

        private void RenderMeshInternal(
            float initTime,
            string meshName,
            CvdGeometryNode node,
            Dictionary<string, Texture2D> textureCache,
            GameObject parent)
        {
            var meshObject = new GameObject(meshName);
            meshObject.transform.SetParent(parent.transform, false);

            if (node.IsGeometryNode)
            {
                var nodeMeshes = (node, new Dictionary<int, RenderMeshComponent>());

                var frameIndex = GetFrameIndex(node.Mesh.AnimationTimeKeys, initTime);
                Matrix4x4 frameMatrix = GetFrameMatrix(initTime, node);

                var influence = 0f;
                if (initTime > Mathf.Epsilon && frameIndex + 1 < node.Mesh.AnimationTimeKeys.Length)
                {
                    influence = (initTime - node.Mesh.AnimationTimeKeys[frameIndex]) /
                                (node.Mesh.AnimationTimeKeys[frameIndex + 1] -
                                 node.Mesh.AnimationTimeKeys[frameIndex]);
                }

                for (var i = 0; i < node.Mesh.MeshSections.Length; i++)
                {
                    CvdMeshSection meshSection = node.Mesh.MeshSections[i];

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

                    string textureName = meshSection.Material.TextureFileNames[0];

                    if (string.IsNullOrEmpty(textureName) || !textureCache.ContainsKey(textureName)) continue;

                    var meshSectionObject = new GameObject($"{sectionHashKey}");

                    // Attach BlendFlag and GameBoxMaterial to the GameObject for better debuggability.
                    #if UNITY_EDITOR
                    var materialInfoPresenter = meshObject.AddComponent<MaterialInfoPresenter>();
                    materialInfoPresenter.blendFlag = meshSection.BlendFlag;
                    materialInfoPresenter.material = meshSection.Material;
                    #endif

                    Material[] materials = _materialFactory.CreateStandardMaterials(
                        RendererType.Cvd,
                        (textureName, textureCache[textureName]),
                        shadowTexture: (null, null), // CVD models don't have shadow textures
                        _tintColor,
                        meshSection.BlendFlag);

                    var meshRenderer = meshSectionObject.AddComponent<StaticMeshRenderer>();
                    Mesh renderMesh = meshRenderer.Render(
                        ref meshDataBuffer.VertexBuffer,
                        ref meshSection.Triangles,
                        ref meshDataBuffer.NormalBuffer,
                        ref meshDataBuffer.UvBuffer,
                        ref meshDataBuffer.UvBuffer,
                        ref materials,
                        true);

                    renderMesh.RecalculateNormals();
                    renderMesh.RecalculateTangents();
                    renderMesh.RecalculateBounds();

                    nodeMeshes.Item2[i] = new RenderMeshComponent
                    {
                        Mesh = renderMesh,
                        MeshRenderer = meshRenderer,
                        MeshDataBuffer = meshDataBuffer
                    };

                    meshSectionObject.transform.SetParent(meshObject.transform, false);
                }

                _renderers.Add(nodeMeshes);
            }

            for (var i = 0; i < node.Children.Length; i++)
            {
                var childMeshName = $"{meshName}-{i}";
                RenderMeshInternal(initTime,
                    childMeshName,
                    node.Children[i],
                    textureCache,
                    meshObject);
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
                    meshDataBuffer.VertexBuffer[i] = matrix.MultiplyPoint3x4(frameVertices[i].Position);
                    meshDataBuffer.UvBuffer[i] = frameVertices[i].Uv;
                }
            }
            else
            {
                for (var i = 0; i < frameVertices.Length; i++)
                {
                    var toFrameVertices = meshSection.FrameVertices[frameIndex + 1];
                    Vector3 lerpPosition = Vector3.Lerp(frameVertices[i].Position, toFrameVertices[i].Position, influence);
                    meshDataBuffer.VertexBuffer[i] = matrix.MultiplyPoint3x4(lerpPosition);
                    Vector2 lerpUv = Vector2.Lerp(frameVertices[i].Uv, toFrameVertices[i].Uv, influence);
                    meshDataBuffer.UvBuffer[i] = lerpUv;
                }
            }
        }

        private void UpdateMesh(float time)
        {
            _currentTime = time;

            foreach ((CvdGeometryNode node, var renderMeshComponents) in _renderers)
            {
                var frameIndex = GetFrameIndex(node.Mesh.AnimationTimeKeys, time);
                Matrix4x4 frameMatrix = GetFrameMatrix(time, node);

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

                    RenderMeshComponent renderMeshComponent = renderMeshComponents[i];

                    CvdMeshSection meshSection = node.Mesh.MeshSections[i];

                    MeshDataBuffer meshDataBuffer = renderMeshComponent.MeshDataBuffer;
                    UpdateMeshDataBuffer(ref meshDataBuffer,
                        meshSection,
                        frameIndex,
                        influence,
                        frameMatrix);

                    renderMeshComponent.Mesh.SetVertices(meshDataBuffer.VertexBuffer);
                    renderMeshComponent.Mesh.SetUVs(0, meshDataBuffer.UvBuffer);
                    renderMeshComponent.Mesh.RecalculateBounds();
                }
            }
        }

        public IEnumerator PlayOneTimeAnimationAsync(bool startFromBeginning, float timeScale = 1f)
        {
            yield return PlayAnimationAsync(timeScale, 1, 1f, startFromBeginning);
        }

        public void StartOneTimeAnimation(bool startFromBeginning, float timeScale = 1f, Action onFinished = null)
        {
            StartCoroutine(PlayAnimationAsync(timeScale, 1, 1f, startFromBeginning, onFinished));
        }

        public void LoopAnimation(float timeScale = 1f)
        {
            StartCoroutine(PlayAnimationAsync(timeScale, -1, 1f, true));
        }

        /// <summary>
        /// Play CVD animation.
        /// </summary>
        /// <param name="timeScale">1f: default scale of time, -1f: reverse animation with default speed</param>
        /// <param name="loopCount">Loop count, -1 means loop forever</param>
        /// <param name="durationPercentage">1f: full length of the animation, .5f: half of the animation</param>
        /// <param name="startFromBeginning">Start the animation from beginning instead of current time</param>
        /// <param name="onFinished">On animation finished playing</param>
        public IEnumerator PlayAnimationAsync(float timeScale,
            int loopCount,
            float durationPercentage,
            bool startFromBeginning,
            Action onFinished = null)
        {
            if (timeScale == 0f ||
                durationPercentage is <= 0f or > 1f ||
                _animationDuration < Mathf.Epsilon ||
                _renderers.Count == 0)
            {
                Debug.LogError($"[{nameof(CvdModelRenderer)}] Invalid parameters for playing CVD animation.");
                yield break;
            }

            StopCurrentAnimation();

            if (!_animationCts.IsCancellationRequested) _animationCts.Cancel();

            _animationCts = new CancellationTokenSource();

            yield return PlayAnimationInternalAsync(timeScale,
                _animationDuration * durationPercentage,
                loopCount,
                startFromBeginning,
                onFinished,
                _animationCts.Token);
        }

        private IEnumerator PlayAnimationInternalAsync(float timeScale,
            float duration,
            int loopCount,
            bool startFromBeginning,
            Action onFinished,
            CancellationToken cancellationToken)
        {
            if (loopCount == -1)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    yield return PlayOneTimeAnimationInternalAsync(timeScale, duration, startFromBeginning, cancellationToken);
                }
            }
            else if (loopCount > 0)
            {
                while (!cancellationToken.IsCancellationRequested && --loopCount >= 0)
                {
                    yield return PlayOneTimeAnimationInternalAsync(timeScale, duration, startFromBeginning, cancellationToken);
                }
            }

            onFinished?.Invoke();
        }

        private IEnumerator PlayOneTimeAnimationInternalAsync(float timeScale,
            float duration,
            bool startFromBeginning,
            CancellationToken cancellationToken)
        {
            float startTime;

            if (startFromBeginning)
            {
                startTime = Time.timeSinceLevelLoad;
            }
            else
            {
                startTime = Time.timeSinceLevelLoad - _currentTime;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var currentTime = timeScale > 0 ?
                        (Time.timeSinceLevelLoad - startTime) * timeScale :
                        (duration - (Time.timeSinceLevelLoad - startTime)) * -timeScale;

                if ((timeScale > 0f && currentTime >= duration) ||
                    (timeScale < 0f && currentTime <= 0f))
                {
                    yield break;
                }

                UpdateMesh(currentTime);

                yield return null;
            }
        }

        private Vector3 GetPosition(float time, CvdAnimationPositionKeyFrame[] nodePositionInfo)
        {
            if (nodePositionInfo.Length == 1) return nodePositionInfo[0].Position;

            // Find the two keyframes that the current time lies between
            // using binary search
            int startIndex = 0;
            int endIndex = nodePositionInfo.Length - 1;
            while (endIndex - startIndex > 1)
            {
                int midIndex = (startIndex + endIndex) / 2;
                if (nodePositionInfo[midIndex].Time > time)
                {
                    endIndex = midIndex;
                }
                else
                {
                    startIndex = midIndex;
                }
            }

            // Interpolate between the two keyframes based on the time
            CvdAnimationPositionKeyFrame fromKeyFrame = nodePositionInfo[startIndex];
            CvdAnimationPositionKeyFrame toKeyFrame = nodePositionInfo[endIndex];
            float influence = (time - fromKeyFrame.Time) / (toKeyFrame.Time - fromKeyFrame.Time);
            return Vector3.Lerp(fromKeyFrame.Position, toKeyFrame.Position, influence);
        }

        private (Vector3 Scale, Quaternion Rotation) GetScale(float time,
            CvdAnimationScaleKeyFrame[] nodeScaleInfo)
        {
            if (nodeScaleInfo.Length == 1)
            {
                CvdAnimationScaleKeyFrame scaleKeyFrame = nodeScaleInfo[0];
                return (scaleKeyFrame.Scale, scaleKeyFrame.Rotation);
            }

            // Find the two keyframes that the current time lies between
            // using binary search
            int startIndex = 0;
            int endIndex = nodeScaleInfo.Length - 1;
            while (endIndex - startIndex > 1)
            {
                int midIndex = (startIndex + endIndex) / 2;
                if (nodeScaleInfo[midIndex].Time > time)
                {
                    endIndex = midIndex;
                }
                else
                {
                    startIndex = midIndex;
                }
            }

            // Interpolate between the two keyframes based on the time
            CvdAnimationScaleKeyFrame fromKeyFrame = nodeScaleInfo[startIndex];
            CvdAnimationScaleKeyFrame toKeyFrame = nodeScaleInfo[endIndex];
            float influence = (time - fromKeyFrame.Time) / (toKeyFrame.Time - fromKeyFrame.Time);
            Quaternion calculatedRotation = Quaternion.Slerp(fromKeyFrame.Rotation, toKeyFrame.Rotation, influence);
            Vector3 calculatedScale = Vector3.Lerp(fromKeyFrame.Scale, toKeyFrame.Scale, influence);
            return (calculatedScale, calculatedRotation);
        }

        private Quaternion GetRotation(float time,
            CvdAnimationRotationKeyFrame[] nodeRotationInfo)
        {
            if (nodeRotationInfo.Length == 1) return nodeRotationInfo[0].Rotation;

            // Find the two keyframes that the current time lies between
            int startIndex = 0;
            int endIndex = nodeRotationInfo.Length - 1;
            while (endIndex - startIndex > 1)
            {
                int midIndex = (startIndex + endIndex) / 2;
                if (nodeRotationInfo[midIndex].Time > time)
                {
                    endIndex = midIndex;
                }
                else
                {
                    startIndex = midIndex;
                }
            }

            // Interpolate between the two keyframes based on the curve type
            CvdAnimationRotationKeyFrame fromKeyFrame = nodeRotationInfo[startIndex];
            CvdAnimationRotationKeyFrame toKeyFrame = nodeRotationInfo[endIndex];
            float influence = (time - fromKeyFrame.Time) / (toKeyFrame.Time - fromKeyFrame.Time);
            return Quaternion.Slerp(fromKeyFrame.Rotation, toKeyFrame.Rotation, influence);
        }

        public void StopCurrentAnimation()
        {
            _animationCts.Cancel();
        }

        private void OnDisable()
        {
            Dispose();
        }

        public void Dispose()
        {
            StopCurrentAnimation();

            foreach (StaticMeshRenderer meshRenderer in GetComponentsInChildren<StaticMeshRenderer>())
            {
                _materialFactory.ReturnToPool(meshRenderer.GetMaterials());
                Destroy(meshRenderer.gameObject);
            }
        }
    }
}