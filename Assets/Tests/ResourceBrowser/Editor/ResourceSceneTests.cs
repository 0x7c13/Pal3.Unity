// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser.Tests
{
    using Core.Contract.Enums;
    using Core.DataReader.Nav;
    using Core.DataReader.Scn;
    using Core.Primitives;
    using NUnit.Framework;
    using UnityEngine;

    public sealed class ResourceSceneTests
    {
        [Test]
        public void ScenePathsMatchRuntimeNightAndObjectRules()
        {
            var scene = new ScnSceneInfo { CityName = "q02", Model = "3", LightMap = 1 };
            Assert.That(ResourceSceneData.BackgroundPath(scene), Is.EqualTo("q02.cpk\\3\\1\\3.pol"));
            Assert.That(ResourceSceneData.ObjectPath(scene, new ScnObjectInfo { Name = "_a.pol" }), Is.EqualTo("q02.cpk\\3\\_a.pol"));
            Assert.That(ResourceSceneData.ObjectPath(scene, new ScnObjectInfo { Name = "+effect" }), Is.Null);
        }

        [Test]
        public void NavPickingUsesBottomLeftOriginAndPreservesBlockedTileMeaning()
        {
            var layer = new NavLayer { Width = 4, Height = 2 };
            var rect = new Rect(10, 20, 40, 20);
            Assert.That(ResourceSceneData.TileAt(new Vector2(15, 25), rect, layer), Is.EqualTo(new Vector2Int(0, 1)));
            Assert.That(ResourceSceneData.TileAt(new Vector2(45, 35), rect, layer), Is.EqualTo(new Vector2Int(3, 0)));
            var obstacle = new NavTile { IsObstacle = true };
            var nearObstacle = new NavTile { IsObstacle = false, DistanceToNearestObstacle = 0 };
            var walkable = new NavTile { IsObstacle = false, DistanceToNearestObstacle = 1 };
            Assert.That(ResourceSceneData.TileColor(nearObstacle), Is.Not.EqualTo(ResourceSceneData.TileColor(obstacle)));
            Assert.That(ResourceSceneData.TileColor(nearObstacle), Is.Not.EqualTo(ResourceSceneData.TileColor(walkable)));
        }

        [Test]
        public void NpcMarkersUseNavigationHeightUnlessHolding()
        {
            var npc = new ScnNpcInfo { GameBoxXPosition = 6, GameBoxZPosition = 6, GameBoxYPosition = 100, LayerIndex = 0 };
            var layer = new NavLayer { Width = 1, Height = 1, GameBoxMinWorldPosition = new GameBoxVector3(0, 0, 0), Tiles = new[] { new NavTile { GameBoxYPosition = 40 } } };
            var nav = new NavFile(new[] { layer }, System.Array.Empty<NavMeshData>());
            Assert.That(ResourceSceneData.NpcPosition(npc, nav), Is.EqualTo(new Vector3(-0.3f, 2, 0.3f)));
            npc.InitBehaviour = ActorBehaviourType.Hold;
            Assert.That(ResourceSceneData.NpcPosition(npc, nav).y, Is.EqualTo(5));
        }
    }
}
