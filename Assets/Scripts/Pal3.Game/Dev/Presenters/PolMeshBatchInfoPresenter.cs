// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

#if UNITY_EDITOR
namespace Pal3.Game.Dev.Presenters
{
    using System;
    using Core.Primitives;
    using Engine.Core.Implementation;

    /// <summary>Editor-only provenance for the source groups in one POL render mesh.</summary>
    public sealed class PolMeshBatchInfoPresenter : GameEntityScript
    {
        [Serializable]
        public sealed class SourceGroup
        {
            public string nodeName;
            public int materialGroupIndex;
            public GameBoxBlendFlag blendFlag;
            public GameBoxMaterial material;
            // Zero-based, contiguous vertex range [vertexStart, vertexStart + vertexCount).
            public int vertexStart;
            public int vertexCount;
        }

        public SourceGroup[] sources;
    }
}
#endif
