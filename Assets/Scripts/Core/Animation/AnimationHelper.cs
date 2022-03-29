// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Animation
{
    using System;
    using System.Collections;
    using UnityEngine;

    public enum AnimationCurveType
    {
        Linear = 0,
        Sine,
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
                _ => progress
            };
        }

        public static IEnumerator EnumerateValue(float from,
            float to,
            float duration,
            AnimationCurveType curveType,
            Action<float> onValueChanged)
        {
            if (Mathf.Abs(duration) < Mathf.Epsilon)
            {
                onValueChanged?.Invoke(to);
                yield break;
            }

            var timePast = 0f;
            while (timePast < duration)
            {
                var newValue = Mathf.Lerp(from, to,
                    GetInterpolationRatio(timePast / duration, curveType));
                onValueChanged?.Invoke(newValue);
                timePast += Time.deltaTime;
                yield return null;
            }

            onValueChanged?.Invoke(to);
            yield return null;
        }

        public static IEnumerator MoveTransform(Transform target,
            Vector3 toPosition,
            float duration,
            AnimationCurveType curveType = AnimationCurveType.Linear)
        {
            var oldPosition = target.position;

            var timePast = 0f;
            while (timePast < duration && target != null)
            {
                target.position = oldPosition + (toPosition - oldPosition) *
                    GetInterpolationRatio(timePast / duration, curveType);
                timePast += Time.deltaTime;
                yield return null;
            }

            if (target != null) target.position = toPosition;
            yield return null;
        }

        public static IEnumerator ShakeTransform(Transform target, float duration, float amplitude,
            bool shakeOnXAxis, bool shakeOnYAxis, bool shakeOnZAxis)
        {
            var originalPosition = target.localPosition;

            while (duration > 0 && target != null)
            {
                var delta = UnityEngine.Random.insideUnitSphere * amplitude;
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

        public static IEnumerator OrbitTransformAroundCenterPoint(Transform target,
            Quaternion toRotation,
            Vector3 centerPoint,
            float duration,
            AnimationCurveType curveType)
        {
            var distance = Vector3.Distance(target.position, centerPoint);
            var startRotation = target.rotation;

            var timePast = 0f;
            while (timePast < duration && target != null)
            {
                var rotation = Quaternion.Lerp(startRotation, toRotation,
                    GetInterpolationRatio(timePast / duration, curveType));

                var direction = (rotation * Vector3.forward).normalized;
                var newPosition = centerPoint + direction * -distance;

                target.rotation = rotation;
                target.position = newPosition;

                timePast += Time.deltaTime;
                yield return null;
            }

            if (target != null)
            {
                target.rotation = toRotation;
                target.position = centerPoint + (toRotation * Vector3.forward).normalized * -distance;
            }
            yield return null;
        }

        public static IEnumerator RotateTransform(Transform target,
            Quaternion toRotation,
            float duration,
            AnimationCurveType curveType)
        {
            var startRotation = target.rotation;

            var timePast = 0f;
            while (timePast < duration && target != null)
            {
                var rotation = Quaternion.Lerp(startRotation, toRotation,
                    GetInterpolationRatio(timePast / duration, curveType));

                target.rotation = rotation;

                timePast += Time.deltaTime;
                yield return null;
            }

            if (target != null) target.rotation = toRotation;
            yield return null;
        }
    }
}