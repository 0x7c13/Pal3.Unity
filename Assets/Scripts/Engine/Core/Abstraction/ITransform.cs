// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Core.Abstraction
{
    using Quaternion = UnityEngine.Quaternion;
    using Vector3 = UnityEngine.Vector3;

    /// <summary>
    /// Interface for a transform object, which represents the position, rotation, and scale of a game object.
    /// </summary>
    public interface ITransform : IManagedObject
    {
        /// <summary>
        /// The position of the transform in world space.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// The position of the transform relative to the parent transform.
        /// </summary>
        public Vector3 LocalPosition { get; set; }

        /// <summary>
        /// The rotation of the transform in world space.
        /// </summary>
        public Quaternion Rotation { get; set; }

        /// <summary>
        /// The rotation of the transform relative to the parent transform.
        /// </summary>
        public Quaternion LocalRotation { get; set; }

        /// <summary>
        /// The scale of the transform relative to the parent transform.
        /// </summary>
        public Vector3 LocalScale { get; set; }

        /// <summary>
        /// The forward direction of the transform in world space.
        /// </summary>
        public Vector3 Forward { get; set; }

        /// <summary>
        /// The right direction of the transform in world space.
        /// </summary>
        public Vector3 Right { get; set; }

        /// <summary>
        /// The up direction of the transform in world space.
        /// </summary>
        public Vector3 Up { get; set; }

        /// <summary>
        /// The rotation of the transform expressed as euler angles.
        /// </summary>
        public Vector3 EulerAngles { get; set; }

        /// <summary>
        /// Translates the transform by the given vector.
        /// </summary>
        /// <param name="translation">The vector to translate by.</param>
        public void Translate(Vector3 translation);

        /// <summary>
        /// Gets the position and rotation of the transform in world space.
        /// </summary>
        /// <param name="position">The position of the transform.</param>
        /// <param name="rotation">The rotation of the transform.</param>
        public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

        /// <summary>
        /// Sets the position and rotation of the transform in world space.
        /// </summary>
        /// <param name="position">The new position of the transform.</param>
        /// <param name="rotation">The new rotation of the transform.</param>
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation);

        /// <summary>
        /// Gets the position and rotation of the transform relative to the parent transform.
        /// </summary>
        /// <param name="position">The local position of the transform.</param>
        /// <param name="rotation">The local rotation of the transform.</param>
        public void GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation);

        /// <summary>
        /// Sets the position and rotation of the transform relative to the parent transform.
        /// </summary>
        /// <param name="position">The new local position of the transform.</param>
        /// <param name="rotation">The new local rotation of the transform.</param>
        public void SetLocalPositionAndRotation(Vector3 position, Quaternion rotation);

        /// <summary>
        /// Rotates the transform to face the given target position.
        /// </summary>
        /// <param name="target">The position to look at.</param>
        public void LookAt(Vector3 target);

        /// <summary>
        /// Rotates the transform around the given axis by the given angle.
        /// </summary>
        /// <param name="centerPoint">The point to rotate around.</param>
        /// <param name="axis">The axis to rotate around.</param>
        /// <param name="angle">The angle to rotate by.</param>
        public void RotateAround(Vector3 centerPoint, Vector3 axis, float angle);
    }
}