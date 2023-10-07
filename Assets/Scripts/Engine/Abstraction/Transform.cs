// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Abstraction
{
    using System.Runtime.CompilerServices;
    using UnityEngine;

    public sealed class Transform : ITransform
    {
        private readonly UnityEngine.Transform _transform;

        public Transform(UnityEngine.Transform transform)
        {
            _transform = transform;
        }

        public Vector3 Position
        {
            get => _transform.position;
            set => _transform.position = value;
        }

        public Vector3 LocalPosition
        {
            get => _transform.localPosition;
            set => _transform.localPosition = value;
        }

        public Quaternion Rotation
        {
            get => _transform.rotation;
            set => _transform.rotation = value;
        }

        public Quaternion LocalRotation
        {
            get => _transform.localRotation;
            set => _transform.localRotation = value;
        }

        public Vector3 LocalScale
        {
            get => _transform.localScale;
            set => _transform.localScale = value;
        }

        public Vector3 Forward
        {
            get => _transform.forward;
            set => _transform.forward = value;
        }

        public Vector3 Right
        {
            get => _transform.right;
            set => _transform.right = value;
        }

        public Vector3 Up
        {
            get => _transform.up;
            set => _transform.up = value;
        }

        public Vector3 EulerAngles
        {
            get => _transform.eulerAngles;
            set => _transform.eulerAngles = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Translate(Vector3 translation)
        {
            _transform.Translate(translation);
        }

        public bool IsDisposed => _transform == null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation)
        {
            _transform.GetPositionAndRotation(out position, out rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            _transform.SetPositionAndRotation(position, rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation)
        {
            _transform.GetLocalPositionAndRotation(out position, out rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            _transform.SetLocalPositionAndRotation(position, rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LookAt(Vector3 worldPosition)
        {
            _transform.LookAt(worldPosition);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateAround(Vector3 centerPoint, Vector3 axis, float angle)
        {
            _transform.RotateAround(centerPoint, axis, angle);
        }

        public UnityEngine.Transform GetUnityTransform()
        {
            return _transform;
        }
    }
}