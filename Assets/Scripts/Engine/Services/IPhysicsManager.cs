// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Services
{
    using Core.Abstraction;
    using UnityEngine;

    /// <summary>
    /// Interface for managing physics in the game.
    /// </summary>
    public interface IPhysicsManager
    {
        public int MaxRaycastHitCount { get; }

        /// <summary>
        /// Attempts to perform a raycast from a screen point in the camera's view.
        /// </summary>
        /// <param name="screenPoint">The screen point to cast the ray from.</param>
        /// <param name="hitResult">The resulting hit point and collider game entity, if any.</param>
        /// <param name="maxDistance">The maximum distance to cast the ray.</param>
        /// <returns>True if the raycast hit a collider, false otherwise.</returns>
        public bool TryCameraRaycastFromScreenPoint(Vector2 screenPoint,
            out (Vector3 hitPoint, IGameEntity colliderGameEntity) hitResult,
            float maxDistance = float.PositiveInfinity);

        /// <summary>
        /// Casts a ray from a screen point into the scene and returns information about any colliders hit.
        /// </summary>
        /// <param name="screenPoint">The screen point to cast the ray from.</param>
        /// <param name="hitResults">An array of hit results, each containing information about the hit point and the game entity of the collider.</param>
        /// <param name="maxDistance">The maximum distance to cast the ray.</param>
        /// <returns>The number of colliders hit.</returns>
        public int CameraRaycastFromScreenPoint(Vector2 screenPoint,
            (Vector3 hitPoint, IGameEntity colliderGameEntity)[] hitResults,
            float maxDistance = float.PositiveInfinity);

        /// <summary>
        /// Casts a box along a ray and returns all hits in the path.
        /// </summary>
        /// <param name="center">The center of the box.</param>
        /// <param name="halfExtents">Half the size of the box in each dimension.</param>
        /// <param name="direction">The direction in which to cast the box.</param>
        /// <param name="orientation">The orientation of the box.</param>
        /// <param name="hitResults">An array of hit results, each containing the hit point and the game entity of the collider.</param>
        /// <returns>The number of hits found.</returns>
        public int BoxCast(Vector3 center,
            Vector3 halfExtents,
            Vector3 direction,
            Quaternion orientation,
            (Vector3 hitPoint, IGameEntity colliderGameEntity)[] hitResults);
    }
}