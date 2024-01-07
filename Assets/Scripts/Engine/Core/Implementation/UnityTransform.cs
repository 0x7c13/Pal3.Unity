// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Implementation
{
    using System.Runtime.CompilerServices;
    using Abstraction;
    using UnityEngine;

    public sealed class UnityTransform : ITransform
    {
        private readonly Transform _transform;

        public object NativeObject
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _transform;
        }

        public bool IsNativeObjectDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _transform == null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnityTransform(Transform transform)
        {
            _transform = transform;
        }

        public Vector3 Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _transform.position;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _transform.position = value;
        }

        public Vector3 LocalPosition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _transform.localPosition;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _transform.localPosition = value;
        }

        public Quaternion Rotation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _transform.rotation;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _transform.rotation = value;
        }

        public Quaternion LocalRotation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _transform.localRotation;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _transform.localRotation = value;
        }

        public Vector3 LocalScale
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _transform.localScale;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _transform.localScale = value;
        }

        public Vector3 Forward
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _transform.forward;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _transform.forward = value;
        }

        public Vector3 Right
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _transform.right;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _transform.right = value;
        }

        public Vector3 Up
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _transform.up;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _transform.up = value;
        }

        public Vector3 EulerAngles
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _transform.eulerAngles;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _transform.eulerAngles = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Translate(Vector3 translation)
        {
            _transform.Translate(translation);
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Transform(UnityTransform t) => t.NativeObject as Transform;

        public void Destroy()
        {
            // Do nothing, since transform is owned by game object.
        }
    }
}