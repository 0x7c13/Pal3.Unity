// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Animation
{
    using System;
    using System.Collections;
    using System.Threading;
    using UnityEngine;

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
    public static class AnimationHelper
    {
        // Progress -> (0,1)
        private static float GetInterpolationRatio(float progress, AnimationCurveType curveType)
        {
            return curveType switch
            {
                AnimationCurveType.Linear => progress,
                AnimationCurveType.Sine => Mathf.Sin(progress * (Mathf.PI / 2)),
                AnimationCurveType.Quadratic => Mathf.Pow(progress, 2),
                AnimationCurveType.Cubic => Mathf.Pow(progress, 3),
                AnimationCurveType.Exponential => Mathf.Exp(progress) - 1,
                AnimationCurveType.EaseIn => Mathf.Pow(progress, 3),
                AnimationCurveType.EaseOut => 1 - Mathf.Pow(1 - progress, 3),
                _ => progress
            };
        }

        public static IEnumerator EnumerateValueAsync(float from,
            float to,
            float duration,
            AnimationCurveType curveType,
            Action<float> onValueChanged,
            CancellationToken cancellationToken = default)
        {
            if (Mathf.Abs(duration) < Mathf.Epsilon)
            {
                onValueChanged?.Invoke(to);
                yield break;
            }

            var timePast = 0f;
            while (timePast < duration && !cancellationToken.IsCancellationRequested)
            {
                var newValue = Mathf.Lerp(from, to,
                    GetInterpolationRatio(timePast / duration, curveType));
                onValueChanged?.Invoke(newValue);
                timePast += Time.deltaTime;
                yield return null;
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                onValueChanged?.Invoke(to);
            }
            yield return null;
        }

        public static IEnumerator MoveTransformAsync(Transform target,
            Vector3 toPosition,
            float duration,
            AnimationCurveType curveType = AnimationCurveType.Linear,
            CancellationToken cancellationToken = default)
        {
            Vector3 oldPosition = target.position;

            var timePast = 0f;
            while (timePast < duration && target != null && !cancellationToken.IsCancellationRequested)
            {
                target.position = oldPosition + (toPosition - oldPosition) *
                    GetInterpolationRatio(timePast / duration, curveType);
                timePast += Time.deltaTime;
                yield return null;
            }

            if (target != null && !cancellationToken.IsCancellationRequested) target.position = toPosition;
            yield return null;
        }

        public static IEnumerator ShakeTransformAsync(Transform target,
            float duration,
            float amplitude,
            bool shakeOnXAxis,
            bool shakeOnYAxis,
            bool shakeOnZAxis)
        {
            Vector3 originalPosition = target.localPosition;

            while (duration > 0 && target != null)
            {
                Vector3 delta = UnityEngine.Random.insideUnitSphere * amplitude;
                target.localPosition = originalPosition + new Vector3(
                    shakeOnXAxis ? delta.x : 0f,
                    shakeOnYAxis ? delta.y : 0f,
                    shakeOnZAxis ? delta.z : 0f);
                duration -= Time.deltaTime;
                yield return null;
            }

            if (target != null) target.localPosition = originalPosition;
            yield return null;
        }

        public static IEnumerator OrbitTransformAroundCenterPointAsync(Transform target,
            Quaternion toRotation,
            Vector3 centerPoint,
            float duration,
            AnimationCurveType curveType,
            float distanceDelta,
            CancellationToken cancellationToken = default)
        {
            var distance = Vector3.Distance(target.position, centerPoint);
            Quaternion startRotation = target.rotation;

            var timePast = 0f;
            while (timePast < duration && target != null && !cancellationToken.IsCancellationRequested)
            {
                Quaternion rotation = Quaternion.Lerp(startRotation, toRotation,
                    GetInterpolationRatio(timePast / duration, curveType));

                Vector3 direction = (rotation * Vector3.forward).normalized;
                Vector3 newPosition = centerPoint + direction * -(distance + (timePast / duration) * distanceDelta);

                target.rotation = rotation;
                target.position = newPosition;

                timePast += Time.deltaTime;
                yield return null;
            }

            if (target != null && !cancellationToken.IsCancellationRequested)
            {
                target.rotation = toRotation;
                target.position = centerPoint + (toRotation * Vector3.forward).normalized * -(distance + distanceDelta);
            }

            yield return null;
        }

        public static IEnumerator RotateTransformAsync(Transform target,
            Quaternion toRotation,
            float duration,
            AnimationCurveType curveType,
            CancellationToken cancellationToken = default)
        {
            Quaternion startRotation = target.rotation;

            var timePast = 0f;
            while (timePast < duration && target != null && !cancellationToken.IsCancellationRequested)
            {
                Quaternion rotation = Quaternion.Lerp(startRotation, toRotation,
                    GetInterpolationRatio(timePast / duration, curveType));

                target.rotation = rotation;

                timePast += Time.deltaTime;
                yield return null;
            }

            if (target != null && !cancellationToken.IsCancellationRequested) target.rotation = toRotation;
            yield return null;
        }
    }
}