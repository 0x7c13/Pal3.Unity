// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Nav;
    using Core.DataReader.Scn;
    using Core.Primitives;
    using Engine.Extensions;
    using Game.Scene;
    using UnityEngine;
    using Color32 = UnityEngine.Color32;

    public static class ResourceSceneData
    {
        public static string BackgroundPath(ScnSceneInfo scene) =>
            $"{scene.CityName}.cpk\\{scene.Model}\\{(scene.LightMap == 1 ? "1\\" : "")}{scene.Model}.pol";

        public static string ObjectPath(ScnSceneInfo scene, ScnObjectInfo item)
        {
            if (string.IsNullOrEmpty(item.Name) || item.Name.StartsWith('+'))
                return null;
            if (item.Name.StartsWith('_'))
                return FileConstants.GetGameObjectModelFileVirtualPath(scene, item.Name);
            return item.Name.Contains('.') ? FileConstants.GetGameObjectModelFileVirtualPath(item.Name) : FileConstants.GetGameItemModelFileVirtualPath(item.Name);
        }

        public static Vector3 NpcPosition(ScnNpcInfo npc, NavFile nav)
        {
            Vector3 position = new GameBoxVector3(npc.GameBoxXPosition, npc.GameBoxYPosition, npc.GameBoxZPosition).ToUnityPosition();
            if (nav != null && npc.InitBehaviour != ActorBehaviourType.Hold && npc.LayerIndex >= 0 && npc.LayerIndex < nav.Layers.Length)
            {
                var map = new Tilemap(nav);
                if (map.TryGetTile(position, npc.LayerIndex, out NavTile tile))
                    position.y = tile.GameBoxYPosition.ToUnityYPosition();
            }
            return position;
        }

        public static Color32 TileColor(NavTile tile) => tile.IsObstacle ? new Color32(150, 48, 48, 255) :
            tile.IsWalkable() ? new Color32(72, 150, 100, 255) : new Color32(196, 145, 47, 255);

        public static Vector2Int TileAt(Vector2 point, Rect image, NavLayer layer)
        {
            int x = Mathf.FloorToInt((point.x - image.x) / image.width * layer.Width);
            int y = layer.Height - 1 - Mathf.FloorToInt((point.y - image.y) / image.height * layer.Height);
            return new Vector2Int(x, y);
        }
    }
}
