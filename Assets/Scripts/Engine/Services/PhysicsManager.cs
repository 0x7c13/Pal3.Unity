// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Services
{
    using System;
    using Core.Abstraction;
    using Core.Implementation;
    using UnityEngine;

    public sealed class PhysicsManager : IPhysicsManager
    {
        private const int MAX_RAYCAST_HIT_COUNT = 10;
        private readonly RaycastHit[] _raycastHits = new RaycastHit[MAX_RAYCAST_HIT_COUNT]; // Cache
        private readonly Camera _camera;

        public PhysicsManager(Camera camera)
        {
            _camera = camera;
        }

        public int MaxRaycastHitCount => MAX_RAYCAST_HIT_COUNT;

        public bool TryCameraRaycastFromScreenPoint(Vector2 screenPoint,
            out (Vector3 hitPoint, IGameEntity colliderGameEntity) hitResult,
            float maxDistance = float.PositiveInfinity)
        {
            Ray ray = _camera.ScreenPointToRay(screenPoint);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                hitResult = (hit.point, new GameEntity(hit.collider.gameObject));
                return true;
            }
            else
            {
                hitResult = default;
                return false;
            }
        }

        public int CameraRaycastFromScreenPoint(Vector2 screenPoint,
            (Vector3 hitPoint, IGameEntity colliderGameEntity)[] hitResults,
            float maxDistance = float.PositiveInfinity)
        {
            Ray ray = _camera.ScreenPointToRay(screenPoint);

            int hitCount = Physics.RaycastNonAlloc(ray, _raycastHits, maxDistance);

            for (int i = 0; i < Math.Min(hitCount, hitResults.Length); i++)
            {
                hitResults[i] = (_raycastHits[i].point, new GameEntity(_raycastHits[i].collider.gameObject));
            }

            return hitCount;
        }

        public int BoxCast(Vector3 center,
            Vector3 halfExtents,
            Vector3 direction,
            Quaternion orientation,
            (Vector3 hitPoint, IGameEntity colliderGameEntity)[] hitResults)
        {
            int hitCount = Physics.BoxCastNonAlloc(center,
                halfExtents,
                direction,
                _raycastHits,
                orientation);

            for (int i = 0; i < Math.Min(hitCount, hitResults.Length); i++)
            {
                hitResults[i] = (_raycastHits[i].point, new GameEntity(_raycastHits[i].collider.gameObject));
            }

            return hitCount;
        }
    }
}