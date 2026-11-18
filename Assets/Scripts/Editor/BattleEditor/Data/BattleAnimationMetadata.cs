// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.DataReader.Mv3;

    // Catalogs need timing, markers and source collision bounds, not the
    // expanded rendering geometry or regenerated normals of every keyframe.
    public sealed class BattleAnimationMetadata
    {
        public uint TotalGameBoxTicks
        {
            get;
        }

        public IReadOnlyList<Mv3AnimationEvent> AnimationEvents
        {
            get;
        }

        public bool HasBounds
        {
            get;
        }

        public float MinimumZ
        {
            get;
        }

        public float MaximumZ
        {
            get;
        }

        public BattleAnimationMetadata(Mv3File source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            TotalGameBoxTicks = source.TotalGameBoxTicks;
            AnimationEvents = Array.AsReadOnly(source.AnimationEvents.ToArray());
            HasBounds = source.Meshes.Length > 0;
            if (HasBounds)
            {
                MinimumZ = source.Meshes.Min(mesh => mesh.GameBoxBoundsMin.Z);
                MaximumZ = source.Meshes.Max(mesh => mesh.GameBoxBoundsMax.Z);
            }
        }
    }
}
