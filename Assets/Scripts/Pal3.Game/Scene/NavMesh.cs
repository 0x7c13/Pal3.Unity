// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene
{
    using Core.DataReader.Nav;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using UnityEngine;

    /// <summary>
    /// Represents a navigation mesh used for pathfinding in a game scene.
    /// </summary>
    public sealed class NavMesh : GameEntityScript
    {
        public int NavLayerIndex { get; private set; }

        private MeshCollider _meshCollider;

        protected override void OnEnableGameEntity()
        {
            // Set layer to NavMeshes for the attached GameEntity
            // Because NavMeshes should not collide with anything else, it is only used for raycast.
            // This behaviour is configured in Physics settings (Edit -> Project Settings -> Physics)
            GameEntity.Layer = LayerMask.NameToLayer("NavMeshes");
        }

        protected override void OnDisableGameEntity()
        {
            if (_meshCollider != null)
            {
                _meshCollider.sharedMesh.Destroy();
                _meshCollider.Destroy();
                _meshCollider = null;
            }
        }

        public void Init(int layerIndex, NavMeshData navMeshData)
        {
            NavLayerIndex = layerIndex;

            _meshCollider = GameEntity.AddComponent<MeshCollider>();
            _meshCollider.convex = false;
            _meshCollider.sharedMesh = new Mesh()
            {
                vertices = navMeshData.GameBoxVertices.ToUnityPositions(),
                triangles = navMeshData.GameBoxTriangles.ToUnityTriangles(),
            };
        }
    }
}