// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Animation
{
    using System;
    using System.Collections;
    using System.Threading;
    using Abstraction;
    using Pal3.Core.Utilities;
    using Services;

    using Quaternion = UnityEngine.Quaternion;
    using Vector3 = UnityEngine.Vector3;

    public enum AnimationCurveType
    {
        // OG game only has these two
        Linear = 0,
        Sine   = 1,

        // New ones added by me
        Quadratic    = 2,
        Cubic        = 3,
        Exponential  = 4,
        EaseIn       = 5,
        EaseOut      = 6,
    }

    /// <summary>
    /// Provides helper functions to do Transform animations etc.
    /// </summary>
    public static class CoreAnimation
    {
        // Progress -> (0,1)
        private static float GetInterpolationRatio(float progress, AnimationCurveType curveType)
        {
            return curveType switch
            {
                AnimationCurveType.Linear => progress,
                AnimationCurveType.Sine => MathF.Sin(progress * (MathF.PI / 2)),
                AnimationCurveType.Quadratic => MathF.Pow(progress, 2),
                AnimationCurveType.Cubic => MathF.Pow(progress, 3),
                AnimationCurveType.Exponential => MathF.Exp(progress) - 1,
                AnimationCurveType.EaseIn => MathF.Pow(progress, 3),
                AnimationCurveType.EaseOut => 1f - MathF.Pow(1 - progress, 3),
                _ => progress
            };
        }

        private static float Clamp01(float value)
        {
            if ((double) value < 0.0) return 0.0f;
            return (double) value > 1.0 ? 1f : value;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

        public static IEnumerator EnumerateValueAsync(float from,
            float to,
            float duration,
            AnimationCurveType curveType,
            Action<float> onValueChanged,
            CancellationToken cancellationToken = default)
        {
            if (MathF.Abs(duration) < float.Epsilon)
            {
                onValueChanged?.Invoke(to);
                yield break;
            }

            var timePast = 0f;
            while (timePast < duration && !cancellationToken.IsCancellationRequested)
            {
                var newValue = Lerp(from, to, GetInterpolationRatio(timePast / duration, curveType));
                onValueChanged?.Invoke(newValue);
                timePast += GameTimeProvider.Instance.DeltaTime;
                yield return null;
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                onValueChanged?.Invoke(to);
            }
            yield return null;
        }

        public static IEnumerator MoveAsync(this ITransform target,
            Vector3 toPosition,
            float duration,
            AnimationCurveType curveType = AnimationCurveType.Linear,
            CancellationToken cancellationToken = default)
        {
            Vector3 oldPosition = target.Position;

            var timePast = 0f;
            while (timePast < duration && !target.IsNativeObjectDisposed && !cancellationToken.IsCancellationRequested)
            {
                target.Position = oldPosition + (toPosition - oldPosition) *
                    GetInterpolationRatio(timePast / duration, curveType);
                timePast += GameTimeProvider.Instance.DeltaTime;
                yield return null;
            }

            if (!target.IsNativeObjectDisposed && !cancellationToken.IsCancellationRequested) target.Position = toPosition;
            yield return null;
        }

        public static IEnumerator ShakeAsync(this ITransform target,
            float duration,
            float xAxisAmplitude,
            float yAxisAmplitude,
            float zAxisAmplitude)
        {
            Vector3 originalPosition = target.LocalPosition;

            while (duration > 0 && !target.IsNativeObjectDisposed)
            {
                (float x, float y, float z) = RandomGenerator.RandomPointInsideUnitSphere();
                target.LocalPosition = originalPosition + new Vector3(
                    x * xAxisAmplitude / 2f,
                    y * yAxisAmplitude / 2f,
                    z * zAxisAmplitude / 2f);
                duration -= GameTimeProvider.Instance.DeltaTime;
                yield return null;
            }

            if (!target.IsNativeObjectDisposed) target.LocalPosition = originalPosition;
            yield return null;
        }

        public static IEnumerator OrbitAroundCenterPointAsync(this ITransform target,
            Quaternion toRotation,
            Vector3 centerPoint,
            float duration,
            AnimationCurveType curveType,
            float distanceDelta,
            CancellationToken cancellationToken = default)
        {
            var distance = Vector3.Distance(target.Position, centerPoint);
            Quaternion startRotation = target.Rotation;

            var timePast = 0f;
            while (timePast < duration && !target.IsNativeObjectDisposed && !cancellationToken.IsCancellationRequested)
            {
                Quaternion newRotation = Quaternion.Slerp(startRotation, toRotation,
                    GetInterpolationRatio(timePast / duration, curveType));

                Vector3 direction = (newRotation * Vector3.forward).normalized;
                Vector3 newPosition = centerPoint + direction * -(distance + (timePast / duration) * distanceDelta);

                target.SetPositionAndRotation(newPosition, newRotation);

                timePast += GameTimeProvider.Instance.DeltaTime;
                yield return null;
            }

            if (!target.IsNativeObjectDisposed && !cancellationToken.IsCancellationRequested)
            {
                Vector3 newPosition = centerPoint + (toRotation * Vector3.forward).normalized * -(distance + distanceDelta);
                target.SetPositionAndRotation(newPosition, toRotation);
            }

            yield return null;
        }

        public static IEnumerator RotateAsync(this ITransform target,
            Quaternion toRotation,
            float duration,
            AnimationCurveType curveType,
            CancellationToken cancellationToken = default)
        {
            Quaternion startRotation = target.Rotation;

            var timePast = 0f;
            while (timePast < duration && !target.IsNativeObjectDisposed && !cancellationToken.IsCancellationRequested)
            {
                Quaternion rotation = Quaternion.Slerp(startRotation, toRotation,
                    GetInterpolationRatio(timePast / duration, curveType));

                target.Rotation = rotation;

                timePast += GameTimeProvider.Instance.DeltaTime;
                yield return null;
            }

            if (!target.IsNativeObjectDisposed && !cancellationToken.IsCancellationRequested) target.Rotation = toRotation;
            yield return null;
        }
    }
}