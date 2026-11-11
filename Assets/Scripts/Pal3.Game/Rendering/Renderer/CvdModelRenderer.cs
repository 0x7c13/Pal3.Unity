// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
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
        private readonly Dictionary<string, ITexture2D> _textureCache = new ();
        private readonly List<CvdNodeRenderState> _renderers = new();

        private sealed class CvdNodeRenderState
        {
            public readonly CvdGeometryNode Node;
            public readonly List<(int index, RenderMeshComponent renderMesh)> Sections = new();
            public (Matrix4x4 matrix, int frameIndex, float influence) Pose;

            public CvdNodeRenderState(CvdGeometryNode node, (Matrix4x4 matrix, int frameIndex, float influence) pose)
            {
                Node = node;
                Pose = pose;
            }
        }

        private Color _tintColor;

        private float _currentAnimationTime;
        private float _animationDuration;
        private CancellationTokenSource _animationCts = new ();

        private IMaterialManager _materialManager;

        protected override void OnEnableGameEntity()
        {
        }

        protected override void OnDisableGameEntity()
        {
            Dispose();
        }

        public void Init(CvdFile cvdFile,
            ITextureResourceProvider textureProvider,
            IMaterialManager materialManager,
            Color? tintColor = default,
            float initTime = 0f)
        {
            _materialManager = materialManager;
            _animationDuration = cvdFile.AnimationDuration;
            _tintColor = tintColor ?? Color.White;
            _currentAnimationTime = initTime;

            foreach (CvdGeometryNode node in cvdFile.RootNodes)
            {
                BuildTextureCache(node, textureProvider, _textureCache);
            }

            IGameEntity root = GameEntityFactory.Create("Cvd Mesh",
                GameEntity, worldPositionStays: false);

            for (int i = 0; i < cvdFile.RootNodes.Length; i++)
            {
                string hashKey = $"{i}";
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
            bool boundsInitialized = false;
            Bounds bounds = new (Transform.Position, Vector3.one);

            foreach (CvdNodeRenderState node in _renderers)
            {
                foreach (var (_, meshComponent) in node.Sections)
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
            bool boundsInitialized = false;
            Bounds bounds = new (Vector3.zero, Vector3.one);

            foreach (CvdNodeRenderState node in _renderers)
            {
                foreach (var (_, meshComponent) in node.Sections)
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
            Dictionary<string, ITexture2D> textureCache)
        {
            if (node.IsGeometryNode)
            {
                foreach (CvdMeshSection meshSection in node.Mesh.MeshSections)
                {
                    string textureName = meshSection.Material.TextureFileNames[0];
                    if (string.IsNullOrEmpty(textureName)) continue;
                    if (textureCache.ContainsKey(textureName)) continue;
                    ITexture2D texture2D = textureProvider.GetTexture(textureName);
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
            Dictionary<string, ITexture2D> textureCache,
            IGameEntity parent)
        {
            IGameEntity meshEntity = GameEntityFactory.Create(meshName, parent, worldPositionStays: false);

            if (node.IsGeometryNode)
            {
                var nodeMeshes = new CvdNodeRenderState(node, SamplePose(node, initTime));
                var (frameMatrix, frameIndex, influence) = nodeMeshes.Pose;
                bool usesToon = _materialManager.UsesToonRendering;

                for (int i = 0; i < node.Mesh.MeshSections.Length; i++)
                {
                    CvdMeshSection meshSection = node.Mesh.MeshSections[i];

                    string sectionHashKey = $"{meshName}_{i}";

                    string textureName = meshSection.Material.TextureFileNames[0];
                    if (string.IsNullOrEmpty(textureName) || !textureCache.TryGetValue(textureName, out ITexture2D texture))
                        continue;

                    int vertexCount = meshSection.FrameVertices[frameIndex].Length;
                    MeshDataBuffer meshDataBuffer = new(vertexCount, 0, vertexCount, meshSection.GameBoxTriangles.Length);
                    meshSection.GameBoxTriangles.ToUnityTriangles(meshDataBuffer.TriangleBuffer);
                    UpdateMeshDataBuffer(meshDataBuffer, meshSection, frameIndex, influence, frameMatrix);

                    IGameEntity meshSectionEntity = GameEntityFactory.Create($"{sectionHashKey}",
                        meshEntity, worldPositionStays: false);

                    // Attach BlendFlag and GameBoxMaterial to the GameEntity for better debuggability.
                    #if UNITY_EDITOR
                    MaterialInfoPresenter materialInfoPresenter = meshEntity.AddComponent<MaterialInfoPresenter>();
                    materialInfoPresenter.blendFlag = meshSection.BlendFlag;
                    materialInfoPresenter.material = meshSection.Material;
                    #endif

                    IMaterial[] materials = _materialManager.CreateStandardMaterials(
                        RendererType.Cvd,
                        mainTexture: (textureName, texture),
                        bakedLightmap: default, // CVD models don't have shadow textures
                        tintColor: _tintColor,
                        blendFlag: meshSection.BlendFlag);

                    StaticMeshRenderer meshRenderer = meshSectionEntity.AddComponent<StaticMeshRenderer>();
                    Mesh renderMesh = meshRenderer.Render(
                        vertices: meshDataBuffer.VertexBuffer,
                        triangles: meshDataBuffer.TriangleBuffer,
                        normals: null,
                        mainTextureUvs: (channel: 0, uvs: meshDataBuffer.UvBuffer),
                        secondaryTextureUvs: default, // CVD models don't have secondary texture
                        materials: materials,
                        isDynamic: true);

                    if (usesToon) renderMesh.RecalculateNormals();

                    nodeMeshes.Sections.Add((i, new RenderMeshComponent
                    {
                        Mesh = renderMesh,
                        MeshRenderer = meshRenderer,
                        Materials = materials,
                        MeshDataBuffer = meshDataBuffer
                    }));
                }

                _renderers.Add(nodeMeshes);
            }

            for (int i = 0; i < node.Children.Length; i++)
            {
                string childMeshName = $"{meshName}-{i}";
                RenderMeshInternal(initTime,
                    childMeshName,
                    node.Children[i],
                    textureCache,
                    meshEntity);
            }
        }

        private (Matrix4x4 matrix, int frameIndex, float influence) SamplePose(CvdGeometryNode node, float time)
        {
            float[] times = node.Mesh.AnimationTimeKeys;
            int frameIndex = GetFrameIndex(times, time);
            float influence = time > float.Epsilon && frameIndex + 1 < times.Length
                ? (time - times[frameIndex]) / (times[frameIndex + 1] - times[frameIndex])
                : 0f;
            return (GetFrameMatrix(time, node), frameIndex, influence);
        }

        private static void UpdateMeshDataBuffer(MeshDataBuffer meshDataBuffer,
            CvdMeshSection meshSection,
            int frameIndex,
            float influence,
            Matrix4x4 matrix,
            bool updateUvs = true)
        {
            CvdVertex[] frameVertices = meshSection.FrameVertices[frameIndex];

            if (influence < float.Epsilon)
            {
                for (int i = 0; i < frameVertices.Length; i++)
                {
                    meshDataBuffer.VertexBuffer[i] = matrix.MultiplyPoint3x4(
                        frameVertices[i].GameBoxPosition.CvdPositionToUnityPosition());
                    if (updateUvs) meshDataBuffer.UvBuffer[i] = frameVertices[i].Uv.ToUnityVector2();
                }
            }
            else
            {
                CvdVertex[] toFrameVertices = meshSection.FrameVertices[frameIndex + 1];
                for (int i = 0; i < frameVertices.Length; i++)
                {
                    Vector3 lerpPosition = Vector3.Lerp(
                        frameVertices[i].GameBoxPosition.CvdPositionToUnityPosition(),
                        toFrameVertices[i].GameBoxPosition.CvdPositionToUnityPosition(), influence);
                    meshDataBuffer.VertexBuffer[i] = matrix.MultiplyPoint3x4(lerpPosition);
                    if (updateUvs)
                    {
                        meshDataBuffer.UvBuffer[i] = Vector2.Lerp(frameVertices[i].Uv.ToUnityVector2(),
                            toFrameVertices[i].Uv.ToUnityVector2(), influence);
                    }
                }
            }
        }

        private void UpdateMesh(float time)
        {
            _currentAnimationTime = time;
            if (_renderers.Count == 0) return;

            bool usesToon = _materialManager.UsesToonRendering;
            foreach (CvdNodeRenderState node in _renderers)
            {
                var pose = SamplePose(node.Node, time);
                var previous = node.Pose;
                bool matrixChanged = !pose.matrix.Equals(previous.matrix);
                bool vertexFrameChanged = pose.frameIndex != previous.frameIndex || pose.influence != previous.influence;
                if (!matrixChanged && !vertexFrameChanged) continue;
                node.Pose = pose;

                bool normalTransformChanged = usesToon &&
                    (!pose.matrix.GetColumn(0).Equals(previous.matrix.GetColumn(0)) ||
                     !pose.matrix.GetColumn(1).Equals(previous.matrix.GetColumn(1)) ||
                     !pose.matrix.GetColumn(2).Equals(previous.matrix.GetColumn(2)));

                foreach (var (sectionIndex, renderMesh) in node.Sections)
                {
                    Mesh mesh = renderMesh.Mesh;
                    MeshDataBuffer buffer = renderMesh.MeshDataBuffer;
                    CvdMeshSection source = node.Node.Mesh.MeshSections[sectionIndex];
                    UpdateMeshDataBuffer(buffer, source, pose.frameIndex, pose.influence, pose.matrix, vertexFrameChanged);
                    mesh.SetVertices(buffer.VertexBuffer);
                    if (usesToon && (vertexFrameChanged || normalTransformChanged)) mesh.RecalculateNormals();
                    if (vertexFrameChanged) mesh.SetUVs(0, buffer.UvBuffer);
                    mesh.RecalculateBounds();
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

            foreach (CvdNodeRenderState node in _renderers)
            {
                foreach (var (_, component) in node.Sections)
                {
                    if (component.MeshRenderer != null)
                    {
                        component.MeshRenderer.Dispose();
                        component.MeshRenderer.Destroy();
                    }
                    _materialManager.ReturnToPool(component.Materials);
                }
            }
            _renderers.Clear();
        }
    }
}
