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
    using Core.DataReader.Cvd;
    using Core.Utilities;
    using Dev.Presenters;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.DataLoader;
    using Engine.Extensions;
    using Engine.Logging;
    using Engine.Renderer;
    using Engine.Services;
    using Material;
    using UnityEngine;
    using Color = Core.Primitives.Color;

    /// <summary>
    /// CVD(.cvd) model renderer
    /// </summary>
    public sealed class CvdModelRenderer : GameEntityScript, IDisposable
    {
        private readonly Dictionary<string, Texture2D> _textureCache = new ();
        private readonly List<(CvdGeometryNode, Dictionary<int, RenderMeshComponent>)> _renderers = new ();

        private Color _tintColor;

        private float _currentAnimationTime;
        private float _animationDuration;
        private CancellationTokenSource _animationCts = new ();

        private IMaterialFactory _materialFactory;

        protected override void OnEnableGameEntity()
        {
        }

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

        public void Init(CvdFile cvdFile,
            ITextureResourceProvider textureProvider,
            IMaterialFactory materialFactory,
            Color? tintColor = default,
            float initTime = 0f)
        {
            _materialFactory = materialFactory;
            _animationDuration = cvdFile.AnimationDuration;
            _tintColor = tintColor ?? Color.White;
            _currentAnimationTime = initTime;

            foreach (CvdGeometryNode node in cvdFile.RootNodes)
            {
                BuildTextureCache(node, textureProvider, _textureCache);
            }

            IGameEntity root = GameEntityFactory.Create("Cvd Mesh",
                GameEntity, worldPositionStays: false);

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
        }

        public float GetCurrentAnimationTime()
        {
            return _currentAnimationTime;
        }

        public void SetCurrentTime(float time)
        {
            UpdateMesh(time);
        }

        public float GetDefaultAnimationDuration(float timeScale = 1f)
        {
            if (timeScale == 0f) return 0f;
            return _animationDuration / MathF.Abs(timeScale);
        }

        public Bounds GetRendererBounds()
        {
            var boundsInitialized = false;
            var bounds = new Bounds(Transform.Position, Vector3.one);

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
                frameIndex = CoreUtility.GetFloorIndex(keyTimes, time);
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
            IGameEntity parent)
        {
            IGameEntity meshEntity = GameEntityFactory.Create(meshName, parent, worldPositionStays: false);

            if (node.IsGeometryNode)
            {
                var nodeMeshes = (node, new Dictionary<int, RenderMeshComponent>());

                var frameIndex = GetFrameIndex(node.Mesh.AnimationTimeKeys, initTime);
                Matrix4x4 frameMatrix = GetFrameMatrix(initTime, node);

                var influence = 0f;
                if (initTime > float.Epsilon && frameIndex + 1 < node.Mesh.AnimationTimeKeys.Length)
                {
                    influence = (initTime - node.Mesh.AnimationTimeKeys[frameIndex]) /
                                (node.Mesh.AnimationTimeKeys[frameIndex + 1] -
                                 node.Mesh.AnimationTimeKeys[frameIndex]);
                }

                for (var i = 0; i < node.Mesh.MeshSections.Length; i++)
                {
                    CvdMeshSection meshSection = node.Mesh.MeshSections[i];

                    string sectionHashKey = $"{meshName}_{i}";

                    MeshDataBuffer meshDataBuffer = new()
                    {
                        VertexBuffer = new Vector3[meshSection.FrameVertices[frameIndex].Length],
                        NormalBuffer = new Vector3[meshSection.FrameVertices[frameIndex].Length],
                        UvBuffer = new Vector2[meshSection.FrameVertices[frameIndex].Length],
                        TriangleBuffer = meshSection.GameBoxTriangles.ToUnityTriangles()
                    };

                    UpdateMeshDataBuffer(ref meshDataBuffer,
                        meshSection,
                        frameIndex,
                        influence,
                        frameMatrix);

                    string textureName = meshSection.Material.TextureFileNames[0];

                    if (string.IsNullOrEmpty(textureName) || !textureCache.ContainsKey(textureName)) continue;

                    IGameEntity meshSectionEntity = GameEntityFactory.Create($"{sectionHashKey}",
                        meshEntity, worldPositionStays: false);

                    // Attach BlendFlag and GameBoxMaterial to the GameEntity for better debuggability.
                    #if UNITY_EDITOR
                    var materialInfoPresenter = meshEntity.AddComponent<MaterialInfoPresenter>();
                    materialInfoPresenter.blendFlag = meshSection.BlendFlag;
                    materialInfoPresenter.material = meshSection.Material;
                    #endif

                    Material[] materials = _materialFactory.CreateStandardMaterials(
                        RendererType.Cvd,
                        mainTexture: (textureName, textureCache[textureName]),
                        shadowTexture: default, // CVD models don't have shadow textures
                        tintColor: _tintColor,
                        blendFlag: meshSection.BlendFlag);

                    var meshRenderer = meshSectionEntity.AddComponent<StaticMeshRenderer>();
                    Mesh renderMesh = meshRenderer.Render(
                        vertices: meshDataBuffer.VertexBuffer,
                        triangles: meshDataBuffer.TriangleBuffer,
                        normals: meshDataBuffer.NormalBuffer,
                        mainTextureUvs: (channel: 0, uvs: meshDataBuffer.UvBuffer),
                        secondaryTextureUvs: default, // CVD models don't have secondary texture
                        materials: materials,
                        isDynamic: true);

                    renderMesh.RecalculateNormals();
                    renderMesh.RecalculateTangents();
                    renderMesh.RecalculateBounds();

                    nodeMeshes.Item2[i] = new RenderMeshComponent
                    {
                        Mesh = renderMesh,
                        MeshRenderer = meshRenderer,
                        MeshDataBuffer = meshDataBuffer
                    };
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
                    meshEntity);
            }
        }

        private void UpdateMeshDataBuffer(ref MeshDataBuffer meshDataBuffer,
            CvdMeshSection meshSection,
            int frameIndex,
            float influence,
            Matrix4x4 matrix)
        {
            var frameVertices = meshSection.FrameVertices[frameIndex];

            if (influence < float.Epsilon)
            {
                for (var i = 0; i < frameVertices.Length; i++)
                {
                    meshDataBuffer.VertexBuffer[i] = matrix.MultiplyPoint3x4(
                        frameVertices[i].GameBoxPosition.CvdPositionToUnityPosition());
                    meshDataBuffer.UvBuffer[i] = frameVertices[i].Uv.ToUnityVector2();
                }
            }
            else
            {
                for (var i = 0; i < frameVertices.Length; i++)
                {
                    var toFrameVertices = meshSection.FrameVertices[frameIndex + 1];
                    Vector3 lerpPosition = Vector3.Lerp(
                        frameVertices[i].GameBoxPosition.CvdPositionToUnityPosition(),
                        toFrameVertices[i].GameBoxPosition.CvdPositionToUnityPosition(), influence);
                    meshDataBuffer.VertexBuffer[i] = matrix.MultiplyPoint3x4(lerpPosition);
                    Vector2 lerpUv = Vector2.Lerp(
                        frameVertices[i].Uv.ToUnityVector2(),
                        toFrameVertices[i].Uv.ToUnityVector2(), influence);
                    meshDataBuffer.UvBuffer[i] = lerpUv;
                }
            }
        }

        private void UpdateMesh(float time)
        {
            _currentAnimationTime = time;

            foreach ((CvdGeometryNode node, var renderMeshComponents) in _renderers)
            {
                var frameIndex = GetFrameIndex(node.Mesh.AnimationTimeKeys, time);
                Matrix4x4 frameMatrix = GetFrameMatrix(time, node);

                var influence = 0f;
                if (time > float.Epsilon && frameIndex + 1 < node.Mesh.AnimationTimeKeys.Length)
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
                _animationDuration < float.Epsilon ||
                _renderers.Count == 0)
            {
                EngineLogger.LogError("Invalid parameters for playing CVD animation");
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
            double startTime = GameTimeProvider.Instance.TimeSinceStartup - (startFromBeginning ? 0 : _currentAnimationTime);

            while (!cancellationToken.IsCancellationRequested)
            {
                // Calculate the elapsed time and adjust it based on the time scale.
                float elapsed = (float)(GameTimeProvider.Instance.TimeSinceStartup - startTime);
                float adjustedTime = timeScale > 0 ? elapsed * timeScale : (duration - elapsed) * -timeScale;

                // Check if the animation is complete based on the time scale and either the duration or the zero-bound.
                bool isComplete = timeScale > 0 ? adjustedTime >= duration : adjustedTime <= 0;
                if (isComplete)
                {
                    yield break;
                }

                UpdateMesh(adjustedTime);

                yield return null;
            }
        }

        private Vector3 GetPosition(float time, CvdAnimationPositionKeyFrame[] nodePositionInfo)
        {
            if (nodePositionInfo.Length == 1) return nodePositionInfo[0].GameBoxPosition.CvdPositionToUnityPosition();

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
            return Vector3.Lerp(fromKeyFrame.GameBoxPosition.CvdPositionToUnityPosition(),
                toKeyFrame.GameBoxPosition.CvdPositionToUnityPosition(), influence);
        }

        private (Vector3 Scale, Quaternion Rotation) GetScale(float time,
            CvdAnimationScaleKeyFrame[] nodeScaleInfo)
        {
            if (nodeScaleInfo.Length == 1)
            {
                CvdAnimationScaleKeyFrame scaleKeyFrame = nodeScaleInfo[0];
                return (scaleKeyFrame.GameBoxScale.CvdScaleToUnityScale(),
                    scaleKeyFrame.GameBoxRotation.CvdQuaternionToUnityQuaternion());
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
            Quaternion calculatedRotation = Quaternion.Slerp(
                fromKeyFrame.GameBoxRotation.CvdQuaternionToUnityQuaternion(),
                toKeyFrame.GameBoxRotation.CvdQuaternionToUnityQuaternion(), influence);
            Vector3 calculatedScale = Vector3.Lerp(
                fromKeyFrame.GameBoxScale.CvdScaleToUnityScale(),
                toKeyFrame.GameBoxScale.CvdScaleToUnityScale(), influence);
            return (calculatedScale, calculatedRotation);
        }

        private Quaternion GetRotation(float time,
            CvdAnimationRotationKeyFrame[] nodeRotationInfo)
        {
            if (nodeRotationInfo.Length == 1) return nodeRotationInfo[0].GameBoxRotation.CvdQuaternionToUnityQuaternion();

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
            return Quaternion.Slerp(
                fromKeyFrame.GameBoxRotation.CvdQuaternionToUnityQuaternion(),
                toKeyFrame.GameBoxRotation.CvdQuaternionToUnityQuaternion(), influence);
        }

        public void StopCurrentAnimation()
        {
            if (!_animationCts.IsCancellationRequested)
            {
                _animationCts.Cancel();
            }
        }

        public void Dispose()
        {
            StopCurrentAnimation();

            foreach (StaticMeshRenderer meshRenderer in GetComponentsInChildren<StaticMeshRenderer>())
            {
                _materialFactory.ReturnToPool(meshRenderer.GetMaterials());
                meshRenderer.Dispose();
                meshRenderer.Destroy();
            }
        }
    }
}