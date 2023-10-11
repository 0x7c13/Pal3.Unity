// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Abstraction
{
    using Quaternion = UnityEngine.Quaternion;
    using Vector3 = UnityEngine.Vector3;

    public interface ITransform : IManagedObject
    {
        public Vector3 Position { get; set; }

        public Vector3 LocalPosition { get; set; }

        public Quaternion Rotation { get; set; }

        public Quaternion LocalRotation { get; set; }

        public Vector3 LocalScale { get; set; }

        public Vector3 Forward { get; set; }

        public Vector3 Right { get; set; }

        public Vector3 Up { get; set; }

        public Vector3 EulerAngles { get; set; }

        public void Translate(Vector3 translation);

        public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation);

        public void GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation);

        public void SetLocalPositionAndRotation(Vector3 position, Quaternion rotation);

        public void LookAt(Vector3 target);

        public void RotateAround(Vector3 centerPoint, Vector3 axis, float angle);
    }
}