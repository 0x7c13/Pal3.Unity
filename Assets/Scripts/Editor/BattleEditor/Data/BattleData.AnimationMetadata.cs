// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Core.DataReader.Mv3;

    public sealed partial class BattleData
    {
        private readonly Dictionary<string, BattleAnimationMetadata> _animationMetadata = new(StringComparer.OrdinalIgnoreCase);
        private bool _animationMetadataDisposed;

        public BattleAnimationMetadata GetAnimationMetadata(string path)
        {
            if (_animationMetadataDisposed)
                throw new ObjectDisposedException(nameof(BattleData));
            var entry = Resources.Find(path) ?? throw new FileNotFoundException("Missing battle animation: " + path);
            if (_animationMetadata.TryGetValue(entry.Path, out BattleAnimationMetadata metadata))
                return metadata;
            // Keep full reader validation on the first read. Only the small
            // immutable projection survives in this data owner's cache.
            Mv3File model = Read(entry.Path, new Mv3FileReader());
            metadata = new BattleAnimationMetadata(model);
            _animationMetadata.Add(entry.Path, metadata);
            return metadata;
        }

        private void DisposeAnimationMetadata()
        {
            _animationMetadataDisposed = true;
            _animationMetadata.Clear();
        }
    }
}
