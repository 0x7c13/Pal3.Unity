// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Abstraction
{
    using UnityEngine;

    public interface ITransform
    {
        Vector3 Position { get; set; }

        Vector3 LocalPosition { get; set; }

        Quaternion Rotation { get; set; }

        Quaternion LocalRotation { get; set; }

        Vector3 LocalScale { get; set; }

        Vector3 Forward { get; set; }

        Vector3 Right { get; set; }

        Vector3 Up { get; set; }

        Vector3 EulerAngles { get; set; }

        void Translate(Vector3 translation);

        bool IsDisposed { get; }

        void GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

        void SetPositionAndRotation(Vector3 position, Quaternion rotation);

        void GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation);

        void SetLocalPositionAndRotation(Vector3 position, Quaternion rotation);

        void LookAt(Vector3 target);

        void RotateAround(Vector3 centerPoint, Vector3 axis, float angle);

        UnityEngine.Transform GetUnityTransform();
    }
}