// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Native
{
    using System;
    using Core.DataReader.Mv3;
    using Engine.Extensions;
    using UnityEngine;

    /// <summary>A captured native shadow radius and the current role position.</summary>
    public readonly struct GameBoxBattleShadow
    {
        public Vector3 Position
        {
            get;
        }

        public float NativeRadius
        {
            get;
        }

        public GameBoxBattleShadow(Vector3 position, float nativeRadius)
        {
            Position = position;
            NativeRadius = nativeRadius;
        }

        public Vector3 Corner(int index)
        {
            float radius = NativeRadius / UnityPrimitivesConvertor.GameBoxUnitToUnityUnit;
            float height = .5f / UnityPrimitivesConvertor.GameBoxUnitToUnityUnit;
            return Position + (index switch
            {
                0 => new Vector3(radius, height, radius),
                1 => new Vector3(-radius, height, radius),
                2 => new Vector3(-radius, height, -radius),
                3 => new Vector3(radius, height, -radius),
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            });
        }

        // GetCollideOBB takes the union of the action's authored mesh bounds.
        // The combat shadow uses the complete X/Z width as its radius.
        public static float CaptureRadius(Mv3File model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (model.Meshes.Length == 0)
                throw new ArgumentException("A shadow model must contain meshes.", nameof(model));
            float minX = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxZ = float.MinValue;
            foreach (Mv3Mesh mesh in model.Meshes)
            {
                minX = Math.Min(minX, mesh.GameBoxBoundsMin.X);
                minZ = Math.Min(minZ, mesh.GameBoxBoundsMin.Z);
                maxX = Math.Max(maxX, mesh.GameBoxBoundsMax.X);
                maxZ = Math.Max(maxZ, mesh.GameBoxBoundsMax.Z);
            }
            return Math.Min(maxX - minX, maxZ - minZ);
        }

        public static bool IsVisible(bool pal3A, bool valid, bool modelAlive, bool ally, int hp, float blend) =>
            valid && (!pal3A || blend >= .5f) && (modelAlive || ally && hp > 0);
    }
}
